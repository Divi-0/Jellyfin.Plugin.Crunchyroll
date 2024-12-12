using System.Text.Json;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Domain;
using Jellyfin.Plugin.Crunchyroll.Domain.Entities;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.ImageProvider.Episode.GetEpisodeImageInfos;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.ImageProvider.Episode.GetEpisodeImageInfos;

public class GetEpisodeImageInfosServiceTests
{
    private readonly GetEpisodeImageInfosService _sut;
    private readonly IGetEpisodeImageInfosRepository _repository;

    public GetEpisodeImageInfosServiceTests()
    {
        _repository = Substitute.For<IGetEpisodeImageInfosRepository>();
        var logger = Substitute.For<ILogger<GetEpisodeImageInfosService>>();
        _sut = new GetEpisodeImageInfosService(_repository, logger);
    }
    
    [Fact]
    public async Task ReturnsImageInfos_WhenSuccessful_GivenEpisodeWithEpisodeId()
    {
        //Arrange
        var episode = EpisodeFaker.GenerateWithEpisodeId();
        var episodeId = episode.ProviderIds[CrunchyrollExternalKeys.EpisodeId];
        var crunchyrollEpisode = CrunchyrollEpisodeFaker.Generate();
        var thumbnail = JsonSerializer.Deserialize<ImageSource>(crunchyrollEpisode.Thumbnail)!;
        
        _repository
            .GetEpisodeAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CancellationToken>())
            .Returns(crunchyrollEpisode);

        //Act
        var imageInfos = await _sut.GetImageInfosAsync(episode, CancellationToken.None);

        //Assert
        imageInfos.Should().HaveCount(2);
        imageInfos.Should().ContainEquivalentOf(new RemoteImageInfo
        {
            Url = thumbnail.Uri,
            Width = thumbnail.Width,
            Height = thumbnail.Height,
            Type = ImageType.Primary
        });
        imageInfos.Should().ContainEquivalentOf(new RemoteImageInfo
        {
            Url = thumbnail.Uri,
            Width = thumbnail.Width,
            Height = thumbnail.Height,
            Type = ImageType.Thumb
        });

        await _repository
            .Received(1)
            .GetEpisodeAsync(episodeId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnsEmptyImageInfos_WhenRepositoryGetEpisodeReturnedNull_GivenEpisodeWithEpisodeId()
    {
        //Arrange
        var episode = EpisodeFaker.GenerateWithEpisodeId();
        var episodeId = episode.ProviderIds[CrunchyrollExternalKeys.EpisodeId];
        
        _repository
            .GetEpisodeAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok<Domain.Entities.Episode?>(null));

        //Act
        var imageInfos = await _sut.GetImageInfosAsync(episode, CancellationToken.None);

        //Assert
        imageInfos.Should().BeEmpty();

        await _repository
            .Received(1)
            .GetEpisodeAsync(episodeId, Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsEmptyImageInfos_WhenRepositoryGetEpisodeFails_GivenEpisodeWithEpisodeId()
    {
        //Arrange
        var episode = EpisodeFaker.GenerateWithEpisodeId();
        var episodeId = episode.ProviderIds[CrunchyrollExternalKeys.EpisodeId];
        
        _repository
            .GetEpisodeAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail(Guid.NewGuid().ToString()));

        //Act
        var imageInfos = await _sut.GetImageInfosAsync(episode, CancellationToken.None);

        //Assert
        imageInfos.Should().BeEmpty();

        await _repository
            .Received(1)
            .GetEpisodeAsync(episodeId, Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsEmptyImageInfos_WhenEpisodeHasNoEpisodeId_GivenEpisodeWithEpisodeId()
    {
        //Arrange
        var episode = EpisodeFaker.Generate();

        //Act
        var imageInfos = await _sut.GetImageInfosAsync(episode, CancellationToken.None);

        //Assert
        imageInfos.Should().BeEmpty();

        await _repository
            .DidNotReceive()
            .GetEpisodeAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CancellationToken>());
    }
}