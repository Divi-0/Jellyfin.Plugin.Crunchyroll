using System;
using System.Collections.Generic;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Image.Entites;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities;

public record TitleMetadata
{
    public Guid Id { get; init; } = default;
    public required string CrunchyrollId { get; init; }
    public required string SlugTitle { get; set; }
    public required string Description { get; set; }
    public required string Studio { get; set; }
    public required string Title { get; set; }
    /// <summary>
    /// <see cref="ImageSource"/> as json serialized
    /// </summary>
    public required string PosterTall { get; set; }
    /// <summary>
    /// <see cref="ImageSource"/> as json serialized
    /// </summary>
    public required string PosterWide { get; set; }
    public required string Language { get; init; }
    public List<Season> Seasons { get; init; } = [];
}