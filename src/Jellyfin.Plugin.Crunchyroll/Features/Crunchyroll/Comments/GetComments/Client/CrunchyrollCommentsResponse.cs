using System.Collections.Generic;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Comments.GetComments.Client;

public record CrunchyrollCommentsResponse
{
    public IReadOnlyList<CrunchyrollCommentsItem> Items { get; init; } = new List<CrunchyrollCommentsItem>();
    public int Total { get; init; }
}