using System.Collections.Generic;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities;

public record Season
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string SlugTitle { get; init; }
    public required int SeasonNumber { get; init; }
    public required List<Episode> Episodes { get; set; } = new List<Episode>();
}