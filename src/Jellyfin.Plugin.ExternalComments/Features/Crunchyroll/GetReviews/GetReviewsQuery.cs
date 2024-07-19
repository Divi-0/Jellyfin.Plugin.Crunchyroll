using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.ExternalComments.Configuration;
using Jellyfin.Plugin.ExternalComments.Contracts.Reviews;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.GetReviews.Client;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using Mediator;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.GetReviews;

public record GetReviewsQuery(string Id, int PageNumber, int PageSize) : ICrunchyrollCommand, IRequest<Result<ReviewsResponse>>;

public class GetReviewsQueryHandler : IRequestHandler<GetReviewsQuery, Result<ReviewsResponse>>
{
    private readonly ICrunchyrollGetReviewsClient _client;
    private readonly ILibraryManager _libraryManager;
    private readonly PluginConfiguration _config;
    private readonly IGetReviewsSession _getReviewsSession;

    public GetReviewsQueryHandler(ICrunchyrollGetReviewsClient client, ILibraryManager libraryManager,
        PluginConfiguration config, IGetReviewsSession getReviewsSession)
    {
        _client = client;
        _libraryManager = libraryManager;
        _config = config;
        _getReviewsSession = getReviewsSession;
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
            return reviewsResult.IsFailed ? 
                reviewsResult.ToResult() : 
                Result.Ok(new ReviewsResponse { Reviews = reviewsResult.Value });
        }
        else
        {
            return await _client.GetReviewsAsync(titleId, pageNumber, pageSize, cancellationToken);
        }
    }
}