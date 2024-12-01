using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Avatar.AddAvatar;

public interface IAddAvatarRepository
{
    public Task<Result> AddAvatarImageAsync(string fileName, Stream imageStream, CancellationToken cancellationToken);
    public Result<bool> AvatarExists(string fileName);
}