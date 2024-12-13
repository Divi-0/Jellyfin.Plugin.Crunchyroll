using System.Globalization;
using Bogus;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.GetMetadata.GetMovieCrunchyrollId;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.GetMetadata.GetMovieCrunchyrollId.Client;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.MetadataProvider.Movie.GetMetadata.GetMovieCrunchyrollId;

public class GetMovieCrunchyrollIdServiceTests
{
    private readonly GetMovieCrunchyrollIdService _sut;
    private readonly ICrunchyrollMovieEpisodeIdClient _client;

    private readonly Faker _faker;
    
    public GetMovieCrunchyrollIdServiceTests()
    {
        _client = Substitute.For<ICrunchyrollMovieEpisodeIdClient>();
        _sut = new GetMovieCrunchyrollIdService(_client);

        _faker = new Faker();
    }

    [Fact]
    public async Task ReturnsResult_WhenSuccessful_GivenFilename()
    {
        //Arrange
        var fileName = _faker.Random.Words(Random.Shared.Next(1, 10));
        var language = new CultureInfo("en-US");
        var seriesId = CrunchyrollIdFaker.Generate();
        var seasonId = CrunchyrollIdFaker.Generate();
        var episodeId = CrunchyrollIdFaker.Generate();

        _client
            .SearchTitleIdAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(new SearchResponse
            {
                SeriesId = seriesId,
                SeasonId = seasonId,
                EpisodeId = episodeId,
                EpisodeSlugTitle = string.Empty,
                SeriesSlugTitle = string.Empty
            });

        //Act
        var result = await _sut.GetCrunchyrollIdAsync(fileName, language, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        var movieCrunchyrollIdResult = result.Value;
        movieCrunchyrollIdResult!.SeriesId.Should().Be(seriesId);
        movieCrunchyrollIdResult.SeasonId.Should().Be(seasonId);
        movieCrunchyrollIdResult.EpisodeId.Should().Be(episodeId);

        await _client
            .Received(1)
            .SearchTitleIdAsync(fileName, language, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnsNull_WhenClientReturnsNull_GivenFilename()
    {
        //Arrange
        var fileName = _faker.Random.Words(Random.Shared.Next(1, 10));
        var language = new CultureInfo("en-US");

        _client
            .SearchTitleIdAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok<SearchResponse?>(null));

        //Act
        var result = await _sut.GetCrunchyrollIdAsync(fileName, language, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
        
        await _client
            .Received(1)
            .SearchTitleIdAsync(fileName, language, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnsFailed_WhenClientFailed_GivenFilename()
    {
        //Arrange
        var fileName = _faker.Random.Words(Random.Shared.Next(1, 10));
        var language = new CultureInfo("en-US");

        var error = Guid.NewGuid().ToString();
        _client
            .SearchTitleIdAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail(error));

        //Act
        var result = await _sut.GetCrunchyrollIdAsync(fileName, language, CancellationToken.None);

        //Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.First().Message.Should().Be(error);
        
        await _client
            .Received(1)
            .SearchTitleIdAsync(fileName, language, Arg.Any<CancellationToken>());
    }
}