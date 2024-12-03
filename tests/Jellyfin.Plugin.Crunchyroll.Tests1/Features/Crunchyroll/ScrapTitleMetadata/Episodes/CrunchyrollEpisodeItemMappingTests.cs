using System.Globalization;
using Bogus;
using FluentAssertions;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Episodes;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Episodes.Dtos;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Image.Entites;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.ScrapTitleMetadata.Episodes;

public class CrunchyrollEpisodeItemMappingTests
{
    private readonly Faker _faker;
    
    public CrunchyrollEpisodeItemMappingTests()
    {
        _faker = new Faker();
    }
    
    [Fact]
    public void ReturnsFullEntity_WhenSuccessful_GivenFullCrunchyrollItem()
    {
        //Arrange
        var seasonId = Guid.NewGuid();
        var thumbnailUri = _faker.Internet.UrlWithPath(fileExt: "png");
        var language = new CultureInfo("en-GB");
        var crunchyrollEpisodeItem = new CrunchyrollEpisodeItem
        {
            Id = CrunchyrollIdFaker.Generate(),
            Title = _faker.Commerce.ProductName(),
            Description = _faker.Commerce.ProductDescription(),
            SlugTitle = CrunchyrollSlugFaker.Generate(),
            Episode = "3",
            EpisodeNumber = 3,
            Images = new CrunchyrollEpisodeImages
            {
                Thumbnail = [[new CrunchyrollEpisodeThumbnailSizes
                {
                    Height = 0,
                    Width = 0,
                    Source = thumbnailUri,
                    Type = "thumbnail"
                }]]
            },
            SequenceNumber = 123.4,
            SeasonId = CrunchyrollIdFaker.Generate(),
            SeriesId = CrunchyrollIdFaker.Generate(),
            SeriesSlugTitle= CrunchyrollSlugFaker.Generate()
        };

        //Act
        var entity = crunchyrollEpisodeItem.ToEpisodeEntity(seasonId, language);

        //Assert
        var expectedEntity = new Episode
        {
            CrunchyrollId = crunchyrollEpisodeItem.Id,
            Title = crunchyrollEpisodeItem.Title,
            Description = crunchyrollEpisodeItem.Description,
            SlugTitle = crunchyrollEpisodeItem.SlugTitle,
            EpisodeNumber = crunchyrollEpisodeItem.Episode,
            Thumbnail = JsonSerializer.Serialize(new ImageSource
            {
                Uri = thumbnailUri,
                Width = 0,
                Height = 0
            }),
            SequenceNumber = 123.4,
            SeasonId = seasonId,
            Language = language.Name
        };
        
        entity.Should().BeEquivalentTo(expectedEntity);
    }
    
    [Fact]
    public void ReturnsFullEntity_WhenEpisodeIsEmpty_GivenCrunchyrollItem()
    {
        //Arrange
        var seasonId = Guid.NewGuid();
        var thumbnailUri = _faker.Internet.UrlWithPath(fileExt: "png");
        var language = new CultureInfo("en-GB");
        var crunchyrollEpisodeItem = new CrunchyrollEpisodeItem
        {
            Id = CrunchyrollIdFaker.Generate(),
            Title = _faker.Commerce.ProductName(),
            Description = _faker.Commerce.ProductDescription(),
            SlugTitle = CrunchyrollSlugFaker.Generate(),
            Episode = string.Empty,
            EpisodeNumber = 5,
            Images = new CrunchyrollEpisodeImages
            {
                Thumbnail = [[new CrunchyrollEpisodeThumbnailSizes
                {
                    Height = 34,
                    Width = 12,
                    Source = thumbnailUri,
                    Type = "thumbnail"
                }]]
            },
            SequenceNumber = 123.4,
            SeasonId = CrunchyrollIdFaker.Generate(),
            SeriesId = CrunchyrollIdFaker.Generate(),
            SeriesSlugTitle= CrunchyrollSlugFaker.Generate()
        };

        //Act
        var entity = crunchyrollEpisodeItem.ToEpisodeEntity(seasonId, language);

        //Assert
        var expectedEntity = new Episode
        {
            CrunchyrollId = crunchyrollEpisodeItem.Id,
            Title = crunchyrollEpisodeItem.Title,
            Description = crunchyrollEpisodeItem.Description,
            SlugTitle = crunchyrollEpisodeItem.SlugTitle,
            EpisodeNumber = "5",
            Thumbnail = JsonSerializer.Serialize(new ImageSource
            {
                Uri = thumbnailUri,
                Width = 12,
                Height = 34
            }),
            SequenceNumber = 123.4,
            SeasonId = seasonId,
            Language = language.Name
        };
        
        entity.Should().BeEquivalentTo(expectedEntity);
    }
    
    [Fact]
    public void ReturnsEmptyImages_WhenItemHasNoImages_GivenCrunchyrollItem()
    {
        //Arrange
        var seasonId = Guid.NewGuid();
        var language = new CultureInfo("en-GB");
        var crunchyrollEpisodeItem = new CrunchyrollEpisodeItem
        {
            Id = CrunchyrollIdFaker.Generate(),
            Title = _faker.Commerce.ProductName(),
            Description = _faker.Commerce.ProductDescription(),
            SlugTitle = CrunchyrollSlugFaker.Generate(),
            Episode = string.Empty,
            EpisodeNumber = 5,
            Images = new CrunchyrollEpisodeImages(),
            SequenceNumber = 123.4,
            SeasonId = CrunchyrollIdFaker.Generate(),
            SeriesId = CrunchyrollIdFaker.Generate(),
            SeriesSlugTitle= CrunchyrollSlugFaker.Generate()
        };

        //Act
        var entity = crunchyrollEpisodeItem.ToEpisodeEntity(seasonId, language);

        //Assert
        var expectedEntity = new Episode
        {
            CrunchyrollId = crunchyrollEpisodeItem.Id,
            Title = crunchyrollEpisodeItem.Title,
            Description = crunchyrollEpisodeItem.Description,
            SlugTitle = crunchyrollEpisodeItem.SlugTitle,
            EpisodeNumber = "5",
            Thumbnail = JsonSerializer.Serialize(new ImageSource
            {
                Uri = string.Empty,
                Width = 0,
                Height = 0
            }),
            SequenceNumber = 123.4,
            SeasonId = seasonId,
            Language = language.Name
        };
        
        entity.Should().BeEquivalentTo(expectedEntity);
    }
}