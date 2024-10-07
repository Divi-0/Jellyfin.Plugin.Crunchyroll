using System.Net.Http.Json;
using FluentAssertions;
using Jellyfin.Plugin.Crunchyroll.Tests.Integration.Shared;
using Jellyfin.Plugin.Crunchyroll.Tests.Integration.Shared.MockData;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using Jellyfin.Plugin.Crunchyroll.Contracts.Comments;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Integration.WaybackMachine.Tests.Crunchyroll.GetComments;

[Collection(CollectionNames.Plugin)]
public class GetCommentsTests
{
    private readonly CrunchyrollDatabaseFixture _crunchyrollDatabaseFixture;
    private readonly HttpClient _httpClient;
    private readonly ILibraryManager _libraryManager;

    public GetCommentsTests(CrunchyrollDatabaseFixture crunchyrollDatabaseFixture)
    {
        _crunchyrollDatabaseFixture = crunchyrollDatabaseFixture;
        _httpClient = PluginWebApplicationFactory.Instance.CreateClient();
        
        _libraryManager =
            PluginWebApplicationFactory.Instance.Services.GetRequiredService<ILibraryManager>();
    }

    [Fact]
    public async Task ReturnsComments_WhenWaybackMachineIsEnabled_GivenCommentsInDatabase()
    {
        //Arrange
        var episode = EpisodeFaker.GenerateWithEpisodeId();

        _libraryManager
            .RetrieveItem(episode.Id)
            .Returns(episode);
        
        var comments = DatabaseMockHelper.InsertRandomComments(
            _crunchyrollDatabaseFixture.DbFilePath, 
            episode.ProviderIds[CrunchyrollExternalKeys.EpisodeId]);
        
        //Act
        var response = await _httpClient.GetAsync($"api/crunchyrollPlugin/crunchyroll/comments/{episode.Id}");
        
        //Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var commentsResponse = await response.Content.ReadFromJsonAsync<CommentsResponse>();

        commentsResponse.Should().NotBeNull();
        commentsResponse!.Comments.Should().BeEquivalentTo(comments);
    }

    [Fact]
    public async Task ReturnsEmpty_WhenWaybackMachineIsEnabled_GivenCommentsInDatabase()
    {
        //Arrange
        var episode = EpisodeFaker.GenerateWithEpisodeId();

        _libraryManager
            .RetrieveItem(episode.Id)
            .Returns(episode);
        
        //Act
        var response = await _httpClient.GetAsync($"api/crunchyrollPlugin/crunchyroll/comments/{episode.Id}");
        
        //Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var commentsResponse = await response.Content.ReadFromJsonAsync<CommentsResponse>();

        commentsResponse.Should().NotBeNull();
        commentsResponse!.Comments.Should().BeEmpty();
        commentsResponse.Total.Should().Be(0);
    }
}