using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using SocketException = System.Net.Sockets.SocketException;

namespace Jellyfin.Plugin.Crunchyroll.Common;

public static class WaybackMachineRequestResiliencePipeline
{
    public static ResiliencePipeline Get(ILogger logger, int waitTimeoutInSeconds = 180)
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = args => ValueTask.FromResult(
                    args.Outcome.Exception is HttpRequestException { InnerException: SocketException } || 
                    args.Outcome.Result is HttpResponseMessage { StatusCode: HttpStatusCode.TooManyRequests }),
                OnRetry = async _ =>
                {
                    logger.LogInformation("Request was blocked by wayback machine. Waiting {Minutes}sec to retry", 
                        waitTimeoutInSeconds);
                    await Task.Delay(TimeSpan.FromSeconds(waitTimeoutInSeconds));
                },
                MaxRetryAttempts = 3,
                MaxDelay = TimeSpan.Zero
            })
            .Build();
    }
}