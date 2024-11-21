using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Login;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.Interfaces;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.SetMovieEpisodeId;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.SetMovieEpisodeId.Client;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.PostScan.SetMovieEpisodeId;

public class SetMovieEpisodeIdTaskTests
{
    private readonly SetMovieEpisodeIdTask _sut;
    private readonly IPostEpisodeIdSetTask[] _postEpisodeIdSetTasks;
    private readonly ICrunchyrollMovieEpisodeIdClient _client;
    private readonly ILoginService _loginService;
    private readonly IMediaSourceManager _mediaSourceManager;

    public SetMovieEpisodeIdTaskTests()
    {
        _postEpisodeIdSetTasks = Enumerable.Range(0, Random.Shared.Next(1, 10))
            .Select(_ => Substitute.For<IPostEpisodeIdSetTask>())
            .ToArray();
        
        var logger = Substitute.For<ILogger<SetMovieEpisodeIdTask>>();
        _client = Substitute.For<ICrunchyrollMovieEpisodeIdClient>();
        _loginService = Substitute.For<ILoginService>();
        _mediaSourceManager = MockHelper.MediaSourceManager;
        
        _sut = new SetMovieEpisodeIdTask(logger, _postEpisodeIdSetTasks, _client, _loginService);
    }
    
    [Fact]
    public async Task SetsSeriesIdAndEpisodeId_WhenWasSuccessful_GivenMovieItemWithoutId()
    {
        //Arrange
        var movie = MovieFaker.Generate();
        var crunchyrollSeriesId = CrunchyrollIdFaker.Generate();
        var crunchyrollSeriesSlug = CrunchyrollSlugFaker.Generate();
        var crunchyrollEpisodeId = CrunchyrollIdFaker.Generate();
        var crunchyrollEpisodeSlug = CrunchyrollSlugFaker.Generate();

        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        _mediaSourceManager
            .GetPathProtocol(movie.Path)
            .Returns(MediaProtocol.File);

        _client
            .SearchTitleIdAsync(movie.FileNameWithoutExtension, Arg.Any<CancellationToken>())
            .Returns(Result.Ok<SearchResponse?>(new SearchResponse
            {
                EpisodeId = crunchyrollEpisodeId,
                EpisodeSlugTitle = crunchyrollEpisodeSlug,
                SeriesId = crunchyrollSeriesId,
                SeriesSlugTitle = crunchyrollSeriesSlug
            }));

        //Act
        await _sut.RunAsync(movie, CancellationToken.None);
        
        //Assert

        foreach (var task in _postEpisodeIdSetTasks)
        {
            await task
                .Received(1)
                .RunAsync(Arg.Any<Movie>(), Arg.Any<CancellationToken>());
        }
    }
    
    [Fact]
    public async Task SetsEmptyIds_WhenNothingFound_GivenMovieItemWithoutId()
    {
        //Arrange
        var movie = MovieFaker.Generate();
        
        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _mediaSourceManager
            .GetPathProtocol(movie.Path)
            .Returns(MediaProtocol.File);
        
        _client
            .SearchTitleIdAsync(movie.FileNameWithoutExtension, Arg.Any<CancellationToken>())
            .Returns(Result.Ok<SearchResponse?>(null));

        //Act
        await _sut.RunAsync(movie, CancellationToken.None);
        
        //Assert

        foreach (var task in _postEpisodeIdSetTasks)
        {
            await task
                .Received(1)
                .RunAsync(Arg.Any<Movie>(), Arg.Any<CancellationToken>());
        }
    }
    
    [Fact]
    public async Task SkipsItem_WhenSearchFailed_GivenMovieItemWithoutId()
    {
        //Arrange
        var movie = MovieFaker.Generate();
        
        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        _mediaSourceManager
            .GetPathProtocol(movie.Path)
            .Returns(MediaProtocol.File);
        
        _client
            .SearchTitleIdAsync(movie.FileNameWithoutExtension, Arg.Any<CancellationToken>())
            .Returns(Result.Fail<SearchResponse?>("error"));

        //Act
        await _sut.RunAsync(movie, CancellationToken.None);
        
        //Assert

        foreach (var task in _postEpisodeIdSetTasks)
        {
            await task
                .DidNotReceive()
                .RunAsync(Arg.Any<Movie>(), Arg.Any<CancellationToken>());
        }
    }
    
    [Fact]
    public async Task SkipsItem_WhenLoginFailed_GivenMovieItemWithoutId()
    {
        //Arrange
        var movie = MovieFaker.Generate();

        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Fail("error"));
        
        _mediaSourceManager
            .GetPathProtocol(movie.Path)
            .Returns(MediaProtocol.File);

        //Act
        await _sut.RunAsync(movie, CancellationToken.None);
        
        //Assert
        movie.ProviderIds.Should().NotContainKey(CrunchyrollExternalKeys.SeriesId);
        movie.ProviderIds.Should().NotContainKey(CrunchyrollExternalKeys.SeriesSlugTitle);
        movie.ProviderIds.Should().NotContainKey(CrunchyrollExternalKeys.EpisodeId);
        movie.ProviderIds.Should().NotContainKey(CrunchyrollExternalKeys.EpisodeSlugTitle);

        foreach (var task in _postEpisodeIdSetTasks)
        {
            await task
                .DidNotReceive()
                .RunAsync(Arg.Any<Movie>(), Arg.Any<CancellationToken>());
        }
    }
    
    [Fact]
    public async Task DoesNothing_WhenBaseItemIsNotMovie_GivenSeriesItem()
    {
        //Arrange
        var series = SeriesFaker.Generate();

        //Act
        await _sut.RunAsync(series, CancellationToken.None);
        
        //Assert
        series.ProviderIds.Should().NotContainKey(CrunchyrollExternalKeys.SeriesId);
        series.ProviderIds.Should().NotContainKey(CrunchyrollExternalKeys.SeriesSlugTitle);
        series.ProviderIds.Should().NotContainKey(CrunchyrollExternalKeys.EpisodeId);
        series.ProviderIds.Should().NotContainKey(CrunchyrollExternalKeys.EpisodeSlugTitle);

        foreach (var task in _postEpisodeIdSetTasks)
        {
            await task
                .DidNotReceive()
                .RunAsync(Arg.Any<Movie>(), Arg.Any<CancellationToken>());
        }
    }
}