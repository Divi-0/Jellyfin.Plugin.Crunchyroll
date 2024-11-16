using System.Collections.Generic;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities;

public record Season
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string SlugTitle { get; init; }
    public required string Identifier { get; init; }
    public string SeasonDisplayNumber { get; init; } = string.Empty;
    public required int SeasonNumber { get; init; }
    public required int SeasonSequenceNumber { get; init; }
    public required List<Episode> Episodes { get; set; } = [];
}