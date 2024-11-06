using System.IO;
using System.Threading.Tasks;
using FluentResults;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Avatar;

public interface IAddAvatarSession
{
    public ValueTask<Result> AddAvatarImageAsync(string url, Stream imageStream);
    public ValueTask<bool> AvatarExistsAsync(string url);
}