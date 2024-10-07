using System.Threading;
using System.Threading.Tasks;
using FluentResults;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Login.Client;

public interface ICrunchyrollLoginClient
{
    public Task<Result<CrunchyrollAuthResponse>> LoginAnonymousAsync(CancellationToken cancellationToken);
}