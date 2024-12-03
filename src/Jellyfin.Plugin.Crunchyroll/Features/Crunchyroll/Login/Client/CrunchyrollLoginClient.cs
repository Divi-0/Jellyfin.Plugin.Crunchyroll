using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Domain.Constants;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Login.Client;

public class CrunchyrollLoginClient : ICrunchyrollLoginClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CrunchyrollLoginClient> _logger;

    private readonly JsonSerializerOptions _jsonSerializerOptions;
    
    public CrunchyrollLoginClient(HttpClient httpClient, PluginConfiguration pluginConfiguration, ILogger<CrunchyrollLoginClient> logger,
        ICrunchyrollSessionRepository crunchyrollSessionRepository)
    {
        _httpClient = httpClient;
        _logger = logger;

        _httpClient.BaseAddress =
            new Uri(pluginConfiguration.CrunchyrollUrl);

        _jsonSerializerOptions = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true
        };
    }
    
    /// <summary>
    /// Calling Crunchyroll index page to avoid bot detection
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<Result> InitialCrunchyrollCallAsync(CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync(string.Empty, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Crunchyroll start page not reachable");
            return Result.Fail(ErrorCodes.CrunchyrollRequestFailed);
        }

        return Result.Ok();
    }
    
    public async Task<Result<CrunchyrollAuthResponse>> LoginAnonymousAsync(CancellationToken cancellationToken)
    {
        var initialCrunchyrollCallResult = await InitialCrunchyrollCallAsync(cancellationToken);
        
        if (initialCrunchyrollCallResult.IsFailed)
        {
            return Result.Fail(initialCrunchyrollCallResult.Errors);
        }
        
        var requestMessage = new HttpRequestMessage()
        {
            RequestUri = new Uri("auth/v1/token", UriKind.Relative),
            Method = HttpMethod.Post,
            Headers =
            {
                { HeaderNames.Accept, "application/json, text/plain, */*" },
                { HeaderNames.Authorization, "Basic Y3Jfd2ViOg==" }
            },
            Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_id")
            })
        };
        
        var response = await _httpClient.SendAsync(requestMessage, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Crunchyroll anonymous auth was not successful");
            return Result.Fail(ErrorCodes.CrunchyrollRequestFailed);
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var authResponse = JsonSerializer.Deserialize<CrunchyrollAuthResponse>(content, _jsonSerializerOptions);

        if (authResponse is null)
        {
            _logger.LogError("crunchyroll auth response is null");
            return Result.Fail(ErrorCodes.CrunchyrollLoginFailed);
        }

        return authResponse;
    }
}