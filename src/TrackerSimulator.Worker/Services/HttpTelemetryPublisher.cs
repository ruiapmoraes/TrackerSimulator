using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TrackerSimulator.Contracts;
using TrackerSimulator.Worker.Options;

namespace TrackerSimulator.Worker.Services;

/// <summary>
/// Publishes tracker telemetry as JSON over HTTP POST, or logs payloads in DryRun mode.
/// </summary>
public sealed class HttpTelemetryPublisher(
    HttpClient httpClient,
    IOptions<SimulationOptions> options,
    ILogger<HttpTelemetryPublisher> logger) : ITelemetryPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task PublishAsync(TrackerTelemetry telemetry, CancellationToken cancellationToken)
    {
        var settings = options.Value;

        if (settings.DryRun)
        {
            logger.LogInformation(
                "[DryRun] {DeviceId} seq={Seq} lat={Lat} lon={Lon} speed={Speed}km/h ign={Ignition} bat={Battery}%",
                telemetry.DeviceId,
                telemetry.SequenceNumber,
                telemetry.Latitude,
                telemetry.Longitude,
                telemetry.SpeedKmh,
                telemetry.IgnitionOn,
                telemetry.BatteryPercent);
            return;
        }

        try
        {
            using var response = await httpClient.PostAsJsonAsync(
                settings.EndpointUrl,
                telemetry,
                JsonOptions,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation(
                    "Sent {DeviceId} seq={Seq} -> HTTP {StatusCode}",
                    telemetry.DeviceId,
                    telemetry.SequenceNumber,
                    (int)response.StatusCode);
            }
            else
            {
                logger.LogWarning(
                    "Failed {DeviceId} seq={Seq} -> HTTP {StatusCode} ({ReasonPhrase})",
                    telemetry.DeviceId,
                    telemetry.SequenceNumber,
                    (int)response.StatusCode,
                    response.ReasonPhrase);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error sending {DeviceId} seq={Seq} to {EndpointUrl}",
                telemetry.DeviceId,
                telemetry.SequenceNumber,
                settings.EndpointUrl);
        }
    }
}
