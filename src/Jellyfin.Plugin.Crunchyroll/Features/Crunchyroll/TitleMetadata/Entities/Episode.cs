using System;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Image.Entites;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities;

public record Episode
{
    public Guid Id { get; init; } = default;
    public required string CrunchyrollId { get; init; }
    public required string Title { get; init; }
    public required string SlugTitle { get; init; }
    public required string Description { get; init; }
    public required string EpisodeNumber { get; init; }
    /// <summary>
    /// <see cref="ImageSource"/> as array json serialized
    /// </summary>
    public required string Thumbnail { get; init; }
    public required double SequenceNumber { get; init; }
    public required Guid SeasonId { get; init; }
    public Season? Season { get; init; }
    public required string Language { get; init; }
}