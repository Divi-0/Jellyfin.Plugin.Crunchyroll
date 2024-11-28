using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Jellyfin.Plugin.Crunchyroll.Tests.Integration.Shared;
using Jellyfin.Plugin.Crunchyroll.Tests.Integration.Shared.MockData;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared;
using Jellyfin.Plugin.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Contracts.Reviews;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Avatar;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using WireMock.Client;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Integration.WaybackMachine.Tests.Crunchyroll.GetReviews;

[Collection(CollectionNames.Plugin)]
public class GetReviewsWaybackMachineTests
{
    private readonly CrunchyrollDatabaseFixture _crunchyrollDatabaseFixture;
    private readonly HttpClient _httpClient;
    private readonly IWireMockAdminApi _wireMockAdminApi;

    public GetReviewsWaybackMachineTests(WireMockFixture wireMockFixture, CrunchyrollDatabaseFixture crunchyrollDatabaseFixture)
    {
        _crunchyrollDatabaseFixture = crunchyrollDatabaseFixture;
        _httpClient = PluginWebApplicationFactory.Instance.CreateClient();
        _wireMockAdminApi = wireMockFixture.AdminApiClient;
    }

    [Fact]
    public async Task ReturnsReviews_WhenRequestReviews_GivenSeriesItemId()
    {
        //Arrange
        var itemId = Guid.NewGuid();
        var series = SeriesFaker.GenerateWithTitleId();
        const int pageNumber = 1;
        const int pageSize = 10;

        PluginWebApplicationFactory.LibraryManagerMock
            .RetrieveItem(itemId)
            .Returns(series);

        var locacle = new CultureInfo("en-US");
        
        await _wireMockAdminApi.MockRootPageAsync();
        await _wireMockAdminApi.MockAnonymousAuthAsync();

        var mockedReviews = DatabaseMockHelper.InsertRandomReviews(_crunchyrollDatabaseFixture.DbFilePath, 
            series.ProviderIds[CrunchyrollExternalKeys.SeriesId]);

        //Act
        var path = $"api/crunchyrollPlugin/crunchyroll/reviews/{itemId}?pageNumber={pageNumber}&pageSize={pageSize}";
        var response = await _httpClient.GetAsync(path);

        //Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var reviews = await response.Content.ReadFromJsonAsync<ReviewsResponse>();

        reviews!.Reviews.Should().Contain(x => x.Body == mockedReviews[0].Body);

        reviews.Reviews.Should().AllSatisfy(x =>
        {
            x.Author.AvatarUri.Should().Contain($"{Routes.Root}/{AvatarConstants.GetAvatarSubRoute}");
        });

        locacle.IsNeutralCulture.Should().BeFalse();
    }
}