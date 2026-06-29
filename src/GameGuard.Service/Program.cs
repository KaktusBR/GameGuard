using System.Runtime.Versioning;
using GameGuard.Service;

[assembly: SupportedOSPlatform("windows")]

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(o => o.ServiceName = "GameGuard");
builder.Services.AddHostedService<Worker>();
var host = builder.Build();
host.Run();
