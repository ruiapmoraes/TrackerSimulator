using System.ComponentModel.DataAnnotations;

namespace TrackerSimulator.Worker.Options;

/// <summary>
/// Configuration options controlling tracker fleet simulation and HTTP telemetry delivery.
/// </summary>
public sealed class SimulationOptions
{
    public const string SectionName = "Simulation";

    [Range(1, 10_000)]
    public int DeviceCount { get; set; } = 10;

    [Range(100, 3_600_000)]
    public int IntervalMs { get; set; } = 2000;

    [Range(0, 60_000)]
    public int JitterMs { get; set; } = 500;

    [Range(1, 1000)]
    public int MaxConcurrentRequests { get; set; } = 20;

    [Required]
    public string DeviceIdPrefix { get; set; } = "TRK";

    [Required]
    public string EndpointUrl { get; set; } = "http://localhost:5000/api/telemetry";

    [Range(1, 120)]
    public int HttpTimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// When true, logs telemetry payloads without performing network POST requests.
    /// </summary>
    public bool DryRun { get; set; } = true;

    [Range(-90.0, 90.0)]
    public double BaseLatitude { get; set; } = -23.550520;

    [Range(-180.0, 180.0)]
    public double BaseLongitude { get; set; } = -46.633308;
}
