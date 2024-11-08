using System;
using System.Threading.Tasks;
using Jellyfin.Plugin.Crunchyroll.Features.WaybackMachine.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;

namespace Jellyfin.Plugin.Crunchyroll.Features.WaybackMachine;

public static class WaybackMachineServiceExtensions
{
    public static IServiceCollection AddWaybackMachine(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddHttpClient<IWaybackMachineClient, WaybackMachineClient>()
            .AddPollyWaybackMachineHttpClientPolicy();
        
        return serviceCollection;
    }
    
    private static IHttpClientBuilder AddPollyWaybackMachineHttpClientPolicy(this IHttpClientBuilder httpClientBuilder)
    {
        httpClientBuilder.AddPolicyHandler((serviceProvider, request) =>
            HttpPolicyExtensions.HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.Forbidden)
            .WaitAndRetryAsync(5, _ => TimeSpan.FromSeconds(10),  async (result, _) =>
            {
                if (result.Result.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    var logger = serviceProvider
                        .GetService<ILoggerFactory>()?
                        .CreateLogger(nameof(WaybackMachineServiceExtensions));
                    
                    const int minutesToWait = 3;
                    logger?.LogInformation("Request was blocked by wayback machine. Waiting {Minutes}min to retry", 
                        minutesToWait);
                    await Task.Delay(TimeSpan.FromMinutes(minutesToWait));
                }
            }));
        
        return httpClientBuilder;
    }
}