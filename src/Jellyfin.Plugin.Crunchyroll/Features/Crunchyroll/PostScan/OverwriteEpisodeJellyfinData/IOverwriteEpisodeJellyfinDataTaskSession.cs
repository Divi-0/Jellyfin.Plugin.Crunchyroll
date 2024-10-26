using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.OverwriteEpisodeJellyfinData;

public interface IOverwriteEpisodeJellyfinDataTaskSession
{
    public ValueTask<Result<Episode>> GetEpisodeAsync(string episodeId);
}