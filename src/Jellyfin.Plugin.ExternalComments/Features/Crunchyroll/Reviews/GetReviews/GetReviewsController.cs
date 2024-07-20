using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.ExternalComments.Common;
using Jellyfin.Plugin.ExternalComments.Contracts;
using Jellyfin.Plugin.ExternalComments.Contracts.Comments;
using Jellyfin.Plugin.ExternalComments.Contracts.Reviews;
using Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Reviews.GetReviews;

[Route($"{Routes.Root}/crunchyroll/reviews")]
public class GetReviewsController : ControllerBase
{
    private readonly IMediator _mediator;

    public GetReviewsController(ILogger<GetReviewsController> logger)
    {
        if (ExternalCommentsPlugin.Instance is null)
        {
            logger.LogError("{ClassName} instance is not set, can not continue", nameof(ExternalCommentsPlugin));
            throw new ArgumentNullException(nameof(ExternalCommentsPlugin.Instance));
        }
        
        _mediator = ExternalCommentsPlugin.Instance.ServiceProvider.GetRequiredService<IMediator>();
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
            if (reviewsResult.Errors.First().Message == GetReviewsErrorCodes.ItemNotFound
                || reviewsResult.Errors.First().Message == GetReviewsErrorCodes.ItemHasNoProviderId)
            {
                return NotFound();
            }
            
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
        
        return Ok(reviewsResult.Value);
    }
}