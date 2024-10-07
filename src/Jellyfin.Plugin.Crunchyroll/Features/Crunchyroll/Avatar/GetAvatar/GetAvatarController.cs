using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Jellyfin.Plugin.Crunchyroll.Common;
using Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Avatar.GetAvatar;

[Route($"{Routes.Root}/{AvatarConstants.GetAvatarSubRoute}")]
public class GetAvatarController : ControllerBase
{
    private readonly IMediator _mediator;

    public GetAvatarController(ILogger<GetAvatarController> logger)
    {
        if (CrunchyrollPlugin.Instance is null)
        {
            logger.LogError("{ClassName} instance is not set, can not continue", nameof(CrunchyrollPlugin));
            throw new ArgumentNullException(nameof(CrunchyrollPlugin.Instance));
        }
        
        _mediator = CrunchyrollPlugin.Instance.ServiceProvider.GetRequiredService<IMediator>();
    }
    
    [HttpGet("{url}")]
    [Produces("image/png")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Stream>> GetAvatarAsync(string url, CancellationToken cancellationToken = default)
    {
        var avatarResult = await _mediator.Send(new AvatarQuery{Url = HttpUtility.UrlDecode(url)}, cancellationToken);
        
        if (avatarResult.IsFailed)
        {
            return StatusCode(StatusCodes.Status500InternalServerError);
        }

        if (avatarResult.Value is null)
        {
            return NotFound();
        }
        
        return File(avatarResult.Value, "image/png");
    }
}