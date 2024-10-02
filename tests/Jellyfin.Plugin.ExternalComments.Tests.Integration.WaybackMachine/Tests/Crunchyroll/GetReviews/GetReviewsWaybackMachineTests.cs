using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Jellyfin.Plugin.ExternalComments.Common;
using Jellyfin.Plugin.ExternalComments.Configuration;
using Jellyfin.Plugin.ExternalComments.Contracts.Reviews;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Avatar;
using Jellyfin.Plugin.ExternalComments.Tests.Integration.Shared;
using Jellyfin.Plugin.ExternalComments.Tests.Integration.Shared.MockData;
using Jellyfin.Plugin.ExternalComments.Tests.Shared;
using Microsoft.Extensions.DependencyInjection;
using WireMock.Client;

namespace Jellyfin.Plugin.ExternalComments.Tests.Integration.WaybackMachine.Tests.Crunchyroll.GetReviews;

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
        var titleId = Guid.NewGuid().ToString();
        const int pageNumber = 1;
        const int pageSize = 10;

        PluginWebApplicationFactory.LibraryManagerMock.MockRetrieveItem(itemId, titleId);

        var config = ExternalCommentsPlugin.Instance!.ServiceProvider.GetRequiredService<PluginConfiguration>();
        var locacle = new CultureInfo(config.CrunchyrollLanguage);
        
        await _wireMockAdminApi.MockRootPageAsync();
        await _wireMockAdminApi.MockAnonymousAuthAsync();

        var mockedReviews = DatabaseMockHelper.InsertRandomReviews(_crunchyrollDatabaseFixture.DbFilePath, titleId);

        //Act
        var path = $"api/externalcomments/crunchyroll/reviews/{itemId}?pageNumber={pageNumber}&pageSize={pageSize}";
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