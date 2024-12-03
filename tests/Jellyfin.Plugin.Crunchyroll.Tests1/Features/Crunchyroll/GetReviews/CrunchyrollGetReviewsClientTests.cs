using System.Globalization;
using System.Net;
using AutoFixture;
using FluentAssertions;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews.GetReviews;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews.GetReviews.Client;
using Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.GetReviews.MockHelper;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Fixture;
using Microsoft.Extensions.Logging;
using RichardSzalay.MockHttp;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.GetReviews;

public class CrunchyrollGetReviewsClientTests
{
    private readonly CrunchyrollGetReviewsClient _sut;
    private readonly MockHttpMessageHandler _mockHttpMessageHandler;
    private readonly PluginConfiguration _configuration;
    private readonly ILogger<CrunchyrollGetReviewsClient> _logger;
    private readonly ICrunchyrollSessionRepository _sessionRepository;

    private readonly IFixture _fixture;
    
    public CrunchyrollGetReviewsClientTests()
    {
        _mockHttpMessageHandler = new MockHttpMessageHandler();
        _logger = Substitute.For<ILogger<CrunchyrollGetReviewsClient>>();
        _configuration = new PluginConfiguration();
        _sessionRepository = Substitute.For<ICrunchyrollSessionRepository>();

        _fixture = new Fixture()
            .Customize(new CrunchyrollReviewsResponseCustomization())
            .Customize(new CrunchyrollReviewsResponseReviewItemRatingCustomization());
        
        _sut = new CrunchyrollGetReviewsClient(
            _mockHttpMessageHandler.ToHttpClient(),
            _configuration,
            _logger,
            _sessionRepository
            );
    }

    [Fact]
    public async Task ReturnsReview_WhenRequestReviewForTitle_GivenTitleId()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        var language = new CultureInfo("en-US");
        const int pageNumber = 1;
        const int pageSize = 10;
        var bearerToken = _fixture.Create<string>();

        _sessionRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(bearerToken);

        var response = _fixture.Create<CrunchyrollReviewsResponse>();
        _mockHttpMessageHandler.MockCrunchyrollReviewsResponse(titleId, language, pageNumber, pageSize, bearerToken, response);
        
        //Act
        var result = await _sut.GetReviewsAsync(titleId, pageNumber, pageSize, language, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Reviews.Should().Contain(x => x.Title.Equals(response.Items.First().Review.Title));
    }

    [Fact]
    public async Task ReturnsFailed_WhenAuthorRatingIsNotRecognized_GivenTitleId()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        var language = new CultureInfo("en-US");
        const int pageNumber = 1;
        const int pageSize = 10;
        var bearerToken = _fixture.Create<string>();

        _sessionRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(bearerToken);

        var response = _fixture
            .Customize(new CrunchyrollReviewsResponseInvalidAuthorRatingCustomization())
            .Create<CrunchyrollReviewsResponse>();
        
        _mockHttpMessageHandler.MockCrunchyrollReviewsResponse(titleId, language, pageNumber, pageSize, bearerToken, response);
        
        //Act
        var result = await _sut.GetReviewsAsync(titleId, pageNumber, pageSize, language, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.First().Message.Should().Be(GetReviewsErrorCodes.MappingFailed);
    }

    [Fact]
    public async Task ReturnsFailed_WhenJsonSerializationFails_GivenTitleId()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        var language = new CultureInfo("en-US");
        const int pageNumber = 1;
        const int pageSize = 10;
        var bearerToken = _fixture.Create<string>();

        _sessionRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(bearerToken);
        
        _mockHttpMessageHandler.MockCrunchyrollReviewsResponse(titleId, language, pageNumber, pageSize, bearerToken, string.Empty);
        
        //Act
        var result = await _sut.GetReviewsAsync(titleId, pageNumber, pageSize, language, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.First().Message.Should().Be(GetReviewsErrorCodes.InvalidResponse);
    }

    [Fact]
    public async Task ReturnsFailed_WhenJsonSerializationReturnsNull_GivenTitleId()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        var language = new CultureInfo("en-US");
        const int pageNumber = 1;
        const int pageSize = 10;
        var bearerToken = _fixture.Create<string>();

        _sessionRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(bearerToken);
        
        _mockHttpMessageHandler.MockCrunchyrollReviewsResponse(titleId, language, pageNumber, pageSize, bearerToken, "null");
        
        //Act
        var result = await _sut.GetReviewsAsync(titleId, pageNumber, pageSize, language, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.First().Message.Should().Be(GetReviewsErrorCodes.InvalidResponse);
    }

    [Fact]
    public async Task ReturnsFailed_WhenNoSessionExists_GivenTitleId()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        const int pageNumber = 1;
        const int pageSize = 10;

        _sessionRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<string?>(null));
        
        //Act
        var result = await _sut.GetReviewsAsync(titleId, pageNumber, pageSize, new CultureInfo("en-US"), CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.First().Message.Should().Be(GetReviewsErrorCodes.NoSession);
    }

    [Fact]
    public async Task ReturnsFailed_WhenRequestWasNotSuccessful_GivenTitleId()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        var language = new CultureInfo("en-US");
        const int pageNumber = 1;
        const int pageSize = 10;
        var bearerToken = _fixture.Create<string>();

        _sessionRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<string?>(bearerToken));
        
        _mockHttpMessageHandler.MockCrunchyrollReviewsResponse(titleId, language, pageNumber, pageSize, bearerToken, HttpStatusCode.BadRequest);
        
        //Act
        var result = await _sut.GetReviewsAsync(titleId, pageNumber, pageSize, language, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.First().Message.Should().Be(GetReviewsErrorCodes.RequestFailed);
    }
}