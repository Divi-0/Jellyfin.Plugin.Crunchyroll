using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Common.Persistence;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.DeleteTitleMetadata;
using Jellyfin.Plugin.Crunchyroll.Features.FlareSolverrTest;
using Jellyfin.Plugin.Crunchyroll.Features.WaybackMachine;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll;

public class CrunchyrollPlugin : MediaBrowser.Common.Plugins.BasePlugin<PluginConfiguration>, IHasWebPages
{
    private readonly ILogger<CrunchyrollPlugin> _logger;
    private readonly ILibraryManager _libraryManager;
    private readonly Action<IServiceCollection>? _serviceCollectionOptions;

    public override string Name => "Crunchyroll";
    public static readonly Guid PluginId = Guid.Parse("c6f8461a-9a6f-4c65-8bb9-825866cabc91");
    public override Guid Id => PluginId;
    public IServiceProvider ServiceProvider { get; private set; } = null!;
    public static CrunchyrollPlugin? Instance { get; private set; }
    
    public bool IsMigratingDone { get; private set; }

    public CrunchyrollPlugin(ILogger<CrunchyrollPlugin> logger, ILoggerFactory loggerFactory, IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, 
        ILibraryManager libraryManager, Action<IServiceCollection>? serviceCollectionOptions = null) : base(applicationPaths, xmlSerializer)
    {
        _logger = logger;
        _libraryManager = libraryManager;
        _serviceCollectionOptions = serviceCollectionOptions;

        PluginDataPath.Initialize(applicationPaths);
        BuildServiceCollection(loggerFactory);

        RunMigrations();
        
        ServiceProvider.UseDeleteTitleMetadata();
        
        Instance = this;
    }

    private void BuildServiceCollection(ILoggerFactory loggerFactory)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);
        serviceCollection.AddMemoryCache();
        
        serviceCollection.AddDbContext<CrunchyrollDbContext>();
        
        serviceCollection.AddScoped<IFileSystem, FileSystem>();
        serviceCollection.AddScoped<IFile>(serviceProvider => serviceProvider.GetRequiredService<IFileSystem>().File);
        serviceCollection.AddScoped<IDirectory>(serviceProvider => serviceProvider.GetRequiredService<IFileSystem>().Directory);
        
        serviceCollection.AddCrunchyroll();
        serviceCollection.AddWaybackMachine();
        serviceCollection.AddFlareSolverrTest();

        serviceCollection.AddHttpMeasureRequestTimeMessageHandler();

        serviceCollection.AddTransient<PluginConfiguration>(_ => Configuration);
        serviceCollection.AddSingleton<ILibraryManager>(_libraryManager);
        
        serviceCollection.AddSingleton<ILoggerFactory>(loggerFactory);

        _serviceCollectionOptions?.Invoke(serviceCollection);

        ServiceProvider = serviceCollection.BuildServiceProvider();
    }

    private void RunMigrations()
    {
        using var scope = ServiceProvider.CreateScope();
        using var dbContext = scope.ServiceProvider.GetRequiredService<CrunchyrollDbContext>();
        dbContext.Database.Migrate();

        IsMigratingDone = true;
    }
    
    public IEnumerable<PluginPageInfo> GetPages()
    {
        yield return new PluginPageInfo
        {
            Name = Name,
            EmbeddedResourcePath = GetType().Namespace + ".Configuration.configPage.html"
        };
    }
}