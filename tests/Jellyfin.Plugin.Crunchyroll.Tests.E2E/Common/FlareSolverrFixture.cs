using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Jellyfin.Plugin.Crunchyroll.Tests.E2E.Common;

public sealed class FlareSolverrFixture : IAsyncLifetime
{
    private readonly IContainer _container;
    private const int ContainerPort = 8191;
    
    private const string ContainerName = "flareSolverr-e2e";
    
    public FlareSolverrFixture()
    {
        _container = new ContainerBuilder()
            .WithName(ContainerName)
            .WithImage("ghcr.io/flaresolverr/flaresolverr:latest")
            .WithPortBinding(ContainerPort, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(Convert.ToUInt16(ContainerPort))))
            .WithNetwork(DockerNetwork.NetworkName)
            .Build();
    }
    
    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.StopAsync();
        await _container.DisposeAsync();
    }
    
    public static string Url => 
        new UriBuilder(
            Uri.UriSchemeHttp, 
            ContainerName, 
            ContainerPort)
        .ToString();
}