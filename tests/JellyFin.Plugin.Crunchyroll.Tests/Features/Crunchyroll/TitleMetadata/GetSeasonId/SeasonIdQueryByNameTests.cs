using AutoFixture;
using FluentAssertions;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.GetSeasonId;

namespace JellyFin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.TitleMetadata.GetSeasonId;

public class SeasonIdQueryByNameTests
{
    private readonly Fixture _fixture;
    
    private readonly SeasonIdQueryByNameHandler _sut;
    private readonly IGetSeasonSession _getSeasonSession;

    public SeasonIdQueryByNameTests()
    {
        _fixture = new Fixture();
        
        _getSeasonSession = Substitute.For<IGetSeasonSession>();
        _sut = new SeasonIdQueryByNameHandler(_getSeasonSession);
    }

    [Fact]
    public async Task ReturnsId_WhenSeasonFound_GivenTitleIdAndSeasonNumberAndDuplicateNumberOne()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        var seasonName = _fixture.Create<string>();
        var seasonId = _fixture.Create<string>();

        _getSeasonSession
            .GetSeasonIdByNameAsync(titleId, seasonName)
            .Returns(seasonId);

        //Act
        var query = new SeasonIdQueryByName(titleId, seasonName);
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

        _getSeasonSession
            .GetSeasonIdByNameAsync(titleId, seasonName)
            .Returns((string?)null);

        //Act
        var query = new SeasonIdQueryByName(titleId, seasonName);
        var result = await _sut.Handle(query, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }
}