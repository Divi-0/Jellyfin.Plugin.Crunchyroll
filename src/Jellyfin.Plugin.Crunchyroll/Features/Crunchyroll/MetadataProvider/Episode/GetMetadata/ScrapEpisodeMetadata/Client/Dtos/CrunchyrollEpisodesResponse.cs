using System.Collections.Generic;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.ScrapEpisodeMetadata.Client.Dtos;

public record CrunchyrollEpisodesResponse
{
    public IReadOnlyList<CrunchyrollEpisodeItem> Data { get; init; } = new List<CrunchyrollEpisodeItem>();
}