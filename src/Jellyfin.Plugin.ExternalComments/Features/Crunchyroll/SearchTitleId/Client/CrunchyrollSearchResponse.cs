using System.Collections.Generic;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.SearchTitleId.Client;

public record CrunchyrollSearchResponse
{
    public int Total { get; init; }
    public IReadOnlyList<CrunchyrollSearchData> Data { get; init; } = new List<CrunchyrollSearchData>();
}