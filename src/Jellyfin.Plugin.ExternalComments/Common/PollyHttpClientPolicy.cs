using System;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

namespace Jellyfin.Plugin.ExternalComments.Common;

public static class PollyHttpClientPolicy
{
    public static IHttpClientBuilder AddPollyHttpClientDefaultPolicy(this IHttpClientBuilder httpClientBuilder)
    {
        httpClientBuilder.AddPolicyHandler(HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, _ => TimeSpan.FromSeconds(5)));
        
        return httpClientBuilder;
    }
}