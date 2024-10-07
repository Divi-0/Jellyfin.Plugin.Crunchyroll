using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Avatar.Client;

public interface IAvatarClient
{
    public Task<Result<Stream>> GetAvatarStreamAsync(string uri, CancellationToken cancellationToken);
}