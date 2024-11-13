using System.Threading;
using System.Threading.Tasks;
using FluentResults;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Avatar.AddAvatar;

public interface IAddAvatarService
{
    /// <returns>the archived uri by waybackmachine</returns>
    public ValueTask<Result<string>> AddAvatarIfNotExists(string uri, CancellationToken cancellationToken);
}