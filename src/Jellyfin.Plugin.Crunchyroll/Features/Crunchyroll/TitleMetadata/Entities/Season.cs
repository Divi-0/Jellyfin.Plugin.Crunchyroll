using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities;

public record Season
{
    public Guid Id { get; init; } = default;
    public required string CrunchyrollId { get; init; }
    public required string Title { get; init; }
    public required string SlugTitle { get; init; }
    public required string Identifier { get; init; }
    public string SeasonDisplayNumber { get; init; } = string.Empty;
    public required int SeasonNumber { get; init; }
    public required int SeasonSequenceNumber { get; init; }
    public List<Episode> Episodes { get; init; } = [];
    public required Guid SeriesId { get; init; }
    public TitleMetadata? Series { get; init; }
    public required string Language { get; init; }
}