using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using FluentResults;
using FluentResults.Extensions;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Contracts.Comments;
using Jellyfin.Plugin.Crunchyroll.Domain.Constants;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Avatar.AddAvatar;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Comments.Entites;
using Jellyfin.Plugin.Crunchyroll.Features.WaybackMachine.Client;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Comments.ExtractComments;

public record ExtractCommentsCommand(string EpisodeId, CultureInfo Language) : IRequest<Result>;

public class ExtractCommentsCommandHandler : IRequestHandler<ExtractCommentsCommand, Result>
{
    private readonly IHtmlCommentsExtractor _extractor;
    private readonly IExtractCommentsRepository _repository;
    private readonly PluginConfiguration _config;
    private readonly IWaybackMachineClient _waybackMachineClient;
    private readonly IAddAvatarService _addAvatarService;
    private readonly ILogger<ExtractCommentsCommandHandler> _logger;

    private static readonly DateTime DateWhenCommentsWereDeleted = new DateTime(2024, 07, 10);

    public ExtractCommentsCommandHandler(IHtmlCommentsExtractor extractor, IExtractCommentsRepository repository,
        PluginConfiguration config, IWaybackMachineClient waybackMachineClient, IAddAvatarService addAvatarService,
        ILogger<ExtractCommentsCommandHandler> logger)
    {
        _extractor = extractor;
        _repository = repository;
        _config = config;
        _waybackMachineClient = waybackMachineClient;
        _addAvatarService = addAvatarService;
        _logger = logger;
    }
    
    public async ValueTask<Result> Handle(ExtractCommentsCommand request, CancellationToken cancellationToken)
    {
        var commentsExistsResult = await _repository.CommentsForEpisodeExistsAsync(request.EpisodeId, cancellationToken);

        if (commentsExistsResult.IsFailed)
        {
            return commentsExistsResult.ToResult();
        }
        
        if (commentsExistsResult.Value)
        {
            _logger.LogDebug("Comments already exist for episode with id {EpisodeId}. Skipping...", request.EpisodeId);
            return Result.Ok();
        }

        var slugTitleResult = await _repository.GetEpisodeSlugTitleAsync(request.EpisodeId, cancellationToken);

        if (slugTitleResult.IsFailed)
        {
            return slugTitleResult.ToResult();
        }

        //can be empty, but not null
        if (slugTitleResult.Value is null)
        {
            _logger.LogError("Failed to get slug title for episode with id {EpisodeId}", request.EpisodeId);
            return Result.Fail(ErrorCodes.NotFound);
        }
        
        var crunchyrollUrl = Path.Combine(
                _config.CrunchyrollUrl.Contains("www") ? _config.CrunchyrollUrl.Split("www.")[1] : _config.CrunchyrollUrl.Split("//")[1], 
                request.Language.TwoLetterISOLanguageName == "en" ? string.Empty : request.Language.TwoLetterISOLanguageName,
                "watch",
                request.EpisodeId,
                slugTitleResult.Value)
            .Replace('\\', '/');
        
        var searchResult = await _waybackMachineClient.SearchAsync(HttpUtility.UrlEncode(crunchyrollUrl), DateWhenCommentsWereDeleted, cancellationToken);

        if (searchResult.IsFailed)
        {
            return searchResult.ToResult();
        }

        //if invalid html error: retry with next timestamp; from last to first
        var commentsResult = Result.Ok<IReadOnlyList<CommentItem>>([]);
        for (var index = searchResult.Value.Count - 1; index >= 0; index--)
        {
            var searchResponse = searchResult.Value[index];
            var snapshotUrl = Path.Combine(
                    _config.ArchiveOrgUrl,
                    "web",
                    searchResponse.Timestamp.ToString("yyyyMMddHHmmss"),
                    crunchyrollUrl)
                .Replace('\\', '/');

            commentsResult = await _extractor.GetCommentsAsync(snapshotUrl, request.Language, cancellationToken);

            if (commentsResult.IsFailed)
            {
                continue;
            }

            break;
        }

        var comments = commentsResult.ValueOrDefault ?? [];
        
        await HandleAvatarUris(comments, cancellationToken);

        var dbResult = await _repository.AddCommentsAsync(new EpisodeComments()
        {
            CrunchyrollEpisodeId = request.EpisodeId,
            Comments = JsonSerializer.Serialize(comments.ToArray()),
            Language = request.Language.Name
        }, cancellationToken)
        .Bind(async () => await _repository.SaveChangesAsync(cancellationToken));

        return dbResult.IsFailed 
            ? dbResult 
            : Result.Ok();
    }

    private async ValueTask HandleAvatarUris(IReadOnlyCollection<CommentItem> comments, CancellationToken cancellationToken)
    {
        var parallelOptions = new ParallelOptions
        {
            CancellationToken = cancellationToken
        };
        
        await Parallel.ForEachAsync(comments, parallelOptions, async (comment, token) =>
        {
            if (string.IsNullOrWhiteSpace(comment.AvatarIconUri))
            {
                return;
            }
            
            var addAvatarResult = await _addAvatarService.AddAvatarIfNotExists(comment.AvatarIconUri, token);
            if (addAvatarResult.IsSuccess)
            {
                comment.AvatarIconUri = addAvatarResult.Value;
            }
        });
    }
}