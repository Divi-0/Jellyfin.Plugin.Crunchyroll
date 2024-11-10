using System.Linq;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Episodes.Dtos;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Episodes;

public static class CrunchyrollEpisodeItemExtensions
{
    public static Episode ToEpisodeEntity(this CrunchyrollEpisodeItem item)
    {
        var thumbnailUri = string.Empty;

        if (item.Images.Thumbnail.Length != 0 && item.Images.Thumbnail[0].Length != 0)
        {
            thumbnailUri = item.Images.Thumbnail.First().Last().Source;
        }
        
        //map field "episode" to EpisodeNumber because some episodes are displayed as "6.5"
        //if field "episode" is empty use field "episode_number" e.g. One Piece Episode "ONE PIECE FAN LETTER"
        return new Episode
        {
            Id = item.Id,
            Title = item.Title,
            SlugTitle = item.SlugTitle,
            Description = item.Description,
            EpisodeNumber = string.IsNullOrWhiteSpace(item.Episode) 
                ? item.EpisodeNumber.HasValue ? item.EpisodeNumber.Value.ToString() : string.Empty 
                : item.Episode,
            ThumbnailUrl = thumbnailUri,
            SequenceNumber = item.SequenceNumber
        };
    }
}