using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Common.Persistence;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.DeleteTitleMetadata;
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
    public override Guid Id => Guid.Parse("c6f8461a-9a6f-4c65-8bb9-825866cabc91");
    public IServiceProvider ServiceProvider { get; private set; } = null!;
    public static CrunchyrollPlugin? Instance { get; private set; }
    
    public bool IsMigratingDone { get; private set; }

    public CrunchyrollPlugin(ILogger<CrunchyrollPlugin> logger, ILoggerFactory loggerFactory, IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, 
        ILibraryManager libraryManager, Action<IServiceCollection>? serviceCollectionOptions = null) : base(applicationPaths, xmlSerializer)
    {
        _logger = logger;
        _libraryManager = libraryManager;
        _serviceCollectionOptions = serviceCollectionOptions;

        BuildServiceCollection(loggerFactory);
        InjectClientSideScriptIntoIndexFile(applicationPaths);

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

        serviceCollection.AddHttpMeasureRequestTimeMessageHandler();

        serviceCollection.AddTransient<PluginConfiguration>(_ => Configuration);
        serviceCollection.AddSingleton<ILibraryManager>(_libraryManager);
        
        serviceCollection.AddSingleton<ILoggerFactory>(loggerFactory);

        _serviceCollectionOptions?.Invoke(serviceCollection);

        ServiceProvider = serviceCollection.BuildServiceProvider();
    }

    private void InjectClientSideScriptIntoIndexFile(IApplicationPaths applicationPaths)
    {
        const string scriptHtmlElement = $"<script src=\"/{Routes.Root}/Script\"></script>";
        
        var indexFilePath = Path.Combine(applicationPaths.WebPath, "index.html");
        string indexFileContent;

        try
        {
            indexFileContent = File.ReadAllText(indexFilePath);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Could not read index.html");
            return;
        }
        
        if (indexFileContent.Contains(scriptHtmlElement))
        {
            return;
        }

        var lastIndexOfBodyContent = indexFileContent.LastIndexOf("</body>", StringComparison.Ordinal);

        if (lastIndexOfBodyContent <= 0)
        {
            _logger.LogError("end of body tag was not found in index.html");
            return;
        }

        string newIndexContent;
        try
        {
            newIndexContent = indexFileContent.Insert(lastIndexOfBodyContent, scriptHtmlElement);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Could not inject script into index.html");
            return;
        }
        
        try
        {
            File.WriteAllText(indexFilePath, newIndexContent);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Writing new index.html file failed");
            return;
        }
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