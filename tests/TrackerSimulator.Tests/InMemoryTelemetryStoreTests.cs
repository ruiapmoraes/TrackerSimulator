using TrackerSimulator.Contracts;
using TrackerSimulator.Web.Services;

namespace TrackerSimulator.Tests;

public sealed class InMemoryTelemetryStoreTests
{
    [Fact]
    public void Upsert_CapsTrailLengthAndPublishesEvent()
    {
        var store = new InMemoryTelemetryStore(maxTrailPoints: 3);
        DeviceTelemetryState? lastEvent = null;
        store.TelemetryUpdated += state => lastEvent = state;

        for (var i = 1; i <= 5; i++)
        {
            store.Upsert(new TrackerTelemetry(
                DeviceId: "TRK-0001",
                TimestampUtc: DateTimeOffset.UtcNow,
                Latitude: -23.55 + i * 0.001,
                Longitude: -46.63 + i * 0.001,
                SpeedKmh: 50,
                HeadingDegrees: 90,
                IgnitionOn: true,
                BatteryPercent: 90,
                OdometerKm: 1000 + i,
                SequenceNumber: i));
        }

        var snapshot = store.GetSnapshot();
        var device = Assert.Single(snapshot);
        Assert.Equal(5, device.Latest.SequenceNumber);
        Assert.Equal(3, device.Trail.Count);
        Assert.NotNull(lastEvent);
        Assert.Equal(5, lastEvent.Latest.SequenceNumber);
    }
}
