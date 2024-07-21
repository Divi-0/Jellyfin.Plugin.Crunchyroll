using System.IO;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll;

public interface IGetAvatarSession
{
    public ValueTask<Stream?> GetAvatarImageAsync(string url);
}