namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.GetReviews.Client;

public record CrunchyrollReviewItemAuthor
{
    public required string Username { get; init; }
    public required string Avatar { get; init; }
}