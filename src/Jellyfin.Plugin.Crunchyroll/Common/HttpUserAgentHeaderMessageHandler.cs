using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.Crunchyroll.Common;

public class HttpUserAgentHeaderMessageHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Remove("User-Agent");
        request.Headers.Add("User-Agent", "Chrome/130.0.0.0");
        return await base.SendAsync(request, cancellationToken);
    }
}