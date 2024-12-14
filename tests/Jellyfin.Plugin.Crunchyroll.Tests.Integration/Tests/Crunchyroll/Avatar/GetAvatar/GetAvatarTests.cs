using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using Bogus;
using Jellyfin.Plugin.Crunchyroll.Tests.Integration.Shared;
using Jellyfin.Plugin.Crunchyroll.Tests.Integration.Shared.MockData;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Integration.Tests.Crunchyroll.Avatar.GetAvatar;

[Collection(CollectionNames.Plugin)]
public class GetAvatarTests
{
    private readonly Fixture _fixture;
    
    private readonly HttpClient _httpClient;

    public GetAvatarTests()
    {
        _fixture = new Fixture();
        _httpClient = PluginWebApplicationFactory.Instance.CreateClient();
    }

    [Fact]
    public async Task ReturnsImage_WhenRequestingExistingAvatar_GivenUrl()
    {
        //Arrange
        var imageUrl = new Faker().Internet.UrlWithPath(fileExt: "png");
        
        DatabaseMockHelper.InsertAvatarImage(imageUrl, 
            new MemoryStream(Encoding.UTF8.GetBytes(Properties.Resources.AvatarImageYuzu)));
        
        //Act
        var response = await _httpClient.GetAsync($"api/crunchyrollPlugin/crunchyroll/avatar/{UrlEncoder.Default.Encode(imageUrl)}");
        
        //Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var actualImage = await response.Content.ReadAsStringAsync();
        actualImage.Should().Be(Properties.Resources.AvatarImageYuzu);
    }

    [Fact]
    public async Task ReturnsNotFound_WhenImageIsNotPresentInDatabase_GivenUrl()
    {
        //Arrange
        var imageUrl = new Faker().Internet.UrlWithPath(fileExt: "png");
        
        //Act
        var response = await _httpClient.GetAsync($"api/crunchyrollPlugin/crunchyroll/avatar/{UrlEncoder.Default.Encode(imageUrl)}");
        
        //Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}