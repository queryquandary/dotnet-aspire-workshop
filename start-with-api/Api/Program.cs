var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Aspire part 1 of 2 start
builder.AddServiceDefaults();
// Aspire part 1 of 2 end

builder.Services.AddNwsManager();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Map the endpoints for the API
app.MapApiEndpoints();

// Aspire part 2 of 2 start
app.MapDefaultEndpoints();
// Aspire part 2 of 2 end

app.Run();
