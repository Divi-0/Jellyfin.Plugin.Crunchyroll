using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.Comments;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.Comments;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.Reviews;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.Reviews;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[assembly: InternalsVisibleTo("Jellyfin.Plugin.Crunchyroll.Tests")]

namespace Jellyfin.Plugin.Crunchyroll.Features.WaybackMachine;

public sealed class ExtractWaybackReviewsAndCommentsTask : IScheduledTask
{
    private readonly ILibraryManager _libraryManager;
    private readonly IServiceProvider? _serviceProviderOverride;
    private readonly ILogger<ExtractWaybackReviewsAndCommentsTask> _logger;

    public string Name => "Extract Crunchyroll Reviews and Comments";
    public string Key => "CrunchyrollWaybackExtract";
    public string Description => "Fetches archived Crunchyroll reviews and comments from the Wayback Machine without blocking library scans.";
    public string Category => "Crunchyroll";

    public ExtractWaybackReviewsAndCommentsTask(
        ILibraryManager libraryManager,
        ILogger<ExtractWaybackReviewsAndCommentsTask> logger)
    {
        _libraryManager = libraryManager;
        _logger = logger;
    }

    internal ExtractWaybackReviewsAndCommentsTask(
        ILibraryManager libraryManager,
        IServiceProvider serviceProvider,
        ILogger<ExtractWaybackReviewsAndCommentsTask> logger)
    {
        _libraryManager = libraryManager;
        _serviceProviderOverride = serviceProvider;
        _logger = logger;
    }

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        return
        [
            new TaskTriggerInfo
            {
                Type = TaskTriggerInfoType.DailyTrigger,
                TimeOfDayTicks = TimeSpan.FromHours(3).Ticks
            }
        ];
    }

    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var serviceProvider = _serviceProviderOverride ?? CrunchyrollPlugin.Instance?.ServiceProvider;
        if (serviceProvider is null)
        {
            progress.Report(100);
            return;
        }

        var config = serviceProvider.GetRequiredService<PluginConfiguration>();
        if (!config.IsFeatureReviewsEnabled && !config.IsFeatureCommentsEnabled)
        {
            progress.Report(100);
            return;
        }

        var includeTypes = new List<BaseItemKind>();
        if (config.IsFeatureReviewsEnabled)
        {
            includeTypes.Add(BaseItemKind.Series);
        }

        if (config.IsFeatureReviewsEnabled || config.IsFeatureCommentsEnabled)
        {
            includeTypes.Add(BaseItemKind.Movie);
        }

        if (config.IsFeatureCommentsEnabled)
        {
            includeTypes.Add(BaseItemKind.Episode);
        }

        var items = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [.. includeTypes],
            Recursive = true
        });

        var workItems = CollectWorkItems(items, config);
        if (workItems.Count == 0)
        {
            progress.Report(100);
            return;
        }

        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        for (var i = 0; i < workItems.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                await workItems[i](scope.ServiceProvider, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to extract Wayback Machine data for scheduled task item");
            }

            progress.Report((double)(i + 1) / workItems.Count * 100);
        }

        progress.Report(100);
    }

    private static List<Func<IServiceProvider, CancellationToken, Task>> CollectWorkItems(
        IReadOnlyList<BaseItem> items,
        PluginConfiguration config)
    {
        var seenKeys = new HashSet<string>(StringComparer.Ordinal);
        var workItems = new List<Func<IServiceProvider, CancellationToken, Task>>();

        foreach (var item in items)
        {
            if (item.PremiereDate.HasValue && item.PremiereDate.Value > WaybackMachineConstants.CrunchyrollCommentsDeletedDate)
            {
                continue;
            }

            var lang = item.GetPreferredMetadataCultureInfo().Name;
            switch (item)
            {
                case Series series when config.IsFeatureReviewsEnabled:
                    if (series.ProviderIds.TryGetValue(CrunchyrollExternalKeys.SeriesId, out var seriesId)
                        && !string.IsNullOrWhiteSpace(seriesId)
                        && seenKeys.Add($"review:{seriesId}:{lang}"))
                    {
                        workItems.Add((sp, ct) => sp.GetRequiredService<ICrunchyrollSeriesReviewsService>().ScrapReviewsAsync(series, ct));
                    }
                    break;

                case Episode episode when config.IsFeatureCommentsEnabled:
                    if (episode.ProviderIds.TryGetValue(CrunchyrollExternalKeys.EpisodeId, out var epId)
                        && !string.IsNullOrWhiteSpace(epId)
                        && seenKeys.Add($"comment:{epId}:{lang}"))
                    {
                        workItems.Add((sp, ct) => sp.GetRequiredService<ICrunchyrollEpisodeCommentsService>().ScrapCommentsAsync(episode, ct));
                    }
                    break;

                case Movie movie:
                    if (config.IsFeatureReviewsEnabled
                        && movie.ProviderIds.TryGetValue(CrunchyrollExternalKeys.SeriesId, out var movieSeriesId)
                        && !string.IsNullOrWhiteSpace(movieSeriesId)
                        && seenKeys.Add($"review:{movieSeriesId}:{lang}"))
                    {
                        workItems.Add((sp, ct) => sp.GetRequiredService<ICrunchyrollMovieReviewsService>().ScrapReviewsAsync(movie, ct));
                    }

                    if (config.IsFeatureCommentsEnabled
                        && movie.ProviderIds.TryGetValue(CrunchyrollExternalKeys.EpisodeId, out var movieEpId)
                        && !string.IsNullOrWhiteSpace(movieEpId)
                        && seenKeys.Add($"comment:{movieEpId}:{lang}"))
                    {
                        workItems.Add((sp, ct) => sp.GetRequiredService<ICrunchyrollMovieCommentsService>().ScrapCommentsAsync(movie, ct));
                    }
                    break;
            }
        }

        return workItems;
    }
}
