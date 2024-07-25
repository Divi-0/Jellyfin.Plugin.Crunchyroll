using System.Net;
using System.Text.Json;
using AutoFixture;
using FluentAssertions;
using Jellyfin.Plugin.ExternalComments.Configuration;
using Jellyfin.Plugin.ExternalComments.Domain.Constants;
using Jellyfin.Plugin.ExternalComments.Features.WaybackMachine;
using Jellyfin.Plugin.ExternalComments.Features.WaybackMachine.Client;
using Jellyfin.Plugin.ExternalComments.Features.WaybackMachine.Client.Dto;
using JellyFin.Plugin.ExternalComments.Tests.Features.WaybackMachine.Helper;
using Microsoft.Extensions.Logging;
using RichardSzalay.MockHttp;

namespace JellyFin.Plugin.ExternalComments.Tests.Features.WaybackMachine;

public class WaybackMachineClientTests
{
    private readonly Fixture _fixture;
    
    private readonly WaybackMachineClient _sut;
    private readonly MockHttpMessageHandler _mockHttpMessageHandler;
    private readonly PluginConfiguration _configuration;
    private readonly ILogger<WaybackMachineClient> _logger;

    public WaybackMachineClientTests()
    {
        _fixture = new Fixture();
        
        _mockHttpMessageHandler = new MockHttpMessageHandler();
        _configuration = new PluginConfiguration();
        _logger = Substitute.For<ILogger<WaybackMachineClient>>();
        _sut = new WaybackMachineClient(_mockHttpMessageHandler.ToHttpClient(), _configuration, _logger);
    }

    [Fact]
    public async Task ReturnsSearchResponse_WhenRequestingSnapshot_GivenUrlAndTimeStamp()
    {
        //Arrange
        var url = _fixture.Create<string>();
        var timeStamp = _fixture.Create<DateTime>();
        
        var (mockedRequest, searchResponses) = _mockHttpMessageHandler.MockSearchRequest(url, timeStamp);
        
        //Act
        var response = await _sut.SearchAsync(url, timeStamp, CancellationToken.None);

        //Assert
        response.IsSuccess.Should().BeTrue();
        response.Value.Should().NotBeNull();
        response.Value.Should().BeEquivalentTo(searchResponses, cfg =>
            cfg.Using<DateTime>(ctx => ctx.Subject.ToString("yyyyMMddHHmmss").Should().Be(ctx.Expectation.ToString("yyyyMMddHHmmss")))
                .WhenTypeIs<DateTime>());
        
        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().BePositive();
    }

    [Fact]
    public async Task ReturnsFailed_WhenRequestFails_GivenUrlAndTimeStamp()
    {
        //Arrange
        var url = _fixture.Create<string>();
        var timeStamp = _fixture.Create<DateTime>();
        
        var mockedRequest = _mockHttpMessageHandler.MockGetAvailableRequestFails(url, timeStamp, HttpStatusCode.BadRequest);
        
        //Act
        var response = await _sut.SearchAsync(url, timeStamp, CancellationToken.None);

        //Assert
        response.IsSuccess.Should().BeFalse();
        response.Errors.Should().Contain(x => x.Message == WaybackMachineErrorCodes.WaybackMachineRequestFailed);
        
        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().BePositive();
    }

    [Fact]
    public async Task ReturnsFailed_WhenJsonDeserializationFails_GivenUrlAndTimeStamp()
    {
        //Arrange
        var url = _fixture.Create<string>();
        var timeStamp = _fixture.Create<DateTime>();
        
        var mockedRequest = _mockHttpMessageHandler.MockGetAvailableRequestNullResponse(url, timeStamp);
        
        //Act
        var response = await _sut.SearchAsync(url, timeStamp, CancellationToken.None);

        //Assert
        response.IsSuccess.Should().BeFalse();
        response.Errors.Should().Contain(x => x.Message == WaybackMachineErrorCodes.WaybackMachineGetAvailabilityFailed);
        
        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().BePositive();
    }

    [Fact]
    public async Task ReturnsFailed_WhenInvalidJson_GivenUrlAndTimeStamp()
    {
        //Arrange
        var url = _fixture.Create<string>();
        var timeStamp = _fixture.Create<DateTime>();
        
        var (mockedRequest, _) = _mockHttpMessageHandler.MockSearchRequest(url, timeStamp, "invalid json");
        
        //Act
        var response = await _sut.SearchAsync(url, timeStamp, CancellationToken.None);

        //Assert
        response.IsSuccess.Should().BeFalse();
        response.Errors.Should().Contain(x => x.Message == WaybackMachineErrorCodes.WaybackMachineGetAvailabilityFailed);
        
        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().BePositive();
    }

    [Fact]
    public async Task ReturnsNotFound_WhenArrayIsEmpty_GivenUrlAndTimeStamp()
    {
        //Arrange
        var url = _fixture.Create<string>();
        var timeStamp = _fixture.Create<DateTime>();
        
        var (mockedRequest, _) = _mockHttpMessageHandler.MockSearchRequest(url, timeStamp, JsonSerializer.Serialize(Array.Empty<string>()));
        
        //Act
        var response = await _sut.SearchAsync(url, timeStamp, CancellationToken.None);
    
        //Assert
        response.IsSuccess.Should().BeFalse();
        response.Errors.Should().Contain(x => x.Message == WaybackMachineErrorCodes.WaybackMachineNotFound);
        
        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().BePositive();
    }

    [Fact]
    public async Task ReturnsFailed_WhenHttpClientThrows_GivenUrlAndTimeStamp()
    {
        //Arrange
        var url = _fixture.Create<string>();
        var timeStamp = _fixture.Create<DateTime>();
        
        var mockedRequest = _mockHttpMessageHandler.MockSearchRequestThrows(url, timeStamp, new TimeoutException());
        
        //Act
        var response = await _sut.SearchAsync(url, timeStamp, CancellationToken.None);
    
        //Assert
        response.IsSuccess.Should().BeFalse();
        response.Errors.Should().Contain(x => x.Message == WaybackMachineErrorCodes.WaybackMachineRequestFailed);
        
        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().BePositive();
    }
}