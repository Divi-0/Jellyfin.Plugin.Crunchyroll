using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Episodes.Dtos;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Image.Entites;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Episodes;

public static class CrunchyrollEpisodeItemExtensions
{
    public static Episode ToEpisodeEntity(this CrunchyrollEpisodeItem item, Guid seasonId, CultureInfo language)
    {
        CrunchyrollEpisodeThumbnailSizes? thumbnail = null;

        if (item.Images.Thumbnail.Length != 0 && item.Images.Thumbnail[0].Length != 0)
        {
            thumbnail = item.Images.Thumbnail.First().Last();
        }
        
        //map field "episode" to EpisodeNumber because some episodes are displayed as "6.5"
        //if field "episode" is empty use field "episode_number" e.g. One Piece Episode "ONE PIECE FAN LETTER"
        return new Episode
        {
            CrunchyrollId = item.Id,
            Title = item.Title,
            SlugTitle = item.SlugTitle,
            Description = item.Description,
            EpisodeNumber = string.IsNullOrWhiteSpace(item.Episode) 
                ? item.EpisodeNumber.HasValue ? item.EpisodeNumber.Value.ToString() : string.Empty 
                : item.Episode,
            Thumbnail = JsonSerializer.Serialize(new ImageSource
            {
                Uri = thumbnail?.Source ?? string.Empty,
                Width = thumbnail?.Width ?? 0,
                Height = thumbnail?.Height ?? 0
            }),
            SequenceNumber = item.SequenceNumber,
            SeasonId = seasonId,
            Language = language.Name
        };
    }
}