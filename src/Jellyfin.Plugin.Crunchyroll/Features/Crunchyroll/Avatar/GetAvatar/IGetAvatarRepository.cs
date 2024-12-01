using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Avatar.GetAvatar;

public interface IGetAvatarRepository
{
    public Task<Result<Stream?>> GetAvatarImageAsync(string fileName, CancellationToken cancellationToken);
}