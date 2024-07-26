using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Reviews.ExtractReviews;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Avatar.Client;

public class AvatarClient : IAvatarClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AvatarClient> _logger;

    public AvatarClient(HttpClient httpClient, ILogger<AvatarClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        _httpClient.Timeout = TimeSpan.FromSeconds(180);
    }
    
    public async Task<Result<Stream>> GetAvatarStreamAsync(string uri, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            var request = new HttpRequestMessage()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(uri),
                Headers =
                {
                    { HeaderNames.Accept, "image/png,image/jpeg" }
                }
            };
            
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "request failed for Uri {Uri}", uri);
            return Result.Fail(GetAvatarImageErrorCodes.GetAvatarImageRequestFailed);
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("request failed for Uri {Uri} with statuscode {StatusCode}", uri, response.StatusCode);
            return Result.Fail(GetAvatarImageErrorCodes.GetAvatarImageRequestFailed);
        }

        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }
}