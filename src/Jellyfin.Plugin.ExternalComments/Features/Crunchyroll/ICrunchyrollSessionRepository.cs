using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll;

public interface ICrunchyrollSessionRepository
{
    public ValueTask SetAsync(string token, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    public ValueTask<string?> GetAsync(CancellationToken cancellationToken = default);
}