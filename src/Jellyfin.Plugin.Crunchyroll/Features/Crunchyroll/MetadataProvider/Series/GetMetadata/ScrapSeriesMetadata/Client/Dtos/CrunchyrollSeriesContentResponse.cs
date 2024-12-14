using System.Collections.Generic;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.GetMetadata.ScrapSeriesMetadata.Client.Dtos;

public record CrunchyrollSeriesContentResponse
{
    public IReadOnlyList<CrunchyrollSeriesContentItem> Data { get; init; } = [];
}