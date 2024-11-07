using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.Interfaces;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Login;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan;

public class CrunchyrollScan : ILibraryPostScanTask
{
    private readonly ILogger<CrunchyrollScan> _logger;
    private readonly ILibraryManager _libraryManager;
    private readonly IEnumerable<IPostScanTask>? _postScanTasks;
    private readonly PluginConfiguration _config;

    public CrunchyrollScan(ILogger<CrunchyrollScan> logger, ILibraryManager libraryManager, 
        IEnumerable<IPostScanTask> postScanTasks, PluginConfiguration? config = null)
    {
        _logger = logger;
        _libraryManager = libraryManager;
        
        var scanTasks = postScanTasks.ToArray();
        _postScanTasks = scanTasks.Length != 0 ? 
            scanTasks : 
            CrunchyrollPlugin.Instance!.ServiceProvider.GetServices<IPostScanTask>();
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
            }).Where(x => x is Series or Movie).ToList();

        var percent = 0.0;

        foreach (var item in allItems)
        {
            try
            {
                foreach (var postScanTask in _postScanTasks ?? [])
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

        var elapsedTime = Stopwatch.GetElapsedTime(startTimestamp);
        _logger.LogInformation("CrunchyrollScan took {ElapsedTime}", elapsedTime.ToString("g"));

        progress.Report(100);
    }

    private bool IsConfigValid()
    {
        if (string.IsNullOrWhiteSpace(_config.CrunchyrollUrl) ||
            string.IsNullOrWhiteSpace(_config.CrunchyrollLanguage) ||
            string.IsNullOrWhiteSpace(_config.FlareSolverrUrl) ||
            _config.FlareSolverrTimeout == 0)
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