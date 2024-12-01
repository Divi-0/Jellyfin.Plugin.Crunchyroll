using System.Globalization;
using AutoFixture;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities;
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
    public async Task ReturnsId_WhenSeasonFound_GivenTitleIdAndSeasonNumberAndDuplicateNumberOne()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        var seasonNumber = 1;
        var duplicateNumber = 1;
        var seasonId = _fixture.Create<string>();
        var language = new CultureInfo("en-US");

        _repository
            .GetSeasonIdByNumberAsync(titleId, seasonNumber, duplicateNumber, language, Arg.Any<CancellationToken>())
            .Returns(seasonId);

        //Act
        var query = new SeasonIdQueryByNumber(titleId, seasonNumber, duplicateNumber, language);
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
        var language = new CultureInfo("en-US");

        _repository
            .GetSeasonIdByNumberAsync(titleId, seasonNumber, duplicateNumber, language, 
                Arg.Any<CancellationToken>())
            .Returns((string?)null);
        
        _repository
            .GetAllSeasonsAsync(titleId, language, Arg.Any<CancellationToken>())
            .Returns(Result.Ok<IReadOnlyList<Season>>([]));

        //Act
        var query = new SeasonIdQueryByNumber(titleId, seasonNumber, duplicateNumber, language);
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
        var language = new CultureInfo("en-US");

        _repository
            .GetSeasonIdByNumberAsync(titleId, season.SeasonNumber, 1, language,
                Arg.Any<CancellationToken>())
            .Returns((string?)null!);

        _repository
            .GetAllSeasonsAsync(titleId, language, Arg.Any<CancellationToken>())
            .Returns(Result.Ok<IReadOnlyList<Season>>([season, CrunchyrollSeasonFaker.Generate()]));

        //Act
        var query = new SeasonIdQueryByNumber(titleId, season.SeasonNumber, 1, language);
        var result = await _sut.Handle(query, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(season.CrunchyrollId);
    }
    
    [Fact]
    public async Task ReturnsNull_WhenSeasonNotFoundByIdentifier_GivenTitleIdAndSeasonNumberAndDuplicateNumberOne()
    {
        //Arrange
        var titleId = CrunchyrollIdFaker.Generate();
        var season = CrunchyrollSeasonFaker.Generate();
        var language = new CultureInfo("en-US");

        _repository
            .GetSeasonIdByNumberAsync(titleId, season.SeasonNumber, 1, language, 
                Arg.Any<CancellationToken>())
            .Returns((string?)null!);

        _repository
            .GetAllSeasonsAsync(titleId, language, Arg.Any<CancellationToken>())
            .Returns(Result.Ok<IReadOnlyList<Season>>([CrunchyrollSeasonFaker.Generate(), CrunchyrollSeasonFaker.Generate()]));

        //Act
        var query = new SeasonIdQueryByNumber(titleId, season.SeasonNumber, 1, language);
        var result = await _sut.Handle(query, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }
    
    [Fact]
    public async Task ReturnsFailed_WhenGetSeasonIdByNumberFails_GivenTitleIdAndSeasonNumberAndDuplicateNumberOne()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        var seasonNumber = 1;
        var duplicateNumber = 1;
        var language = new CultureInfo("en-US");

        var error = Guid.NewGuid().ToString();
        _repository
            .GetSeasonIdByNumberAsync(titleId, seasonNumber, duplicateNumber, language, 
                Arg.Any<CancellationToken>())
            .Returns(Result.Fail(error));

        //Act
        var query = new SeasonIdQueryByNumber(titleId, seasonNumber, duplicateNumber, language);
        var result = await _sut.Handle(query, CancellationToken.None);

        //Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.First().Message.Should().Be(error);
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