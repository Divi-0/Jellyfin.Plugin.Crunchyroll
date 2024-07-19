namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.SearchAndAssignTitleId;

public record SearchResponse
{
    public required string Id { get; init; }
    public required string SlugTitle { get; init; }
}