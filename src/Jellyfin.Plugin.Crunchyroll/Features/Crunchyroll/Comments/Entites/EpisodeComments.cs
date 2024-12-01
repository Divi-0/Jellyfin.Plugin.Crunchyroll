using System;
using Jellyfin.Plugin.Crunchyroll.Contracts.Comments;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Comments.Entites;

public record EpisodeComments
{
    public Guid Id { get; init; } = default;
    public required string CrunchyrollEpisodeId { get; init; }
    /// <summary>
    /// <see cref="CommentItem"/> as array json serialized
    /// </summary>
    public required string Comments { get; init; } = string.Empty;
    public required string Language { get; init; }
}