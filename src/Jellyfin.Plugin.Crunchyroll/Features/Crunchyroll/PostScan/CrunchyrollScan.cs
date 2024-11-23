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

public class CrunchyrollScan : ILibraryPostScanTask
{
    private readonly ILogger<CrunchyrollScan> _logger;
    private readonly ILibraryManager _libraryManager;
    private readonly IEnumerable<IPostSeriesScanTask>? _postSeriesScanTasks;
    private readonly IEnumerable<IPostMovieScanTask>? _postMovieScanTasks;
    private readonly PluginConfiguration _config;

    public CrunchyrollScan(ILogger<CrunchyrollScan> logger, ILibraryManager libraryManager, 
        IEnumerable<IPostSeriesScanTask> postSeriesScanTasks, IEnumerable<IPostMovieScanTask> postMovieScanTasks, 
        PluginConfiguration? config = null)
    {
        _logger = logger;
        _libraryManager = libraryManager;
        
        var seriesScanTasks = postSeriesScanTasks.ToArray();
        _postSeriesScanTasks = seriesScanTasks.Length != 0 
            ? seriesScanTasks 
            : CrunchyrollPlugin.Instance!.ServiceProvider.GetServices<IPostSeriesScanTask>();
        
        var movieScanTasks = postMovieScanTasks.ToArray();
        _postMovieScanTasks = movieScanTasks.Length != 0 
            ? movieScanTasks 
            : CrunchyrollPlugin.Instance!.ServiceProvider.GetServices<IPostMovieScanTask>();
        
        _config = config ?? CrunchyrollPlugin.Instance!.ServiceProvider.GetRequiredService<PluginConfiguration>();
    }

    public async Task Run(IProgress<double> progress, CancellationToken cancellationToken)
    {
        if (!IsConfigValid())
        {
            _logger.LogWarning("Invalid crunchyroll plugin configuration. Skipping...");
            progress.Report(100);
            return;
        }
        
        var startTimestamp = Stopwatch.GetTimestamp();
        
        Guid? topParentId = null;
        if (!string.IsNullOrWhiteSpace(_config.LibraryPath))
        {
            var result = _libraryManager.GetItemIds(new InternalItemsQuery()
            {
                Path = _config.LibraryPath
            });

            topParentId = result.Count != 0 ? result[0] : null;
        }
        
        var allItems = _libraryManager.GetItemList(new InternalItemsQuery()
            {                                                                                                                                                                                                                      
                TopParentIds = topParentId.HasValue ? [topParentId.Value] : []
            });

        var percent = 0.0;

        foreach (var item in allItems.Where(x => x is Series))
        {
            try
            {
                foreach (var postScanTask in _postSeriesScanTasks ?? [])
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

        await Parallel.ForEachAsync(allItems.Where(x => x is Movie), cancellationToken, async (movie, _) =>
        {
            foreach (var postScanTask in _postMovieScanTasks ?? [])
            {
                await postScanTask.RunAsync(movie, cancellationToken);
            }
        });

        var elapsedTime = Stopwatch.GetElapsedTime(startTimestamp);
        _logger.LogInformation("CrunchyrollScan took {ElapsedTime}", elapsedTime.ToString("g"));

        progress.Report(100);
    }

    private bool IsConfigValid()
    {
        if (string.IsNullOrWhiteSpace(_config.CrunchyrollUrl) ||
            string.IsNullOrWhiteSpace(_config.CrunchyrollLanguage))
        {
            return false;
        }

        if (_config.IsWaybackMachineEnabled && string.IsNullOrWhiteSpace(_config.ArchiveOrgUrl))
        {
            return false;
        }
        
        return true;
    }
}