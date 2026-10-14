using System.Globalization;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Common.Crunchyroll.SearchDto;
using Jellyfin.Plugin.Crunchyroll.Domain;
using Jellyfin.Plugin.Crunchyroll.Domain.Constants;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Login;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Search.Client;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.GetMetadata.ScrapSeriesMetadata.Client;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.GetMetadata.ScrapSeriesMetadata.Client.Dtos;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.SearchSeries;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.MetadataProvider.Series.SearchSeries;

public class SearchSeriesServiceTests
{
    private readonly SearchSeriesService _sut;
    private readonly ILoginService _loginService;
    private readonly ICrunchyrollSearchClient _searchClient;
    private readonly ICrunchyrollSeriesClient _seriesClient;

    public SearchSeriesServiceTests()
    {
        _loginService = Substitute.For<ILoginService>();
        _searchClient = Substitute.For<ICrunchyrollSearchClient>();
        _seriesClient = Substitute.For<ICrunchyrollSeriesClient>();
        _sut = new SearchSeriesService(_loginService, _searchClient, _seriesClient);

        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
    }

    [Fact]
    public async Task ReturnsSearchResults_WhenSearchWasSuccessful_GivenSeriesInfoWithoutId()
    {
        //Arrange
        var seriesInfo = SeriesInfoFaker.Generate();
        seriesInfo.ProviderIds.Clear();

        var items = Enumerable.Range(0, Random.Shared.Next(1, 10))
            .Select(i => new CrunchyrollSearchDataItem
            {
                Id = CrunchyrollIdFaker.Generate(),
                Title = $"title-{i}",
                Description = $"description-{i}",
                SeriesMetadata = new CrunchyrollSearchDataSeriesMetadata { SeriesLaunchYear = 2000 + i },
                Images = new CrunchyrollSearchDataImages
                {
                    PosterTall =
                    [
                        [
                            new CrunchyrollSearchDataImage { Source = $"small-{i}", Width = 60 },
                            new CrunchyrollSearchDataImage { Source = $"large-{i}", Width = 1200 },
                            new CrunchyrollSearchDataImage { Source = $"medium-{i}", Width = 480 }
                        ]
                    ]
                }
            })
            .ToList();

        _searchClient
            .SearchAsync(seriesInfo.Name, "series", Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok<IReadOnlyList<CrunchyrollSearchDataItem>>(items));

        //Act
        var result = await _sut.SearchAsync(seriesInfo, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(items.Count);

        for (var i = 0; i < items.Count; i++)
        {
            var searchResult = result.Value[i];
            searchResult.Name.Should().Be(items[i].Title);
            searchResult.Overview.Should().Be(items[i].Description);
            searchResult.ProductionYear.Should().Be(2000 + i);
            searchResult.ImageUrl.Should().Be($"large-{i}");
            searchResult.SearchProviderName.Should().Be(CrunchyrollExternalKeys.ProviderName);
            searchResult.ProviderIds.Should().ContainKey(CrunchyrollExternalKeys.SeriesId)
                .WhoseValue.Should().Be(items[i].Id);
        }

        await _seriesClient
            .DidNotReceive()
            .GetSeriesMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnsSingleResult_WhenSeriesWasFound_GivenSeriesInfoWithId()
    {
        //Arrange
        var seriesInfo = SeriesInfoFaker.Generate();
        string seriesId = CrunchyrollIdFaker.Generate();
        seriesInfo.ProviderIds[CrunchyrollExternalKeys.SeriesId] = seriesId;

        _seriesClient
            .GetSeriesMetadataAsync(Arg.Is<CrunchyrollId>(x => x.ToString() == seriesId), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns(new CrunchyrollSeriesContentItem
            {
                Id = seriesId,
                Title = "title",
                SlugTitle = "slug",
                Description = "description",
                ContentProvider = "provider",
                Images = new CrunchyrollSeriesImageItem
                {
                    PosterTall =
                    [
                        [
                            new CrunchyrollSeriesImage { Source = "small", Width = 60, Height = 90, Type = "poster_tall" },
                            new CrunchyrollSeriesImage { Source = "large", Width = 1200, Height = 1800, Type = "poster_tall" }
                        ]
                    ],
                    PosterWide = [[]]
                }
            });

        //Act
        var result = await _sut.SearchAsync(seriesInfo, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        var searchResult = result.Value.Should().ContainSingle().Subject;
        searchResult.Name.Should().Be("title");
        searchResult.Overview.Should().Be("description");
        searchResult.ImageUrl.Should().Be("large");
        searchResult.ProviderIds[CrunchyrollExternalKeys.SeriesId].Should().Be(seriesId);

        await _searchClient
            .DidNotReceive()
            .SearchAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnsEmptyList_WhenNameIsEmpty_GivenSeriesInfoWithoutId()
    {
        //Arrange
        var seriesInfo = SeriesInfoFaker.Generate();
        seriesInfo.ProviderIds.Clear();
        seriesInfo.Name = string.Empty;

        //Act
        var result = await _sut.SearchAsync(seriesInfo, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();

        await _searchClient
            .DidNotReceive()
            .SearchAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnsFailed_WhenSearchFailed_GivenSeriesInfoWithoutId()
    {
        //Arrange
        var seriesInfo = SeriesInfoFaker.Generate();
        seriesInfo.ProviderIds.Clear();

        _searchClient
            .SearchAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail(ErrorCodes.CrunchyrollSearchFailed));

        //Act
        var result = await _sut.SearchAsync(seriesInfo, CancellationToken.None);

        //Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.First().Message.Should().Be(ErrorCodes.CrunchyrollSearchFailed);
    }

    [Fact]
    public async Task ReturnsFailed_WhenLoginFailed_GivenSeriesInfo()
    {
        //Arrange
        var seriesInfo = SeriesInfoFaker.Generate();

        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Fail(ErrorCodes.CrunchyrollLoginFailed));

        //Act
        var result = await _sut.SearchAsync(seriesInfo, CancellationToken.None);

        //Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.First().Message.Should().Be(ErrorCodes.CrunchyrollLoginFailed);
    }
}