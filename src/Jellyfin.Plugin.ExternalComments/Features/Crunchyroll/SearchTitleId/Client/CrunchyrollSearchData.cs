using System.Collections.Generic;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.SearchTitleId.Client;

public record CrunchyrollSearchData
{
    public string Type { get; init; } = string.Empty;
    public int Count { get; init; }
    public IReadOnlyList<CrunchyrollSearchDataItem> Items { get; init; } = new List<CrunchyrollSearchDataItem>();
}