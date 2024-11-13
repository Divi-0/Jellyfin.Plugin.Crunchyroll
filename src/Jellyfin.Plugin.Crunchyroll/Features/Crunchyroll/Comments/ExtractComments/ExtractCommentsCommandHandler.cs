using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Contracts.Comments;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Avatar.AddAvatar;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Avatar.Client;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Comments.Entites;
using Jellyfin.Plugin.Crunchyroll.Features.WaybackMachine.Client;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Comments.ExtractComments;

public record ExtractCommentsCommand(string EpisodeId, string EpisodeSlugTitle) : IRequest<Result>;

public class ExtractCommentsCommandHandler : IRequestHandler<ExtractCommentsCommand, Result>
{
    private readonly IHtmlCommentsExtractor _extractor;
    private readonly IExtractCommentsSession _session;
    private readonly PluginConfiguration _config;
    private readonly IWaybackMachineClient _waybackMachineClient;
    private readonly IAddAvatarService _addAvatarService;
    private readonly ILogger<ExtractCommentsCommandHandler> _logger;

    private static readonly DateTime DateWhenCommentsWereDeleted = new DateTime(2024, 07, 10);

    public ExtractCommentsCommandHandler(IHtmlCommentsExtractor extractor, IExtractCommentsSession session,
        PluginConfiguration config, IWaybackMachineClient waybackMachineClient, IAddAvatarService addAvatarService,
        ILogger<ExtractCommentsCommandHandler> logger)
    {
        _extractor = extractor;
        _session = session;
        _config = config;
        _waybackMachineClient = waybackMachineClient;
        _addAvatarService = addAvatarService;
        _logger = logger;
    }
    
    public async ValueTask<Result> Handle(ExtractCommentsCommand request, CancellationToken cancellationToken)
    {
        if (await _session.CommentsForEpisodeExists(request.EpisodeId))
        {
            _logger.LogDebug("Comments already exist for episode with id {EpisodeId}. Skipping...", request.EpisodeId);
            return Result.Ok();
        }
        
        var twoLetterIsoLanguageName = new CultureInfo(_config.CrunchyrollLanguage).TwoLetterISOLanguageName;
        var crunchyrollUrl = Path.Combine(
                _config.CrunchyrollUrl.Contains("www") ? _config.CrunchyrollUrl.Split("www.")[1] : _config.CrunchyrollUrl.Split("//")[1], 
                twoLetterIsoLanguageName == "en" ? string.Empty : twoLetterIsoLanguageName,
                "watch",
                request.EpisodeId,
                request.EpisodeSlugTitle)
            .Replace('\\', '/');
        
        var searchResult = await _waybackMachineClient.SearchAsync(HttpUtility.UrlEncode(crunchyrollUrl), DateWhenCommentsWereDeleted, cancellationToken);

        if (searchResult.IsFailed)
        {
            return searchResult.ToResult();
        }

        //if invalid html error: retry with next timestamp; from last to first
        Result<IReadOnlyList<CommentItem>> commentsResult = null!;
        for (var index = searchResult.Value.Count - 1; index >= 0; index--)
        {
            var searchResponse = searchResult.Value[index];
            var snapshotUrl = Path.Combine(
                    _config.ArchiveOrgUrl,
                    "web",
                    searchResponse.Timestamp.ToString("yyyyMMddHHmmss"),
                    crunchyrollUrl)
                .Replace('\\', '/');

            commentsResult = await _extractor.GetCommentsAsync(snapshotUrl, cancellationToken);

            if (commentsResult.IsFailed &&
                (commentsResult.Errors.First().Message == ExtractCommentsErrorCodes.HtmlExtractorInvalidCrunchyrollCommentsPage 
                 || commentsResult.Errors.First().Message == ExtractCommentsErrorCodes.HtmlUrlRequestFailed))
            {
                continue;
            }

            break;
        }

        if (commentsResult.IsFailed)
        {
            return commentsResult.ToResult();
        }

        await _session.InsertComments(new EpisodeComments()
        {
            EpisodeId = request.EpisodeId,
            Comments = commentsResult.Value
        });
        
        await HandleAvatarUris(commentsResult.Value, cancellationToken);
        
        return Result.Ok();
    }

    private async ValueTask HandleAvatarUris(IReadOnlyCollection<CommentItem> comments, CancellationToken cancellationToken)
    {
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount / 2,
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