using System.Threading.Tasks;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.GetEpisodeId;

public interface IGetEpisodeSession
{
    public ValueTask<string?> GetEpisodeIdAsync(string titleId, string seasonId, string episodeIdentifier);
}