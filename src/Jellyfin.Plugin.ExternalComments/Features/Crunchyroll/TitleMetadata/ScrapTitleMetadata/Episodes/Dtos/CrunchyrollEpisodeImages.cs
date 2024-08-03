using System;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Episodes.Dtos;

public record CrunchyrollEpisodeImages
{
    public CrunchyrollEpisodeThumbnailSizes[][] Thumbnail { get; init; } = new [] { Array.Empty<CrunchyrollEpisodeThumbnailSizes>() };
}