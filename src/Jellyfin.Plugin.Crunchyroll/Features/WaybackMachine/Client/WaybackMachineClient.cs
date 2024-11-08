using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Features.WaybackMachine.Client.Dto;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.WaybackMachine.Client;

public class WaybackMachineClient : IWaybackMachineClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WaybackMachineClient> _logger;

    public WaybackMachineClient(HttpClient httpClient, PluginConfiguration config, ILogger<WaybackMachineClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        _httpClient.BaseAddress =
            new Uri(config.ArchiveOrgUrl);
    }

    public async Task<Result<IReadOnlyList<SearchResponse>>> SearchAsync(string url, DateTime timestamp, CancellationToken cancellationToken = default)
    {
        var path = $"cdx/search/cdx?url={url}&output=json&limit=-5&to={timestamp.ToString("yyyyMMdd000000")}" +
                   $"&fastLatest=true&fl=timestamp,mimetype,statuscode&filter=statuscode:200";
        
        HttpResponseMessage response;
        try
        {
            response = await WaybackMachineRequestResiliencePipeline
                .Get(_logger)
                .ExecuteAsync(
                    async _ => await _httpClient.GetAsync(path, cancellationToken), 
                    cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error occurred while searching snapshot for url {Url} and timestamp {Timestamp}", 
                url, timestamp);
            return Result.Fail(WaybackMachineErrorCodes.WaybackMachineRequestFailed);
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("request for url {Url} with timestamp {Timestamp} was not successful. StatusCode: {StatusCode}", 
                url, timestamp, response.StatusCode);
            return Result.Fail(WaybackMachineErrorCodes.WaybackMachineRequestFailed);
        }

        string[][]? jsonArray;
        try
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            jsonArray = System.Text.Json.JsonSerializer.Deserialize<string[][]>(content);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "error on deserialization of json from wayback machine response");
            return Result.Fail(WaybackMachineErrorCodes.WaybackMachineGetAvailabilityFailed);
        }

        if (jsonArray is null)
        {
            _logger.LogError("null response from wayback machine");
            return Result.Fail(WaybackMachineErrorCodes.WaybackMachineGetAvailabilityFailed);
        }

        if (jsonArray.Length == 0)
        {
            return Result.Fail(WaybackMachineErrorCodes.WaybackMachineNotFound);
        }

        var searchResponses = jsonArray.Skip(1).Select(searchData => new SearchResponse
        {
            Timestamp = DateTime.ParseExact(searchData[0], "yyyyMMddHHmmss", CultureInfo.InvariantCulture),
            MimeType = searchData[1],
            Status = searchData[2],
        }).ToList();
        
        return searchResponses;
    }
}