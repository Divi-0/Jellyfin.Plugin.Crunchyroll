using System.Globalization;
using AutoFixture;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Domain.Entities;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.GetSeasonId;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.TitleMetadata.GetSeasonId;

public class SeasonIdQueryByNumberTests
{
    private readonly Fixture _fixture;
    
    private readonly SeasonIdQueryByNumberHandler _sut;
    private readonly IGetSeasonRepository _repository;

    public SeasonIdQueryByNumberTests()
    {
        _fixture = new Fixture();
        
        _repository = Substitute.For<IGetSeasonRepository>();
        _sut = new SeasonIdQueryByNumberHandler(_repository);
    }
    
    [Fact]
    public async Task ReturnsFailed_WhenGetAllSeasonsFails_GivenTitleIdAndSeasonNumberAndDuplicateNumberOne()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        var seasonNumber = 1;
        var duplicateNumber = 1;
        var seasonId = _fixture.Create<string>();
        var language = new CultureInfo("en-US");

        _repository
            .GetSeasonIdByNumberAsync(titleId, seasonNumber, duplicateNumber, language, 
                Arg.Any<CancellationToken>())
            .Returns(seasonId);

        //Act
        var query = new SeasonIdQueryByNumber(titleId, seasonNumber, duplicateNumber, language);
        var result = await _sut.Handle(query, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(seasonId);
    }
}