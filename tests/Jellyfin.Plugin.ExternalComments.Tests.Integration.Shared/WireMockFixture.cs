using Jellyfin.Plugin.ExternalComments.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WireMock.Client;
using WireMock.Net.Testcontainers;
using Xunit;

namespace Jellyfin.Plugin.ExternalComments.Tests.Integration.Shared;

public sealed class WireMockFixture : IAsyncLifetime
{
    private readonly WireMockContainer _container;
    
    public WireMockFixture()
    {
        _container = new WireMockContainerBuilder()
            .WithAutoRemove(true)
            .WithCleanUp(true)
            .Build();
    }
    
    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        AdminApiClient = _container.CreateWireMockAdminClient();

        _ = Task.Run(() =>
        {
            while (ExternalCommentsPlugin.Instance is null)
            {
            }

            var config = ExternalCommentsPlugin.Instance.ServiceProvider.GetRequiredService<PluginConfiguration>();
            config.CrunchyrollUrl = _container.GetPublicUrl();
            config.ArchiveOrgUrl = _container.GetPublicUrl();
        });
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
    
    public IWireMockAdminApi AdminApiClient { get; private set; } = null!;
}