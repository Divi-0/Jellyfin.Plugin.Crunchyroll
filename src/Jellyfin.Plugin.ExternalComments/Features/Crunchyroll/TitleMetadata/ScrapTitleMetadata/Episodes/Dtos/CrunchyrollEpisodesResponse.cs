using System.Collections.Generic;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Episodes.Dtos;

public record CrunchyrollEpisodesResponse
{
    public IReadOnlyList<CrunchyrollEpisodeItem> Data { get; init; } = new List<CrunchyrollEpisodeItem>();
}