using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews.GetReviews.Client;

public record CrunchyrollReviewItemReview
{
    public required string Title { get; init; }
    public required string Body { get; init; }
    [JsonPropertyName("created_at")]
    public required string CreatedAt { get; init; }
    [JsonPropertyName("modified_at")]
    public required string ModifiedAt { get; init; }
    public required bool Spoiler { get; init; }
}