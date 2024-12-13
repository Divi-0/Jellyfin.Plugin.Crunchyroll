using System.Text.Json;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Domain;
using Jellyfin.Plugin.Crunchyroll.Domain.Entities;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.ImageProvider.Movie.GetMovieImageInfos;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.ImageProvider.Movie.GetMovieImageInfos;

public class GetMovieImageInfosServiceTests
{
    private readonly GetMovieImageInfosService _sut;
    private IGetMovieImageInfosRepository _repository;

    public GetMovieImageInfosServiceTests()
    {
        _repository = Substitute.For<IGetMovieImageInfosRepository>();
        var logger = Substitute.For<ILogger<GetMovieImageInfosService>>();
        _sut = new GetMovieImageInfosService(_repository, logger);
    }
    
    [Fact]
    public async Task ReturnsImageInfos_WhenSuccessful_GivenMovieWithSeriesId()
    {
        //Arrange
        var movie = MovieFaker.GenerateWithCrunchyrollIds();
        var episodeId = movie.ProviderIds[CrunchyrollExternalKeys.EpisodeId];
        var titleMetadata = CrunchyrollTitleMetadataFaker.Generate(movie);
        var thumbnail = JsonSerializer.Deserialize<ImageSource>(titleMetadata.PosterTall)!;
        
        _repository
            .GetEpisodeThumbnailAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CancellationToken>())
            .Returns(thumbnail);

        //Act
        var imageInfos = await _sut.GetImageInfosAsync(movie, CancellationToken.None);

        //Assert
        imageInfos.Should().HaveCount(1);
        imageInfos.Should().ContainEquivalentOf(new RemoteImageInfo
        {
            Url = thumbnail.Uri,
            Width = thumbnail.Width,
            Height = thumbnail.Height,
            Type = ImageType.Thumb
        });

        await _repository
            .Received(1)
            .GetEpisodeThumbnailAsync(episodeId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnsEmptyImageInfos_WhenRepositoryGetEpisodeThumbnailReturnedNull_GivenMovieWithSeriesId()
    {
        //Arrange
        var movie = MovieFaker.GenerateWithCrunchyrollIds();
        var episodeId = movie.ProviderIds[CrunchyrollExternalKeys.EpisodeId];
        
        _repository
            .GetEpisodeThumbnailAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok<ImageSource?>(null));

        //Act
        var imageInfos = await _sut.GetImageInfosAsync(movie, CancellationToken.None);

        //Assert
        imageInfos.Should().BeEmpty();

        await _repository
            .Received(1)
            .GetEpisodeThumbnailAsync(episodeId, Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsEmptyImageInfos_WhenRepositoryGetEpisodeThumbnailFailed_GivenMovieWithSeriesId()
    {
        //Arrange
        var movie = MovieFaker.GenerateWithCrunchyrollIds();
        var episodeId = movie.ProviderIds[CrunchyrollExternalKeys.EpisodeId];
        
        _repository
            .GetEpisodeThumbnailAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail(Guid.NewGuid().ToString()));

        //Act
        var imageInfos = await _sut.GetImageInfosAsync(movie, CancellationToken.None);

        //Assert
        imageInfos.Should().BeEmpty();

        await _repository
            .Received(1)
            .GetEpisodeThumbnailAsync(episodeId, Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsEmptyImageInfos_WhenMovieHasNoEpisodeId_GivenMovieWithoutSeriesId()
    {
        //Arrange
        var movie = MovieFaker.Generate();
        
        //Act
        var imageInfos = await _sut.GetImageInfosAsync(movie, CancellationToken.None);

        //Assert
        imageInfos.Should().BeEmpty();

        await _repository
            .DidNotReceive()
            .GetEpisodeThumbnailAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CancellationToken>());
    }
}