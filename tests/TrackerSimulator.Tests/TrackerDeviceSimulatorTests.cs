using TrackerSimulator.Worker.Services;

namespace TrackerSimulator.Tests;

public sealed class TrackerDeviceSimulatorTests
{
    [Fact]
    public void NextTelemetry_IncrementsSequenceAndKeepsValidCoordinates()
    {
        var simulator = new TrackerDeviceSimulator("TRK-0001", -23.550520, -46.633308, seed: 42);
        var startTime = new DateTimeOffset(2026, 11, 25, 18, 0, 0, TimeSpan.Zero);

        var first = simulator.NextTelemetry(TimeSpan.FromSeconds(2), startTime);
        var second = simulator.NextTelemetry(TimeSpan.FromSeconds(2), startTime.AddSeconds(2));

        Assert.Equal("TRK-0001", first.DeviceId);
        Assert.Equal(1, first.SequenceNumber);
        Assert.Equal(2, second.SequenceNumber);
        Assert.InRange(second.Latitude, -90.0, 90.0);
        Assert.InRange(second.Longitude, -180.0, 180.0);
        Assert.InRange(second.BatteryPercent, 0, 100);
        Assert.True(second.OdometerKm >= first.OdometerKm);
    }

    [Fact]
    public void NextTelemetry_IncreasesOdometerWhenMoving()
    {
        var simulator = new TrackerDeviceSimulator("TRK-0002", -23.550520, -46.633308, seed: 7);

        var previous = simulator.NextTelemetry(TimeSpan.FromSeconds(10));
        var moved = false;

        for (var i = 0; i < 20; i++)
        {
            var current = simulator.NextTelemetry(TimeSpan.FromSeconds(10));
            if (current.OdometerKm > previous.OdometerKm)
            {
                moved = true;
                break;
            }
            previous = current;
        }

        Assert.True(moved);
    }
}
