using System;
using System.Collections.Generic;
using Jellyfin.Plugin.ExternalComments.Contracts.Comments;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Comments.Entites;

public record EpisodeComments
{
    public Guid Id { get; init; }
    public required string EpisodeId { get; init; }
    public required IReadOnlyList<CommentItem> Comments { get; init; }
}