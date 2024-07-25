using System.Net;
using AutoFixture;
using FluentAssertions;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Reviews.ExtractReviews;
using Jellyfin.Plugin.ExternalComments.Features.WaybackMachine;
using JellyFin.Plugin.ExternalComments.Tests.Features.Crunchyroll.ExtractReviews.MockHelper;
using Microsoft.Extensions.Logging;
using RichardSzalay.MockHttp;

namespace JellyFin.Plugin.ExternalComments.Tests.Features.Crunchyroll.ExtractReviews;

public class HtmlReviewsExtractorTests
{
    private readonly Fixture _fixture;
    
    private readonly HtmlReviewsExtractor _sut;
    private readonly MockHttpMessageHandler _mockHttpMessageHandler;
    
    public HtmlReviewsExtractorTests()
    {
        _fixture = new Fixture();
        
        _mockHttpMessageHandler = new MockHttpMessageHandler();
        var logger = Substitute.For<ILogger<HtmlReviewsExtractor>>();
        _sut = new HtmlReviewsExtractor(_mockHttpMessageHandler.ToHttpClient(), logger);
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
}