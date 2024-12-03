using System.Globalization;
using AutoFixture;
using FluentAssertions;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.GetSeasonId;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.TitleMetadata.GetSeasonId;

public class SeasonIdQueryByNameTests
{
    private readonly Fixture _fixture;
    
    private readonly SeasonIdQueryByNameHandler _sut;
    private readonly IGetSeasonRepository _repository;

    public SeasonIdQueryByNameTests()
    {
        _fixture = new Fixture();
        
        _repository = Substitute.For<IGetSeasonRepository>();
        _sut = new SeasonIdQueryByNameHandler(_repository);
    }

    [Fact]
    public async Task ReturnsId_WhenSeasonFound_GivenTitleIdAndSeasonNumberAndDuplicateNumberOne()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        var seasonName = _fixture.Create<string>();
        var seasonId = _fixture.Create<string>();
        var language = new CultureInfo("en-US");

        _repository
            .GetSeasonIdByNameAsync(titleId, seasonName, language, Arg.Any<CancellationToken>())
            .Returns(seasonId);

        //Act
        var query = new SeasonIdQueryByName(titleId, seasonName, language);
        var result = await _sut.Handle(query, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(seasonId);
    }

    [Fact]
    public async Task ReturnsNull_WhenSeasonNotFound_GivenTitleIdAndSeasonNumberAndDuplicateNumberOne()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        var seasonName = _fixture.Create<string>();
        var language = new CultureInfo("en-US");

        _repository
            .GetSeasonIdByNameAsync(titleId, seasonName, language, Arg.Any<CancellationToken>())
            .Returns((string?)null);

        //Act
        var query = new SeasonIdQueryByName(titleId, seasonName, language);
        var result = await _sut.Handle(query, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }
}