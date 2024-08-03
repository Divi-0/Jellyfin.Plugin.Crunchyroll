namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.SearchTitleId;

public record SearchResponse
{
    public required string Id { get; init; }
    public required string SlugTitle { get; init; }
}