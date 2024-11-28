using FluentResults;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Mediator;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.Interfaces;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.SearchTitleId;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan;

public class SetTitleIdTask : IPostSeriesScanTask
{
    private readonly IMediator _mediator;
    private readonly IEnumerable<IPostTitleIdSetTask> _postTitleIdSetTasks;
    private readonly ILogger<SetTitleIdTask> _logger;
    private readonly ILibraryManager _libraryManager;

    public SetTitleIdTask(IMediator mediator, IEnumerable<IPostTitleIdSetTask> postTitleIdSetTasks,
        ILogger<SetTitleIdTask> logger, ILibraryManager libraryManager)
    {
        _mediator = mediator;
        _postTitleIdSetTasks = postTitleIdSetTasks;
        _logger = logger;
        _libraryManager = libraryManager;
    }

    public async Task RunAsync(BaseItem item, CancellationToken cancellationToken)
    {
        var setTitleIdResult = await SetTitleId(item, cancellationToken);

        if (setTitleIdResult.IsFailed)
        {
            return;
        }

        await RunPostTasks(item, cancellationToken);
    }

    private async Task<Result> SetTitleId(BaseItem item, CancellationToken cancellationToken)
    {
        if (item.ProviderIds.TryGetValue(CrunchyrollExternalKeys.SeriesId, out string? id) && !string.IsNullOrWhiteSpace(id))
        {
            _logger.LogDebug("TitleId for Item {Name} is already set", item.FileNameWithoutExtension);
            return Result.Ok();
        }

        try
        {
            var titleIdResult = await _mediator.Send(new TitleIdQuery(
                item.FileNameWithoutExtension,
                item.GetPreferredMetadataCultureInfo()), 
                cancellationToken);

            if (titleIdResult.IsFailed)
            {
                return Result.Ok();
            }

            item.ProviderIds[CrunchyrollExternalKeys.SeriesId] = titleIdResult.Value?.Id ?? string.Empty;
            item.ProviderIds[CrunchyrollExternalKeys.SeriesSlugTitle] = titleIdResult.Value?.SlugTitle ?? string.Empty;

            await _libraryManager.UpdateItemAsync(item, item.DisplayParent, ItemUpdateType.MetadataEdit, cancellationToken);

            return Result.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unknown error on getting crunchyroll TitleId for {Name}", item.FileNameWithoutExtension);
            return Result.Fail(ErrorCodes.UnknownError);
        }
    }

    private async Task RunPostTasks(BaseItem baseItem, CancellationToken cancellationToken)
    {
        foreach (var task in _postTitleIdSetTasks)
        {
            await task.RunAsync(baseItem, cancellationToken);
        }
    }
}