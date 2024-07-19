using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.ExternalComments.Configuration;
using Jellyfin.Plugin.ExternalComments.Domain.Constants;
using Jellyfin.Plugin.ExternalComments.Features.WaybackMachine.Client.Dto;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ExternalComments.Features.WaybackMachine.Client;

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

    public async Task<Result<AvailabilityResponse>> GetAvailabilityAsync(string url, DateTime timestamp, CancellationToken cancellationToken = default)
    {
        var path = $"wayback/available?url={url}&timestamp={timestamp.ToString("yyyyMMddhhmmss")}";
        
        var response = await _httpClient.GetAsync(path, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("request for url {Url} with timestamp {Timestamp} was not successful. StatusCode: {StatusCode}", 
                url, timestamp, response.StatusCode);
            return Result.Fail(ErrorCodes.WaybackMachineRequestFailed);
        }

        AvailabilityResponse? availabilityResponse;
        try
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            availabilityResponse = System.Text.Json.JsonSerializer.Deserialize<AvailabilityResponse>(content);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "error on deserialization of json from wayback machine response");
            return Result.Fail(ErrorCodes.WaybackMachineGetAvailabilityFailed);
        }

        if (availabilityResponse is null)
        {
            _logger.LogError("null response from wayback machine");
            return Result.Fail(ErrorCodes.WaybackMachineGetAvailabilityFailed);
        }
        
        return availabilityResponse;
    }
}