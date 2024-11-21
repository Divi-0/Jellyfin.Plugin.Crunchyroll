using System.Collections.Generic;

namespace Jellyfin.Plugin.Crunchyroll.Common.Crunchyroll.SearchDto;

public record CrunchyrollSearchResponse
{
    public int Total { get; init; }
    public IReadOnlyList<CrunchyrollSearchData> Data { get; init; } = new List<CrunchyrollSearchData>();
}