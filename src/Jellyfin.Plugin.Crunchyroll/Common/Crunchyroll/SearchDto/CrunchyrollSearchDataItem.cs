using System.Text.Json.Serialization;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Episodes.Dtos;

namespace Jellyfin.Plugin.Crunchyroll.Common.Crunchyroll.SearchDto;

public record CrunchyrollSearchDataItem
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    [JsonPropertyName("slug_title")]
    public string SlugTitle { get; init; } = string.Empty;
    [JsonPropertyName("episode_metadata")]
    public CrunchyrollEpisodeItem? EpisodeMetadata { get; init; }
}