using System.Collections.Generic;

namespace Jellyfin.Plugin.Crunchyroll.Common.Crunchyroll.SearchDto;

public record CrunchyrollSearchData
{
    public string Type { get; init; } = string.Empty;
    public int Count { get; init; }
    public IReadOnlyList<CrunchyrollSearchDataItem> Items { get; init; } = new List<CrunchyrollSearchDataItem>();
}