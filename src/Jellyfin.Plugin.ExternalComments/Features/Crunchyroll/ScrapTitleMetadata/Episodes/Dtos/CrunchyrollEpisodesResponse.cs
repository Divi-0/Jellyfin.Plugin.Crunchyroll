using System.Collections.Generic;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.ScrapTitleMetadata.Episodes.Dtos;

public record CrunchyrollEpisodesResponse
{
    public IReadOnlyList<CrunchyrollEpisodeItem> Data { get; init; } = new List<CrunchyrollEpisodeItem>();
}