using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Jellyfin.Plugin.Crunchyroll.Tests.E2E.Common.Helpers;
using Microsoft.AspNetCore.Http.Extensions;

namespace Jellyfin.Plugin.Crunchyroll.Tests.E2E.Common;

public sealed class JellyfinFixture : IAsyncLifetime
{
    private readonly IContainer _container;
    private const int ContainerPort = 8096;
    
    private readonly string _localVideoPath;

    public JellyfinFixture()
    {
        _localVideoPath = Path.Combine(Directory.GetCurrentDirectory(), "videos");
        CreateVideoFolderHelper.CreateVideoFolder(_localVideoPath);
        
        _container = new ContainerBuilder()
            .WithName("jellyfin-e2e")
            .WithImage("jellyfin/jellyfin:latest")
            .WithPortBinding(ContainerPort, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Main: Startup complete"))
            .WithBindMount(Path.Combine(Directory.GetCurrentDirectory(), "plugin"), "/config/plugins/Crunchyroll")
            .WithBindMount(_localVideoPath, VideoContainerPath)
            .WithEnvironment("JELLYFIN_Serilog__MinimumLevel__Default", "Debug")
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
        
        try
        {
            Directory.Delete(_localVideoPath, true);
        }
        catch
        {
            // ignored
        }
    }

    public string Url =>
        new UriBuilder(
                Uri.UriSchemeHttp,
                _container.Hostname,
                _container.GetMappedPublicPort(ContainerPort))
            .ToString();
    
    public const string VideoContainerPath = "/videos";
}