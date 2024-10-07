using System;
using System.Collections.Generic;
using Jellyfin.Plugin.Crunchyroll.Contracts.Comments;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Comments.Entites;

public record EpisodeComments
{
    public Guid Id { get; init; }
    public required string EpisodeId { get; init; }
    public required IReadOnlyList<CommentItem> Comments { get; init; }
}