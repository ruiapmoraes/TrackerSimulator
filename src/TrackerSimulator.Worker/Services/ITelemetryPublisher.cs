using TrackerSimulator.Contracts;

namespace TrackerSimulator.Worker.Services;

/// <summary>
/// Publishes simulated telemetry packets to a downstream destination.
/// </summary>
public interface ITelemetryPublisher
{
    Task PublishAsync(TrackerTelemetry telemetry, CancellationToken cancellationToken);
}
