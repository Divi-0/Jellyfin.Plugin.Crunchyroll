using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Contracts.Reviews;
using Jellyfin.Plugin.Crunchyroll.Tests.Integration.Shared;
using Jellyfin.Plugin.Crunchyroll.Tests.Integration.Shared.MockData;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared;
using Jellyfin.Plugin.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using WireMock.Client;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Integration.Tests.Crunchyroll.GetReviews;

[Collection(CollectionNames.Plugin)]
public class GetReviewsTests
{
    private readonly HttpClient _httpClient;
    private readonly IWireMockAdminApi _wireMockAdminApi;

    public GetReviewsTests(WireMockFixture wireMockFixture)
    {
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
        var mockedResponse = await _wireMockAdminApi
            .MockReviewsResponseAsync(series.ProviderIds[CrunchyrollExternalKeys.SeriesId], locacle.Name, pageNumber, pageSize);

        //Act
        var path = $"api/crunchyrollPlugin/crunchyroll/reviews/{itemId}?pageNumber={pageNumber}&pageSize={pageSize}";
        var response = await _httpClient.GetAsync(path);

        //Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var reviews = await response.Content.ReadFromJsonAsync<ReviewsResponse>();

        reviews!.Reviews.Should().Contain(x => x.Body == mockedResponse.Items[0].Review.Body);

        locacle.IsNeutralCulture.Should().BeFalse();
    }

    [Fact]
    public async Task ReturnsInternalServerError_WhenLoginRequestFails_GivenSeriesItemId()
    {
        //Arrange
        var itemId = Guid.NewGuid();
        var titleId = Guid.NewGuid().ToString();
        const int pageNumber = 1;
        const int pageSize = 10;
        
        PluginWebApplicationFactory.LibraryManagerMock.MockRetrieveItem(itemId, titleId);
        
        await _wireMockAdminApi.MockRootPageAsync();

        //Act
        var path = $"api/crunchyrollPlugin/crunchyroll/reviews/{itemId}?pageNumber={pageNumber}&pageSize={pageSize}";
        var response = await _httpClient.GetAsync(path);

        //Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }
}