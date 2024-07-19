using System;

namespace Jellyfin.Plugin.ExternalComments.Contracts.Reviews;

public record ReviewItem
{
    public required int AuthorRating { get; init; }
    public required ReviewItemAuthor Author { get; init; }
    public required string Title { get; init; }
    public required string Body { get; init; }
    public required ReviewItemRating Rating { get; init; }
    public required DateTime CreatedAt { get; init; }
}