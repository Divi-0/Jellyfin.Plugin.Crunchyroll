using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.ExternalComments.Configuration;
using Jellyfin.Plugin.ExternalComments.Contracts.Comments;
using Jellyfin.Plugin.ExternalComments.Domain.Constants;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.GetComments.Mappings;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.GetComments.Client;

public class CrunchyrollGetCommentsClient : ICrunchyrollGetCommentsClient
{
    private readonly HttpClient _httpClient;
    private readonly PluginConfiguration _pluginConfiguration;
    private readonly ILogger<CrunchyrollGetCommentsClient> _logger;
    private readonly ICrunchyrollSessionRepository _crunchyrollSessionRepository;

    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public CrunchyrollGetCommentsClient(HttpClient httpClient, PluginConfiguration pluginConfiguration, ILogger<CrunchyrollGetCommentsClient> logger,
        ICrunchyrollSessionRepository crunchyrollSessionRepository)
    {
        _httpClient = httpClient;
        _pluginConfiguration = pluginConfiguration;
        _logger = logger;
        _crunchyrollSessionRepository = crunchyrollSessionRepository;

        _httpClient.BaseAddress =
            new Uri(pluginConfiguration.CrunchyrollUrl);

        _jsonSerializerOptions = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true
        };
    }
    
    public async Task<Result<CommentsResponse>> GetCommentsAsync(string titleId, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var locacle = new CultureInfo(_pluginConfiguration.CrunchyrollLanguage).Name;
        var path =
            $"talkbox/guestbooks/{titleId}/comments?page={pageNumber}&page_size={pageSize}&order=desc&sort=popular&locale={locacle}";
        
        var bearerToken = await _crunchyrollSessionRepository.GetAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(bearerToken))
        {
            return Result.Fail(ErrorCodes.CrunchyrollSessionMissing);
        }

        var requestMessage = new HttpRequestMessage()
        {
            RequestUri = new Uri(path, UriKind.Relative),
            Method = HttpMethod.Get,
            Headers = { { HeaderNames.Authorization, $"Bearer {bearerToken}" } }
        };
        
        var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Crunchyroll comments request failed");
            return Result.Fail(ErrorCodes.CrunchyrollGetCommentsFailed);
        }

        var crunchyrollCommentsResponse = await response.Content.ReadFromJsonAsync<CrunchyrollCommentsResponse>(_jsonSerializerOptions, 
            cancellationToken: cancellationToken);

        if (crunchyrollCommentsResponse is null)
        {
            _logger.LogError("Failed to deserialize crunchyroll comments response");
            return Result.Fail(ErrorCodes.CrunchyrollGetCommentsFailed);
        }

        return crunchyrollCommentsResponse.ToCommentResponse();
    }
}