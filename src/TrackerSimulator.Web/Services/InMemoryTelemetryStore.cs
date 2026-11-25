using System.Collections.Concurrent;
using TrackerSimulator.Contracts;

namespace TrackerSimulator.Web.Services;

/// <summary>
/// Thread-safe in-memory store keeping the latest state and recent trail for each tracker.
/// </summary>
public sealed class InMemoryTelemetryStore(int maxTrailPoints = 40) : ITelemetryStore
{
    private readonly ConcurrentDictionary<string, DeviceHistory> _devices = new(StringComparer.OrdinalIgnoreCase);

    public event Action<DeviceTelemetryState>? TelemetryUpdated;

    public DeviceTelemetryState Upsert(TrackerTelemetry telemetry)
    {
        var history = _devices.GetOrAdd(telemetry.DeviceId, _ => new DeviceHistory(maxTrailPoints));
        var state = history.Update(telemetry, DateTimeOffset.UtcNow);

        TelemetryUpdated?.Invoke(state);
        return state;
    }

    public IReadOnlyList<DeviceTelemetryState> GetSnapshot()
    {
        return _devices.Values
            .Select(h => h.Snapshot())
            .OrderBy(s => s.Latest.DeviceId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private sealed class DeviceHistory(int maxPoints)
    {
        private readonly Lock _sync = new();
        private readonly Queue<GeoPoint> _trail = new();
        private TrackerTelemetry? _latest;
        private DateTimeOffset _receivedAtUtc;

        public DeviceTelemetryState Update(TrackerTelemetry telemetry, DateTimeOffset receivedAtUtc)
        {
            lock (_sync)
            {
                _latest = telemetry;
                _receivedAtUtc = receivedAtUtc;
                _trail.Enqueue(new GeoPoint(telemetry.Latitude, telemetry.Longitude));

                while (_trail.Count > maxPoints)
                {
                    _trail.Dequeue();
                }

                return new DeviceTelemetryState(_latest, _trail.ToArray(), _receivedAtUtc);
            }
        }

        public DeviceTelemetryState Snapshot()
        {
            lock (_sync)
            {
                return new DeviceTelemetryState(_latest!, _trail.ToArray(), _receivedAtUtc);
            }
        }
    }
}
