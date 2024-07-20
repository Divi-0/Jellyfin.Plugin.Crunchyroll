using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using Jellyfin.Plugin.ExternalComments.Configuration;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll;
using Jellyfin.Plugin.ExternalComments.Features.WaybackMachine;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ExternalComments;

public class ExternalCommentsPlugin : MediaBrowser.Common.Plugins.BasePlugin<PluginConfiguration>, IHasWebPages
{
    private readonly ILogger<ExternalCommentsPlugin> _logger;
    private readonly ILibraryManager _libraryManager;
    private readonly Action<IServiceCollection>? _serviceCollectionOptions;

    public override string Name => "ExternalComments";
    public override Guid Id => Guid.Parse("c6f8461a-9a6f-4c65-8bb9-825866cabc91");
    public IServiceProvider ServiceProvider { get; private set; } = null!;
    public static ExternalCommentsPlugin? Instance { get; private set; }

    public ExternalCommentsPlugin(ILogger<ExternalCommentsPlugin> logger, ILoggerFactory loggerFactory, IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, 
        ILibraryManager libraryManager, Action<IServiceCollection>? serviceCollectionOptions = null) : base(applicationPaths, xmlSerializer)
    {
        _logger = logger;
        _libraryManager = libraryManager;
        _serviceCollectionOptions = serviceCollectionOptions;

        BuildServiceCollection(loggerFactory);
        InjectClientSideScriptIntoIndexFile(applicationPaths);
        
        Instance = this;
    }

    private void BuildServiceCollection(ILoggerFactory loggerFactory)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddMediator();
        serviceCollection.AddMemoryCache();
        
        serviceCollection.AddCrunchyroll(Configuration);
        serviceCollection.AddWaybackMachine();

        serviceCollection.AddSingleton<PluginConfiguration>(Configuration);
        serviceCollection.AddSingleton<ILibraryManager>(_libraryManager);
        
        serviceCollection.AddSingleton<ILoggerFactory>(loggerFactory);

        _serviceCollectionOptions?.Invoke(serviceCollection);

        ServiceProvider = serviceCollection.BuildServiceProvider();
    }

    private void InjectClientSideScriptIntoIndexFile(IApplicationPaths applicationPaths)
    {
        const string scriptHtmlElement = "<script src=\"/api/ExternalComments/Script\"></script>";
        
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
    
    public IEnumerable<PluginPageInfo> GetPages()
    {
        yield return new PluginPageInfo
        {
            Name = Name,
            EmbeddedResourcePath = GetType().Namespace + ".Configuration.configPage.html"
        };
    }
}