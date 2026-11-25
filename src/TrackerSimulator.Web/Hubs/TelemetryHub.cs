using Microsoft.AspNetCore.SignalR;

namespace TrackerSimulator.Web.Hubs;

/// <summary>
/// SignalR hub that broadcasts real-time tracker telemetry updates to connected dashboard clients.
/// </summary>
public sealed class TelemetryHub : Hub
{
    public const string TelemetryUpdatedMethod = "TelemetryUpdated";
}
