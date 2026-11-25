using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using TrackerSimulator.Contracts;
using TrackerSimulator.Worker.Options;
using TrackerSimulator.Worker.Services;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace TrackerSimulator.Tests;

public sealed class HttpTelemetryPublisherTests
{
    [Fact]
    public async Task PublishAsync_InDryRunMode_DoesNotSendHttpRequest()
    {
        var handler = new RecordingHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using var client = new HttpClient(handler);
        var options = MsOptions.Create(new SimulationOptions
        {
            DryRun = true,
            EndpointUrl = "http://localhost:5000/api/telemetry"
        });

        var publisher = new HttpTelemetryPublisher(client, options, NullLogger<HttpTelemetryPublisher>.Instance);
        var telemetry = CreateSampleTelemetry();

        await publisher.PublishAsync(telemetry, CancellationToken.None);

        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task PublishAsync_WhenDryRunIsFalse_PostsJsonTelemetry()
    {
        string? capturedBody = null;
        var handler = new RecordingHttpMessageHandler(request =>
        {
            capturedBody = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        });

        using var client = new HttpClient(handler);
        var options = MsOptions.Create(new SimulationOptions
        {
            DryRun = false,
            EndpointUrl = "http://localhost:5000/api/telemetry"
        });

        var publisher = new HttpTelemetryPublisher(client, options, NullLogger<HttpTelemetryPublisher>.Instance);
        var telemetry = CreateSampleTelemetry();

        await publisher.PublishAsync(telemetry, CancellationToken.None);

        var sentRequest = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, sentRequest.Method);
        Assert.Equal("http://localhost:5000/api/telemetry", sentRequest.RequestUri?.ToString());
        Assert.NotNull(capturedBody);

        var deserialized = JsonSerializer.Deserialize<TrackerTelemetry>(
            capturedBody,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Equal(telemetry, deserialized);
    }

    private static TrackerTelemetry CreateSampleTelemetry() =>
        new(
            DeviceId: "TRK-0001",
            TimestampUtc: new DateTimeOffset(2026, 11, 25, 18, 0, 0, TimeSpan.Zero),
            Latitude: -23.550520,
            Longitude: -46.633308,
            SpeedKmh: 45.5,
            HeadingDegrees: 120.0,
            IgnitionOn: true,
            BatteryPercent: 95,
            OdometerKm: 12345.678,
            SequenceNumber: 1);

    private sealed class RecordingHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(responder(request));
        }
    }
}
