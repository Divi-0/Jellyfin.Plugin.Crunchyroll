using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.ExternalComments.Configuration;
using Jellyfin.Plugin.ExternalComments.Contracts.Reviews;
using Jellyfin.Plugin.ExternalComments.Features.WaybackMachine.Client;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Reviews.ExtractReviews;

public record ExtractReviewsCommand() : IRequest<Result>
{
    public required string TitleId { get; init; }
    public required string SlugTitle { get; init; }
}

public class ExtractReviewsCommandHandler : IRequestHandler<ExtractReviewsCommand, Result>
{
    private readonly IWaybackMachineClient _waybackMachineClient;
    private readonly PluginConfiguration _config;
    private readonly IHtmlReviewsExtractor _htmlReviewsExtractor;
    private readonly IAddReviewsSession _addReviewsSession;
    private readonly IGetReviewsSession _getReviewsSession;
    private readonly ILogger<ExtractReviewsCommandHandler> _logger;
    private readonly IAvatarClient _avatarClient;

    private readonly DateTime _dateWhenReviewsWereDeleted = new DateTime(2024, 07, 10);
    
    public ExtractReviewsCommandHandler(IWaybackMachineClient waybackMachineClient, PluginConfiguration config, 
        IHtmlReviewsExtractor htmlReviewsExtractor, IAddReviewsSession addReviewsSession, IGetReviewsSession getReviewsSession,
        ILogger<ExtractReviewsCommandHandler> logger, IAvatarClient avatarClient)
    {
        _waybackMachineClient = waybackMachineClient;
        _config = config;
        _htmlReviewsExtractor = htmlReviewsExtractor;
        _addReviewsSession = addReviewsSession;
        _getReviewsSession = getReviewsSession;
        _logger = logger;
        _avatarClient = avatarClient;
    }
    
    public async ValueTask<Result> Handle(ExtractReviewsCommand request, CancellationToken cancellationToken)
    {
        var hasReviewsResult = await HasTitleAnyReviews(request.TitleId);

        if (hasReviewsResult.IsFailed)
        {
            return hasReviewsResult.ToResult();
        }

        var hasTitleAnyReviews = hasReviewsResult.Value;

        if (hasTitleAnyReviews)
        {
            _logger.LogDebug("Title with id {TitleId} already has reviews", request.TitleId);
            return Result.Fail(ExtractReviewsErrorCodes.TitleAlreadyHasReviews);
        }
        
        var crunchyrollUrl = Path.Combine(
                _config.CrunchyrollUrl.Contains("www") ? _config.CrunchyrollUrl.Split("www.")[1] : _config.CrunchyrollUrl.Split("//")[1], 
            new CultureInfo(_config.CrunchyrollLanguage).TwoLetterISOLanguageName,
            "series",
            request.TitleId,
            request.SlugTitle)
            .Replace('\\', '/');
        
        var searchResult = await _waybackMachineClient.SearchAsync(crunchyrollUrl, _dateWhenReviewsWereDeleted, cancellationToken);

        if (searchResult.IsFailed)
        {
            return searchResult.ToResult();
        }

        //if invalid html error: retry with next timestamp; from last to first
        Result<IReadOnlyList<ReviewItem>> reviewsResult = null!;
        for (var index = searchResult.Value.Count - 1; index >= 0; index--)
        {
            var searchResponse = searchResult.Value[index];
            var snapshotUrl = Path.Combine(
                    _config.ArchiveOrgUrl,
                    "web",
                    searchResponse.Timestamp.ToString("yyyyMMddHHmmss"),
                    crunchyrollUrl)
                .Replace('\\', '/');

            reviewsResult = await _htmlReviewsExtractor.GetReviewsAsync(snapshotUrl, cancellationToken);

            if (reviewsResult.IsFailed &&
                reviewsResult.Errors.First().Message ==
                ExtractReviewsErrorCodes.HtmlExtractorInvalidCrunchyrollReviewsPage)
            {
                continue;
            }

            break;
        }

        if (reviewsResult.IsFailed)
        {
            return reviewsResult.ToResult();
        }
        
        await _addReviewsSession.AddReviewsForTitleIdAsync(request.TitleId, reviewsResult.Value);
        
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount / 2,
            CancellationToken = cancellationToken
        };
        
        await Parallel.ForEachAsync(reviewsResult.Value.Select(x => x.Author.AvatarUri), parallelOptions, async (avatarUri, token) =>
        {
            var avatarResult = await _avatarClient.GetAvatarStreamAsync(avatarUri, cancellationToken);

            if (avatarResult.IsFailed)
            {
                return;
            }

            var imageStream = avatarResult.Value;
            
            await _addReviewsSession.AddAvatarImageAsync(avatarUri, imageStream);
        });

        return Result.Ok();
    }

    private async ValueTask<Result<bool>> HasTitleAnyReviews(string titleId)
    {
        var result = await _getReviewsSession.GetReviewsForTitleIdAsync(titleId);

        if (result.IsFailed)
        {
            return result.ToResult();
        }
        
        return result.Value?.Any() ?? false;
    }
}