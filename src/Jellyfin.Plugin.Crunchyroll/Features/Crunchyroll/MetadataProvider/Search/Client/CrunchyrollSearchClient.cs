using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Common.Crunchyroll.SearchDto;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Domain.Constants;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Search.Client;

public class CrunchyrollSearchClient : ICrunchyrollSearchClient
{
    private const int MaxResults = 10;

    private readonly HttpClient _httpClient;
    private readonly ILogger<CrunchyrollSearchClient> _logger;
    private readonly ICrunchyrollSessionRepository _crunchyrollSessionRepository;

    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public CrunchyrollSearchClient(HttpClient httpClient, PluginConfiguration pluginConfiguration,
        ILogger<CrunchyrollSearchClient> logger, ICrunchyrollSessionRepository crunchyrollSessionRepository)
    {
        _httpClient = httpClient;
        _logger = logger;
        _crunchyrollSessionRepository = crunchyrollSessionRepository;

        _httpClient.BaseAddress =
            new Uri(pluginConfiguration.CrunchyrollUrl);

        _jsonSerializerOptions = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<Result<IReadOnlyList<CrunchyrollSearchDataItem>>> SearchAsync(string query, string type,
        CultureInfo language, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Searching crunchyroll for {Type} with query {Query}", type, query);

        var urlEncodedQuery = UrlEncoder.Default.Encode(query);
        var path =
            $"content/v2/discover/search?q={urlEncodedQuery}&n={MaxResults}&type={type}&ratings=true&locale={language.Name}";

        var bearerToken = await _crunchyrollSessionRepository.GetAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(bearerToken))
        {
            _logger.LogError("no session found");
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
            _logger.LogError("Crunchyroll search failed with status code {StatusCode}", response.StatusCode);
            return Result.Fail(ErrorCodes.CrunchyrollSearchFailed);
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var crunchyrollSearchResponse = JsonSerializer.Deserialize<CrunchyrollSearchResponse>(content, _jsonSerializerOptions);

        if (crunchyrollSearchResponse is null)
        {
            _logger.LogError("crunchyroll search response for query {Query} was empty", query);
            return Result.Fail(ErrorCodes.CrunchyrollSearchContentIncompatible);
        }

        return crunchyrollSearchResponse.Data
            .SelectMany(x => x.Items)
            .ToList();
    }
}