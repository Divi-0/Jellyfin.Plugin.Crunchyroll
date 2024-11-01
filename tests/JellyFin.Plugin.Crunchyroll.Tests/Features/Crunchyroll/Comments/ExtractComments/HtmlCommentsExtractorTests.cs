using Bogus;
using FluentAssertions;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Comments.ExtractComments;
using JellyFin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.ExtractReviews.MockHelper;
using Microsoft.Extensions.Logging;
using RichardSzalay.MockHttp;

namespace JellyFin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.Comments.ExtractComments;

public class HtmlCommentsExtractorTests
{
    private readonly HtmlCommentsExtractor _sut;
    private readonly MockHttpMessageHandler _mockHttpMessageHandler;
    public HtmlCommentsExtractorTests()
    {
        _mockHttpMessageHandler = new MockHttpMessageHandler();
        var logger = Substitute.For<ILogger<HtmlCommentsExtractor>>();
        
        _sut = new HtmlCommentsExtractor(_mockHttpMessageHandler.ToHttpClient(), logger);
    }

    [Fact]
    public async Task ReturnsComments_WhenSuccessful_GivenValidUrl()
    {
        //Arrange
        var url = new Faker().Internet.Url();

        var mockedRequest = _mockHttpMessageHandler.MockWaybackMachineUrlHtmlCommentsResponse(url);
        
        //Act
        var result = await _sut.GetCommentsAsync(url, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        var comments = result.Value;
        comments.Should().NotBeEmpty();

        comments.Should().AllSatisfy(comment =>
        {
            comment.RepliesCount.Should().Be(0, "Replies are not readable in wayback machine");
        });
        
        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().Be(1);
    }

    [Fact]
    public async Task DeletedCommentsHaveZeroLikes_WhenSuccessful_GivenValidUrl()
    {
        //Arrange
        var url = new Faker().Internet.Url();

        var mockedRequest = _mockHttpMessageHandler.MockWaybackMachineUrlHtmlCommentsResponse(url);
        
        //Act
        var result = await _sut.GetCommentsAsync(url, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        
        var comments = result.Value;
        comments.Should().NotBeEmpty();

        comments.Where(x => x.Message == "[Kommentar gelöscht]").Should().AllSatisfy(comment =>
        {
            comment.Likes.Should().Be(0);
        });
        
        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().Be(1);
    }

    //Because text is not inside the html DOM
    [Fact]
    public async Task CommentsWithSpoilersAreIgnored_WhenSuccessful_GivenValidUrl()
    {
        //Arrange
        var url = new Faker().Internet.Url();

        var mockedRequest = _mockHttpMessageHandler.MockWaybackMachineUrlHtmlCommentsResponse(url);
        
        //Act
        var result = await _sut.GetCommentsAsync(url, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        
        var comments = result.Value;
        comments.Should().NotBeEmpty();

        comments.Should().AllSatisfy(comment =>
        {
            comment.Author.Should().NotBe("dinomarusic");
        });
        
        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().Be(1);
    }

    [Fact]
    public async Task LikesWithKAreTranslatedToThousand_WhenSuccessful_GivenValidUrl()
    {
        //Arrange
        var url = new Faker().Internet.Url();

        var mockedRequest = _mockHttpMessageHandler.MockWaybackMachineUrlHtmlCommentsResponse(url, 
            "325</button>", "71k+</button>");
        
        //Act
        var result = await _sut.GetCommentsAsync(url, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        
        var comments = result.Value;

        comments.First(x => x.Author == "abc543").Likes.Should().Be(71000);
        
        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().Be(1);
    }
}