using TrackerSimulator.Contracts;

namespace TrackerSimulator.Worker.Services;

/// <summary>
/// Generates realistic, stateful telemetry updates for a single simulated GPS tracker.
/// </summary>
public sealed class TrackerDeviceSimulator
{
    private const double EarthRadiusKm = 6371.0;

    private readonly Random _random;
    private double _latitude;
    private double _longitude;
    private double _speedKmh;
    private double _headingDegrees;
    private bool _ignitionOn;
    private int _batteryPercent;
    private double _odometerKm;
    private long _sequenceNumber;

    public string DeviceId { get; }

    public TrackerDeviceSimulator(
        string deviceId,
        double initialLatitude,
        double initialLongitude,
        int? seed = null)
    {
        DeviceId = deviceId;
        _random = seed.HasValue ? new Random(seed.Value) : Random.Shared;
        _latitude = initialLatitude;
        _longitude = initialLongitude;
        _headingDegrees = _random.NextDouble() * 360.0;
        _ignitionOn = _random.NextDouble() > 0.15;
        _speedKmh = _ignitionOn ? 20.0 + _random.NextDouble() * 60.0 : 0.0;
        _batteryPercent = _random.Next(65, 101);
        _odometerKm = Math.Round(1000.0 + _random.NextDouble() * 50000.0, 2);
    }

    /// <summary>
    /// Advances the device state by <paramref name="elapsed"/> and returns the next telemetry packet.
    /// </summary>
    public TrackerTelemetry NextTelemetry(TimeSpan elapsed, DateTimeOffset? timestampUtc = null)
    {
        var elapsedHours = Math.Max(0.0, elapsed.TotalHours);

        // Occasional ignition toggle (low probability per tick)
        if (_random.NextDouble() < 0.02)
        {
            _ignitionOn = !_ignitionOn;
        }

        if (_ignitionOn)
        {
            var speedDelta = (_random.NextDouble() - 0.48) * 12.0;
            _speedKmh = Math.Clamp(_speedKmh + speedDelta, 0.0, 110.0);

            var headingDelta = (_random.NextDouble() - 0.5) * 25.0;
            _headingDegrees = (_headingDegrees + headingDelta + 360.0) % 360.0;

            var distanceKm = _speedKmh * elapsedHours;
            if (distanceKm > 0)
            {
                MoveAlongHeading(distanceKm);
                _odometerKm += distanceKm;
            }

            // Battery charges slowly when ignition is on
            if (_batteryPercent < 100 && _random.NextDouble() < 0.1)
            {
                _batteryPercent++;
            }
        }
        else
        {
            _speedKmh = 0.0;
            if (_batteryPercent > 10 && _random.NextDouble() < 0.05)
            {
                _batteryPercent--;
            }
        }

        _sequenceNumber++;

        return new TrackerTelemetry(
            DeviceId: DeviceId,
            TimestampUtc: timestampUtc ?? DateTimeOffset.UtcNow,
            Latitude: Math.Round(_latitude, 6),
            Longitude: Math.Round(_longitude, 6),
            SpeedKmh: Math.Round(_speedKmh, 1),
            HeadingDegrees: Math.Round(_headingDegrees, 1),
            IgnitionOn: _ignitionOn,
            BatteryPercent: _batteryPercent,
            OdometerKm: Math.Round(_odometerKm, 3),
            SequenceNumber: _sequenceNumber);
    }

    private void MoveAlongHeading(double distanceKm)
    {
        var angularDistance = distanceKm / EarthRadiusKm;
        var bearingRad = DegreesToRadians(_headingDegrees);
        var latRad = DegreesToRadians(_latitude);
        var lonRad = DegreesToRadians(_longitude);

        var nextLatRad = Math.Asin(
            Math.Sin(latRad) * Math.Cos(angularDistance) +
            Math.Cos(latRad) * Math.Sin(angularDistance) * Math.Cos(bearingRad));

        var nextLonRad = lonRad + Math.Atan2(
            Math.Sin(bearingRad) * Math.Sin(angularDistance) * Math.Cos(latRad),
            Math.Cos(angularDistance) - Math.Sin(latRad) * Math.Sin(nextLatRad));

        _latitude = Math.Clamp(RadiansToDegrees(nextLatRad), -90.0, 90.0);
        _longitude = NormalizeLongitude(RadiansToDegrees(nextLonRad));
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;

    private static double RadiansToDegrees(double radians) => radians * 180.0 / Math.PI;

    private static double NormalizeLongitude(double longitude)
    {
        var normalized = (longitude + 180.0) % 360.0;
        if (normalized < 0)
        {
            normalized += 360.0;
        }

        return normalized - 180.0;
    }
}
