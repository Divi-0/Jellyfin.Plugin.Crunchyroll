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
using Jellyfin.Plugin.Crunchyroll.Contracts.Reviews;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Avatar.AddAvatar;
using Jellyfin.Plugin.Crunchyroll.Features.WaybackMachine.Client;
using Jellyfin.Plugin.Crunchyroll.Features.WaybackMachine.Helper;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews.ExtractReviews;

public record ExtractReviewsCommand : IRequest<Result>
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
    private readonly IAddAvatarService _addAvatarService;

    private static readonly DateTime DateWhenReviewsWereDeleted = new DateTime(2024, 07, 10);
    
    public ExtractReviewsCommandHandler(IWaybackMachineClient waybackMachineClient, PluginConfiguration config, 
        IHtmlReviewsExtractor htmlReviewsExtractor, IAddReviewsSession addReviewsSession, IGetReviewsSession getReviewsSession,
        ILogger<ExtractReviewsCommandHandler> logger, IAddAvatarService addAvatarService)
    {
        _waybackMachineClient = waybackMachineClient;
        _config = config;
        _htmlReviewsExtractor = htmlReviewsExtractor;
        _addReviewsSession = addReviewsSession;
        _getReviewsSession = getReviewsSession;
        _logger = logger;
        _addAvatarService = addAvatarService;
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
        
        var twoLetterIsoLanguageName = new CultureInfo(_config.CrunchyrollLanguage).TwoLetterISOLanguageName;
        var crunchyrollUrl = Path.Combine(
                _config.CrunchyrollUrl.Contains("www") ? _config.CrunchyrollUrl.Split("www.")[1] : _config.CrunchyrollUrl.Split("//")[1], 
                twoLetterIsoLanguageName == "en" ? string.Empty : twoLetterIsoLanguageName,
            "series",
            request.TitleId,
            request.SlugTitle)
            .Replace('\\', '/');
        
        var searchResult = await _waybackMachineClient.SearchAsync(HttpUtility.UrlEncode(crunchyrollUrl), DateWhenReviewsWereDeleted, cancellationToken);

        if (searchResult.IsFailed)
        {
            return searchResult.ToResult();
        }

        //if invalid html error: retry with next timestamp; from last to first
        var reviewsResult = Result.Ok<IReadOnlyList<ReviewItem>>([]);
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
                (reviewsResult.Errors.First().Message == ExtractReviewsErrorCodes.HtmlExtractorInvalidCrunchyrollReviewsPage 
                 || reviewsResult.Errors.First().Message == ExtractReviewsErrorCodes.HtmlUrlRequestFailed))
            {
                continue;
            }

            break;
        }

        if (reviewsResult.IsFailed)
        {
            return reviewsResult.ToResult();
        }
        
        await StoreAvatarImagesAndRemoveWebArchivePrefixFromImageUrl(reviewsResult.Value, cancellationToken);
        
        await _addReviewsSession.AddReviewsForTitleIdAsync(request.TitleId, reviewsResult.Value);

        return Result.Ok();
    }

    private async Task StoreAvatarImagesAndRemoveWebArchivePrefixFromImageUrl(IReadOnlyList<ReviewItem> reviews, 
        CancellationToken cancellationToken)
    {
        var parallelOptions = new ParallelOptions
        {
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(reviews.Select(x => x.Author), parallelOptions, async (author, token) =>
        {
            var addAvatarResult = await _addAvatarService.AddAvatarIfNotExists(author.AvatarUri, token);
            if (addAvatarResult.IsSuccess)
            {
                author.AvatarUri = addAvatarResult.Value;
            }
        });
    }

    private async ValueTask<Result<bool>> HasTitleAnyReviews(string titleId)
    {
        var result = await _getReviewsSession.GetReviewsForTitleIdAsync(titleId);

        if (result.IsFailed)
        {
            return result.ToResult();
        }
        
        return result.Value is not null;
    }
}