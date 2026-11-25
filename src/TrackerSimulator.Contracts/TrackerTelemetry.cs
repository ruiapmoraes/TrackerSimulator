namespace TrackerSimulator.Contracts;

/// <summary>
/// Represents a single telemetry packet emitted by a simulated GPS tracker.
/// </summary>
public sealed record TrackerTelemetry(
    string DeviceId,
    DateTimeOffset TimestampUtc,
    double Latitude,
    double Longitude,
    double SpeedKmh,
    double HeadingDegrees,
    bool IgnitionOn,
    int BatteryPercent,
    double OdometerKm,
    long SequenceNumber);
