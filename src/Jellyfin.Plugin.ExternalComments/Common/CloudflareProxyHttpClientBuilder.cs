using FlareSolverrSharp;
using Jellyfin.Plugin.ExternalComments.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.ExternalComments.Common;

public static class CloudflareProxyHttpClientBuilder
{
    public static IHttpClientBuilder AddFlareSolverrProxy(this IHttpClientBuilder httpClientBuilder, PluginConfiguration pluginConfiguration)
    {
        httpClientBuilder.ConfigurePrimaryHttpMessageHandler(() => new ClearanceHandler(pluginConfiguration.FlareSolverrUrl)
        {
            MaxTimeout = pluginConfiguration.FlareSolverrTimeout,
            ProxyUrl = pluginConfiguration.FlareSolverrProxyUrl,
            ProxyUsername = pluginConfiguration.FlareSolverrProxyUsername,
            ProxyPassword = pluginConfiguration.FlareSolverrProxyPassword,
        });
        
        return httpClientBuilder;
    }
}