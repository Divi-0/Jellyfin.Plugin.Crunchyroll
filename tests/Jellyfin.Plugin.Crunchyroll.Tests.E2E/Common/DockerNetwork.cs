using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Networks;

namespace Jellyfin.Plugin.Crunchyroll.Tests.E2E.Common;

public class DockerNetwork : IAsyncLifetime
{
    private INetwork _network;
    
    public const string NetworkName = "Jellyfin.Plugin.Crunchyroll.Tests1.E2E";

    public DockerNetwork()
    {
        _network = new NetworkBuilder()
            .WithName(NetworkName)
            .Build();
    }
    
    public async Task InitializeAsync()
    {
        await _network.CreateAsync();
    }

    public async Task DisposeAsync()
    {
        await _network.DeleteAsync();
        await _network.DisposeAsync();
    }
}