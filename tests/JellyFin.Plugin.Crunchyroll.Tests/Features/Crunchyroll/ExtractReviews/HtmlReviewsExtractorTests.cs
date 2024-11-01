using System.Net;
using AutoFixture;
using FluentAssertions;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews.ExtractReviews;
using Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.ExtractReviews.MockHelper;
using Microsoft.Extensions.Logging;
using RichardSzalay.MockHttp;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.ExtractReviews;

public class HtmlReviewsExtractorTests
{
    private readonly Fixture _fixture;
    
    private readonly HtmlReviewsExtractor _sut;
    private readonly MockHttpMessageHandler _mockHttpMessageHandler;
    private readonly PluginConfiguration _config;
    
    public HtmlReviewsExtractorTests()
    {
        _fixture = new Fixture();
        
        _mockHttpMessageHandler = new MockHttpMessageHandler();
        var logger = Substitute.For<ILogger<HtmlReviewsExtractor>>();
        _config = new PluginConfiguration();
        _sut = new HtmlReviewsExtractor(_mockHttpMessageHandler.ToHttpClient(), logger, _config);
    }

    [Fact]
    public async Task ReturnsListOfReviews_WhenExtractHtmlFromGivenHtmlResponse_GivenUrl()
    {
        //Arrange
        var url = _fixture.Create<Uri>().AbsoluteUri;

        var mockedRequest = _mockHttpMessageHandler.MockWaybackMachineUrlHtmlReviewsResponse(url);
        
        //Act
        var result = await _sut.GetReviewsAsync(url, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrEmpty();

        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().BePositive();
    }

    [Fact]
    public async Task ReturnsFailed_WhenUrlRequestIsNotSuccessfull_GivenUrl()
    {
        //Arrange
        var url = _fixture.Create<Uri>().AbsoluteUri;

        var mockedRequest = _mockHttpMessageHandler.MockWaybackMachineUrlHtmlReviewsResponseFails(url, HttpStatusCode.BadRequest);
        
        //Act
        var result = await _sut.GetReviewsAsync(url, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Message == ExtractReviewsErrorCodes.HtmlUrlRequestFailed);

        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().BePositive();
    }

    [Fact]
    public async Task ReturnsFailed_WhenHttpClientThrowsException_GivenUrl()
    {
        //Arrange
        var url = _fixture.Create<Uri>().AbsoluteUri;

        var mockedRequest = _mockHttpMessageHandler.MockWaybackMachineUrlHtmlReviewsResponseThrows(url, new TimeoutException());
        
        //Act
        var result = await _sut.GetReviewsAsync(url, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Message == ExtractReviewsErrorCodes.HtmlUrlRequestFailed);

        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().BePositive();
    }

    [Fact]
    public async Task ReturnsFailed_WhenCrunchyrollHtmlIsInvalid_GivenUrl()
    {
        //Arrange
        var url = _fixture.Create<Uri>().AbsoluteUri;

        var mockedRequest = _mockHttpMessageHandler.MockWaybackMachineUrlHtmlReviewsResponseInvalidHtml(url, new TimeoutException());
        
        //Act
        var result = await _sut.GetReviewsAsync(url, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Message == ExtractReviewsErrorCodes.HtmlExtractorInvalidCrunchyrollReviewsPage);

        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().BePositive();
    }

    [Fact]
    public async Task ReturnsEmptyCreatedAt_WhenDateCanNotBeParsed_GivenUrl()
    {
        //Arrange
        var url = _fixture.Create<Uri>().AbsoluteUri;

        var mockedRequest = _mockHttpMessageHandler.MockWaybackMachineUrlHtmlReviewsResponseInvalidDate(url);
        
        //Act
        var result = await _sut.GetReviewsAsync(url, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrEmpty();
        result.Value.Should().Contain(x => 
            x.CreatedAt.Day == 1 && 
            x.CreatedAt.Month == 1 && 
            x.CreatedAt.Year == 0001, 
            "a date cannot be parsed");

        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().BePositive();
    }
}