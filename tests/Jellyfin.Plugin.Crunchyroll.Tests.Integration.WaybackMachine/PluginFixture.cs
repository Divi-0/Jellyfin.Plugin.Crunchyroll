using Jellyfin.Plugin.Crunchyroll.Tests.Integration.Shared;
using Jellyfin.Plugin.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Common.Persistence;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Integration.WaybackMachine;

public class PluginFixture : IAsyncLifetime
{
    public PluginFixture()
    {
        var location = typeof(CrunchyrollDbContext).Assembly.Location;
        var filePath = Path.Combine(Path.GetDirectoryName(location)!, "Crunchyroll.db");
        
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        
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
    
    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        PluginWebApplicationFactory.Instance.Dispose();
        
        return Task.CompletedTask;
    }
}

[CollectionDefinition(CollectionNames.Plugin)]
public class PluginCollection : 
    ICollectionFixture<WireMockFixture>,
    ICollectionFixture<CrunchyrollDatabaseFixture>,
    ICollectionFixture<ConfigFixture>,
    ICollectionFixture<PluginFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}

public static class CollectionNames
{
    public const string Plugin = "Plugin";
}