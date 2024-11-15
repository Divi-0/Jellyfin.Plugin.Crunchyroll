using Bogus;
using FluentAssertions;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Seasons;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Seasons.Dtos;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using Xunit.Abstractions;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.ScrapTitleMetadata.Seasons;

public class CrunchyrollSeasonsItemMappingTests
{
    private readonly Faker _faker;

    public CrunchyrollSeasonsItemMappingTests()
    {
        _faker = new Faker();
    }
    
    [Fact]
    public void ReturnsEntity_WhenCrunchyrollSeasonIsValid_GivenCrunchyrollSeason()
    {
        //Arrange
        var crunchyrollSeason = new CrunchyrollSeasonsItem
        {
            Id = CrunchyrollIdFaker.Generate(),
            Title = _faker.Commerce.ProductName(),
            SeasonNumber = _faker.Random.Int(),
            SeasonSequenceNumber = _faker.Random.Int(),
            SlugTitle = CrunchyrollSlugFaker.Generate()
        };

        var expectedEntity = new Season
        {
            Id = crunchyrollSeason.Id,
            Title = crunchyrollSeason.Title,
            SeasonNumber = crunchyrollSeason.SeasonNumber,
            SeasonSequenceNumber = crunchyrollSeason.SeasonSequenceNumber,
            SlugTitle = crunchyrollSeason.SlugTitle,
            Episodes = []
        };
        
        //Act
        var entity = crunchyrollSeason.ToSeasonEntity([]);

        //Assert
        entity.Should().BeEquivalentTo(expectedEntity);
    }
}