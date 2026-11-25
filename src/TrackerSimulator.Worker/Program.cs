using Microsoft.Extensions.Options;
using TrackerSimulator.Worker.Options;
using TrackerSimulator.Worker.Services;
using TrackerSimulator.Worker.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddOptions<SimulationOptions>()
    .BindConfiguration(SimulationOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddHttpClient<ITelemetryPublisher, HttpTelemetryPublisher>((sp, client) =>
{
    var settings = sp.GetRequiredService<IOptions<SimulationOptions>>().Value;
    client.Timeout = TimeSpan.FromSeconds(settings.HttpTimeoutSeconds);
});

builder.Services.AddHostedService<TrackerSimulationWorker>();

var host = builder.Build();
await host.RunAsync();
