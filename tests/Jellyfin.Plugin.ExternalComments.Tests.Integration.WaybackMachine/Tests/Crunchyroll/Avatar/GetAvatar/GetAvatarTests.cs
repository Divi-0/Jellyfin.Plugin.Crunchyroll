using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using AutoFixture;
using FluentAssertions;
using Jellyfin.Plugin.ExternalComments.Tests.Integration.Shared;
using Jellyfin.Plugin.ExternalComments.Tests.Integration.Shared.MockData;

namespace Jellyfin.Plugin.ExternalComments.Tests.Integration.WaybackMachine.Tests.Crunchyroll.Avatar.GetAvatar;

[Collection(CollectionNames.Plugin)]
public class GetAvatarTests
{
    private readonly Fixture _fixture;
    
    private readonly CrunchyrollDatabaseFixture _crunchyrollDatabaseFixture;
    private readonly HttpClient _httpClient;

    public GetAvatarTests(CrunchyrollDatabaseFixture crunchyrollDatabaseFixture)
    {
        _fixture = new Fixture();
        
        _crunchyrollDatabaseFixture = crunchyrollDatabaseFixture;
        _httpClient = PluginWebApplicationFactory.Instance.CreateClient();
    }

    [Fact]
    public async Task ReturnsImage_WhenRequestingExistingAvatar_GivenUrl()
    {
        //Arrange
        var imageUrl = _fixture.Create<Uri>().AbsoluteUri;
        
        DatabaseMockHelper.InsertAvatarImage(_crunchyrollDatabaseFixture.DbFilePath, imageUrl, 
            new MemoryStream(Encoding.UTF8.GetBytes(Properties.Resources.AvatarImageYuzu)));
        
        //Act
        var response = await _httpClient.GetAsync($"api/externalcomments/crunchyroll/avatar/{UrlEncoder.Default.Encode(imageUrl)}");
        
        //Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var actualImage = await response.Content.ReadAsStringAsync();
        actualImage.Should().Be(Properties.Resources.AvatarImageYuzu);
    }

    [Fact]
    public async Task ReturnsNotFound_WhenImageIsNotPresentInDatabase_GivenUrl()
    {
        //Arrange
        var imageUrl = _fixture.Create<Uri>().AbsoluteUri;
        
        //Act
        var response = await _httpClient.GetAsync($"api/externalcomments/crunchyroll/avatar/{UrlEncoder.Default.Encode(imageUrl)}");
        
        //Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}