using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.ExternalComments.Configuration;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Login;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.PostScan.Interfaces;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.PostScan;

public class CrunchyrollScan : ILibraryPostScanTask
{
    private readonly ILogger<CrunchyrollScan> _logger;
    private readonly ILibraryManager _libraryManager;
    private readonly IEnumerable<IPostScanTask>? _postScanTasks;

    public CrunchyrollScan(ILogger<CrunchyrollScan> logger, ILibraryManager libraryManager, 
        IEnumerable<IPostScanTask> postScanTasks)
    {
        _logger = logger;
        _libraryManager = libraryManager;
        
        var scanTasks = postScanTasks.ToArray();
        _postScanTasks = scanTasks.Length != 0 ? 
            scanTasks : 
            ExternalCommentsPlugin.Instance!.ServiceProvider.GetServices<IPostScanTask>();
    }

    public async Task Run(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var allItems = _libraryManager.GetItemList(new InternalItemsQuery())
            .Where(x => x is Series or Movie).ToList();

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

        progress.Report(100);
    }
}