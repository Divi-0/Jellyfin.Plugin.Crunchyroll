using FluentAssertions;
using Jellyfin.Plugin.Crunchyroll.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Common;

public class WaybackBackgroundQueueTests
{
    private static WaybackBackgroundQueue CreateQueue()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var scopeFactory = services.GetRequiredService<IServiceScopeFactory>();
        return new WaybackBackgroundQueue(scopeFactory, Substitute.For<ILogger<WaybackBackgroundQueue>>());
    }

    [Fact]
    public async Task EnqueueIfEligible_SkipsDisabledOrPostCutoffAndRunsEligible()
    {
        using var queue = CreateQueue();
        var calls = new List<string>();

        queue.EnqueueIfEligible(false, new DateTime(2023, 1, 1), (_, _) =>
        {
            calls.Add("disabled");
            return Task.CompletedTask;
        });

        queue.EnqueueIfEligible(true, new DateTime(2025, 1, 1), (_, _) =>
        {
            calls.Add("post-cutoff");
            return Task.CompletedTask;
        });

        queue.EnqueueIfEligible(true, new DateTime(2023, 6, 15), (_, _) =>
        {
            calls.Add("eligible-old");
            return Task.CompletedTask;
        });

        queue.EnqueueIfEligible(true, null, (_, _) =>
        {
            calls.Add("eligible-null-date");
            return Task.CompletedTask;
        });

        await queue.WaitForIdleAsync(CancellationToken.None);

        calls.Should().BeEquivalentTo(["eligible-old", "eligible-null-date"]);
    }
}
