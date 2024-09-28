using FluentResults;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.PostScan.Interfaces;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.SearchTitleId;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Mediator;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.PostScan;

public class SetTitleIdTask : IPostScanTask
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
        if (item.ProviderIds.TryGetValue(CrunchyrollExternalKeys.Id, out string? id) && !string.IsNullOrWhiteSpace(id))
        {
            _logger.LogDebug("TitleId for Item with {Name} is already set", item.Name);
            return Result.Ok();
        }

        try
        {
            var titleIdResult = await _mediator.Send(new TitleIdQuery(item.Name), cancellationToken);

            if (titleIdResult.IsFailed)
            {
                return Result.Ok();
            }

            item.ProviderIds[CrunchyrollExternalKeys.Id] = titleIdResult.Value?.Id ?? string.Empty;
            item.ProviderIds[CrunchyrollExternalKeys.SlugTitle] = titleIdResult.Value?.SlugTitle ?? string.Empty;

            await _libraryManager.UpdateItemAsync(item, item.DisplayParent, ItemUpdateType.MetadataEdit, cancellationToken);

            return Result.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unknown error on getting crunchyroll TitleId for {Name}", item.Name);
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