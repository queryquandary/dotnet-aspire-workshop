# Database Integration

## Introduction

In this module, we will integrate a PostgreSQL database with our application. We will use Entity Framework Core (EF Core) to interact with the database. Additionally, we will set up PgAdmin to manage our PostgreSQL database.

## Prerequisites

- Docker Desktop or Podman
- PostgreSQL
- PgAdmin
- Entity Framework Core

## Setting Up PostgreSQL and PgAdmin

1. Create a `docker-compose.yml` file in the root of your project with the following content:

    ```yaml
    version: '3.8'
    services:
      postgres:
        image: postgres:latest
        environment:
          POSTGRES_USER: myuser
          POSTGRES_PASSWORD: mypassword
          POSTGRES_DB: myweatherhub
        ports:
          - "5432:5432"
        volumes:
          - postgres_data:/var/lib/postgresql/data
      pgadmin:
        image: dpage/pgadmin4
        environment:
          PGADMIN_DEFAULT_EMAIL: admin@admin.com
          PGADMIN_DEFAULT_PASSWORD: admin
        ports:
          - "5050:80"
    volumes:
      postgres_data:
    ```

2. Run the following command to start the PostgreSQL and PgAdmin containers:

    ```bash
    docker-compose up -d
    ```

3. Open PgAdmin by navigating to [http://localhost:5050](http://localhost:5050) in your browser. Log in with the email `admin@admin.com` and password `admin`.

4. Add a new server in PgAdmin with the following details:
    - Name: MyWeatherHub
    - Host: postgres
    - Port: 5432
    - Username: myuser
    - Password: mypassword

## Integrating EF Core with PostgreSQL

1. Install the following NuGet packages in the `Api` project:
    - `Npgsql.EntityFrameworkCore.PostgreSQL`
    - `Microsoft.EntityFrameworkCore.Design`

2. Add the following connection string to the `appsettings.json` file in the `Api` project:

    ```json
    "ConnectionStrings": {
      "DefaultConnection": "Host=localhost;Database=myweatherhub;Username=myuser;Password=mypassword"
    }
    ```

3. Create a new class `ApplicationDbContext` in the `Data` folder of the `Api` project with the following content:

    ```csharp
    using Microsoft.EntityFrameworkCore;
    using Common;

    namespace Api.Data
    {
        public class ApplicationDbContext : DbContext
        {
            public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
                : base(options)
            {
            }

            public DbSet<Zone> Zones { get; set; }

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                if (!optionsBuilder.IsConfigured)
                {
                    optionsBuilder.UseNpgsql("Host=localhost;Database=myweatherhub;Username=myuser;Password=mypassword");
                }
            }
        }
    }
    ```

4. Update the `NwsManager` class to use the `ApplicationDbContext`:

    ```csharp
    using Microsoft.EntityFrameworkCore;

    namespace Api
    {
        public class NwsManager
        {
            private readonly HttpClient httpClient;
            private readonly IMemoryCache cache;
            private readonly IWebHostEnvironment webHostEnvironment;
            private readonly ILogger<NwsManager> logger;
            private readonly ApplicationDbContext dbContext;

            public NwsManager(HttpClient httpClient, IMemoryCache cache, IWebHostEnvironment webHostEnvironment, ILogger<NwsManager> logger, ApplicationDbContext dbContext)
            {
                this.httpClient = httpClient;
                this.cache = cache;
                this.webHostEnvironment = webHostEnvironment;
                this.logger = logger;
                this.dbContext = dbContext;
            }

            public async Task<Zone[]?> GetZonesAsync()
            {
                using var activity = NwsManagerDiagnostics.activitySource.StartActivity("GetZonesAsync");

                logger.LogInformation("🚀 Starting zones retrieval with {CacheExpiration} cache expiration", TimeSpan.FromHours(1));

                return await cache.GetOrCreateAsync("zones", async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);

                    var zonesFilePath = Path.Combine(webHostEnvironment.WebRootPath, "zones.json");
                    if (!File.Exists(zonesFilePath))
                    {
                        logger.LogWarning("⚠️ Zones file not found at {ZonesFilePath}", zonesFilePath);
                        activity?.SetTag("cache.hit", false);
                        return [];
                    }

                    using var zonesJson = File.OpenRead(zonesFilePath);
                    var zones = await JsonSerializer.DeserializeAsync<ZonesResponse>(zonesJson, options);

                    if (zones?.Features == null)
                    {
                        logger.LogWarning("⚠️ Failed to deserialize zones from file");
                        activity?.SetTag("cache.hit", false);
                        return [];
                    }

                    var filteredZones = zones.Features
                        .Where(f => f.Properties?.ObservationStations?.Count > 0)
                        .Select(f => (Zone)f)
                        .Distinct()
                        .ToArray();

                    logger.LogInformation(
                        "📊 Retrieved {TotalZones} zones, {FilteredZones} after filtering observation stations",
                        zones.Features.Count,
                        filteredZones.Length
                    );

                    activity?.SetTag("cache.hit", true);

                    // Save zones to the database
                    dbContext.Zones.AddRange(filteredZones);
                    await dbContext.SaveChangesAsync();

                    return filteredZones;
                });
            }

            public async Task<Forecast[]> GetForecastByZoneAsync(string zoneId)
            {
                using var logScope = logger.BeginScope(new Dictionary<string, object>
                {
                    ["ZoneId"] = zoneId,
                    ["RequestNumber"] = Interlocked.Increment(ref forecastCount)
                });

                NwsManagerDiagnostics.forecastRequestCounter.Add(1);
                var stopwatch = Stopwatch.StartNew();

                using var activity = NwsManagerDiagnostics.activitySource.StartActivity("GetForecastByZoneAsync");
                activity?.SetTag("zone.id", zoneId);

                logger.LogInformation("🚀 Starting forecast request for zone {ZoneId}", zoneId);

                // Create an exception every 5 calls to simulate an error for testing
                if (forecastCount % 5 == 0)
                {
                    logger.LogError(
                        "❌ Simulated error on request {RequestCount} for zone {ZoneId}",
                        forecastCount,
                        zoneId
                    );
                    NwsManagerDiagnostics.failedRequestCounter.Add(1);
                    activity?.SetTag("request.success", false);
                    throw new Exception("Random exception thrown by NwsManager.GetForecastAsync");
                }

                try
                {
                    var zoneIdSegment = HttpUtility.UrlEncode(zoneId);
                    var zoneUrl = $"https://api.weather.gov/zones/forecast/{zoneIdSegment}/forecast";

                    logger.LogDebug(
                        "🔍 Requesting forecast from {Url}",
                        zoneUrl
                    );

                    var forecasts = await httpClient.GetFromJsonAsync<ForecastResponse>(zoneUrl, options);

                    stopwatch.Stop();
                    var duration = stopwatch.Elapsed;
                    NwsManagerDiagnostics.forecastRequestDuration.Record(duration.TotalSeconds);
                    activity?.SetTag("request.success", true);

                    logger.LogInformation(
                        "📊 Retrieved forecast for zone {ZoneId} in {Duration:N0}ms with {PeriodCount} periods",
                        zoneId,
                        duration.TotalMilliseconds,
                        forecasts?.Properties?.Periods?.Count ?? 0
                    );

                    return forecasts
                           ?.Properties
                           ?.Periods
                           ?.Select(p => (Forecast)p)
                           .ToArray() ?? [];
                }
                catch (HttpRequestException ex)
                {
                    logger.LogError(
                        ex,
                        "❌ Failed to retrieve forecast for zone {ZoneId}. Status: {StatusCode}",
                        zoneId,
                        ex.StatusCode
                    );
                    NwsManagerDiagnostics.failedRequestCounter.Add(1);
                    activity?.SetTag("request.success", false);
                    throw;
                }
            }
        }
    }
    ```

5. Update the `Program.cs` file in the `Api` project to add the `ApplicationDbContext` to the service collection:

    ```csharp
    var builder = WebApplication.CreateBuilder(args);

    builder.AddRedisOutputCache("cache");

    // Add service defaults & Aspire client integrations.
    builder.AddServiceDefaults();

    // Add services to the container.
    builder.Services.AddProblemDetails();

    // Add ApplicationDbContext to the service collection
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    builder.Services.AddOpenApi();

    builder.Services.AddNwsManager();

    builder.Services.AddOpenTelemetry()
        .WithMetrics(m => m.AddMeter("NwsManagerMetrics"));

    var app = builder.Build();

    app.MapDefaultEndpoints();

    app.UseHttpsRedirection();

    // Map the endpoints for the API
    app.MapApiEndpoints();

    app.Run();
    ```

## Updating the Web App

1. Open the `Home.razor` file in the `MyWeatherHub` project.
2. Add functionality to star and unstar favorite zones:

    ```csharp
    private void ToggleFavorite(Zone zone)
    {
        if (FavoriteZones.Contains(zone))
        {
            FavoriteZones.Remove(zone);
        }
        else
        {
            FavoriteZones.Add(zone);
        }
    }
    ```

3. Update the UI to show favorite zones at the top of the list:

    ```csharp
    IQueryable<Zone> zones
    {
        get
        {
            var results = AllZones
                .AsQueryable();

            results = string.IsNullOrEmpty(StateFilter) ? results.AsQueryable()
                    : results.Where(z => z.State == StateFilter.ToUpper()).AsQueryable();

            results = string.IsNullOrEmpty(NameFilter) ? results
                    : results.Where(z => z.Name.Contains(NameFilter, StringComparison.InvariantCultureIgnoreCase));

            // Show favorite zones at the top of the list
            results = results.OrderByDescending(z => FavoriteZones.Contains(z)).ThenBy(z => z.Name);

            return results;
        }
    }
    ```

## Other Data Options

In addition to PostgreSQL, there are other data options that you can consider for your application. Some of the top data options include:

- **Azure SQL**: A fully managed relational database service provided by Microsoft Azure. It offers high availability, scalability, and security features.
- **MySQL**: An open-source relational database management system. It is widely used and has a large community of users and developers.
- **MongoDB**: A NoSQL database that provides high performance, high availability, and easy scalability. It is designed to handle large amounts of data and is suitable for applications with unstructured or semi-structured data.
- **SQLite**: A lightweight, file-based database that is easy to set up and use. It is suitable for small to medium-sized applications and is often used for development and testing purposes.

Each of these options has its own strengths and weaknesses, and the choice of database will depend on the specific requirements of your application.

**Next**: [Module #9: Deployment](9-deployment.md)
