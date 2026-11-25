using Microsoft.Extensions.Options;
using TrackerSimulator.Worker.Options;
using TrackerSimulator.Worker.Services;

namespace TrackerSimulator.Worker.Workers;

/// <summary>
/// Background service that orchestrates concurrent simulated tracker devices.
/// </summary>
public sealed class TrackerSimulationWorker(
    ITelemetryPublisher publisher,
    IOptions<SimulationOptions> options,
    ILogger<TrackerSimulationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;

        logger.LogInformation(
            "Starting tracker simulation: {DeviceCount} devices, interval={IntervalMs}ms, jitter={JitterMs}ms, dryRun={DryRun}, endpoint={EndpointUrl}",
            settings.DeviceCount,
            settings.IntervalMs,
            settings.JitterMs,
            settings.DryRun,
            settings.EndpointUrl);

        using var concurrencyGate = new SemaphoreSlim(settings.MaxConcurrentRequests, settings.MaxConcurrentRequests);

        var simulators = CreateSimulators(settings);
        var deviceTasks = simulators
            .Select(sim => RunDeviceLoopAsync(sim, settings, concurrencyGate, stoppingToken))
            .ToArray();

        try
        {
            await Task.WhenAll(deviceTasks);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Tracker simulation stopped gracefully.");
        }
    }

    private static List<TrackerDeviceSimulator> CreateSimulators(SimulationOptions settings)
    {
        var simulators = new List<TrackerDeviceSimulator>(settings.DeviceCount);

        for (var i = 1; i <= settings.DeviceCount; i++)
        {
            var deviceId = $"{settings.DeviceIdPrefix}-{i:D4}";
            // Spread initial positions slightly around the configured base coordinates.
            var latOffset = (Random.Shared.NextDouble() - 0.5) * 0.05;
            var lonOffset = (Random.Shared.NextDouble() - 0.5) * 0.05;

            simulators.Add(new TrackerDeviceSimulator(
                deviceId,
                settings.BaseLatitude + latOffset,
                settings.BaseLongitude + lonOffset));
        }

        return simulators;
    }

    private async Task RunDeviceLoopAsync(
        TrackerDeviceSimulator simulator,
        SimulationOptions settings,
        SemaphoreSlim concurrencyGate,
        CancellationToken stoppingToken)
    {
        // Stagger startup across devices so requests don't all fire at the exact same millisecond.
        var initialDelayMs = Random.Shared.Next(0, Math.Max(1, settings.IntervalMs));
        await Task.Delay(initialDelayMs, stoppingToken);

        var lastTick = DateTimeOffset.UtcNow;

        while (!stoppingToken.IsCancellationRequested)
        {
            var jitter = settings.JitterMs > 0
                ? Random.Shared.Next(-settings.JitterMs, settings.JitterMs + 1)
                : 0;
            var delayMs = Math.Max(100, settings.IntervalMs + jitter);

            await Task.Delay(delayMs, stoppingToken);

            var now = DateTimeOffset.UtcNow;
            var elapsed = now - lastTick;
            lastTick = now;

            var telemetry = simulator.NextTelemetry(elapsed, now);

            await concurrencyGate.WaitAsync(stoppingToken);
            try
            {
                await publisher.PublishAsync(telemetry, stoppingToken);
            }
            finally
            {
                concurrencyGate.Release();
            }
        }
    }
}
