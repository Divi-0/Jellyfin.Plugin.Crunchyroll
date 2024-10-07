using System.IO;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Avatar;

public interface IGetAvatarSession
{
    public ValueTask<Stream?> GetAvatarImageAsync(string url);
}