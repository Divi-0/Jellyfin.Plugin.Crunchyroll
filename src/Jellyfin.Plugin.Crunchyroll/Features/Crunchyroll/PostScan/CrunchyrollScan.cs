using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.Interfaces;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan;

public sealed class CrunchyrollScan : ILibraryPostScanTask
{
    private readonly ILogger<CrunchyrollScan> _logger;
    private readonly ILibraryManager _libraryManager;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public CrunchyrollScan(ILogger<CrunchyrollScan> logger, ILibraryManager libraryManager)
    {
        _logger = logger;
        _libraryManager = libraryManager;

        _serviceScopeFactory = CrunchyrollPlugin.Instance!.ServiceProvider
            .GetRequiredService<IServiceScopeFactory>();
    }

    public async Task Run(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var config = CrunchyrollPlugin.Instance!.ServiceProvider.GetRequiredService<PluginConfiguration>();
        
        if (!IsConfigValid(config))
        {
            _logger.LogWarning("Invalid crunchyroll plugin configuration. Skipping...");
            progress.Report(100);
            return;
        }

        using var serviceScope = _serviceScopeFactory.CreateScope();
        
        var startTimestamp = Stopwatch.GetTimestamp();
        
        CollectionFolder? collectionFolder = null;
        if (!config.IsScanInAllLibrariesEnabled && !string.IsNullOrWhiteSpace(config.LibraryName))
        {
            var result = _libraryManager.GetItemList(new InternalItemsQuery()
            {
                Name = config.LibraryName
            });

            collectionFolder = result.FirstOrDefault(x => x is CollectionFolder) as CollectionFolder;
        }
        
        var allItems = collectionFolder is not null 
            ? GetAllSeriesAndMoviesRecursive(collectionFolder.Children) : 
            _libraryManager.GetItemList(new InternalItemsQuery());

        var percent = 0.0;

        foreach (var item in allItems.Where(x => x is Series))
        {
            try
            {
                foreach (var postScanTask in serviceScope.ServiceProvider.GetServices<IPostSeriesScanTask>())
                {
                    await postScanTask.RunAsync(item, cancellationToken);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occured during crunchyroll scan");
            }
            finally
            {
                percent += 100.0 / allItems.Count;
                progress.Report(percent);
            }
        }

        foreach (var movie in allItems.Where(x => x is Movie))
        {
            foreach (var postScanTask in serviceScope.ServiceProvider.GetServices<IPostMovieScanTask>())
            {
                await postScanTask.RunAsync(movie, cancellationToken);
            }
        }

        var elapsedTime = Stopwatch.GetElapsedTime(startTimestamp);
        _logger.LogInformation("CrunchyrollScan took {ElapsedTime}", elapsedTime.ToString("g"));

        progress.Report(100);
    }

    private bool IsConfigValid(PluginConfiguration config)
    {
        if (string.IsNullOrWhiteSpace(config.CrunchyrollUrl))
        {
            _logger.LogError("Crunchyroll url is empty");
            return false;
        }

        if (!config.IsScanInAllLibrariesEnabled && string.IsNullOrWhiteSpace(config.LibraryName))
        {
            _logger.LogError("A name for the library must be set to run the crunchyroll scan");
            return false;
        }

        if (config.IsWaybackMachineEnabled && string.IsNullOrWhiteSpace(config.ArchiveOrgUrl))
        {
            _logger.LogError("ArchiveOrgUrl url is empty");
            return false;
        }
        
        return true;
    }

    private static List<BaseItem> GetAllSeriesAndMoviesRecursive(IEnumerable<BaseItem> items)
    {
        var list = new List<BaseItem>();
        foreach (var item in items)
        {
            if (item is Series or Movie)
            {
                list.Add(item);
            }
            else if(item is Folder folder)
            {
                var children = GetAllSeriesAndMoviesRecursive(folder.Children);
                list.AddRange(children);
            }
        }

        return list;
    }
}