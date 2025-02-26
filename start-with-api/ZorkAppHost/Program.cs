var builder = DistributedApplication.CreateBuilder(args);

// The distributed application builder is getting a reference to
// a .net project
var myapi = builder.AddProject<Projects.Api>("api");

var hub = builder
    .AddProject<Projects.MyWeatherHub>("myweatherhub")
    .WithReference(myapi) // reference the api project from the weather hub project
    .WithExternalHttpEndpoints();  // hides the API project from the outside world

builder.Build().Run();
