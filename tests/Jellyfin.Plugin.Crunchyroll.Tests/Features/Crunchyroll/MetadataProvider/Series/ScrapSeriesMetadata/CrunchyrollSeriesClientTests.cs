using System.Globalization;
using System.Net;
using System.Text.Json;
using AutoFixture;
using Bogus;
using FluentAssertions;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.ScrapSeriesMetadata.Client;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Series;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Series.Dtos;
using Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.ScrapTitleMetadata.MockHelper;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using RichardSzalay.MockHttp;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.MetadataProvider.Series.ScrapSeriesMetadata;

public class CrunchyrollSeriesClientTests
{
    private readonly Fixture _fixture;
    private readonly Faker _faker;
    
    private readonly CrunchyrollSeriesClient _sut;
    private readonly MockHttpMessageHandler _mockHttpMessageHandler;
    private readonly PluginConfiguration _config;
    private readonly ICrunchyrollSessionRepository _crunchyrollSessionRepository;

    public CrunchyrollSeriesClientTests()
    {
        _fixture = new Fixture();
        _faker = new Faker();
        
        _mockHttpMessageHandler = new MockHttpMessageHandler();
        _config = new PluginConfiguration();
        _crunchyrollSessionRepository = Substitute.For<ICrunchyrollSessionRepository>();
        var logger = Substitute.For<ILogger<CrunchyrollSeriesClient>>();
        _sut = new CrunchyrollSeriesClient(_mockHttpMessageHandler.ToHttpClient(), _config, 
            logger, _crunchyrollSessionRepository);
    }

    [Fact]
    public async Task GetSeriesMetadataAsync_ReturnsMetadataResponse_WhenRequestingTitle_GivenTitleId()
    {
        //Arrange
        var titleId = CrunchyrollIdFaker.Generate();
        var culture = new CultureInfo("en-US");
        
        var bearerToken = _fixture.Create<string>();
        _crunchyrollSessionRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(bearerToken);

        var seasonsResponse = _fixture.Create<CrunchyrollSeriesContentResponse>();
        var seasonsMock = _mockHttpMessageHandler.MockCrunchyrollSeriesResponse(titleId, culture, 
            bearerToken, seasonsResponse);
        
        //Act
        var result = await _sut.GetSeriesMetadataAsync(titleId, culture, CancellationToken.None);
        
        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(seasonsResponse.Data[0]);

        await _crunchyrollSessionRepository
            .Received(1)
            .GetAsync(Arg.Any<CancellationToken>());
        
        _mockHttpMessageHandler.GetMatchCount(seasonsMock).Should().Be(1);
    }

    [Fact]
    public async Task GetSeriesMetadataAsync_ReturnsFailed_WhenSessionReturnedEmpty_GivenTitleId()
    {
        //Arrange
        var titleId = CrunchyrollIdFaker.Generate();
        var culture = new CultureInfo("en-US");
        
        var bearerToken = _fixture.Create<string>();
        _crunchyrollSessionRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns((string?)null);
        
        _ = _mockHttpMessageHandler.MockCrunchyrollSeriesResponse(titleId, culture, 
            bearerToken, _fixture.Create<CrunchyrollSeriesContentResponse>());
        
        //Act
        var result = await _sut.GetSeriesMetadataAsync(titleId, culture, CancellationToken.None);
        
        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Message == SeriesErrorCodes.NoSession);

        await _crunchyrollSessionRepository
            .Received(1)
            .GetAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSeriesMetadataAsync_ReturnsFailed_WhenRequestWasNotSuccessful_GivenTitleId()
    {
        //Arrange
        var titleId = CrunchyrollIdFaker.Generate();
        var culture = new CultureInfo("en-US");
        
        var bearerToken = _fixture.Create<string>();
        _crunchyrollSessionRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(bearerToken);
        
        var seasonsMock = _mockHttpMessageHandler.MockCrunchyrollSeriesResponse(titleId, culture, 
            bearerToken, HttpStatusCode.BadRequest);
        
        //Act
        var result = await _sut.GetSeriesMetadataAsync(titleId, culture, CancellationToken.None);
        
        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Message == SeriesErrorCodes.RequestFailed);

        await _crunchyrollSessionRepository
            .Received(1)
            .GetAsync(Arg.Any<CancellationToken>());
        
        _mockHttpMessageHandler.GetMatchCount(seasonsMock).Should().Be(1);
    }

    [Fact]
    public async Task GetSeriesMetadataAsync_ReturnsFailed_WhenRequestThrows_GivenTitleId()
    {
        //Arrange
        var titleId = CrunchyrollIdFaker.Generate();
        var culture = new CultureInfo("en-US");
        
        var bearerToken = _fixture.Create<string>();
        _crunchyrollSessionRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(bearerToken);
        
        var seasonsMock = _mockHttpMessageHandler.MockCrunchyrollSeriesResponseThrows(titleId, culture, 
            bearerToken, new Exception());
        
        //Act
        var result = await _sut.GetSeriesMetadataAsync(titleId, culture, CancellationToken.None);
        
        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Message == SeriesErrorCodes.RequestFailed);

        await _crunchyrollSessionRepository
            .Received(1)
            .GetAsync(Arg.Any<CancellationToken>());
        
        _mockHttpMessageHandler.GetMatchCount(seasonsMock).Should().Be(1);
    }
    
    [Fact]
    public async Task GetSeriesMetadataAsync_ReturnsFailed_WhenJsonIsNotValid_GivenTitleId()
    {
        //Arrange
        var titleId = CrunchyrollIdFaker.Generate();
        var culture = new CultureInfo("en-US");
        
        var bearerToken = _fixture.Create<string>();
        _crunchyrollSessionRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(bearerToken);
        
        var seasonsMock = _mockHttpMessageHandler.MockCrunchyrollSeriesResponse(titleId, culture, 
            bearerToken, "invalidJson");
        
        //Act
        var result = await _sut.GetSeriesMetadataAsync(titleId, culture, CancellationToken.None);
        
        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Message == SeriesErrorCodes.InvalidResponse);

        await _crunchyrollSessionRepository
            .Received(1)
            .GetAsync(Arg.Any<CancellationToken>());
        
        _mockHttpMessageHandler.GetMatchCount(seasonsMock).Should().Be(1);
    }
    
    [Fact]
    public async Task GetSeriesMetadataAsync_ReturnsFailed_WhenJsonIsNull_GivenTitleId()
    {
        //Arrange
        var titleId = CrunchyrollIdFaker.Generate();
        var culture = new CultureInfo("en-US");
        
        var bearerToken = _fixture.Create<string>();
        _crunchyrollSessionRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(bearerToken);
        
        var seasonsMock = _mockHttpMessageHandler.MockCrunchyrollSeriesResponse(titleId, culture, 
            bearerToken, "null");
        
        //Act
        var result = await _sut.GetSeriesMetadataAsync(titleId, culture, CancellationToken.None);
        
        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Message == SeriesErrorCodes.InvalidResponse);

        await _crunchyrollSessionRepository
            .Received(1)
            .GetAsync(Arg.Any<CancellationToken>());
        
        _mockHttpMessageHandler.GetMatchCount(seasonsMock).Should().Be(1);
    }
    
    [Fact]
    public async Task GetPosterImagesAsync_ReturnsFailed_WhenRequestWasNotSuccessful_GivenCrunchyrollSeriesImage()
    {
        //Arrange
        var bearerToken = _fixture.Create<string>();
        _crunchyrollSessionRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(bearerToken);

        var url = _fixture.Create<Uri>().ToString();
        var requestMock = _mockHttpMessageHandler
            .When(url)
            .Respond(HttpStatusCode.BadRequest);
        
        //Act
        var result = await _sut.GetPosterImagesAsync(url, CancellationToken.None);
        
        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Message == SeriesErrorCodes.RequestFailed);
        
        _mockHttpMessageHandler.GetMatchCount(requestMock).Should().Be(1);
    }

    [Fact]
    public async Task GetPosterImagesAsync_ReturnsFailed_WhenRequestThrows_GivenCrunchyrollSeriesImage()
    {
        //Arrange
        var bearerToken = _fixture.Create<string>();
        _crunchyrollSessionRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(bearerToken);
        
        var url = _fixture.Create<Uri>().ToString();
        var requestMock = _mockHttpMessageHandler
            .When(url)
            .Throw(new Exception());
        
        //Act
        var result = await _sut.GetPosterImagesAsync(url, CancellationToken.None);
        
        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Message == SeriesErrorCodes.RequestFailed);
        
        _mockHttpMessageHandler.GetMatchCount(requestMock).Should().Be(1);
    }
    
    [Fact]
    public async Task GetPosterImagesAsync_ReturnsStream_WhenRequestWasSuccessful_GivenCrunchyrollSeriesImage()
    {
        //Arrange
        var bearerToken = _fixture.Create<string>();
        _crunchyrollSessionRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(bearerToken);

        var url = _fixture.Create<Uri>().ToString();
        var content = _fixture.Create<byte[]>();
        var seasonsMock = _mockHttpMessageHandler
            .When(url)
            .Respond(new StreamContent(new MemoryStream(content)));
        
        //Act
        var result = await _sut.GetPosterImagesAsync(url, CancellationToken.None);
        
        //Assert
        result.IsSuccess.Should().BeTrue();
        using var stream = new MemoryStream();
        await result.Value.CopyToAsync(stream);
        stream.ToArray().Should().BeEquivalentTo(content);
        
        _mockHttpMessageHandler.GetMatchCount(seasonsMock).Should().Be(1);
    }
    
    [Fact]
    public async Task GetRatingAsync_ReturnsAverageRating_WhenSuccess_GivenTitleId()
    {
        //Arrange
        var titleId = CrunchyrollIdFaker.Generate();
        var rating = _faker.Random.Float(max: 5);
        
        var bearerToken = _fixture.Create<string>();
        _crunchyrollSessionRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(bearerToken);
        
        var crunchyrollResponse = new CrunchyrollSeriesRatingResponse { Average = rating.ToString("0.#", CultureInfo.InvariantCulture) };
        var url = $"https://www.crunchyroll.com/content-reviews/v2/rating/series/{titleId}";
        var mockedRequest = _mockHttpMessageHandler
            .When(url)
            .WithHeaders(HeaderNames.Authorization, $"Bearer {bearerToken}")
            .Respond("application/json", JsonSerializer.Serialize(crunchyrollResponse));
        
        //Act
        var result = await _sut.GetRatingAsync(titleId, CancellationToken.None);
        
        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(float.Round(rating, 1));

        await _crunchyrollSessionRepository
            .Received(1)
            .GetAsync(Arg.Any<CancellationToken>());
        
        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().Be(1);
    }
    
    [Fact]
    public async Task GetRatingAsync_ReturnsFailed_WhenSessionReturnedEmpty_GivenTitleId()
    {
        //Arrange
        var titleId = CrunchyrollIdFaker.Generate();
        
        _crunchyrollSessionRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns((string?)null);
        
        //Act
        var result = await _sut.GetRatingAsync(titleId, CancellationToken.None);
        
        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Message == SeriesErrorCodes.NoSession);

        await _crunchyrollSessionRepository
            .Received(1)
            .GetAsync(Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task GetRatingAsync_ReturnsFailed_WhenRequestWasNotSuccessful_GivenTitleId()
    {
        //Arrange
        var titleId = CrunchyrollIdFaker.Generate();
        
        var bearerToken = _fixture.Create<string>();
        _crunchyrollSessionRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(bearerToken);
        
        var url = $"https://www.crunchyroll.com/content-reviews/v2/rating/series/{titleId}";
        var mockedRequest = _mockHttpMessageHandler
            .When(url)
            .WithHeaders(HeaderNames.Authorization, $"Bearer {bearerToken}")
            .Respond(HttpStatusCode.BadRequest);
        
        //Act
        var result = await _sut.GetRatingAsync(titleId, CancellationToken.None);
        
        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Message == SeriesErrorCodes.RequestFailed);

        await _crunchyrollSessionRepository
            .Received(1)
            .GetAsync(Arg.Any<CancellationToken>());
        
        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().Be(1);
    }
    
    [Fact]
    public async Task GetRatingAsync_ReturnsFailed_WhenRequestThrows_GivenTitleId()
    {
        //Arrange
        var titleId = CrunchyrollIdFaker.Generate();
        
        var bearerToken = _fixture.Create<string>();
        _crunchyrollSessionRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(bearerToken);
        
        var url = $"https://www.crunchyroll.com/content-reviews/v2/rating/series/{titleId}";
        var mockedRequest = _mockHttpMessageHandler
            .When(url)
            .WithHeaders(HeaderNames.Authorization, $"Bearer {bearerToken}")
            .Throw(new Exception());
        
        //Act
        var result = await _sut.GetRatingAsync(titleId, CancellationToken.None);
        
        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Message == SeriesErrorCodes.RequestFailed);

        await _crunchyrollSessionRepository
            .Received(1)
            .GetAsync(Arg.Any<CancellationToken>());
        
        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().Be(1);
    }
    
    [Fact]
    public async Task GetRatingAsync_ReturnsFailed_WhenJsonIsNotValid_GivenTitleId()
    {
        //Arrange
        var titleId = CrunchyrollIdFaker.Generate();
        
        var bearerToken = _fixture.Create<string>();
        _crunchyrollSessionRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(bearerToken);
        
        var url = $"https://www.crunchyroll.com/content-reviews/v2/rating/series/{titleId}";
        var mockedRequest = _mockHttpMessageHandler
            .When(url)
            .WithHeaders(HeaderNames.Authorization, $"Bearer {bearerToken}")
            .Respond("application/json", "invalid");
        
        //Act
        var result = await _sut.GetRatingAsync(titleId, CancellationToken.None);
        
        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Message == SeriesErrorCodes.InvalidResponse);

        await _crunchyrollSessionRepository
            .Received(1)
            .GetAsync(Arg.Any<CancellationToken>());
        
        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().Be(1);
    }
    
    [Fact]
    public async Task GetRatingAsync_ReturnsFailed_WhenJsonIsNull_GivenTitleId()
    {
        //Arrange
        var titleId = CrunchyrollIdFaker.Generate();
        
        var bearerToken = _fixture.Create<string>();
        _crunchyrollSessionRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(bearerToken);
        
        var url = $"https://www.crunchyroll.com/content-reviews/v2/rating/series/{titleId}";
        var mockedRequest = _mockHttpMessageHandler
            .When(url)
            .WithHeaders(HeaderNames.Authorization, $"Bearer {bearerToken}")
            .Respond("application/json", "null");
        
        //Act
        var result = await _sut.GetRatingAsync(titleId, CancellationToken.None);
        
        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Message == SeriesErrorCodes.InvalidResponse);

        await _crunchyrollSessionRepository
            .Received(1)
            .GetAsync(Arg.Any<CancellationToken>());
        
        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().Be(1);
    }
}