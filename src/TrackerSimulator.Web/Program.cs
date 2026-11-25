using Microsoft.AspNetCore.SignalR;
using TrackerSimulator.Contracts;
using TrackerSimulator.Web.Components;
using TrackerSimulator.Web.Hubs;
using TrackerSimulator.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSignalR();
builder.Services.AddSingleton<ITelemetryStore, InMemoryTelemetryStore>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseAntiforgery();
app.MapStaticAssets();

app.MapPost("/api/telemetry", async (
    TrackerTelemetry telemetry,
    ITelemetryStore store,
    IHubContext<TelemetryHub> hubContext,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(telemetry.DeviceId))
    {
        return Results.BadRequest(new { error = "DeviceId is required." });
    }

    var state = store.Upsert(telemetry);

    await hubContext.Clients.All.SendAsync(
        TelemetryHub.TelemetryUpdatedMethod,
        state,
        cancellationToken);

    return Results.Accepted($"/api/telemetry/{telemetry.DeviceId}", new
    {
        telemetry.DeviceId,
        telemetry.SequenceNumber
    });
});

app.MapGet("/api/telemetry", (ITelemetryStore store) => Results.Ok(store.GetSnapshot()));

app.MapHub<TelemetryHub>("/hubs/telemetry");

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
