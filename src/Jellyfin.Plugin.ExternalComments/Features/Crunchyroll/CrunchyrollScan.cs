using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.ExternalComments.Configuration;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Login;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Reviews.ExtractReviews;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.SearchTitleId;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.GetSeasonId;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll;

public class CrunchyrollScan : ILibraryPostScanTask
{
    private readonly ILogger<CrunchyrollScan> _logger;
    private readonly IMediator _mediator;
    private readonly ILibraryManager _libraryManager;
    private readonly PluginConfiguration _config;
    private readonly ILoginService _loginService;

    public CrunchyrollScan(ILogger<CrunchyrollScan> logger, ILibraryManager libraryManager, PluginConfiguration? config = null,
        IMediator? mediator = null, ILoginService? loginService = null)
    {
        _logger = logger;
        _libraryManager = libraryManager;
        _config = config ?? ExternalCommentsPlugin.Instance!.ServiceProvider.GetRequiredService<PluginConfiguration>();
        _mediator = mediator ?? ExternalCommentsPlugin.Instance!.ServiceProvider.GetRequiredService<IMediator>();
        _loginService = loginService ?? ExternalCommentsPlugin.Instance!.ServiceProvider.GetRequiredService<ILoginService>();
    }
    
    public async Task Run(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var allItems = _libraryManager.GetItemList(new InternalItemsQuery())
            .Where(x => x is Series or Movie).ToList();
        
        var percent = 0.0;

        var loginResult = await _loginService.LoginAnonymously(cancellationToken);

        if (loginResult.IsFailed)
        {
            return;
        }
        
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount / 2
        };
            
        var sumSemaphore = new SemaphoreSlim(1, 1);
        await Parallel.ForEachAsync(allItems, options, async (item, _) =>
        {
            try
            {
                await SearchAndAssignTitleId(item, cancellationToken);
                await ScrapTitleMetadata(item, cancellationToken);
                await SetSeasonIds(item, cancellationToken);

                if (_config.IsWaybackMachineEnabled)
                {
                    await ExtractReviews(item, cancellationToken);
                }

                await sumSemaphore.WaitAsync(cancellationToken);
                try
                {
                    percent += 100.0 / allItems.Count;
                    progress.Report(percent);
                }
                finally
                {
                    sumSemaphore.Release();
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occured during crunchyroll scan");
            }
        });

        progress.Report(100);
    }

    private async ValueTask SearchAndAssignTitleId(BaseItem item, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Found {Name}", item.Name);

        if (item.ProviderIds.TryGetValue(CrunchyrollExternalKeys.Id, out string? id) && !string.IsNullOrWhiteSpace(id))
        {
            //if an id already exists skip this item
            return;
        }

        Result<SearchResponse?> titleIdResult;
        try
        {
            titleIdResult = await _mediator.Send(new TitleIdQuery(item.Name), cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error on getting title id for {Name}", item.Name);
            return;
        }
    
        if (titleIdResult.IsFailed)
        {
            _logger.LogWarning("Failed to get title id for {Name} Error: {@Errors}", item.Name, titleIdResult.Errors);
            return;
        }
        
        item.ProviderIds[CrunchyrollExternalKeys.Id] = titleIdResult.Value?.Id ?? string.Empty;
        item.ProviderIds[CrunchyrollExternalKeys.SlugTitle] = titleIdResult.Value?.SlugTitle ?? string.Empty;
    
        await _libraryManager.UpdateItemAsync(item, item.DisplayParent, ItemUpdateType.MetadataEdit, cancellationToken);
    }

    private async ValueTask SetSeasonIds(BaseItem item, CancellationToken cancellationToken)
    {
        var hasId = item.ProviderIds.TryGetValue(CrunchyrollExternalKeys.Id, out string? id) &&
                    !string.IsNullOrWhiteSpace(id);
        
        if (!hasId)
        {
            //if item has no id, skip this item
            return;
        }

        var seasonNumberDuplicateCounters = new Dictionary<int, int>();
        foreach (var season in ((Folder)item).Children)
        {
            var hasSeasonId = season.ProviderIds.TryGetValue(CrunchyrollExternalKeys.SeasonId, out string? seasonId) && 
                              !string.IsNullOrWhiteSpace(seasonId);
            
            if (!season.IndexNumber.HasValue || hasSeasonId)
            {
                //if the item has no indexNumber or already has a seasonId, ignore it
                continue;
            }
            
            Result<string?> seasonIdResult;
            try
            {
                seasonNumberDuplicateCounters.TryGetValue(season.IndexNumber.Value, out var seasonCounter);
                
                var query = new SeasonIdQueryByNumber(id!, season.IndexNumber.Value, seasonCounter);
                seasonIdResult = await _mediator.Send(query, cancellationToken);
                
                seasonCounter += 1;
                
                if (seasonIdResult.IsFailed)
                {
                    _logger.LogWarning("Failed to get season id by number for {Name} Error: {@Errors}", item.Name, seasonIdResult.Errors);
                    continue;
                }

                if (seasonIdResult.Value is not null)
                {
                    seasonNumberDuplicateCounters[season.IndexNumber.Value] = seasonCounter;
                }

                if (seasonIdResult.Value is null)
                {
                    var byNameQuery = new SeasonIdQueryByName(id!, season.Name);
                    seasonIdResult = await _mediator.Send(byNameQuery, cancellationToken);
                }
                
                if (seasonIdResult.IsFailed)
                {
                    _logger.LogWarning("Failed to get season id by name for {Name} Error: {@Errors}", item.Name, seasonIdResult.Errors);
                    continue;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error on getting season id for {Name}", item.Name);
                continue;
            }
        
            season.ProviderIds[CrunchyrollExternalKeys.SeasonId] = seasonIdResult.Value ?? string.Empty;
            
            await _libraryManager.UpdateItemAsync(season, season.DisplayParent, ItemUpdateType.MetadataEdit, cancellationToken);
        }
    }

    private async ValueTask ScrapTitleMetadata(BaseItem item, CancellationToken cancellationToken)
    {
        var hasId = item.ProviderIds.TryGetValue(CrunchyrollExternalKeys.Id, out string? id) &&
                    !string.IsNullOrWhiteSpace(id);

        var hasSlugTitle = item.ProviderIds.TryGetValue(CrunchyrollExternalKeys.SlugTitle, out string? slugTitle) &&
                           !string.IsNullOrWhiteSpace(slugTitle);
        
        if (!hasId || !hasSlugTitle)
        {
            //if item has no id or slugTitle, skip this item
            return;
        }
            
        _ = await _mediator.Send(new ScrapTitleMetadataCommand()
        {
            TitleId = id!,
            SlugTitle = slugTitle!
        }, cancellationToken);
    }

    private async ValueTask ExtractReviews(BaseItem item, CancellationToken cancellationToken)
    {
        var hasId = item.ProviderIds.TryGetValue(CrunchyrollExternalKeys.Id, out string? id) &&
                    !string.IsNullOrWhiteSpace(id);

        var hasSlugTitle = item.ProviderIds.TryGetValue(CrunchyrollExternalKeys.SlugTitle, out string? slugTitle) &&
                           !string.IsNullOrWhiteSpace(slugTitle);
        
        if (!hasId || !hasSlugTitle)
        {
            //if item has no id or slugTitle, skip this item
            return;
        }
            
        _ = await _mediator.Send(new ExtractReviewsCommand()
        {
            TitleId = item.ProviderIds[CrunchyrollExternalKeys.Id],
            SlugTitle = item.ProviderIds[CrunchyrollExternalKeys.SlugTitle]
        }, cancellationToken);
    }
}