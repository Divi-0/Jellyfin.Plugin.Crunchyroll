using System.Collections.Generic;

namespace Jellyfin.Plugin.ExternalComments.Entities;

public record Season
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string SlugTitle { get; init; }
    public required List<Episode> Episodes { get; set; } = new List<Episode>();
}