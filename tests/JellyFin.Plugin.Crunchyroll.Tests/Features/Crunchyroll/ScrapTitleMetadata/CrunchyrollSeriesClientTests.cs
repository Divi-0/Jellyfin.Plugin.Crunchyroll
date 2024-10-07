using System.Globalization;
using System.Net;
using AutoFixture;
using FluentAssertions;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Series;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Series.Dtos;
using JellyFin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.ScrapTitleMetadata.MockHelper;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using Microsoft.Extensions.Logging;
using RichardSzalay.MockHttp;

namespace JellyFin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.ScrapTitleMetadata;

public class CrunchyrollSeriesClientTests
{
    private readonly Fixture _fixture;
    
    private readonly CrunchyrollSeriesClient _sut;
    private readonly MockHttpMessageHandler _mockHttpMessageHandler;
    private readonly PluginConfiguration _config;
    private readonly ICrunchyrollSessionRepository _crunchyrollSessionRepository;

    public CrunchyrollSeriesClientTests()
    {
        _fixture = new Fixture();
        
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
        
        var bearerToken = _fixture.Create<string>();
        _crunchyrollSessionRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(bearerToken);

        var seasonsResponse = _fixture.Create<CrunchyrollSeriesContentResponse>();
        var seasonsMock = _mockHttpMessageHandler.MockCrunchyrollSeriesResponse(titleId, new CultureInfo(_config.CrunchyrollLanguage), 
            bearerToken, seasonsResponse);
        
        //Act
        var result = await _sut.GetSeriesMetadataAsync(titleId, CancellationToken.None);
        
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
        
        var bearerToken = _fixture.Create<string>();
        _crunchyrollSessionRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns((string?)null);
        
        _ = _mockHttpMessageHandler.MockCrunchyrollSeriesResponse(titleId, new CultureInfo(_config.CrunchyrollLanguage), 
            bearerToken, _fixture.Create<CrunchyrollSeriesContentResponse>());
        
        //Act
        var result = await _sut.GetSeriesMetadataAsync(titleId, CancellationToken.None);
        
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
        
        var bearerToken = _fixture.Create<string>();
        _crunchyrollSessionRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(bearerToken);
        
        var seasonsMock = _mockHttpMessageHandler.MockCrunchyrollSeriesResponse(titleId, new CultureInfo(_config.CrunchyrollLanguage), 
            bearerToken, HttpStatusCode.BadRequest);
        
        //Act
        var result = await _sut.GetSeriesMetadataAsync(titleId, CancellationToken.None);
        
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
        
        var bearerToken = _fixture.Create<string>();
        _crunchyrollSessionRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(bearerToken);
        
        var seasonsMock = _mockHttpMessageHandler.MockCrunchyrollSeriesResponseThrows(titleId, new CultureInfo(_config.CrunchyrollLanguage), 
            bearerToken, new Exception());
        
        //Act
        var result = await _sut.GetSeriesMetadataAsync(titleId, CancellationToken.None);
        
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
        
        var bearerToken = _fixture.Create<string>();
        _crunchyrollSessionRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(bearerToken);
        
        var seasonsMock = _mockHttpMessageHandler.MockCrunchyrollSeriesResponse(titleId, new CultureInfo(_config.CrunchyrollLanguage), 
            bearerToken, "invalidJson");
        
        //Act
        var result = await _sut.GetSeriesMetadataAsync(titleId, CancellationToken.None);
        
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
        
        var bearerToken = _fixture.Create<string>();
        _crunchyrollSessionRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(bearerToken);
        
        var seasonsMock = _mockHttpMessageHandler.MockCrunchyrollSeriesResponse(titleId, new CultureInfo(_config.CrunchyrollLanguage), 
            bearerToken, "null");
        
        //Act
        var result = await _sut.GetSeriesMetadataAsync(titleId, CancellationToken.None);
        
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
        var titleId = CrunchyrollIdFaker.Generate();
        
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
}