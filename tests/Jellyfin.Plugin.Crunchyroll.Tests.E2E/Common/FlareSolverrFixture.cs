using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Jellyfin.Plugin.Crunchyroll.Tests.E2E.Common;

public class FlareSolverrFixture : IAsyncLifetime
{
    private readonly IContainer _container;
    private const int ContainerPort = 8191;

    public FlareSolverrFixture()
    {
        _container = new ContainerBuilder()
            .WithName("flaresolverr-e2e")
            .WithImage("flaresolverr/flaresolverr:latest")
            .WithPortBinding(ContainerPort, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Serving on http://0.0.0.0:8191"))
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
    
    public string DockerNetworkUrl =>
        new UriBuilder(
                Uri.UriSchemeHttp,
                _container.Name.TrimStart('/'),
                ContainerPort, "v1")
            .ToString();
}