namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Reviews.GetReviews.Client;

public record CrunchyrollReviewItemAuthor
{
    public required string Username { get; init; }
    public required string Avatar { get; init; }
}