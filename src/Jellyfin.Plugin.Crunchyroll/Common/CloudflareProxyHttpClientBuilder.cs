using FlareSolverrSharp;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Common;

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