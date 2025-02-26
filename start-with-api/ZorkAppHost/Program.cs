var builder = DistributedApplication.CreateBuilder(args);

// The distributed application builder is getting a reference to
// a .net project
var api = builder.AddProject<Projects.Api>("api");
var hub = builder.AddProject<Projects.Api>("myweatherhub");

builder.Build().Run();
