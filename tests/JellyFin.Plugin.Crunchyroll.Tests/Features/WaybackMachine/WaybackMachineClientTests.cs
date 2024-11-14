using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using AutoFixture;
using FluentAssertions;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Features.WaybackMachine;
using Jellyfin.Plugin.Crunchyroll.Features.WaybackMachine.Client;
using Jellyfin.Plugin.Crunchyroll.Features.WaybackMachine.Client.Dto;
using Jellyfin.Plugin.Crunchyroll.Features.WaybackMachine.Helper;
using Jellyfin.Plugin.Crunchyroll.Tests.Features.WaybackMachine.Helper;
using Microsoft.Extensions.Logging;
using RichardSzalay.MockHttp;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.WaybackMachine;

public class WaybackMachineClientTests
{
    private readonly Fixture _fixture;
    
    private readonly WaybackMachineClient _sut;
    private readonly MockHttpMessageHandler _mockHttpMessageHandler;

    public WaybackMachineClientTests()
    {
        _fixture = new Fixture();
        
        _mockHttpMessageHandler = new MockHttpMessageHandler();
        var config = new PluginConfiguration();
        var logger = Substitute.For<ILogger<WaybackMachineClient>>();
        _sut = new WaybackMachineClient(_mockHttpMessageHandler.ToHttpClient(), config, logger);

        config.WaybackMachineWaitTimeoutInSeconds = 1;
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
            cfg.Using<DateTime>(ctx => 
                    ctx.Subject.ToString(WaybackMachineTimestampHelper.ResponseTimestampFormat)
                        .Should().Be(ctx.Expectation.ToString(WaybackMachineTimestampHelper.ResponseTimestampFormat)))
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
    public async Task ReturnsEmptyArray_WhenResponseArrayIsEmpty_GivenUrlAndTimeStamp()
    {
        //Arrange
        var url = _fixture.Create<string>();
        var timeStamp = _fixture.Create<DateTime>();
        
        var (mockedRequest, _) = _mockHttpMessageHandler.MockSearchRequest(url, timeStamp, JsonSerializer.Serialize(Array.Empty<string>()));
        
        //Act
        var response = await _sut.SearchAsync(url, timeStamp, CancellationToken.None);
    
        //Assert
        response.IsSuccess.Should().BeTrue();
        response.Value.Should().BeEmpty();
        
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
    
    [Fact]
    public async Task RetriesAndReturnsSearchResponse_WhenRequestReturnedConnectionRefused_GivenUrlAndTimeStamp()
    {
        //Arrange
        var url = _fixture.Create<string>();
        var timeStamp = _fixture.Create<DateTime>();
        
        var searchResponses = _fixture.CreateMany<SearchResponse>().ToList();

        string[][] searchReponseArray = [
            ["", "", ""], 
            [
                searchResponses[0].Timestamp.ToString(WaybackMachineTimestampHelper.ResponseTimestampFormat),
                searchResponses[0].MimeType,
                searchResponses[0].Status
            ],
            [
                searchResponses[1].Timestamp.ToString(WaybackMachineTimestampHelper.ResponseTimestampFormat),
                searchResponses[1].MimeType,
                searchResponses[1].Status
            ],
            [
                searchResponses[2].Timestamp.ToString(WaybackMachineTimestampHelper.ResponseTimestampFormat),
                searchResponses[2].MimeType,
                searchResponses[2].Status
            ],
        ];
        
        var canellationTokenSource = new CancellationTokenSource();
        
        var mockedRequest = _mockHttpMessageHandler
            .When($"http://web.archive.org/cdx/search/cdx?url={url}&output=json&limit=-5" +
                  $"&to={timeStamp.ToString(WaybackMachineTimestampHelper.RequestTimestampFormat)}" +
                  $"&fastLatest=true&fl=timestamp,mimetype,statuscode")
            .Respond(_ =>
            {
                if (canellationTokenSource.IsCancellationRequested)
                {
                    return new HttpResponseMessage()
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = JsonContent.Create(searchReponseArray)
                    };
                }
                
                canellationTokenSource.Cancel();
                throw new HttpRequestException("error", new SocketException());
            });
        
        //Act
        var response = await _sut.SearchAsync(url, timeStamp, CancellationToken.None);

        //Assert
        response.IsSuccess.Should().BeTrue();
        response.Value.Should().NotBeNull();
        response.Value.Should().BeEquivalentTo(searchResponses, cfg =>
            cfg.Using<DateTime>(ctx => 
                    ctx.Subject.ToString(WaybackMachineTimestampHelper.ResponseTimestampFormat)
                        .Should().Be(ctx.Expectation.ToString(WaybackMachineTimestampHelper.ResponseTimestampFormat)))
                .WhenTypeIs<DateTime>());
        
        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().Be(2);
    }
}