using AutoFixture;
using FluentAssertions;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.GetSeasonId;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.TitleMetadata.GetSeasonId;

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
        
        _getSeasonSession
            .GetAllSeasonsAsync(titleId)
            .Returns([]);

        //Act
        var query = new SeasonIdQueryByNumber(titleId, seasonNumber, duplicateNumber);
        var result = await _sut.Handle(query, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }
    
    [Fact]
    public async Task ReturnsId_WhenSeasonFoundByIdentifier_GivenTitleIdAndSeasonNumberAndDuplicateNumberOne()
    {
        //Arrange
        var titleId = CrunchyrollIdFaker.Generate();
        var season = CrunchyrollSeasonFaker.Generate();

        _getSeasonSession
            .GetSeasonIdByNumberAsync(titleId, season.SeasonNumber, 1)
            .Returns((string?)null!);

        _getSeasonSession
            .GetAllSeasonsAsync(titleId)
            .Returns([season, CrunchyrollSeasonFaker.Generate()]);

        //Act
        var query = new SeasonIdQueryByNumber(titleId, season.SeasonNumber, 1);
        var result = await _sut.Handle(query, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(season.Id);
    }
    
    [Fact]
    public async Task ReturnsNull_WhenSeasonNotFoundByIdentifier_GivenTitleIdAndSeasonNumberAndDuplicateNumberOne()
    {
        //Arrange
        var titleId = CrunchyrollIdFaker.Generate();
        var season = CrunchyrollSeasonFaker.Generate();

        _getSeasonSession
            .GetSeasonIdByNumberAsync(titleId, season.SeasonNumber, 1)
            .Returns((string?)null!);

        _getSeasonSession
            .GetAllSeasonsAsync(titleId)
            .Returns([CrunchyrollSeasonFaker.Generate(), CrunchyrollSeasonFaker.Generate()]);

        //Act
        var query = new SeasonIdQueryByNumber(titleId, season.SeasonNumber, 1);
        var result = await _sut.Handle(query, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }
}