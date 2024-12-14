using System.Globalization;
using System.Net;
using System.Net.Mime;
using AutoFixture;
using FluentAssertions;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.ScrapEpisodeMetadata.Client;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.ScrapEpisodeMetadata.Client.Dtos;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using RichardSzalay.MockHttp;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.ScrapEpisodeMetadata.Client;

public class ScrapEpisodeCrunchyrollClientTests
{
    private readonly Fixture _fixture;
    
    private readonly ScrapEpisodeCrunchyrollClient _sut;
    private readonly MockHttpMessageHandler _mockHttpMessageHandler;
    private readonly PluginConfiguration _config;
    private readonly ICrunchyrollSessionRepository _crunchyrollSessionRepository;

    public ScrapEpisodeCrunchyrollClientTests()
    {
        _fixture = new Fixture();
        
        _mockHttpMessageHandler = new MockHttpMessageHandler();
        _config = new PluginConfiguration();
        _crunchyrollSessionRepository = Substitute.For<ICrunchyrollSessionRepository>();
        var logger = Substitute.For<ILogger<ScrapEpisodeCrunchyrollClient>>();
        _sut = new ScrapEpisodeCrunchyrollClient(_mockHttpMessageHandler.ToHttpClient(), _config, 
            _crunchyrollSessionRepository, logger);
    }

    [Fact]
    public async Task ReturnsSeasonsResponse_WhenRequestingSeasons_GivenSeasonId()
    {
        //Arrange
        var seasonId = _fixture.Create<string>();
        var culture = new CultureInfo("en-US");
        
        var bearerToken = _fixture.Create<string>();
        _crunchyrollSessionRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(bearerToken);

        var seasonsResponse = _fixture.Create<CrunchyrollEpisodesResponse>();
        var seasonsMock = _mockHttpMessageHandler.MockCrunchyrollEpisodesResponse(seasonId, culture, 
            bearerToken, seasonsResponse);
        
        //Act
        var result = await _sut.GetEpisodesAsync(seasonId, culture, CancellationToken.None);
        
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
        var culture = new CultureInfo("en-US");
        
        //Act
        var result = await _sut.GetEpisodesAsync(titleId, culture, CancellationToken.None);
        
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
        var culture = new CultureInfo("en-US");
        
        var bearerToken = _fixture.Create<string>();
        _crunchyrollSessionRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(bearerToken);
        
        var seasonsMock = _mockHttpMessageHandler.MockCrunchyrollEpisodesResponse(titleId, culture, 
            bearerToken, HttpStatusCode.BadRequest);
        
        //Act
        var result = await _sut.GetEpisodesAsync(titleId, culture, CancellationToken.None);
        
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
        var culture = new CultureInfo("en-US");
        
        var bearerToken = _fixture.Create<string>();
        _crunchyrollSessionRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(bearerToken);
        
        var seasonsMock = _mockHttpMessageHandler.MockCrunchyrollEpisodesResponseThrows(titleId, culture, 
            bearerToken, new Exception());
        
        //Act
        var result = await _sut.GetEpisodesAsync(titleId, culture, CancellationToken.None);
        
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
        var culture = new CultureInfo("en-US");
        
        var bearerToken = _fixture.Create<string>();
        _crunchyrollSessionRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(bearerToken);
        
        var seasonsMock = _mockHttpMessageHandler.MockCrunchyrollEpisodesResponse(titleId, culture, 
            bearerToken, "invalidJson");
        
        //Act
        var result = await _sut.GetEpisodesAsync(titleId, culture, CancellationToken.None);
        
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
        var culture = new CultureInfo("en-US");
        
        var bearerToken = _fixture.Create<string>();
        _crunchyrollSessionRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(bearerToken);
        
        var seasonsMock = _mockHttpMessageHandler.MockCrunchyrollEpisodesResponse(titleId, culture, 
            bearerToken, "null");
        
        //Act
        var result = await _sut.GetEpisodesAsync(titleId, culture, CancellationToken.None);
        
        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Message == EpisodesErrorCodes.InvalidResponse);

        await _crunchyrollSessionRepository
            .Received(1)
            .GetAsync(Arg.Any<CancellationToken>());
        
        _mockHttpMessageHandler.GetMatchCount(seasonsMock).Should().Be(1);
    }

    [Fact]
    public async Task ReturnsEpisodeResponse_WhenGetEpisodeAsyncRequestingEpisode_GivenSeasonId()
    {
        //Arrange
        var episodeId = CrunchyrollIdFaker.Generate();
        var culture = new CultureInfo("en-US");
        
        var bearerToken = _fixture.Create<string>();
        _crunchyrollSessionRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(bearerToken);

        var episodeResponse = _fixture.Create<CrunchyrollEpisodeResponse>();
        var mockedRequest = _mockHttpMessageHandler
            .When($"https://www.crunchyroll.com/content/v2/cms/objects/{episodeId}?ratings=true&locale={culture.Name}")
            .WithHeaders(HeaderNames.Authorization, $"Bearer {bearerToken}")
            .Respond(MediaTypeNames.Application.Json, JsonSerializer.Serialize(episodeResponse));
        
        //Act
        var result = await _sut.GetEpisodeAsync(episodeId, culture, CancellationToken.None);
        
        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(episodeResponse.Data[0]);

        await _crunchyrollSessionRepository
            .Received(1)
            .GetAsync(Arg.Any<CancellationToken>());
        
        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().Be(1);
    }

    [Fact]
    public async Task ReturnsFailed_WhenGetEpisodeSessionReturnedEmpty_GivenSeasonId()
    {
        //Arrange
        var episodeId = CrunchyrollIdFaker.Generate();
        var culture = new CultureInfo("en-US");
        
        //Act
        var result = await _sut.GetEpisodeAsync(episodeId, culture, CancellationToken.None);
        
        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Message == EpisodesErrorCodes.NoSession);

        await _crunchyrollSessionRepository
            .Received(1)
            .GetAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnsFailed_WhenGetEpisodeRequestWasNotSuccessful_GivenSeasonId()
    {
        //Arrange
        var episodeId = _fixture.Create<string>();
        var culture = new CultureInfo("en-US");
        
        var bearerToken = _fixture.Create<string>();
        _crunchyrollSessionRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(bearerToken);
        
        var mockedRequest = _mockHttpMessageHandler
            .When($"https://www.crunchyroll.com/content/v2/cms/objects/{episodeId}?ratings=true&locale={culture.Name}")
            .WithHeaders(HeaderNames.Authorization, $"Bearer {bearerToken}")
            .Respond(HttpStatusCode.BadRequest);
        
        //Act
        var result = await _sut.GetEpisodeAsync(episodeId, culture, CancellationToken.None);
        
        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Message == EpisodesErrorCodes.RequestFailed);

        await _crunchyrollSessionRepository
            .Received(1)
            .GetAsync(Arg.Any<CancellationToken>());
        
        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().Be(1);
    }

    [Fact]
    public async Task ReturnsFailed_WhenGetEpisodeRequestThrows_GivenSeasonId()
    {
        //Arrange
        var episodeId = _fixture.Create<string>();
        var culture = new CultureInfo("en-US");
        
        var bearerToken = _fixture.Create<string>();
        _crunchyrollSessionRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(bearerToken);

        var mockedRequest = _mockHttpMessageHandler
            .When($"https://www.crunchyroll.com/content/v2/cms/objects/{episodeId}?ratings=true&locale={culture.Name}")
            .WithHeaders(HeaderNames.Authorization, $"Bearer {bearerToken}")
            .Throw(new Exception());
        
        //Act
        var result = await _sut.GetEpisodeAsync(episodeId, culture, CancellationToken.None);
        
        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Message == EpisodesErrorCodes.RequestFailed);

        await _crunchyrollSessionRepository
            .Received(1)
            .GetAsync(Arg.Any<CancellationToken>());
        
        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().Be(1);
    }
    
    [Fact]
    public async Task ReturnsFailed_WhenGetEpisodeJsonIsNotValid_GivenSeasonId()
    {
        //Arrange
        var episodeId = _fixture.Create<string>();
        var culture = new CultureInfo("en-US");
        
        var bearerToken = _fixture.Create<string>();
        _crunchyrollSessionRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(bearerToken);
        
        var mockedRequest = _mockHttpMessageHandler
            .When($"https://www.crunchyroll.com/content/v2/cms/objects/{episodeId}?ratings=true&locale={culture.Name}")
            .WithHeaders(HeaderNames.Authorization, $"Bearer {bearerToken}")
            .Respond(MediaTypeNames.Application.Json, "invalidjson");
        
        //Act
        var result = await _sut.GetEpisodeAsync(episodeId, culture, CancellationToken.None);
        
        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Message == EpisodesErrorCodes.InvalidResponse);

        await _crunchyrollSessionRepository
            .Received(1)
            .GetAsync(Arg.Any<CancellationToken>());
        
        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().Be(1);
    }
    
    [Fact]
    public async Task ReturnsFailed_WhenGetEpisodeJsonIsNull_GivenSeasonId()
    {
        //Arrange
        var episodeId = _fixture.Create<string>();
        var culture = new CultureInfo("en-US");
        
        var bearerToken = _fixture.Create<string>();
        _crunchyrollSessionRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(bearerToken);
        
        var mockedRequest = _mockHttpMessageHandler
            .When($"https://www.crunchyroll.com/content/v2/cms/objects/{episodeId}?ratings=true&locale={culture.Name}")
            .WithHeaders(HeaderNames.Authorization, $"Bearer {bearerToken}")
            .Respond(MediaTypeNames.Application.Json, "null");
        
        //Act
        var result = await _sut.GetEpisodeAsync(episodeId, culture, CancellationToken.None);
        
        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Message == EpisodesErrorCodes.InvalidResponse);

        await _crunchyrollSessionRepository
            .Received(1)
            .GetAsync(Arg.Any<CancellationToken>());
        
        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().Be(1);
    }
}