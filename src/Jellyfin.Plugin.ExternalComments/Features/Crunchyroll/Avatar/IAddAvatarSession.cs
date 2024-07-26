using System.IO;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Avatar;

public interface IAddAvatarSession
{
    public ValueTask AddAvatarImageAsync(string url, Stream imageStream);
    public ValueTask<bool> ExistsAsync(string url);
}