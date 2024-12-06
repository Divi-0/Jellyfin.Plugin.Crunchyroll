using System;
using System.Globalization;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using J2N.Collections.Generic;
using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Contracts.Reviews;
using Jellyfin.Plugin.Crunchyroll.Domain.Constants;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Avatar;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Login;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews.GetReviews.Client;
using MediaBrowser.Controller.Library;
using Mediator;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews.GetReviews;

public record GetReviewsQuery(string Id, int PageNumber, int PageSize) : IRequest<Result<ReviewsResponse?>>;

public class GetReviewsQueryHandler : IRequestHandler<GetReviewsQuery, Result<ReviewsResponse?>>
{
    private readonly ICrunchyrollGetReviewsClient _client;
    private readonly ILibraryManager _libraryManager;
    private readonly PluginConfiguration _config;
    private readonly IGetReviewsRepository _repository;
    private readonly ILoginService _loginService;

    public GetReviewsQueryHandler(ICrunchyrollGetReviewsClient client, ILibraryManager libraryManager,
        PluginConfiguration config, IGetReviewsRepository repository, ILoginService loginService)
    {
        _client = client;
        _libraryManager = libraryManager;
        _config = config;
        _repository = repository;
        _loginService = loginService;
    }
    
    public async ValueTask<Result<ReviewsResponse?>> Handle(GetReviewsQuery request, CancellationToken cancellationToken)
    {
        var item = _libraryManager.RetrieveItem(Guid.Parse(request.Id));

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (item is null)
        {
            return Result.Fail(GetReviewsErrorCodes.ItemNotFound);
        }

        if (!item.ProviderIds.TryGetValue(CrunchyrollExternalKeys.SeriesId, out string? cruncyrollId) 
            || string.IsNullOrWhiteSpace(cruncyrollId))
        {
            return Result.Fail(GetReviewsErrorCodes.ItemHasNoProviderId);
        }

        var reviewsResult = await GetReviewsAsync(cruncyrollId, request.PageNumber, request.PageSize, 
            item.GetPreferredMetadataCultureInfo(), cancellationToken);

        return reviewsResult;
    }

    private async ValueTask<Result<ReviewsResponse?>> GetReviewsAsync(string titleId, int pageNumber, int pageSize, 
        CultureInfo language, CancellationToken cancellationToken)
    {
        if (_config.IsWaybackMachineEnabled)
        {
            var reviewsResult = await _repository.GetReviewsForTitleIdAsync(titleId, language, cancellationToken);

            if (reviewsResult.IsFailed)
            {
                return reviewsResult.ToResult();
            }

            var reviews = reviewsResult.Value;

            if (reviews is null)
            {
                return Result.Ok<ReviewsResponse?>(null);
            }

            foreach (var review in reviews)
            {
                review.Author.AvatarUri = $"/{Routes.Root}/{AvatarConstants.GetAvatarSubRoute}/{UrlEncoder.Default.Encode(review.Author.AvatarUri)}";
            }
            
            return Result.Ok(new ReviewsResponse { Reviews = reviews })!;
        }
        
        //temp disabled, to not show reviews section with real api
        return Result.Fail<ReviewsResponse?>(ErrorCodes.FeatureDisabled);

        // var loginResult = await _loginService.LoginAnonymouslyAsync(cancellationToken);
        // return (loginResult.IsFailed ? 
        //     loginResult :
        //     await _client.GetReviewsAsync(titleId, pageNumber, pageSize, language, cancellationToken))!;
        
    }
}