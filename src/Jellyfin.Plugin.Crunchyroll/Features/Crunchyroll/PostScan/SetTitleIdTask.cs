using FluentResults;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Mediator;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.Interfaces;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.SearchTitleId;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan;

public partial class SetTitleIdTask : IPostSeriesScanTask
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
        try
        {
            var extractedResult = ExtractIdFromFileNameAndSetProviderId(item);

            string? crunchyrollId = null;
            string? crunchyrollSlugTitle = null;
            if (extractedResult.IsFailed)
            {
                if (item.ProviderIds.TryGetValue(CrunchyrollExternalKeys.SeriesId, out string? id) && !string.IsNullOrWhiteSpace(id))
                {
                    _logger.LogDebug("TitleId for Item {Name} is already set", item.FileNameWithoutExtension);
                    return Result.Ok();
                }
                
                var titleIdResult = await SearchForTitleAndSetProviderIdAsync(item, cancellationToken);

                if (titleIdResult.IsFailed)
                {
                    return titleIdResult.ToResult();
                }

                crunchyrollId = titleIdResult.Value?.Id;
                crunchyrollSlugTitle = titleIdResult.Value?.SlugTitle;
            }
            else
            {
                crunchyrollId = extractedResult.Value;
                crunchyrollSlugTitle = extractedResult.Value;
            }
            
            item.ProviderIds[CrunchyrollExternalKeys.SeriesId] = crunchyrollId ?? string.Empty;
            item.ProviderIds[CrunchyrollExternalKeys.SeriesSlugTitle] = crunchyrollSlugTitle ?? string.Empty;

            await _libraryManager.UpdateItemAsync(item, item.DisplayParent, ItemUpdateType.MetadataEdit, cancellationToken);

            return Result.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unknown error on getting crunchyroll TitleId for {Name}", item.FileNameWithoutExtension);
            return Result.Fail(ErrorCodes.UnknownError);
        }
    }

    private static Result<string> ExtractIdFromFileNameAndSetProviderId(BaseItem item)
    {
        var regex = FileNameAttributeCrunchyrollIdRegex();
        var match = regex.Match(item.FileNameWithoutExtension);

        if (!match.Success)
        {
            return Result.Fail(Domain.Constants.ErrorCodes.NotFound);
        }

        return Result.Ok(match.Groups[1].Value);
    }

    private async Task<Result<SearchResponse?>> SearchForTitleAndSetProviderIdAsync(BaseItem item, CancellationToken cancellationToken)
    {
        var seriesFileNameExtraDataInNameRegex = SeriesFileNameExtraDataInNameRegex();
        var match = seriesFileNameExtraDataInNameRegex.Match(item.FileNameWithoutExtension);

        var title = match.Success
            ? match.Groups[1].Value
            : item.FileNameWithoutExtension;
                
        var titleIdResult = await _mediator.Send(new TitleIdQuery(
                title,
                item.GetPreferredMetadataCultureInfo()), 
            cancellationToken);

        return titleIdResult;
    } 

    private async Task RunPostTasks(BaseItem baseItem, CancellationToken cancellationToken)
    {
        foreach (var task in _postTitleIdSetTasks)
        {
            await task.RunAsync(baseItem, cancellationToken);
        }
    }

    [GeneratedRegex(@"^(.*) \(| \[")]
    private static partial Regex SeriesFileNameExtraDataInNameRegex();
    [GeneratedRegex(@"\[CrunchyrollId\-(.*)\]")]
    private static partial Regex FileNameAttributeCrunchyrollIdRegex();
}