namespace Jellyfin.Plugin.Crunchyroll.Contracts.Reviews;

public class ReviewItemRating
{
    public required int Likes { get; init; }
    public required int Dislikes { get; init; }
    public required int Total { get; init; }
}