using Bogus;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Domain;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.GetSpecialEpisodeCrunchyrollId;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.GetSpecialEpisodeCrunchyrollId;

public class GetSpecialEpisodeCrunchyrollIdServiceTests
{
    private readonly GetSpecialEpisodeCrunchyrollIdService _sut;
    private readonly IGetSpecialEpisodeCrunchyrollIdRepository _repository;

    private readonly Faker _faker;

    public GetSpecialEpisodeCrunchyrollIdServiceTests()
    {
        _repository = Substitute.For<IGetSpecialEpisodeCrunchyrollIdRepository>();
        _sut = new GetSpecialEpisodeCrunchyrollIdService(_repository);

        _faker = new Faker();
    }

    [Fact]
    public async Task ReturnsCrunchyrollId_WhenRepositoryFoundByName_GivenValidName()
    {
        //Arrange
        var seriesId = CrunchyrollIdFaker.Generate();
        var fileName = _faker.Random.Words(Random.Shared.Next(1, 10));
        var episodeId = CrunchyrollIdFaker.Generate();

        _repository
            .GetEpisodeIdByNameAsync(Arg.Any<CrunchyrollId>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(episodeId);

        //Act
        var result = await _sut.GetEpisodeIdAsync(seriesId, fileName, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(episodeId);
    }

    [Fact]
    public async Task ReturnsNull_WhenRepositoryFindByNameReturnsNull_GivenValidName()
    {
        //Arrange
        var seriesId = CrunchyrollIdFaker.Generate();
        var fileName = _faker.Random.Words(Random.Shared.Next(1, 10));

        _repository
            .GetEpisodeIdByNameAsync(Arg.Any<CrunchyrollId>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok<CrunchyrollId?>(null));

        //Act
        var result = await _sut.GetEpisodeIdAsync(seriesId, fileName, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task ReturnsFailed_WhenRepositoryFindByNameFailed_GivenValidName()
    {
        //Arrange
        var seriesId = CrunchyrollIdFaker.Generate();
        var fileName = _faker.Random.Words(Random.Shared.Next(1, 10));

        var error = Guid.NewGuid().ToString();
        _repository
            .GetEpisodeIdByNameAsync(Arg.Any<CrunchyrollId>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail(error));

        //Act
        var result = await _sut.GetEpisodeIdAsync(seriesId, fileName, CancellationToken.None);

        //Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.First().Message.Should().Be(error);
    }
}