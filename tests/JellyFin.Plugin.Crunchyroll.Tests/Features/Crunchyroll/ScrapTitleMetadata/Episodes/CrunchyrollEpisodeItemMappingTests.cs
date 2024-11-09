using Bogus;
using FluentAssertions;
using J2N;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Episodes;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Episodes.Dtos;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using Xunit.Abstractions;

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
        var thumbnailUri = _faker.Internet.UrlWithPath(fileExt: "png");
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
            }
        };

        //Act
        var entity = crunchyrollEpisodeItem.ToEpisodeEntity();

        //Assert
        var expectedEntity = new Episode
        {
            Id = crunchyrollEpisodeItem.Id,
            Title = crunchyrollEpisodeItem.Title,
            Description = crunchyrollEpisodeItem.Description,
            SlugTitle = crunchyrollEpisodeItem.SlugTitle,
            EpisodeNumber = crunchyrollEpisodeItem.Episode,
            ThumbnailUrl = thumbnailUri
        };
        
        entity.Should().BeEquivalentTo(expectedEntity);
    }
    
    [Fact]
    public void ReturnsFullEntity_WhenEpisodeIsEmpty_GivenCrunchyrollItem()
    {
        //Arrange
        var thumbnailUri = _faker.Internet.UrlWithPath(fileExt: "png");
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
                    Height = 0,
                    Width = 0,
                    Source = thumbnailUri,
                    Type = "thumbnail"
                }]]
            }
        };

        //Act
        var entity = crunchyrollEpisodeItem.ToEpisodeEntity();

        //Assert
        var expectedEntity = new Episode
        {
            Id = crunchyrollEpisodeItem.Id,
            Title = crunchyrollEpisodeItem.Title,
            Description = crunchyrollEpisodeItem.Description,
            SlugTitle = crunchyrollEpisodeItem.SlugTitle,
            EpisodeNumber = "5",
            ThumbnailUrl = thumbnailUri
        };
        
        entity.Should().BeEquivalentTo(expectedEntity);
    }
    
    [Fact]
    public void ReturnsEmptyImages_WhenItemHasNoImages_GivenCrunchyrollItem()
    {
        //Arrange
        var thumbnailUri = _faker.Internet.UrlWithPath(fileExt: "png");
        var crunchyrollEpisodeItem = new CrunchyrollEpisodeItem
        {
            Id = CrunchyrollIdFaker.Generate(),
            Title = _faker.Commerce.ProductName(),
            Description = _faker.Commerce.ProductDescription(),
            SlugTitle = CrunchyrollSlugFaker.Generate(),
            Episode = string.Empty,
            EpisodeNumber = 5,
            Images = new CrunchyrollEpisodeImages()
        };

        //Act
        var entity = crunchyrollEpisodeItem.ToEpisodeEntity();

        //Assert
        var expectedEntity = new Episode
        {
            Id = crunchyrollEpisodeItem.Id,
            Title = crunchyrollEpisodeItem.Title,
            Description = crunchyrollEpisodeItem.Description,
            SlugTitle = crunchyrollEpisodeItem.SlugTitle,
            EpisodeNumber = "5",
            ThumbnailUrl = string.Empty
        };
        
        entity.Should().BeEquivalentTo(expectedEntity);
    }
}