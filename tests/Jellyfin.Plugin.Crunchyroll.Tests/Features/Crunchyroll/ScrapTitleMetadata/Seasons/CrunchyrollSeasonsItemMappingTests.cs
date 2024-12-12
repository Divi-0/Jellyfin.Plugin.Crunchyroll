using System.Globalization;
using Bogus;
using FluentAssertions;
using Jellyfin.Plugin.Crunchyroll.Domain.Entities;
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
        var seriesId = Guid.NewGuid();
        var language = new CultureInfo("en-US");
        var crunchyrollSeason = new CrunchyrollSeasonsItem
        {
            Id = CrunchyrollIdFaker.Generate(),
            Title = _faker.Commerce.ProductName(),
            SeasonNumber = _faker.Random.Int(),
            SeasonDisplayNumber = _faker.Random.Int().ToString(),
            SeasonSequenceNumber = _faker.Random.Int(),
            SlugTitle = CrunchyrollSlugFaker.Generate(),
            Identifier = _faker.Random.Word(),
        };

        var expectedEntity = new Season
        {
            CrunchyrollId = crunchyrollSeason.Id,
            Title = crunchyrollSeason.Title,
            SeasonNumber = crunchyrollSeason.SeasonNumber,
            SeasonSequenceNumber = crunchyrollSeason.SeasonSequenceNumber,
            SeasonDisplayNumber = crunchyrollSeason.SeasonDisplayNumber,
            SlugTitle = crunchyrollSeason.SlugTitle,
            Identifier = crunchyrollSeason.Identifier,
            Episodes = [],
            SeriesId = seriesId,
            Language = language.Name
        };
        
        //Act
        var entity = crunchyrollSeason.ToSeasonEntity(seriesId, language);

        //Assert
        entity.Should().BeEquivalentTo(expectedEntity);
    }
}