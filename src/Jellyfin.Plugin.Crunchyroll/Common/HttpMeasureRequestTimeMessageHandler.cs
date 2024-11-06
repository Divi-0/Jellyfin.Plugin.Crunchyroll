using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Common;

public class HttpMeasureRequestTimeMessageHandler : DelegatingHandler
{
    private readonly ILogger<HttpMeasureRequestTimeMessageHandler> _logger;

    public HttpMeasureRequestTimeMessageHandler(ILogger<HttpMeasureRequestTimeMessageHandler> logger)
    {
        _logger = logger;
    }
    
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var timestamp = Stopwatch.GetTimestamp();
        
        try
        {
            return await base.SendAsync(request, cancellationToken);
        }
        finally
        {
            var elapsedTime = Stopwatch.GetElapsedTime(timestamp);
            _logger.LogDebug("Request {Method} {Uri} took {ElapsedTime}ms", 
                request.Method.Method, 
                request.RequestUri, 
                elapsedTime.ToString("g"));
        }
    }
}

public static class HttpMeasureRequestMessageHandlerExtensions
{
    public static IServiceCollection AddHttpMeasureRequestTimeMessageHandler(this IServiceCollection services)
    {
        services.AddScoped<HttpMeasureRequestTimeMessageHandler>();
        
        services.ConfigureAll<HttpClientFactoryOptions>(options =>
        {
            options.HttpMessageHandlerBuilderActions.Add(builder =>
            {
                builder.AdditionalHandlers.Add(builder.Services
                    .GetRequiredService<HttpMeasureRequestTimeMessageHandler>());
            });
        });

        return services;
    }
}