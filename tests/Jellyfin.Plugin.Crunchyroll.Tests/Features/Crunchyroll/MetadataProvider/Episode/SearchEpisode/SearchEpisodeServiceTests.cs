using System.Globalization;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Common.Crunchyroll.SearchDto;
using Jellyfin.Plugin.Crunchyroll.Domain.Constants;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Login;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.SearchEpisode;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Search.Client;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.MetadataProvider.Episode.SearchEpisode;

public class SearchEpisodeServiceTests
{
    private readonly SearchEpisodeService _sut;
    private readonly ILoginService _loginService;
    private readonly ICrunchyrollSearchClient _searchClient;

    public SearchEpisodeServiceTests()
    {
        _loginService = Substitute.For<ILoginService>();
        _searchClient = Substitute.For<ICrunchyrollSearchClient>();
        _sut = new SearchEpisodeService(_loginService, _searchClient);

        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
    }

    [Fact]
    public async Task ReturnsSearchResults_WhenSearchWasSuccessful_GivenEpisodeInfoWithoutEpisodeId()
    {
        //Arrange
        var episodeInfo = EpisodeInfoFaker.Generate();
        episodeInfo.ProviderIds.Clear();

        var airDate = new DateTime(2021, 7, 30);
        var episodeItem = CreateEpisodeItem(CrunchyrollIdFaker.Generate(), airDate);

        var itemWithoutEpisodeMetadata = new CrunchyrollSearchDataItem
        {
            Id = CrunchyrollIdFaker.Generate(),
            Title = "not an episode"
        };

        _searchClient
            .SearchAsync(episodeInfo.Name, "episode", Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok<IReadOnlyList<CrunchyrollSearchDataItem>>([episodeItem, itemWithoutEpisodeMetadata]));

        //Act
        var result = await _sut.SearchAsync(episodeInfo, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        var searchResult = result.Value.Should().ContainSingle().Subject;
        searchResult.Name.Should().Be(episodeItem.Title);
        searchResult.Overview.Should().Be(episodeItem.Description);
        searchResult.IndexNumber.Should().Be(episodeItem.EpisodeMetadata!.EpisodeNumber);
        searchResult.PremiereDate.Should().Be(airDate);
        searchResult.ProductionYear.Should().Be(2021);
        searchResult.ImageUrl.Should().Be("large");
        searchResult.SearchProviderName.Should().Be(CrunchyrollExternalKeys.ProviderName);
        searchResult.ProviderIds.Should().ContainSingle();
        searchResult.ProviderIds[CrunchyrollExternalKeys.EpisodeId].Should().Be(episodeItem.Id);
    }

    [Fact]
    public async Task ReturnsEpisodesOfSameSeriesFirst_WhenSearchWasSuccessful_GivenEpisodeInfoWithSeriesId()
    {
        //Arrange
        var episodeInfo = EpisodeInfoFaker.Generate();
        episodeInfo.ProviderIds.Clear();
        var seriesId = episodeInfo.SeriesProviderIds[CrunchyrollExternalKeys.SeriesId];

        var otherSeriesItem = CreateEpisodeItem(CrunchyrollIdFaker.Generate(), null);
        var sameSeriesItem = CreateEpisodeItem(seriesId, null);
        var otherSeriesItem2 = CreateEpisodeItem(CrunchyrollIdFaker.Generate(), null);

        _searchClient
            .SearchAsync(episodeInfo.Name, "episode", Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok<IReadOnlyList<CrunchyrollSearchDataItem>>([otherSeriesItem, sameSeriesItem, otherSeriesItem2]));

        //Act
        var result = await _sut.SearchAsync(episodeInfo, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Select(x => x.ProviderIds[CrunchyrollExternalKeys.EpisodeId])
            .Should().ContainInOrder(sameSeriesItem.Id, otherSeriesItem.Id, otherSeriesItem2.Id);
    }

    [Fact]
    public async Task ReturnsSingleResultWithoutSearching_WhenEpisodeIdIsSet_GivenEpisodeInfoWithEpisodeId()
    {
        //Arrange
        var episodeInfo = EpisodeInfoFaker.Generate();
        episodeInfo.ProviderIds.Clear();
        episodeInfo.ProviderIds[CrunchyrollExternalKeys.EpisodeId] = CrunchyrollIdFaker.Generate();

        //Act
        var result = await _sut.SearchAsync(episodeInfo, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        var searchResult = result.Value.Should().ContainSingle().Subject;
        searchResult.Name.Should().Be(episodeInfo.Name);
        searchResult.IndexNumber.Should().Be(episodeInfo.IndexNumber);
        searchResult.ProviderIds.Should().BeEquivalentTo(episodeInfo.ProviderIds);

        await _searchClient
            .DidNotReceive()
            .SearchAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnsEmpty_WhenNameIsEmpty_GivenEpisodeInfoWithoutEpisodeId()
    {
        //Arrange
        var episodeInfo = EpisodeInfoFaker.Generate();
        episodeInfo.ProviderIds.Clear();
        episodeInfo.Name = string.Empty;

        //Act
        var result = await _sut.SearchAsync(episodeInfo, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();

        await _searchClient
            .DidNotReceive()
            .SearchAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnsFailed_WhenSearchFailed_GivenEpisodeInfoWithoutEpisodeId()
    {
        //Arrange
        var episodeInfo = EpisodeInfoFaker.Generate();
        episodeInfo.ProviderIds.Clear();

        _searchClient
            .SearchAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail(ErrorCodes.CrunchyrollSearchFailed));

        //Act
        var result = await _sut.SearchAsync(episodeInfo, CancellationToken.None);

        //Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.First().Message.Should().Be(ErrorCodes.CrunchyrollSearchFailed);
    }

    [Fact]
    public async Task ReturnsFailed_WhenLoginFailed_GivenEpisodeInfoWithoutEpisodeId()
    {
        //Arrange
        var episodeInfo = EpisodeInfoFaker.Generate();
        episodeInfo.ProviderIds.Clear();

        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Fail(ErrorCodes.CrunchyrollLoginFailed));

        //Act
        var result = await _sut.SearchAsync(episodeInfo, CancellationToken.None);

        //Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.First().Message.Should().Be(ErrorCodes.CrunchyrollLoginFailed);
    }

    private static CrunchyrollSearchDataItem CreateEpisodeItem(string seriesId, DateTime? airDate)
    {
        return new CrunchyrollSearchDataItem
        {
            Id = CrunchyrollIdFaker.Generate(),
            Title = "episode",
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
                Episode = "5",
                EpisodeNumber = 5,
                SequenceNumber = 5,
                SeasonId = CrunchyrollIdFaker.Generate(),
                SeriesId = seriesId,
                SeriesSlugTitle = CrunchyrollSlugFaker.Generate(),
                EpisodeAirDate = airDate
            }
        };
    }
}