using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.ExternalComments.Common;
using Jellyfin.Plugin.ExternalComments.Contracts;
using Jellyfin.Plugin.ExternalComments.Contracts.Comments;
using Jellyfin.Plugin.ExternalComments.Domain.Constants;
using Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.GetComments;

[Route($"{Routes.Root}/crunchyroll/comments")]
public class CommentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CommentsController(ILogger<CommentsController> logger)
    {
        if (ExternalCommentsPlugin.Instance is null)
        {
            logger.LogError("{ClassName} instance is not set, can not continue", nameof(ExternalCommentsPlugin));
            throw new ArgumentNullException(nameof(ExternalCommentsPlugin.Instance));
        }
        
        _mediator = ExternalCommentsPlugin.Instance.ServiceProvider.GetRequiredService<IMediator>();
    }
    
    [HttpGet("{title}")]
    [Produces(typeof(CommentsResponse))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CommentsResponse>> GetCommentsAsync(string title, [FromQuery] PageRequest request, CancellationToken cancellationToken = default)
    {
        var commentsResult = await _mediator.Send(new GetCommentsQuery(title, request.PageNumber, request.PageSize), cancellationToken);
        
        if (commentsResult.IsFailed)
        {
            if (commentsResult.Errors.First().Message == ErrorCodes.CrunchyrollTitleIdNotFound)
            {
                return NotFound();
            }
            
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
        
        return Ok(commentsResult.Value);
    }
}