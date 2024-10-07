using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities;

public record TitleMetadata
{
    public Guid Id { get; init; }
    public required string TitleId { get; init; }
    public required string SlugTitle { get; set; }
    public required string Description { get; set; }
    public required string Studio { get; set; }
    public required string Title { get; set; }
    public required string PosterTallUri { get; set; }
    public required string PosterWideUri { get; set; }
    public List<Season> Seasons { get; init; } = new List<Season>();
}