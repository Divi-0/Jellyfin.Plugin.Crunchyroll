using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Jellyfin.Plugin.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Integration.Shared;

public class FlareSolverrFixture : IAsyncLifetime
{
    private readonly IContainer _container;
    private const int CONTAINER_PORT = 8191;
    
    public FlareSolverrFixture()
    {
        _container = new ContainerBuilder()
            .WithImage("ghcr.io/flaresolverr/flaresolverr:latest")
            .WithPortBinding(CONTAINER_PORT, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(Convert.ToUInt16(CONTAINER_PORT))))
            .Build();
    }
    
    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        _ = Task.Run(() =>
        {
            while (CrunchyrollPlugin.Instance is null)
            {
            }

            var config = CrunchyrollPlugin.Instance.ServiceProvider.GetRequiredService<PluginConfiguration>();
            config.FlareSolverrUrl =
                new UriBuilder(Uri.UriSchemeHttp, "127.0.0.1", _container.GetMappedPublicPort(CONTAINER_PORT))
                    .ToString();
        });
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}