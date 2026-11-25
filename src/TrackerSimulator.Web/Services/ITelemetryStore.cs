using TrackerSimulator.Contracts;

namespace TrackerSimulator.Web.Services;

public sealed record GeoPoint(double Latitude, double Longitude);

public sealed record DeviceTelemetryState(
    TrackerTelemetry Latest,
    IReadOnlyList<GeoPoint> Trail,
    DateTimeOffset ReceivedAtUtc);

public interface ITelemetryStore
{
    event Action<DeviceTelemetryState>? TelemetryUpdated;

    DeviceTelemetryState Upsert(TrackerTelemetry telemetry);

    IReadOnlyList<DeviceTelemetryState> GetSnapshot();
}
