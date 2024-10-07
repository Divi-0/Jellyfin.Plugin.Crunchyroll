using System.Globalization;
using System.Net;
using AutoFixture;
using FluentAssertions;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Episodes;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Episodes.Dtos;
using JellyFin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.ScrapTitleMetadata.MockHelper;
using Microsoft.Extensions.Logging;
using RichardSzalay.MockHttp;

namespace JellyFin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.ScrapTitleMetadata;

public class CrunchyrollEpisodesClientTests
{
    private readonly Fixture _fixture;
    
    private readonly CrunchyrollEpisodesClient _sut;
    private readonly MockHttpMessageHandler _mockHttpMessageHandler;
    private readonly PluginConfiguration _config;
    private readonly ICrunchyrollSessionRepository _crunchyrollSessionRepository;
    private readonly ILogger<CrunchyrollEpisodesClient> _logger;

    public CrunchyrollEpisodesClientTests()
    {
        _fixture = new Fixture();
        
        _mockHttpMessageHandler = new MockHttpMessageHandler();
        _config = new PluginConfiguration();
        _crunchyrollSessionRepository = Substitute.For<ICrunchyrollSessionRepository>();
        _logger = Substitute.For<ILogger<CrunchyrollEpisodesClient>>();
        _sut = new CrunchyrollEpisodesClient(_mockHttpMessageHandler.ToHttpClient(), _config, 
            _crunchyrollSessionRepository, _logger);
    }

    [Fact]
    public async Task ReturnsSeasonsResponse_WhenRequestingSeasons_GivenSeasonId()
    {
        //Arrange
        var seasonId = _fixture.Create<string>();
        
        var bearerToken = _fixture.Create<string>();
        _crunchyrollSessionRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(bearerToken);

        var seasonsResponse = _fixture.Create<CrunchyrollEpisodesResponse>();
        var seasonsMock = _mockHttpMessageHandler.MockCrunchyrollEpisodesResponse(seasonId, new CultureInfo(_config.CrunchyrollLanguage), 
            bearerToken, seasonsResponse);
        
        //Act
        var result = await _sut.GetEpisodesAsync(seasonId, CancellationToken.None);
        
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
        
        var seasonsMock = _mockHttpMessageHandler.MockCrunchyrollEpisodesResponse(titleId, new CultureInfo(_config.CrunchyrollLanguage), 
            bearerToken, _fixture.Create<CrunchyrollEpisodesResponse>());
        
        //Act
        var result = await _sut.GetEpisodesAsync(titleId, CancellationToken.None);
        
        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Message == EpisodesErrorCodes.NoSession);

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
        
        var seasonsMock = _mockHttpMessageHandler.MockCrunchyrollEpisodesResponse(titleId, new CultureInfo(_config.CrunchyrollLanguage), 
            bearerToken, HttpStatusCode.BadRequest);
        
        //Act
        var result = await _sut.GetEpisodesAsync(titleId, CancellationToken.None);
        
        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Message == EpisodesErrorCodes.RequestFailed);

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
        
        var seasonsMock = _mockHttpMessageHandler.MockCrunchyrollEpisodesResponseThrows(titleId, new CultureInfo(_config.CrunchyrollLanguage), 
            bearerToken, new Exception());
        
        //Act
        var result = await _sut.GetEpisodesAsync(titleId, CancellationToken.None);
        
        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Message == EpisodesErrorCodes.RequestFailed);

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
        
        var seasonsMock = _mockHttpMessageHandler.MockCrunchyrollEpisodesResponse(titleId, new CultureInfo(_config.CrunchyrollLanguage), 
            bearerToken, "invalidJson");
        
        //Act
        var result = await _sut.GetEpisodesAsync(titleId, CancellationToken.None);
        
        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Message == EpisodesErrorCodes.InvalidResponse);

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
        
        var seasonsMock = _mockHttpMessageHandler.MockCrunchyrollEpisodesResponse(titleId, new CultureInfo(_config.CrunchyrollLanguage), 
            bearerToken, "null");
        
        //Act
        var result = await _sut.GetEpisodesAsync(titleId, CancellationToken.None);
        
        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Message == EpisodesErrorCodes.InvalidResponse);

        await _crunchyrollSessionRepository
            .Received(1)
            .GetAsync(Arg.Any<CancellationToken>());
        
        _mockHttpMessageHandler.GetMatchCount(seasonsMock).Should().Be(1);
    }
}