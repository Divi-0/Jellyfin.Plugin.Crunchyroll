using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Series.Dtos;

public record CrunchyrollSeriesContentResponse
{
    public IReadOnlyList<CrunchyrollSeriesContentItem> Data { get; init; } = [];
}