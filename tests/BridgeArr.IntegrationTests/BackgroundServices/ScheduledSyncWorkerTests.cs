using BridgeArr.Infrastructure.BackgroundServices;
using Microsoft.Extensions.Configuration;

namespace BridgeArr.IntegrationTests.BackgroundServices;

public class ScheduledSyncWorkerTests
{
    [Theory]
    [InlineData(null, ScheduledSyncWorker.DefaultIntervalMinutes)]
    [InlineData("", ScheduledSyncWorker.DefaultIntervalMinutes)]
    [InlineData("invalid", ScheduledSyncWorker.DefaultIntervalMinutes)]
    [InlineData("0", ScheduledSyncWorker.DefaultIntervalMinutes)]
    [InlineData("-5", ScheduledSyncWorker.DefaultIntervalMinutes)]
    [InlineData("15", 15)]
    public void GetInterval_ConfigurationValue_ReturnsExpectedMinutes(string? value, int expectedMinutes)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["SYNC_INTERVAL_MINUTES"] = value })
            .Build();

        Assert.Equal(TimeSpan.FromMinutes(expectedMinutes), ScheduledSyncWorker.GetInterval(configuration));
    }
}
