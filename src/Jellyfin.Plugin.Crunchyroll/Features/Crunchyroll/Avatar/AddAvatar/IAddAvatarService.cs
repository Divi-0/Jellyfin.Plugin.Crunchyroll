using System.Threading;
using System.Threading.Tasks;
using FluentResults;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Avatar.AddAvatar;

public interface IAddAvatarService
{
    public ValueTask<Result> AddAvatarIfNotExists(string uri, CancellationToken cancellationToken);
}