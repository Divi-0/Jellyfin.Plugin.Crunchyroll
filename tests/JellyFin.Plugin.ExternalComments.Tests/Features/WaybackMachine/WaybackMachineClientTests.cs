using System.Net;
using AutoFixture;
using FluentAssertions;
using Jellyfin.Plugin.ExternalComments.Configuration;
using Jellyfin.Plugin.ExternalComments.Domain.Constants;
using Jellyfin.Plugin.ExternalComments.Features.WaybackMachine.Client;
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
    public async Task ReturnsArchivedSnapshot_WhenRequestingSnapshot_GivenUrlAndTimeStamp()
    {
        //Arrange
        var url = _fixture.Create<string>();
        var timeStamp = _fixture.Create<DateTime>();
        
        var (mockedRequest, availabilityResponse) = _mockHttpMessageHandler.MockGetAvailableRequest(url, timeStamp);
        
        //Act
        var response = await _sut.GetAvailabilityAsync(url, timeStamp, CancellationToken.None);

        //Assert
        response.IsSuccess.Should().BeTrue();
        response.Value.Should().NotBeNull();
        response.Value.Should().Be(availabilityResponse);
        
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
        var response = await _sut.GetAvailabilityAsync(url, timeStamp, CancellationToken.None);

        //Assert
        response.IsSuccess.Should().BeFalse();
        response.Errors.Should().Contain(x => x.Message == ErrorCodes.WaybackMachineRequestFailed);
        
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
        var response = await _sut.GetAvailabilityAsync(url, timeStamp, CancellationToken.None);

        //Assert
        response.IsSuccess.Should().BeFalse();
        response.Errors.Should().Contain(x => x.Message == ErrorCodes.WaybackMachineGetAvailabilityFailed);
        
        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().BePositive();
    }

    [Fact]
    public async Task ReturnsFailed_WhenInvalidJson_GivenUrlAndTimeStamp()
    {
        //Arrange
        var url = _fixture.Create<string>();
        var timeStamp = _fixture.Create<DateTime>();
        
        var (mockedRequest, _) = _mockHttpMessageHandler.MockGetAvailableRequest(url, timeStamp, "invalid json");
        
        //Act
        var response = await _sut.GetAvailabilityAsync(url, timeStamp, CancellationToken.None);

        //Assert
        response.IsSuccess.Should().BeFalse();
        response.Errors.Should().Contain(x => x.Message == ErrorCodes.WaybackMachineGetAvailabilityFailed);
        
        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().BePositive();
    }
}