using Jellyfin.Plugin.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Login.Client;
using Jellyfin.Plugin.Crunchyroll.Tests.Integration.Shared;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Integration;

public class PluginFixture : IDisposable
{
    public PluginFixture()
    {
        PluginWebApplicationFactory.CreateInstance();
        var applicationPaths = PluginWebApplicationFactory.Instance.Services.GetRequiredService<IApplicationPaths>();
        var xmlSerializer = PluginWebApplicationFactory.Instance.Services.GetRequiredService<IXmlSerializer>();
        var logger = PluginWebApplicationFactory.Instance.Services.GetRequiredService<ILogger<CrunchyrollPlugin>>();
        var loggerFactory = PluginWebApplicationFactory.Instance.Services.GetRequiredService<ILoggerFactory>();
        var libraryManager = PluginWebApplicationFactory.Instance.Services.GetRequiredService<ILibraryManager>();
        _ = new CrunchyrollPlugin(logger, loggerFactory, applicationPaths, xmlSerializer, libraryManager, ExtendServiceCollection);
    }

    private static void ExtendServiceCollection(IServiceCollection serviceCollection)
    {

    }

    public void Dispose()
    {
        PluginWebApplicationFactory.Instance.Dispose();
    }
}

[CollectionDefinition(CollectionNames.Plugin)]
public class PluginCollection : 
    ICollectionFixture<PluginFixture>,
    ICollectionFixture<WireMockFixture>,
    ICollectionFixture<FlareSolverrFixture>,
    ICollectionFixture<WaybackMachineDisabledConfigFixture>,
    ICollectionFixture<CrunchyrollDatabaseFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}

public static class CollectionNames
{
    public const string Plugin = "Plugin";
}