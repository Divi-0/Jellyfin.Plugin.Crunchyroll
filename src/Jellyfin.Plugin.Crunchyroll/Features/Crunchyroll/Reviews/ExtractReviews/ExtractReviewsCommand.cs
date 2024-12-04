using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using FluentResults;
using FluentResults.Extensions;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Contracts.Reviews;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Avatar.AddAvatar;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews.Entities;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews.GetReviews;
using Jellyfin.Plugin.Crunchyroll.Features.WaybackMachine.Client;
using Mediator;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews.ExtractReviews;

public record ExtractReviewsCommand : IRequest<Result>
{
    public required string TitleId { get; init; }
    public required string SlugTitle { get; init; }
    public required CultureInfo Language { get; init; }
}

public class ExtractReviewsCommandHandler : IRequestHandler<ExtractReviewsCommand, Result>
{
    private readonly IWaybackMachineClient _waybackMachineClient;
    private readonly PluginConfiguration _config;
    private readonly IHtmlReviewsExtractor _htmlReviewsExtractor;
    private readonly IAddReviewsRepistory _addReviewsRepistory;
    private readonly IGetReviewsRepository _getReviewsRepository;
    private readonly ILogger<ExtractReviewsCommandHandler> _logger;
    private readonly IAddAvatarService _addAvatarService;

    private static readonly DateTime DateWhenReviewsWereDeleted = new DateTime(2024, 07, 10);
    
    public ExtractReviewsCommandHandler(IWaybackMachineClient waybackMachineClient, PluginConfiguration config, 
        IHtmlReviewsExtractor htmlReviewsExtractor, IAddReviewsRepistory addReviewsRepistory, IGetReviewsRepository getReviewsRepository,
        ILogger<ExtractReviewsCommandHandler> logger, IAddAvatarService addAvatarService)
    {
        _waybackMachineClient = waybackMachineClient;
        _config = config;
        _htmlReviewsExtractor = htmlReviewsExtractor;
        _addReviewsRepistory = addReviewsRepistory;
        _getReviewsRepository = getReviewsRepository;
        _logger = logger;
        _addAvatarService = addAvatarService;
    }
    
    public async ValueTask<Result> Handle(ExtractReviewsCommand request, CancellationToken cancellationToken)
    {
        var hasReviewsResult = await HasTitleAnyReviews(request.TitleId, request.Language, cancellationToken);

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
        
        var twoLetterIsoLanguageName = request.Language.TwoLetterISOLanguageName;
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

            reviewsResult = await _htmlReviewsExtractor.GetReviewsAsync(snapshotUrl, request.Language, cancellationToken);

            if (reviewsResult.IsFailed)
            {
                continue;
            }

            break;
        }

        var reviews = reviewsResult.ValueOrDefault ?? [];
        
        await StoreAvatarImagesAndRemoveWebArchivePrefixFromImageUrl(reviews, cancellationToken);

        var dbResult = await _addReviewsRepistory.AddReviewsForTitleIdAsync(new TitleReviews
            {
                CrunchyrollSeriesId = request.TitleId,
                Reviews = JsonSerializer.Serialize(reviews),
                Language = request.Language.Name
            }, cancellationToken)
            .Bind(async () => await _addReviewsRepistory.SaveChangesAsync(cancellationToken));

        return dbResult.IsSuccess
            ? Result.Ok() 
            : dbResult;
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

    private async ValueTask<Result<bool>> HasTitleAnyReviews(string titleId, CultureInfo language, CancellationToken cancellationToken)
    {
        var result = await _getReviewsRepository.GetReviewsForTitleIdAsync(titleId, language, cancellationToken);

        if (result.IsFailed)
        {
            return result.ToResult();
        }

        if (result.Value is null)
        {
            return false;
        }
        
        return result.Value.Count != 0;
    }
}