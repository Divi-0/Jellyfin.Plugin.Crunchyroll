using Bogus;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Domain;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.GetEpisodeCrunchyrollId;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata;

public class GetEpisodeCrunchyrollIdServiceTests
{
    private readonly GetEpisodeCrunchyrollIdService _sut;
    private readonly IGetEpisodeCrunchyrollIdRepository _repository;

    private readonly Faker _faker;

    public GetEpisodeCrunchyrollIdServiceTests()
    {
        _repository = Substitute.For<IGetEpisodeCrunchyrollIdRepository>();
        var logger = Substitute.For<ILogger<GetEpisodeCrunchyrollIdService>>();
        _sut = new GetEpisodeCrunchyrollIdService(_repository, logger);

        _faker = new Faker();
    }
    
    [Fact]
    public async Task SetsEpisodeIdAndRunsPostTasks_WhenSuccessful_GivenWithIndexNumber()
    {
        //Arrange
        var seasonId = CrunchyrollIdFaker.Generate();
        var fileName = _faker.Random.Word();
        var indexNumber = (int?)Random.Shared.Next(1, int.MaxValue);
        var episodeId = CrunchyrollIdFaker.Generate();

        _repository
            .GetEpisodeIdByNumber(Arg.Any<CrunchyrollId>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(episodeId);
        
        //Act
        var result = await _sut.GetEpisodeIdAsync(seasonId, fileName, indexNumber, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(episodeId);

        await _repository
            .DidNotReceive()
            .GetEpisodeIdByName(Arg.Any<CrunchyrollId>(), Arg.Any<string>(), Arg.Any<CancellationToken>());

        await _repository
            .Received(1)
            .GetEpisodeIdByNumber(seasonId, indexNumber.Value.ToString(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("S01E1.5 - grfes", "1.5")]
    [InlineData("S06E15.5", "15.5")]
    [InlineData("S06E0032.9", "32.9")]
    public async Task
        SetsEpisodeIdAndRunsPostTasks_WhenEpisodeHasIndexNumberButNameIncludesDecimal_GivenWithIndexNumber(
            string fileName, string episodeNumber)
    {
        //Arrange
        var seasonId = CrunchyrollIdFaker.Generate();
        var episodeId = CrunchyrollIdFaker.Generate();
        var indexNumber = (int?)Random.Shared.Next(1, int.MaxValue);

        _repository
            .GetEpisodeIdByNumber(Arg.Any<CrunchyrollId>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(episodeId);

        //Act
        var result = await _sut.GetEpisodeIdAsync(seasonId, fileName, indexNumber, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(episodeId);
        
        await _repository
            .DidNotReceive()
            .GetEpisodeIdByName(Arg.Any<CrunchyrollId>(), Arg.Any<string>(), Arg.Any<CancellationToken>());

        await _repository
            .Received(1)
            .GetEpisodeIdByNumber(seasonId, episodeNumber, Arg.Any<CancellationToken>());
    }
    
    [Theory]
    [InlineData("S03E13A", "13A")]
    [InlineData("S2E52b", "52b")]
    public async Task SetsEpisodeIdAndRunsPostTasks_WhenEpisodeHasIndexNumberButNameIncludesNumberWithLetter_GivenSeasonWithSeasonId(
        string fileName, string episodeNumber)
    {
        //Arrange
        var seasonId = CrunchyrollIdFaker.Generate();
        var episodeId = CrunchyrollIdFaker.Generate();
        var indexNumber = (int?)Random.Shared.Next(1, int.MaxValue);

        _repository
            .GetEpisodeIdByNumber(Arg.Any<CrunchyrollId>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(episodeId);

        //Act
        var result = await _sut.GetEpisodeIdAsync(seasonId, fileName, indexNumber, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(episodeId);
        
        await _repository
            .DidNotReceive()
            .GetEpisodeIdByName(Arg.Any<CrunchyrollId>(), Arg.Any<string>(), Arg.Any<CancellationToken>());

        await _repository
            .Received(1)
            .GetEpisodeIdByNumber(seasonId, episodeNumber, Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsNull_WhenEpisodeHasNoIndexNumberAndWasNotFoundByName_GivenWithNoIndexNumber()
    {
        //Arrange
        var seasonId = CrunchyrollIdFaker.Generate();
        var fileName = _faker.Random.Word();
        var indexNumber = (int?)null;

        _repository
            .GetEpisodeIdByName(Arg.Any<CrunchyrollId>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok<CrunchyrollId?>(null!));
        
        //Act
        var result = await _sut.GetEpisodeIdAsync(seasonId, fileName, indexNumber, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
        
        await _repository
            .Received(1)
            .GetEpisodeIdByName(seasonId, fileName, Arg.Any<CancellationToken>());

        await _repository
            .DidNotReceive()
            .GetEpisodeIdByNumber(Arg.Any<CrunchyrollId>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsFailed_WhenGetByNameFails_GivenWithNoIndexNumber()
    {
        //Arrange
        var seasonId = CrunchyrollIdFaker.Generate();
        var fileName = _faker.Random.Word();
        var indexNumber = (int?)null;

        var error = Guid.NewGuid().ToString();
        _repository
            .GetEpisodeIdByName(Arg.Any<CrunchyrollId>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail(error));
        
        //Act
        var result = await _sut.GetEpisodeIdAsync(seasonId, fileName, indexNumber, CancellationToken.None);

        //Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.First().Message.Should().Be(error);
        
        await _repository
            .Received(1)
            .GetEpisodeIdByName(seasonId, fileName, Arg.Any<CancellationToken>());

        await _repository
            .DidNotReceive()
            .GetEpisodeIdByNumber(Arg.Any<CrunchyrollId>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task SetsEpisodeId_WhenGetEpisodeByNameReturnsId_GivenSeasonWithSeasonId()
    {
        //Arrange
        var seasonId = CrunchyrollIdFaker.Generate();
        var fileName = _faker.Random.Word();
        var indexNumber = (int?)null;
        var episodeId = CrunchyrollIdFaker.Generate();

        _repository
            .GetEpisodeIdByName(Arg.Any<CrunchyrollId>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok<CrunchyrollId?>(episodeId));
        
        //Act
        var result = await _sut.GetEpisodeIdAsync(seasonId, fileName, indexNumber, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(episodeId);
        
        await _repository
            .DidNotReceive()
            .GetEpisodeIdByNumber(Arg.Any<CrunchyrollId>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        
        await _repository
            .Received(1)
            .GetEpisodeIdByName(seasonId, fileName, Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task SetsEmptyEpisodeIdAndRunsPostTasks_WhenCrunchyrollIdNotFoundByNumber_GivenSeasonWithSeasonId()
    {
        //Arrange
        var seasonId = CrunchyrollIdFaker.Generate();
        var fileName = _faker.Random.Word();
        var indexNumber = (int?)Random.Shared.Next(1, int.MaxValue);

        _repository
            .GetEpisodeIdByNumber(Arg.Any<CrunchyrollId>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok<CrunchyrollId?>(null!));
        
        //Act
        var result = await _sut.GetEpisodeIdAsync(seasonId, fileName, indexNumber, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
        
        await _repository
            .DidNotReceive()
            .GetEpisodeIdByName(Arg.Any<CrunchyrollId>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        
        await _repository
            .Received(1)
            .GetEpisodeIdByNumber(seasonId, indexNumber.Value.ToString(), Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsFailed_WhenGetEpisodeFails_GivenSeasonWithSeasonId()
    {
        //Arrange
        var seasonId = CrunchyrollIdFaker.Generate();
        var fileName = _faker.Random.Word();
        var indexNumber = (int?)Random.Shared.Next(1, int.MaxValue);

        var error = Guid.NewGuid().ToString();
        _repository
            .GetEpisodeIdByNumber(Arg.Any<CrunchyrollId>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail(error));
        
        //Act
        var result = await _sut.GetEpisodeIdAsync(seasonId, fileName, indexNumber, CancellationToken.None);

        //Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.First().Message.Should().Be(error);
        
        await _repository
            .DidNotReceive()
            .GetEpisodeIdByName(Arg.Any<CrunchyrollId>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        
        await _repository
            .Received(1)
            .GetEpisodeIdByNumber(seasonId, indexNumber.Value.ToString(), Arg.Any<CancellationToken>());
    }
    
    [Theory]
    [InlineData("E1124", "1124")]
    [InlineData("E502", "502")]
    [InlineData("E-FMI1", "FMI1")]
    [InlineData("E-FMI2", "FMI2")]
    [InlineData("S13E542", "542")]
    [InlineData("S13E-SP", "SP")]
    [InlineData("S13E-SP1 - abc", "SP1")]
    [InlineData("S1E6.5 - def", "6.5")]
    public async Task SetsEpisodeIdAndRunsPostTasks_WhenHasNoIndexNumberButWasFoundByName_GivenSeasonWithSeasonId(
        string fileName, string episodeNumber)
    {
        //Arrange
        var seasonId = CrunchyrollIdFaker.Generate();
        fileName = $"{fileName} - {_faker.Random.Words(3)}";
        var indexNumber = (int?)null;
        var episodeId = CrunchyrollIdFaker.Generate();
        
        _repository
            .GetEpisodeIdByNumber(Arg.Any<CrunchyrollId>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok<CrunchyrollId?>(episodeId));
        
        //Act
        var result = await _sut.GetEpisodeIdAsync(seasonId, fileName, indexNumber, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(episodeId);
        
        await _repository
            .DidNotReceive()
            .GetEpisodeIdByName(Arg.Any<CrunchyrollId>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        
        await _repository
            .Received(1)
            .GetEpisodeIdByNumber(seasonId, episodeNumber, Arg.Any<CancellationToken>());
    }
}