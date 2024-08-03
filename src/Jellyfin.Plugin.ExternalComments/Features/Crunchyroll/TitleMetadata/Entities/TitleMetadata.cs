using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.Entities;

public record TitleMetadata
{
    public Guid Id { get; init; }
    public required string TitleId { get; init; }
    public required string SlugTitle { get; init; }
    public required string Description { get; init; }
    public List<Season> Seasons { get; init; } = new List<Season>();
}