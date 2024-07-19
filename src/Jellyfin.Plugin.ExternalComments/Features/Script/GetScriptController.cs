using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ExternalComments.Features.Script;

[ApiController]
[Route("api/externalcomments/script")]
public class GetScriptController : ControllerBase
{
    private readonly ILogger<GetScriptController> _logger;

    public GetScriptController()
    {
    }
    
    [HttpGet]
    [Produces("application/javascript")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult> GetScript(CancellationToken cancellationToken)
    {
        const string jsFileName = "externalComments.js";
        
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"{typeof(GetScriptController).Namespace}.{jsFileName}";

        try
        {
            var stream = assembly.GetManifestResourceStream(resourceName);

            if (stream is null)
            {
                _logger.LogError("resource {FileName} was not found", jsFileName);
                return Task.FromResult<ActionResult>(NotFound());
            }
            
            return Task.FromResult<ActionResult>(File(stream, "application/javascript"));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "could not get stream of file {FileName}", jsFileName);
            return Task.FromResult<ActionResult>(StatusCode(StatusCodes.Status500InternalServerError));
        }
    }
}