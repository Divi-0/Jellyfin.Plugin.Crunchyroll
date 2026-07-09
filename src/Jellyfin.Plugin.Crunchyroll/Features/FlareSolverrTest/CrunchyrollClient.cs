using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Domain.Constants;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.FlareSolverrTest;

public interface ICrunchyrollClient
{
    Task<Result> CallCrunchyrollHomePageAsync(CancellationToken cancellationToken);
}

public class CrunchyrollClient : ICrunchyrollClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CrunchyrollClient> _logger;

    public CrunchyrollClient(HttpClient httpClient, PluginConfiguration pluginConfiguration, ILogger<CrunchyrollClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        _httpClient.BaseAddress =
            new Uri(pluginConfiguration.CrunchyrollUrl);
        
    }
    
    public async Task<Result> CallCrunchyrollHomePageAsync(CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync(string.Empty, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Crunchyroll start page not reachable");
            return Result.Fail(ErrorCodes.CrunchyrollRequestFailed);
        }

        return Result.Ok();
    }
}