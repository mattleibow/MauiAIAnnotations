using Microsoft.Maui.DevFlow.Driver;

namespace MauiSampleApp.UITests.Infrastructure;

/// <summary>
/// Shared fixture that connects to a running MauiSampleApp via DevFlow.
/// Configure with environment variables:
///   DEVFLOW_PLATFORM (default: maccatalyst)
///   DEVFLOW_HOST (default: localhost)
///   DEVFLOW_PORT (default: 9223)
/// </summary>
public sealed class DevFlowFixture : IAsyncLifetime
{
    public IAppDriver Driver { get; private set; } = null!;

    public string Platform { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Platform = Environment.GetEnvironmentVariable("DEVFLOW_PLATFORM") ?? "maccatalyst";
        var host = Environment.GetEnvironmentVariable("DEVFLOW_HOST") ?? "localhost";
        var port = int.TryParse(Environment.GetEnvironmentVariable("DEVFLOW_PORT"), out var p) ? p : 9223;

        Driver = AppDriverFactory.Create(Platform);
        await Driver.ConnectAsync(host, port);

        var status = await Driver.GetStatusAsync();
        if (status is null || !status.Running)
            throw new InvalidOperationException(
                $"DevFlow agent not reachable at {host}:{port}. " +
                "Ensure the app is running with AddMauiDevFlowAgent() enabled.");
    }

    public Task DisposeAsync()
    {
        Driver?.Dispose();
        return Task.CompletedTask;
    }
}

[CollectionDefinition("DevFlow")]
public class DevFlowCollection : ICollectionFixture<DevFlowFixture>;
