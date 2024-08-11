using AutoFixture;
using FluentAssertions;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.GetSeasonId;
using Xunit.Abstractions;

namespace JellyFin.Plugin.ExternalComments.Tests.Features.Crunchyroll.TitleMetadata.GetSeasonId;

public class SeasonIdQueryByNumberTests
{
    private readonly Fixture _fixture;
    
    private readonly SeasonIdQueryByNumberHandler _sut;
    private readonly IGetSeasonSession _getSeasonSession;

    public SeasonIdQueryByNumberTests()
    {
        _fixture = new Fixture();
        
        _getSeasonSession = Substitute.For<IGetSeasonSession>();
        _sut = new SeasonIdQueryByNumberHandler(_getSeasonSession);
    }

    [Fact]
    public async Task ReturnsId_WhenSeasonFound_GivenTitleIdAndSeasonNumberAndDuplicateNumberOne()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        var seasonNumber = 1;
        var duplicateNumber = 1;
        var seasonId = _fixture.Create<string>();

        _getSeasonSession
            .GetSeasonIdByNumberAsync(titleId, seasonNumber, duplicateNumber)
            .Returns(seasonId);

        //Act
        var query = new SeasonIdQueryByNumber(titleId, seasonNumber, duplicateNumber);
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
        var seasonNumber = 1;
        var duplicateNumber = 1;

        _getSeasonSession
            .GetSeasonIdByNumberAsync(titleId, seasonNumber, duplicateNumber)
            .Returns((string?)null);

        //Act
        var query = new SeasonIdQueryByNumber(titleId, seasonNumber, duplicateNumber);
        var result = await _sut.Handle(query, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }
}