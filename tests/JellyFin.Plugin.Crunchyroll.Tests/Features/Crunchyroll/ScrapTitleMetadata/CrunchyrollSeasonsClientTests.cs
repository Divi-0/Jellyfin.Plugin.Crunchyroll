using System.Globalization;
using System.Net;
using AutoFixture;
using FluentAssertions;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Seasons;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Seasons.Dtos;
using JellyFin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.ScrapTitleMetadata.MockHelper;
using Microsoft.Extensions.Logging;
using RichardSzalay.MockHttp;

namespace JellyFin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.ScrapTitleMetadata;

public class CrunchyrollSeasonsClientTests
{
    private readonly Fixture _fixture;
    
    private readonly CrunchyrollSeasonsClient _sut;
    private readonly MockHttpMessageHandler _mockHttpMessageHandler;
    private readonly PluginConfiguration _config;
    private readonly ICrunchyrollSessionRepository _crunchyrollSessionRepository;
    private readonly ILogger<CrunchyrollSeasonsClient> _logger;

    public CrunchyrollSeasonsClientTests()
    {
        _fixture = new Fixture();
        
        _mockHttpMessageHandler = new MockHttpMessageHandler();
        _config = new PluginConfiguration();
        _crunchyrollSessionRepository = Substitute.For<ICrunchyrollSessionRepository>();
        _logger = Substitute.For<ILogger<CrunchyrollSeasonsClient>>();
        _sut = new CrunchyrollSeasonsClient(_mockHttpMessageHandler.ToHttpClient(), _config, 
            _crunchyrollSessionRepository, _logger);
    }

    [Fact]
    public async Task ReturnsSeasonsResponse_WhenRequestingSeasons_GivenSeasonId()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        
        var bearerToken = _fixture.Create<string>();
        _crunchyrollSessionRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(bearerToken);

        var seasonsResponse = _fixture.Create<CrunchyrollSeasonsResponse>();
        var seasonsMock = _mockHttpMessageHandler.MockCrunchyrollSeasonsResponse(titleId, new CultureInfo(_config.CrunchyrollLanguage), 
            bearerToken, seasonsResponse);
        
        //Act
        var result = await _sut.GetSeasonsAsync(titleId, CancellationToken.None);
        
        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(seasonsResponse);

        await _crunchyrollSessionRepository
            .Received(1)
            .GetAsync(Arg.Any<CancellationToken>());
        
        _mockHttpMessageHandler.GetMatchCount(seasonsMock).Should().Be(1);
    }

    [Fact]
    public async Task ReturnsFailed_WhenSessionReturnedEmpty_GivenSeasonId()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        
        var bearerToken = _fixture.Create<string>();
        _crunchyrollSessionRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns((string?)null);
        
        var seasonsMock = _mockHttpMessageHandler.MockCrunchyrollSeasonsResponse(titleId, new CultureInfo(_config.CrunchyrollLanguage), 
            bearerToken, _fixture.Create<CrunchyrollSeasonsResponse>());
        
        //Act
        var result = await _sut.GetSeasonsAsync(titleId, CancellationToken.None);
        
        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Message == SeasonsErrorCodes.NoSession);

        await _crunchyrollSessionRepository
            .Received(1)
            .GetAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnsFailed_WhenRequestWasNotSuccessful_GivenSeasonId()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        
        var bearerToken = _fixture.Create<string>();
        _crunchyrollSessionRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(bearerToken);
        
        var seasonsMock = _mockHttpMessageHandler.MockCrunchyrollSeasonsResponse(titleId, new CultureInfo(_config.CrunchyrollLanguage), 
            bearerToken, HttpStatusCode.BadRequest);
        
        //Act
        var result = await _sut.GetSeasonsAsync(titleId, CancellationToken.None);
        
        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Message == SeasonsErrorCodes.RequestFailed);

        await _crunchyrollSessionRepository
            .Received(1)
            .GetAsync(Arg.Any<CancellationToken>());
        
        _mockHttpMessageHandler.GetMatchCount(seasonsMock).Should().Be(1);
    }

    [Fact]
    public async Task ReturnsFailed_WhenRequestThrows_GivenSeasonId()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        
        var bearerToken = _fixture.Create<string>();
        _crunchyrollSessionRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(bearerToken);
        
        var seasonsMock = _mockHttpMessageHandler.MockCrunchyrollSeasonsResponseThrows(titleId, new CultureInfo(_config.CrunchyrollLanguage), 
            bearerToken, new Exception());
        
        //Act
        var result = await _sut.GetSeasonsAsync(titleId, CancellationToken.None);
        
        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Message == SeasonsErrorCodes.RequestFailed);

        await _crunchyrollSessionRepository
            .Received(1)
            .GetAsync(Arg.Any<CancellationToken>());
        
        _mockHttpMessageHandler.GetMatchCount(seasonsMock).Should().Be(1);
    }
    
    [Fact]
    public async Task ReturnsFailed_WhenJsonIsNotValid_GivenSeasonId()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        
        var bearerToken = _fixture.Create<string>();
        _crunchyrollSessionRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(bearerToken);
        
        var seasonsMock = _mockHttpMessageHandler.MockCrunchyrollSeasonsResponse(titleId, new CultureInfo(_config.CrunchyrollLanguage), 
            bearerToken, "invalidJson");
        
        //Act
        var result = await _sut.GetSeasonsAsync(titleId, CancellationToken.None);
        
        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Message == SeasonsErrorCodes.InvalidResponse);

        await _crunchyrollSessionRepository
            .Received(1)
            .GetAsync(Arg.Any<CancellationToken>());
        
        _mockHttpMessageHandler.GetMatchCount(seasonsMock).Should().Be(1);
    }
    
    [Fact]
    public async Task ReturnsFailed_WhenJsonIsNull_GivenSeasonId()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        
        var bearerToken = _fixture.Create<string>();
        _crunchyrollSessionRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(bearerToken);
        
        var seasonsMock = _mockHttpMessageHandler.MockCrunchyrollSeasonsResponse(titleId, new CultureInfo(_config.CrunchyrollLanguage), 
            bearerToken, "null");
        
        //Act
        var result = await _sut.GetSeasonsAsync(titleId, CancellationToken.None);
        
        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Message == SeasonsErrorCodes.InvalidResponse);

        await _crunchyrollSessionRepository
            .Received(1)
            .GetAsync(Arg.Any<CancellationToken>());
        
        _mockHttpMessageHandler.GetMatchCount(seasonsMock).Should().Be(1);
    }
}