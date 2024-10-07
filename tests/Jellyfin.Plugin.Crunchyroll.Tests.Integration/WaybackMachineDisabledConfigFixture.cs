using Jellyfin.Plugin.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Integration;

public class WaybackMachineDisabledConfigFixture : IAsyncLifetime
{
    public Task InitializeAsync()
    {
        while (CrunchyrollPlugin.Instance is null)
        {
        }

        var config = CrunchyrollPlugin.Instance.ServiceProvider.GetRequiredService<PluginConfiguration>();
        config.IsWaybackMachineEnabled = false;
        
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}