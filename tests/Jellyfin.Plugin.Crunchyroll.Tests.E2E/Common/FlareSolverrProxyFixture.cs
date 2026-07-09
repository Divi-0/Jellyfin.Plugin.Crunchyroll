using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Jellyfin.Plugin.Crunchyroll.Tests.E2E.Common;

public class FlareSolverrProxyFixture : IAsyncLifetime
{
    private readonly IContainer _container;
    private const int ContainerPort = 8080;

    public FlareSolverrProxyFixture()
    {
        _container = new ContainerBuilder()
            .WithName("flaresolverr-mitm-proxy-e2e")
            .WithImage("zelak312/flaresolverr-mitm-proxy:latest")
            .WithPortBinding(ContainerPort, true)
            .WithNetwork(DockerNetwork.NetworkName)
            .Build();
    }
    
    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        try
        {
            await _container.StopAsync();
        }
        finally
        {
            await _container.DisposeAsync();   
        }
    }
    
    public string DockerNetworkUrlShort => $"{_container.Name.TrimStart('/')}:{ContainerPort}";
}