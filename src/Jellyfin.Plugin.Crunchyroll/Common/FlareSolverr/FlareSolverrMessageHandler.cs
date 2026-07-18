using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Common.FlareSolverr;

public class FlareSolverrMessageHandler : DelegatingHandler
{
    private readonly IServiceScopeFactory _scopeFactory;
    private static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions()
    {
        PropertyNameCaseInsensitive = true
    };

    public FlareSolverrMessageHandler(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }
    
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var configuration = scope.ServiceProvider.GetRequiredService<PluginConfiguration>();
        
        if (!configuration.IsFlareSolverrEnabled)
        {
            return await base.SendAsync(request, cancellationToken);
        }
        
        ArgumentException.ThrowIfNullOrWhiteSpace(configuration.FlareSolverrUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(configuration.FlareSolverrMitmProxyUrl);
        
        var headerQuery = string.Join("&", request.Headers.Select(h => $"$$headers[]={UrlEncoder.Default.Encode(h.Key)}:{UrlEncoder.Default.Encode(h.Value.First())}"));
        var uri = request.RequestUri!.AbsoluteUri +
                  (string.IsNullOrWhiteSpace(request.RequestUri!.Query) && !string.IsNullOrWhiteSpace(headerQuery)
                      ? "?"
                      : !string.IsNullOrWhiteSpace(headerQuery) ? "&" : string.Empty) + headerQuery;
        
        JsonContent jsonContent;
        if (request.Method == HttpMethod.Post && request.Content is FormUrlEncodedContent)
        {
            jsonContent = JsonContent.Create(new PostDto
            {
                Cmd = "request.post",
                Url = new Uri(uri, UriKind.Absolute),
                Proxy = new DtoProxy(new Uri(configuration.FlareSolverrMitmProxyUrl, UriKind.Absolute)),
                PostData = await request.Content.ReadAsStringAsync(cancellationToken)
            });
        }
        else
        {
            jsonContent = JsonContent.Create(new Dto
            {
                Cmd = $"request.{request.Method.Method.ToLower()}",
                Url = new Uri(uri, UriKind.Absolute),
                Proxy = new DtoProxy(new Uri(configuration.FlareSolverrMitmProxyUrl, UriKind.Absolute)),
            });
        }
        
        var flareSolverrRequest = new HttpRequestMessage
        {
            RequestUri = new Uri(configuration.FlareSolverrUrl, UriKind.Absolute),
            Method = HttpMethod.Post,
            Content = jsonContent
        };
        
        var flareSolverrResponse = await base.SendAsync(flareSolverrRequest, cancellationToken);
        var response = new HttpResponseMessage
        {
            StatusCode = flareSolverrResponse.StatusCode,
            Content = flareSolverrResponse.Content,
            Version = flareSolverrResponse.Version,
            ReasonPhrase = flareSolverrResponse.ReasonPhrase,
            RequestMessage = flareSolverrResponse.RequestMessage
        };

        foreach (var header in flareSolverrResponse.Headers)
        {
            response.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        var flareSolverrResponseContent = await flareSolverrResponse.Content.ReadAsStringAsync(cancellationToken);
        var flareSolverrResponseDto = JsonSerializer.Deserialize<FlareSolverrResponse>(flareSolverrResponseContent, JsonSerializerOptions);

        var doc = new HtmlDocument();
        doc.LoadHtml(flareSolverrResponseDto!.Solution.Response);

        // preNode can be null
        var preNode = doc.DocumentNode.SelectSingleNode("//pre");

        // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
        response.Content = new StringContent(preNode?.InnerText ?? flareSolverrResponseDto.Solution.Response);
        return response;
    }
}