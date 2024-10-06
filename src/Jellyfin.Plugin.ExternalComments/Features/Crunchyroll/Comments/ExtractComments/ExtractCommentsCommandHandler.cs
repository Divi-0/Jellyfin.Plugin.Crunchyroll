using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using FluentResults;
using Jellyfin.Plugin.ExternalComments.Configuration;
using Jellyfin.Plugin.ExternalComments.Contracts.Comments;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Avatar.Client;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Comments.Entites;
using Jellyfin.Plugin.ExternalComments.Features.WaybackMachine.Client;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Comments.ExtractComments;

public record ExtractCommentsCommand(string EpisodeId, string EpisodeSlugTitle) : IRequest<Result>;

public class ExtractCommentsCommandHandler : IRequestHandler<ExtractCommentsCommand, Result>
{
    private readonly IHtmlCommentsExtractor _extractor;
    private readonly IExtractCommentsSession _session;
    private readonly PluginConfiguration _config;
    private readonly IWaybackMachineClient _waybackMachineClient;
    private readonly IAvatarClient _avatarClient;
    private readonly ILogger<ExtractCommentsCommandHandler> _logger;

    private static readonly DateTime DateWhenCommentsWereDeleted = new DateTime(2024, 07, 10);

    public ExtractCommentsCommandHandler(IHtmlCommentsExtractor extractor, IExtractCommentsSession session,
        PluginConfiguration config, IWaybackMachineClient waybackMachineClient, IAvatarClient avatarClient,
        ILogger<ExtractCommentsCommandHandler> logger)
    {
        _extractor = extractor;
        _session = session;
        _config = config;
        _waybackMachineClient = waybackMachineClient;
        _avatarClient = avatarClient;
        _logger = logger;
    }
    
    public async ValueTask<Result> Handle(ExtractCommentsCommand request, CancellationToken cancellationToken)
    {
        if (await _session.CommentsForEpisodeExists(request.EpisodeId))
        {
            _logger.LogDebug("Comments already exist for episode with id {EpisodeId}. Skipping...", request.EpisodeId);
            return Result.Ok();
        }
        
        var crunchyrollUrl = Path.Combine(
                _config.CrunchyrollUrl.Contains("www") ? _config.CrunchyrollUrl.Split("www.")[1] : _config.CrunchyrollUrl.Split("//")[1], 
                new CultureInfo(_config.CrunchyrollLanguage).TwoLetterISOLanguageName,
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
                commentsResult.Errors.First().Message ==
                ExtractCommentsErrorCodes.HtmlExtractorInvalidCrunchyrollCommentsPage)
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
        
        await Parallel.ForEachAsync(comments.Select(x => x.AvatarIconUri), parallelOptions, async (avatarUri, token) =>
        {
            if (string.IsNullOrWhiteSpace(avatarUri))
            {
                return;
            }

            if (await _session.AvatarExistsAsync(avatarUri))
            {
                return;
            }
            
            var avatarResult = await _avatarClient.GetAvatarStreamAsync(avatarUri, token);

            if (avatarResult.IsFailed)
            {
                return;
            }

            var imageStream = avatarResult.Value;
            
            await _session.AddAvatarImageAsync(avatarUri, imageStream);
        });
    }
}