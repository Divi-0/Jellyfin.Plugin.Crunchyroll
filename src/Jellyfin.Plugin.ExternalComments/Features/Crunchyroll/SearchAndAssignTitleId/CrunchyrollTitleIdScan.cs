using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.ExternalComments.Domain.Constants;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.ExtractReviews;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.SearchAndAssignTitleId;

public class CrunchyrollTitleIdScan : ILibraryPostScanTask
{
    private readonly ILogger<CrunchyrollTitleIdScan> _logger;
    private readonly IMediator _mediator;
    private readonly ILibraryManager _libraryManager;
    
    public CrunchyrollTitleIdScan(ILogger<CrunchyrollTitleIdScan> logger, ILibraryManager libraryManager, IMediator? mediator = null)
    {
        _logger = logger;
        _libraryManager = libraryManager;
        _mediator = mediator ?? ExternalCommentsPlugin.Instance!.ServiceProvider.GetRequiredService<IMediator>();
    }
    
    public async Task Run(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var allItems = _libraryManager.GetItemList(new InternalItemsQuery())
            .Where(x => x is Series or Movie).ToList();
        
        var percent = 0.0;
        foreach (var item in allItems)
        {
            //_logger.LogDebug("Found {Name}", item.Name);

            if (item.ProviderIds.TryGetValue(CrunchyrollExternalKeys.Id, out string? id) && !string.IsNullOrWhiteSpace(id))
            {
                //if an id already exists skip this item
                continue;
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
                break;
            }
            
            item.ProviderIds[CrunchyrollExternalKeys.Id] = titleIdResult.Value?.Id ?? string.Empty;
            item.ProviderIds[CrunchyrollExternalKeys.SlugTitle] = titleIdResult.Value?.SlugTitle ?? string.Empty;
        
            await _libraryManager.UpdateItemAsync(item, item.DisplayParent, ItemUpdateType.MetadataEdit, cancellationToken);
        
            //_logger.LogDebug("Ids: {@Ids}", _libraryManager.GetItemById(item.Id)?.ProviderIds);

            if (!string.IsNullOrWhiteSpace(item.ProviderIds[CrunchyrollExternalKeys.Id])
                && !string.IsNullOrWhiteSpace(item.ProviderIds[CrunchyrollExternalKeys.SlugTitle]))
            {
                await _mediator.Send(new ExtractReviewsCommand()
                {
                    TitleId = item.ProviderIds[CrunchyrollExternalKeys.Id],
                    SlugTitle = item.ProviderIds[CrunchyrollExternalKeys.SlugTitle]
                }, cancellationToken);
            }
        
            percent += 100.0 / allItems.Count;
            progress.Report(percent);
        }
        
        progress.Report(100);
    }
}