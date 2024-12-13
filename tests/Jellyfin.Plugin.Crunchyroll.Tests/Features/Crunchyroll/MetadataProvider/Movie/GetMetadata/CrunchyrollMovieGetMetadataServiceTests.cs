using System.Globalization;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Domain;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Login;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.GetMetadata;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.GetMetadata.GetMovieCrunchyrollId;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.GetMetadata.ScrapMovieMetadata;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.GetMetadata.SetMetadataToMovie;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.MetadataProvider.Movie.GetMetadata;

public class CrunchyrollMovieGetMetadataServiceTests
{
    private readonly CrunchyrollMovieGetMetadataService _sut;
    private readonly IGetMovieCrunchyrollIdService _getMovieCrunchyrollIdService;
    private readonly IScrapMovieMetadataService _scrapMovieMetadataService;
    private readonly ISetMetadataToMovieService _setMetadataToMovieService;
    private readonly ILoginService _loginService;

    public CrunchyrollMovieGetMetadataServiceTests()
    {
        _getMovieCrunchyrollIdService = Substitute.For<IGetMovieCrunchyrollIdService>();
        _scrapMovieMetadataService = Substitute.For<IScrapMovieMetadataService>();
        _setMetadataToMovieService = Substitute.For<ISetMetadataToMovieService>();
        _loginService = Substitute.For<ILoginService>();
        var logger = Substitute.For<ILogger<CrunchyrollMovieGetMetadataService>>();
        _sut = new CrunchyrollMovieGetMetadataService(_getMovieCrunchyrollIdService, _scrapMovieMetadataService,
            _setMetadataToMovieService, logger, _loginService);
    }
    
    [Fact]
    public async Task ReturnsMetadata_WhenWasSuccessful_GivenMovieInfoWithoutId()
    {
        //Arrange
        var movieInfo = MovieInfoFaker.Generate();
        var seriesId = CrunchyrollIdFaker.Generate();
        var episodeId = CrunchyrollIdFaker.Generate();
        var seasonId = CrunchyrollSlugFaker.Generate();

        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _getMovieCrunchyrollIdService
            .GetCrunchyrollIdAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(new MovieCrunchyrollIdResult
            {
                SeriesId = seriesId,
                SeasonId = seasonId,
                EpisodeId = episodeId
            });

        _scrapMovieMetadataService
            .ScrapMovieMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CrunchyrollId>(), Arg.Any<CrunchyrollId>(),
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        var newMovie = MovieFaker.Generate();
        _setMetadataToMovieService
            .SetMetadataToMovieAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CrunchyrollId>(), Arg.Any<CrunchyrollId>(),
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(newMovie);

        //Act
        var metadataResult = await _sut.GetMetadataAsync(movieInfo, CancellationToken.None);
        
        //Assert
        metadataResult.HasMetadata.Should().BeTrue();
        var movieWithNewMetadata = metadataResult.Item;

        movieWithNewMetadata.Should().Be(newMovie);
        
        movieWithNewMetadata.ProviderIds.Should().Contain(x =>
            x.Key == CrunchyrollExternalKeys.SeriesId &&
            x.Value == seriesId);
        
        movieWithNewMetadata.ProviderIds.Should().Contain(x =>
            x.Key == CrunchyrollExternalKeys.SeasonId &&
            x.Value == seasonId);
        
        movieWithNewMetadata.ProviderIds.Should().Contain(x =>
            x.Key == CrunchyrollExternalKeys.EpisodeId &&
            x.Value == episodeId);

        await _loginService
            .Received(1)
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());

        await _getMovieCrunchyrollIdService
            .Received(1)
            .GetCrunchyrollIdAsync(Path.GetFileNameWithoutExtension(movieInfo.Path),
                movieInfo.GetPreferredMetadataCultureInfo(), Arg.Any<CancellationToken>());

        await _scrapMovieMetadataService
            .Received(1)
            .ScrapMovieMetadataAsync(seriesId, seasonId, episodeId,
                movieInfo.GetPreferredMetadataCultureInfo(), Arg.Any<CancellationToken>());

        await _setMetadataToMovieService
            .Received(1)
            .SetMetadataToMovieAsync(seriesId, seasonId, episodeId,
                movieInfo.GetPreferredMetadataCultureInfo(), Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsHasMetadataFalse_WhenLoginFailed_GivenMovieInfoWithoutId()
    {
        //Arrange
        var movieInfo = MovieInfoFaker.Generate();

        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Fail(Guid.NewGuid().ToString()));

        //Act
        var metadataResult = await _sut.GetMetadataAsync(movieInfo, CancellationToken.None);
        
        //Assert
        metadataResult.HasMetadata.Should().BeFalse();
        metadataResult.Item.Should().BeEquivalentTo(new MediaBrowser.Controller.Entities.Movies.Movie());

        await _loginService
            .Received(1)
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());

        await _getMovieCrunchyrollIdService
            .DidNotReceive()
            .GetCrunchyrollIdAsync(Arg.Any<string>(),
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        await _scrapMovieMetadataService
            .DidNotReceive()
            .ScrapMovieMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CrunchyrollId>(), Arg.Any<CrunchyrollId>(),
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        await _setMetadataToMovieService
            .DidNotReceive()
            .SetMetadataToMovieAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CrunchyrollId>(), Arg.Any<CrunchyrollId>(),
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsHasMetadataFalse_WhenGetCrunchyrollIdReturnedNull_GivenMovieInfoWithoutId()
    {
        //Arrange
        var movieInfo = MovieInfoFaker.Generate();

        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _getMovieCrunchyrollIdService
            .GetCrunchyrollIdAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok<MovieCrunchyrollIdResult?>(null));

        //Act
        var metadataResult = await _sut.GetMetadataAsync(movieInfo, CancellationToken.None);
        
        //Assert
        metadataResult.HasMetadata.Should().BeFalse();
        metadataResult.Item.Should().BeEquivalentTo(new MediaBrowser.Controller.Entities.Movies.Movie());

        await _loginService
            .Received(1)
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());

        await _getMovieCrunchyrollIdService
            .Received(1)
            .GetCrunchyrollIdAsync(Path.GetFileNameWithoutExtension(movieInfo.Path),
                movieInfo.GetPreferredMetadataCultureInfo(), Arg.Any<CancellationToken>());

        await _scrapMovieMetadataService
            .DidNotReceive()
            .ScrapMovieMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CrunchyrollId>(), Arg.Any<CrunchyrollId>(),
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        await _setMetadataToMovieService
            .DidNotReceive()
            .SetMetadataToMovieAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CrunchyrollId>(), Arg.Any<CrunchyrollId>(),
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsHasMetadataFalse_WhenGetCrunchyrollIdFailed_GivenMovieInfoWithoutId()
    {
        //Arrange
        var movieInfo = MovieInfoFaker.Generate();

        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _getMovieCrunchyrollIdService
            .GetCrunchyrollIdAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail(Guid.NewGuid().ToString()));

        //Act
        var metadataResult = await _sut.GetMetadataAsync(movieInfo, CancellationToken.None);
        
        //Assert
        metadataResult.HasMetadata.Should().BeFalse();
        metadataResult.Item.Should().BeEquivalentTo(new MediaBrowser.Controller.Entities.Movies.Movie());

        await _loginService
            .Received(1)
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());

        await _getMovieCrunchyrollIdService
            .Received(1)
            .GetCrunchyrollIdAsync(Path.GetFileNameWithoutExtension(movieInfo.Path),
                movieInfo.GetPreferredMetadataCultureInfo(), Arg.Any<CancellationToken>());

        await _scrapMovieMetadataService
            .DidNotReceive()
            .ScrapMovieMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CrunchyrollId>(), Arg.Any<CrunchyrollId>(),
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        await _setMetadataToMovieService
            .DidNotReceive()
            .SetMetadataToMovieAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CrunchyrollId>(), Arg.Any<CrunchyrollId>(),
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsMetadata_WhenScrapMovieMetadataFailed_GivenMovieInfoWithoutId()
    {
        //Arrange
        var movieInfo = MovieInfoFaker.Generate();
        var seriesId = CrunchyrollIdFaker.Generate();
        var episodeId = CrunchyrollIdFaker.Generate();
        var seasonId = CrunchyrollSlugFaker.Generate();

        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _getMovieCrunchyrollIdService
            .GetCrunchyrollIdAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(new MovieCrunchyrollIdResult
            {
                SeriesId = seriesId,
                SeasonId = seasonId,
                EpisodeId = episodeId
            });

        _scrapMovieMetadataService
            .ScrapMovieMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CrunchyrollId>(), Arg.Any<CrunchyrollId>(),
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail(Guid.NewGuid().ToString()));

        var newMovie = MovieFaker.Generate();
        _setMetadataToMovieService
            .SetMetadataToMovieAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CrunchyrollId>(), Arg.Any<CrunchyrollId>(),
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(newMovie);

        //Act
        var metadataResult = await _sut.GetMetadataAsync(movieInfo, CancellationToken.None);
        
        //Assert
        metadataResult.HasMetadata.Should().BeTrue();
        var movieWithNewMetadata = metadataResult.Item;

        movieWithNewMetadata.Should().Be(newMovie);
        
        movieWithNewMetadata.ProviderIds.Should().Contain(x =>
            x.Key == CrunchyrollExternalKeys.SeriesId &&
            x.Value == seriesId);
        
        movieWithNewMetadata.ProviderIds.Should().Contain(x =>
            x.Key == CrunchyrollExternalKeys.SeasonId &&
            x.Value == seasonId);
        
        movieWithNewMetadata.ProviderIds.Should().Contain(x =>
            x.Key == CrunchyrollExternalKeys.EpisodeId &&
            x.Value == episodeId);

        await _loginService
            .Received(1)
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());

        await _getMovieCrunchyrollIdService
            .Received(1)
            .GetCrunchyrollIdAsync(Path.GetFileNameWithoutExtension(movieInfo.Path),
                movieInfo.GetPreferredMetadataCultureInfo(), Arg.Any<CancellationToken>());

        await _scrapMovieMetadataService
            .Received(1)
            .ScrapMovieMetadataAsync(seriesId, seasonId, episodeId,
                movieInfo.GetPreferredMetadataCultureInfo(), Arg.Any<CancellationToken>());

        await _setMetadataToMovieService
            .Received(1)
            .SetMetadataToMovieAsync(seriesId, seasonId, episodeId,
                movieInfo.GetPreferredMetadataCultureInfo(), Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsHasMetadataFalse_WhenSetMetadataToMovieFailed_GivenMovieInfoWithoutId()
    {
        //Arrange
        var movieInfo = MovieInfoFaker.Generate();
        var seriesId = CrunchyrollIdFaker.Generate();
        var episodeId = CrunchyrollIdFaker.Generate();
        var seasonId = CrunchyrollSlugFaker.Generate();

        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _getMovieCrunchyrollIdService
            .GetCrunchyrollIdAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(new MovieCrunchyrollIdResult
            {
                SeriesId = seriesId,
                SeasonId = seasonId,
                EpisodeId = episodeId
            });

        _scrapMovieMetadataService
            .ScrapMovieMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CrunchyrollId>(), Arg.Any<CrunchyrollId>(),
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        _setMetadataToMovieService
            .SetMetadataToMovieAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CrunchyrollId>(), Arg.Any<CrunchyrollId>(),
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail(Guid.NewGuid().ToString()));

        //Act
        var metadataResult = await _sut.GetMetadataAsync(movieInfo, CancellationToken.None);
        
        //Assert
        metadataResult.HasMetadata.Should().BeFalse();
        metadataResult.Item.Should().BeEquivalentTo(new MediaBrowser.Controller.Entities.Movies.Movie());
        
        await _loginService
            .Received(1)
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());

        await _getMovieCrunchyrollIdService
            .Received(1)
            .GetCrunchyrollIdAsync(Path.GetFileNameWithoutExtension(movieInfo.Path),
                movieInfo.GetPreferredMetadataCultureInfo(), Arg.Any<CancellationToken>());

        await _scrapMovieMetadataService
            .Received(1)
            .ScrapMovieMetadataAsync(seriesId, seasonId, episodeId,
                movieInfo.GetPreferredMetadataCultureInfo(), Arg.Any<CancellationToken>());

        await _setMetadataToMovieService
            .Received(1)
            .SetMetadataToMovieAsync(seriesId, seasonId, episodeId,
                movieInfo.GetPreferredMetadataCultureInfo(), Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsMetadata_WhenMovieInfoHasIdsAlready_GivenMovieInfoWithId()
    {
        //Arrange
        var movieInfo = MovieInfoFaker.Generate();
        var seriesId = CrunchyrollIdFaker.Generate();
        var episodeId = CrunchyrollIdFaker.Generate();
        var seasonId = CrunchyrollSlugFaker.Generate();
        movieInfo.ProviderIds.Add(CrunchyrollExternalKeys.SeriesId, seriesId);
        movieInfo.ProviderIds.Add(CrunchyrollExternalKeys.SeasonId, seasonId);
        movieInfo.ProviderIds.Add(CrunchyrollExternalKeys.EpisodeId, episodeId);

        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _scrapMovieMetadataService
            .ScrapMovieMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CrunchyrollId>(), Arg.Any<CrunchyrollId>(),
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        var newMovie = MovieFaker.Generate();
        _setMetadataToMovieService
            .SetMetadataToMovieAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CrunchyrollId>(), Arg.Any<CrunchyrollId>(),
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(newMovie);

        //Act
        var metadataResult = await _sut.GetMetadataAsync(movieInfo, CancellationToken.None);
        
        //Assert
        metadataResult.HasMetadata.Should().BeTrue();
        var movieWithNewMetadata = metadataResult.Item;

        movieWithNewMetadata.Should().Be(newMovie);
        
        movieWithNewMetadata.ProviderIds.Should().Contain(x =>
            x.Key == CrunchyrollExternalKeys.SeriesId &&
            x.Value == seriesId);
        
        movieWithNewMetadata.ProviderIds.Should().Contain(x =>
            x.Key == CrunchyrollExternalKeys.SeasonId &&
            x.Value == seasonId);
        
        movieWithNewMetadata.ProviderIds.Should().Contain(x =>
            x.Key == CrunchyrollExternalKeys.EpisodeId &&
            x.Value == episodeId);

        await _loginService
            .Received(1)
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());

        await _getMovieCrunchyrollIdService
            .DidNotReceive()
            .GetCrunchyrollIdAsync(Arg.Any<string>(),
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        await _scrapMovieMetadataService
            .Received(1)
            .ScrapMovieMetadataAsync(seriesId, seasonId, episodeId,
                movieInfo.GetPreferredMetadataCultureInfo(), Arg.Any<CancellationToken>());

        await _setMetadataToMovieService
            .Received(1)
            .SetMetadataToMovieAsync(seriesId, seasonId, episodeId,
                movieInfo.GetPreferredMetadataCultureInfo(), Arg.Any<CancellationToken>());
    }
}