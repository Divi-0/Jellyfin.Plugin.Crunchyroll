using System;
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
    private readonly PluginConfiguration _config;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public CrunchyrollScan(ILogger<CrunchyrollScan> logger, ILibraryManager libraryManager, 
        PluginConfiguration? config = null)
    {
        _logger = logger;
        _libraryManager = libraryManager;

        _serviceScopeFactory = CrunchyrollPlugin.Instance!.ServiceProvider
            .GetRequiredService<IServiceScopeFactory>();
        
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

        using var serviceScope = _serviceScopeFactory.CreateScope();
        
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

    private bool IsConfigValid()
    {
        if (string.IsNullOrWhiteSpace(_config.CrunchyrollUrl))
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