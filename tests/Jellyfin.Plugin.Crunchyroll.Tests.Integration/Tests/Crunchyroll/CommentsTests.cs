using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json;
using Jellyfin.Plugin.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Contracts.Comments;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Tests.Integration.Shared;
using Jellyfin.Plugin.Crunchyroll.Tests.Integration.Shared.MockData;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using RichardSzalay.MockHttp;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Integration.Tests.Crunchyroll;

[Collection(CollectionNames.Plugin)]
public class CrunchyrollCommentsTests
{
    private readonly HttpClient _httpClient;

    public CrunchyrollCommentsTests()
    {
        _httpClient = PluginWebApplicationFactory.Instance.CreateClient();
    }

    [Fact(Skip = "Waiting for Mock/Stub Refactoring")]
    public async Task ReturnsSuccess()
    {
        //Arrange
        var titleId = Guid.NewGuid().ToString();
        var parentId = Guid.NewGuid();
        const string title = "Tokyo Ghoul";

        PluginWebApplicationFactory.LibraryManagerMock
                .GetItemById(parentId)
                .Returns(new Series()
                {
                    ProviderIds = new Dictionary<string, string>
                    {
                        { CrunchyrollExternalKeys.SeriesId, titleId }
                    }
                });

        PluginWebApplicationFactory.LibraryManagerMock
            .GetItemsResult(Arg.Is<InternalItemsQuery>(x => x.AncestorWithPresentationUniqueKey == titleId))
            .Returns(new MediaBrowser.Model.Querying.QueryResult<BaseItem>()
            {
                Items = new List<BaseItem>()
                {
                        new Series()
                        {
                            ParentId = parentId
                        }
                }
            });

        var config = CrunchyrollPlugin.Instance!.ServiceProvider.GetRequiredService<PluginConfiguration>();
        var titleUrlEncoded = UrlEncoder.Default.Encode(title);
        var mockHttp = PluginWebApplicationFactory.CrunchyrollHttpMessageHandlerMock;
        var locale = new CultureInfo("en-US");

        mockHttp.When($"https://www.crunchyroll.com/")
            .Respond(HttpStatusCode.OK);
        
        mockHttp.When($"https://www.crunchyroll.com/auth/v1/token")
            .Respond("application/json", JsonSerializer.Serialize(CrunchyrollDataMock.GetAuthResponseMock()));
        
        mockHttp.When($"https://www.crunchyroll.com/content/v2/discover/search?q={titleUrlEncoded}&n=6&type=series,movie_listing&ratings=true&locale={locale}")
            .Respond("application/json", JsonSerializer.Serialize(CrunchyrollDataMock.GetSearchResponseMock(titleId, title)));

        var crunchyrollCommentsResponse = CrunchyrollDataMock.GetCommentsResponseMock();
        mockHttp.When($"https://www.crunchyroll.com/talkbox/guestbooks/{titleId}/comments?page=1&page_size=50&order=desc&sort=popular&locale={locale}")
            .Respond("application/json", JsonSerializer.Serialize(crunchyrollCommentsResponse));
        
        //Act
        var response = await _httpClient.GetAsync($"api/crunchyrollPlugin/crunchyroll/comments/{titleId}");

        //Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var commentsResponse = await response.Content.ReadFromJsonAsync<CommentsResponse>();

        commentsResponse.Should().NotBeNull();
        commentsResponse!.Total.Should().Be(crunchyrollCommentsResponse.Items.Count);
        commentsResponse.Comments.Should().HaveSameCount(crunchyrollCommentsResponse.Items);
        commentsResponse.Comments.First().Author.Should().Be(crunchyrollCommentsResponse.Items.First().User.Attributes.Username);
        commentsResponse.Comments.First().Message.Should().Be(crunchyrollCommentsResponse.Items.First().Message);
        commentsResponse.Comments.First().Likes.Should().Be(crunchyrollCommentsResponse.Items.First().Votes.Like);
        commentsResponse.Comments.First().RepliesCount.Should().Be(crunchyrollCommentsResponse.Items.First().RepliesCount);
    }
}