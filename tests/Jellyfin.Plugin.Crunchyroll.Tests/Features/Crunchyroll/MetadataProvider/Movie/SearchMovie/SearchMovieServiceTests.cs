using System.Globalization;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Common.Crunchyroll.SearchDto;
using Jellyfin.Plugin.Crunchyroll.Domain.Constants;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Login;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.SearchMovie;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Search.Client;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.MetadataProvider.Movie.SearchMovie;

public class SearchMovieServiceTests
{
    private readonly SearchMovieService _sut;
    private readonly ILoginService _loginService;
    private readonly ICrunchyrollSearchClient _searchClient;

    public SearchMovieServiceTests()
    {
        _loginService = Substitute.For<ILoginService>();
        _searchClient = Substitute.For<ICrunchyrollSearchClient>();
        _sut = new SearchMovieService(_loginService, _searchClient);

        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
    }

    [Fact]
    public async Task ReturnsSearchResults_WhenSearchWasSuccessful_GivenMovieInfoWithoutIds()
    {
        //Arrange
        var movieInfo = MovieInfoFaker.Generate();
        movieInfo.ProviderIds.Clear();

        var airDate = new DateTime(2021, 7, 30);
        var episodeItem = new CrunchyrollSearchDataItem
        {
            Id = CrunchyrollIdFaker.Generate(),
            Title = "movie",
            Description = "description",
            Images = new CrunchyrollSearchDataImages
            {
                Thumbnail =
                [
                    [
                        new CrunchyrollSearchDataImage { Source = "small", Width = 60 },
                        new CrunchyrollSearchDataImage { Source = "large", Width = 1920 }
                    ]
                ]
            },
            EpisodeMetadata = new CrunchyrollSearchDataEpisodeMetadata
            {
                Episode = string.Empty,
                EpisodeNumber = null,
                SequenceNumber = 1,
                SeasonId = CrunchyrollIdFaker.Generate(),
                SeriesId = CrunchyrollIdFaker.Generate(),
                SeriesSlugTitle = CrunchyrollSlugFaker.Generate(),
                EpisodeAirDate = airDate
            }
        };

        var itemWithoutEpisodeMetadata = new CrunchyrollSearchDataItem
        {
            Id = CrunchyrollIdFaker.Generate(),
            Title = "not an episode"
        };

        _searchClient
            .SearchAsync(movieInfo.Name, "episode", Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok<IReadOnlyList<CrunchyrollSearchDataItem>>([episodeItem, itemWithoutEpisodeMetadata]));

        //Act
        var result = await _sut.SearchAsync(movieInfo, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        var searchResult = result.Value.Should().ContainSingle().Subject;
        searchResult.Name.Should().Be(episodeItem.Title);
        searchResult.Overview.Should().Be(episodeItem.Description);
        searchResult.PremiereDate.Should().Be(airDate);
        searchResult.ProductionYear.Should().Be(2021);
        searchResult.ImageUrl.Should().Be("large");
        searchResult.SearchProviderName.Should().Be(CrunchyrollExternalKeys.ProviderName);
        searchResult.ProviderIds[CrunchyrollExternalKeys.SeriesId].Should().Be(episodeItem.EpisodeMetadata.SeriesId);
        searchResult.ProviderIds[CrunchyrollExternalKeys.SeasonId].Should().Be(episodeItem.EpisodeMetadata.SeasonId);
        searchResult.ProviderIds[CrunchyrollExternalKeys.EpisodeId].Should().Be(episodeItem.Id);
    }

    [Fact]
    public async Task ReturnsSingleResultWithoutSearching_WhenAllIdsAreSet_GivenMovieInfoWithIds()
    {
        //Arrange
        var movieInfo = MovieInfoFaker.Generate();
        movieInfo.ProviderIds[CrunchyrollExternalKeys.SeriesId] = CrunchyrollIdFaker.Generate();
        movieInfo.ProviderIds[CrunchyrollExternalKeys.SeasonId] = CrunchyrollIdFaker.Generate();
        movieInfo.ProviderIds[CrunchyrollExternalKeys.EpisodeId] = CrunchyrollIdFaker.Generate();

        //Act
        var result = await _sut.SearchAsync(movieInfo, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        var searchResult = result.Value.Should().ContainSingle().Subject;
        searchResult.ProviderIds.Should().BeEquivalentTo(movieInfo.ProviderIds);

        await _searchClient
            .DidNotReceive()
            .SearchAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnsFailed_WhenSearchFailed_GivenMovieInfoWithoutIds()
    {
        //Arrange
        var movieInfo = MovieInfoFaker.Generate();
        movieInfo.ProviderIds.Clear();

        _searchClient
            .SearchAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail(ErrorCodes.CrunchyrollSearchFailed));

        //Act
        var result = await _sut.SearchAsync(movieInfo, CancellationToken.None);

        //Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.First().Message.Should().Be(ErrorCodes.CrunchyrollSearchFailed);
    }

    [Fact]
    public async Task ReturnsFailed_WhenLoginFailed_GivenMovieInfoWithoutIds()
    {
        //Arrange
        var movieInfo = MovieInfoFaker.Generate();
        movieInfo.ProviderIds.Clear();

        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Fail(ErrorCodes.CrunchyrollLoginFailed));

        //Act
        var result = await _sut.SearchAsync(movieInfo, CancellationToken.None);

        //Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.First().Message.Should().Be(ErrorCodes.CrunchyrollLoginFailed);
    }
}