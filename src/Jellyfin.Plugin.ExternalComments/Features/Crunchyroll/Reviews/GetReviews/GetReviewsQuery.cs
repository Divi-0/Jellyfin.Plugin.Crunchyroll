using System;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using J2N.Collections.Generic;
using Jellyfin.Plugin.ExternalComments.Common;
using Jellyfin.Plugin.ExternalComments.Configuration;
using Jellyfin.Plugin.ExternalComments.Contracts.Reviews;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Avatar;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Login;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Reviews.GetReviews.Client;
using MediaBrowser.Controller.Library;
using Mediator;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Reviews.GetReviews;

public record GetReviewsQuery(string Id, int PageNumber, int PageSize) : IRequest<Result<ReviewsResponse>>;

public class GetReviewsQueryHandler : IRequestHandler<GetReviewsQuery, Result<ReviewsResponse>>
{
    private readonly ICrunchyrollGetReviewsClient _client;
    private readonly ILibraryManager _libraryManager;
    private readonly PluginConfiguration _config;
    private readonly IGetReviewsSession _getReviewsSession;
    private readonly ILoginService _loginService;

    public GetReviewsQueryHandler(ICrunchyrollGetReviewsClient client, ILibraryManager libraryManager,
        PluginConfiguration config, IGetReviewsSession getReviewsSession, ILoginService loginService)
    {
        _client = client;
        _libraryManager = libraryManager;
        _config = config;
        _getReviewsSession = getReviewsSession;
        _loginService = loginService;
    }
    
    public async ValueTask<Result<ReviewsResponse>> Handle(GetReviewsQuery request, CancellationToken cancellationToken)
    {
        var item = _libraryManager.RetrieveItem(Guid.Parse(request.Id));

        if (item is null)
        {
            return Result.Fail(GetReviewsErrorCodes.ItemNotFound);
        }

        if (!item.ProviderIds.TryGetValue(CrunchyrollExternalKeys.Id, out string? cruncyrollId) 
            || string.IsNullOrWhiteSpace(cruncyrollId))
        {
            return Result.Fail(GetReviewsErrorCodes.ItemHasNoProviderId);
        }

        var reviewsResult = await GetReviewsAsync(cruncyrollId, request.PageNumber, request.PageSize, cancellationToken);

        return reviewsResult;
    }

    private async ValueTask<Result<ReviewsResponse>> GetReviewsAsync(string titleId, int pageNumber, int pageSize, 
        CancellationToken cancellationToken)
    {
        if (_config.IsWaybackMachineEnabled)
        {
            var reviewsResult = await _getReviewsSession.GetReviewsForTitleIdAsync(titleId);

            if (reviewsResult.IsFailed)
            {
                return reviewsResult.ToResult();
            }

            var reviews = reviewsResult.Value ?? Array.Empty<ReviewItem>().ToList();

            foreach (var review in reviews)
            {
                review.Author.AvatarUri = $"/{Routes.Root}/{AvatarConstants.GetAvatarSubRoute}/{UrlEncoder.Default.Encode(review.Author.AvatarUri)}";
            }
            
            return Result.Ok(new ReviewsResponse { Reviews = reviews });
        }
        else
        {
            var loginResult = await _loginService.LoginAnonymously(cancellationToken);
            return loginResult.IsFailed ? 
                loginResult :
                await _client.GetReviewsAsync(titleId, pageNumber, pageSize, cancellationToken);
        }
    }
}