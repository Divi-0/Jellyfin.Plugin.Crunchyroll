using System.Net;
using System.Net.Sockets;
using System.Text;
using Bogus;
using FluentAssertions;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Comments.ExtractComments;
using Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.ExtractReviews.MockHelper;
using Microsoft.Extensions.Logging;
using RichardSzalay.MockHttp;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.Comments.ExtractComments;

public class HtmlCommentsExtractorTests
{
    private readonly HtmlCommentsExtractor _sut;
    private readonly MockHttpMessageHandler _mockHttpMessageHandler;
    private readonly PluginConfiguration _config;
    
    public HtmlCommentsExtractorTests()
    {
        _mockHttpMessageHandler = new MockHttpMessageHandler();
        var logger = Substitute.For<ILogger<HtmlCommentsExtractor>>();
        _config = new PluginConfiguration
        {
            WaybackMachineWaitTimeoutInSeconds = 1
        };

        _sut = new HtmlCommentsExtractor(_mockHttpMessageHandler.ToHttpClient(), logger, _config);
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

    [Theory]
    [InlineData("71k", 71000, "en-US")]
    [InlineData("5k+", 5000, "en-US")]
    [InlineData("13.4k", 13400, "en-US")]
    public async Task LikesWithKAreTranslatedToThousand_WhenSuccessful_GivenValidUrl(string likesAsString, 
        int likesAsNumber, string language)
    {
        //Arrange
        _config.CrunchyrollLanguage = language;
        
        var url = new Faker().Internet.Url();

        var mockedRequest = _mockHttpMessageHandler.MockWaybackMachineUrlHtmlCommentsResponse(url, 
            "325</button>", $"{likesAsString}</button>");
        
        //Act
        var result = await _sut.GetCommentsAsync(url, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        
        var comments = result.Value;

        comments.First(x => x.Author == "abc543").Likes.Should().Be(likesAsNumber);
        
        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().Be(1);
    }
    
    [Fact]
    public async Task RetriesAndReturnsComments_WhenRequestReturnedConnectionRefused_GivenValidUrl()
    {
        //Arrange
        var url = new Faker().Internet.Url();
        
        var canellationTokenSource = new CancellationTokenSource();
        
        var mockedRequest = _mockHttpMessageHandler
            .When(url)
            .Respond(_ =>
            {
                if (canellationTokenSource.IsCancellationRequested)
                {
                    return new HttpResponseMessage()
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(Properties.Resources.WaybackHtmlCrunchyrollComments, 
                            Encoding.UTF8, 
                            "text/html")
                    };
                }
                
                canellationTokenSource.Cancel();
                throw new HttpRequestException("error", new SocketException());
            });
        
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
        
        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().Be(2);
    }
}