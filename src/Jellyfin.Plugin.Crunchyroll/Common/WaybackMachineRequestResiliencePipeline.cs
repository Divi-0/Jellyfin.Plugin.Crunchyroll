using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using SocketException = System.Net.Sockets.SocketException;

namespace Jellyfin.Plugin.Crunchyroll.Common;

public static class WaybackMachineRequestResiliencePipeline
{
    public static ResiliencePipeline Get(ILogger logger)
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = args => ValueTask.FromResult(
                    args.Outcome.Exception is HttpRequestException { InnerException: SocketException }),
                OnRetry = async _ =>
                {
                    const int minutesToWait = 3;
                    logger.LogInformation("Request was blocked by wayback machine. Waiting {Minutes}min to retry", 
                        minutesToWait);
                    await Task.Delay(TimeSpan.FromMinutes(minutesToWait));
                }
            })
            .Build();
    }
}