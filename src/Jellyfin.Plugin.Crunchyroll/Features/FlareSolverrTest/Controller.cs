using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Crunchyroll.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.FlareSolverrTest;

[ApiController]
[Route($"{Routes.Root}/flaresolverr/test")]
public class Controller : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> FlareSolverrTest(CancellationToken cancellationToken)
    {
        await using var scope = CrunchyrollPlugin.Instance!.ServiceProvider.CreateAsyncScope();
        var client = scope.ServiceProvider.GetRequiredService<ICrunchyrollClient>();
        var result = await client.CallCrunchyrollHomePageAsync(cancellationToken);
        return result.IsSuccess 
            ? Ok()
            : StatusCode(StatusCodes.Status500InternalServerError);
    }
}