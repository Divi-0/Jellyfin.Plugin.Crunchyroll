using Jellyfin.Plugin.ExternalComments.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.ExternalComments.Tests.Integration;

public class WaybackMachineDisabledConfigFixture : IAsyncLifetime
{
    public Task InitializeAsync()
    {
        while (ExternalCommentsPlugin.Instance is null)
        {
        }

        var config = ExternalCommentsPlugin.Instance.ServiceProvider.GetRequiredService<PluginConfiguration>();
        config.IsWaybackMachineEnabled = false;
        
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}