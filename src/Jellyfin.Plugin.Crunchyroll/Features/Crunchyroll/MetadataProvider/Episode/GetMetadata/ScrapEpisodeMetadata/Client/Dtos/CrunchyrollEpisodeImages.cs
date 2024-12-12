using System;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.ScrapEpisodeMetadata.Client.Dtos;

public record CrunchyrollEpisodeImages
{
    public CrunchyrollEpisodeThumbnailSizes[][] Thumbnail { get; init; } = new [] { Array.Empty<CrunchyrollEpisodeThumbnailSizes>() };
}