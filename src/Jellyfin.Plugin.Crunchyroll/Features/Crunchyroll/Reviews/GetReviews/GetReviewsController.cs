using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Contracts;
using Jellyfin.Plugin.Crunchyroll.Contracts.Comments;
using Jellyfin.Plugin.Crunchyroll.Contracts.Reviews;
using Jellyfin.Plugin.Crunchyroll.Domain.Constants;
using Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews.GetReviews;

[Route($"{Routes.Root}/crunchyroll/reviews")]
public class GetReviewsController : ControllerBase
{
    private readonly IMediator _mediator;

    public GetReviewsController(ILogger<GetReviewsController> logger)
    {
        if (CrunchyrollPlugin.Instance is null)
        {
            logger.LogError("{ClassName} instance is not set, can not continue", nameof(CrunchyrollPlugin));
            throw new ArgumentNullException(nameof(CrunchyrollPlugin.Instance));
        }
        
        var scope = CrunchyrollPlugin.Instance.ServiceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();
        _mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
    }
    
    [HttpGet("{itemId}")]
    [Produces(typeof(CommentsResponse))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ReviewsResponse>> GetReviewsAsync(string itemId, [FromQuery] PageRequest request, CancellationToken cancellationToken = default)
    {
        var reviewsResult = await _mediator.Send(new GetReviewsQuery(itemId, request.PageNumber, request.PageSize), cancellationToken);
        
        if (reviewsResult.IsFailed)
        {
            switch (reviewsResult.Errors.First().Message)
            {
                case GetReviewsErrorCodes.ItemNotFound:
                case GetReviewsErrorCodes.ItemHasNoProviderId:
                case ErrorCodes.FeatureDisabled:
                    return NotFound();
                default:
                    return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        if (reviewsResult.Value is null)
        {
            return NotFound();
        }
        
        return Ok(reviewsResult.Value);
    }
}