using System.Threading.Tasks;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.GetEpisodeId;

public interface IGetEpisodeSession
{
    public ValueTask<EpisodeIdResult?> GetEpisodeIdAsync(string titleId, string seasonId, string episodeIdentifier);
}