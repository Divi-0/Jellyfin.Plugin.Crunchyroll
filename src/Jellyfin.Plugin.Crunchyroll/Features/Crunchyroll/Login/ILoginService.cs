using System.Threading;
using System.Threading.Tasks;
using FluentResults;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Login;

public interface ILoginService
{
    public ValueTask<Result> LoginAnonymously(CancellationToken cancellationToken);
}