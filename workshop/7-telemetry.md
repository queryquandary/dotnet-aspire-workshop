# Telemetry Module

## Introduction

In this module, we will add more advanced telemetry to the application. This includes additional logging and tracing, distributed tracing, custom metrics, and browser telemetry support using OpenTelemetry Protocol (OTLP) over HTTP and cross-origin resource sharing (CORS).

## Additional Logging and Tracing

1. Open the `Program.cs` file in the `Api` and `MyWeatherHub` projects.
2. Add the following code to configure additional logging and tracing:

    ```csharp
    builder.Logging.AddOpenTelemetry(logging =>
    {
        logging.IncludeFormattedMessage = true;
        logging.IncludeScopes = true;
    });

    builder.Services.AddOpenTelemetry()
        .WithMetrics(metrics =>
        {
            metrics.AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation();
        })
        .WithTracing(tracing =>
        {
            tracing.AddSource(builder.Environment.ApplicationName)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation();
        });
    ```

## Distributed Tracing

1. Open the `Program.cs` file in the `Api` and `MyWeatherHub` projects.
2. Add the following code to configure distributed tracing:

    ```csharp
    builder.Services.AddOpenTelemetry()
        .WithTracing(tracing =>
        {
            tracing.AddSource(builder.Environment.ApplicationName)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation();
        });
    ```

## Custom Metrics

1. Open the `Program.cs` file in the `Api` and `MyWeatherHub` projects.
2. Add the following code to configure custom metrics:

    ```csharp
    builder.Services.AddOpenTelemetry()
        .WithMetrics(metrics =>
        {
            metrics.AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation();
        });
    ```

## Diagnostics in NwsManager

1. Open the `NwsManager.cs` file in the `Api` project.
2. Add the following code to include diagnostics:

    ```csharp
    using Api.Diagnostics;
    using System.Diagnostics;

    public async Task<Zone[]?> GetZonesAsync()
    {
        using var activity = NwsManagerDiagnostics.activitySource.StartActivity("GetZonesAsync");
        // ...existing code...
    }

    public async Task<Forecast[]> GetForecastByZoneAsync(string zoneId)
    {
        NwsManagerDiagnostics.forecastRequestCounter.Add(1);
        var stopwatch = Stopwatch.StartNew();

        using var activity = NwsManagerDiagnostics.activitySource.StartActivity("GetForecastByZoneAsync");
        activity?.SetTag("zone.id", zoneId);

        // ...existing code...

        try
        {
            // ...existing code...
            stopwatch.Stop();
            NwsManagerDiagnostics.forecastRequestDuration.Record(stopwatch.Elapsed.TotalSeconds);
            activity?.SetTag("request.success", true);

            // ...existing code...
        }
        catch (HttpRequestException)
        {
            NwsManagerDiagnostics.failedRequestCounter.Add(1);
            activity?.SetTag("request.success", false);
            throw;
        }
    }
    ```

## Adding NwsManagerDiagnostics

1. Create a new file `NwsManagerDiagnostics.cs` in the `Api` project.
2. Add the following code to define diagnostics, meters, and counters:

    ```csharp
    // filepath: /d:/Users/Jon/Documents/GitHub/letslearn-dotnet-aspire/complete/Api/Data/NwsManagerDiagnostics.cs
    using System.Diagnostics;
    using System.Diagnostics.Metrics;

    namespace Api.Diagnostics
    {
        public class NwsManagerDiagnostics
        {
            private static readonly Meter meter = new Meter("NwsManagerMetrics", "1.0");
            public static readonly Counter<int> forecastRequestCounter = meter.CreateCounter<int>("forecast_requests_total", "Total number of forecast requests");
            public static readonly Histogram<double> forecastRequestDuration = meter.CreateHistogram<double>("forecast_request_duration_seconds", "Histogram of forecast request durations");
            public static readonly Counter<int> failedRequestCounter = meter.CreateCounter<int>("failed_requests_total", "Total number of failed requests");
            public static readonly Counter<int> cacheHitCounter = meter.CreateCounter<int>("cache_hits_total", "Total number of cache hits");
            public static readonly Counter<int> cacheMissCounter = meter.CreateCounter<int>("cache_misses_total", "Total number of cache misses");
            public static readonly ActivitySource activitySource = new ActivitySource("NwsManager");
        }
    }
    ```

### Explanation

- **Meters**: Meters are used to create and manage metrics. They act as a factory for creating different types of metrics like counters and histograms.
- **Counters**: Counters are used to count occurrences of events. They only increase and are useful for counting things like the number of requests or errors.
- **Histograms**: Histograms are used to measure the distribution of values, such as request durations. They provide statistical information about the data they collect.
- **Diagnostics**: Diagnostics include tracing and logging activities. They help in tracking the flow of execution and capturing detailed information about the application's behavior.

## Running the Application and Observing Output

1. Run the application and click on a few cities the .NET Aspire dashboard to observe the telemetry data.
2. In the .NET Aspire dashboard, navigate to the "Metrics" section to view the custom metrics.
3. Navigate to the "Tracing" section to view the distributed traces for activities like `GetZonesAsync` and `GetForecastByZoneAsync`.

### Steps to Observe Telemetry Data

1. Open the .NET Aspire dashboard in your browser.
2. Navigate to the "Metrics" section to view the custom metrics such as `forecast_requests_total`, `forecast_request_duration_seconds`, `failed_requests_total`, `cache_hits_total`, and `cache_misses_total`.
3. Navigate to the "Tracing" section to view the distributed traces for activities like `GetZonesAsync` and `GetForecastByZoneAsync`.
4. Analyze the data to understand the application's performance and behavior.

## Telemetry Integrations

Consider using telemetry integrations including Application Insights, New Relic, DataDog, etc.

**Next**: Module #8 - Next Module
