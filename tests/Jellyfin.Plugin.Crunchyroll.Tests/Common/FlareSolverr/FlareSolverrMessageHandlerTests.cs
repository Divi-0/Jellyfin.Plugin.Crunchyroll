using System.Net;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json;
using Jellyfin.Plugin.Crunchyroll.Common.FlareSolverr;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Microsoft.Net.Http.Headers;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Common.FlareSolverr;

public class FlareSolverrMessageHandlerTests
{
    private readonly FlareSolverrMessageHandlerTestWrapper _sut;
    private readonly ReceiverMessageHandler _receiver;
    private readonly Bogus.Faker _faker;
    private readonly PluginConfiguration _configuration;

    public FlareSolverrMessageHandlerTests()
    {
        _faker = new Bogus.Faker();
        
        _receiver = new ReceiverMessageHandler();
        _configuration = new PluginConfiguration
        {
            FlareSolverrUrl = _faker.Internet.Url(),
            FlareSolverrMitmProxyUrl = _faker.Internet.Url()
        };
        _sut = new FlareSolverrMessageHandlerTestWrapper(_configuration)
        {
            InnerHandler = _receiver
        };
    }

    public static TheoryData<string> SuccessTestUrls()
    {
        var faker = new Bogus.Faker();
        return [faker.Internet.UrlWithPath() + "?abc=123", faker.Internet.UrlWithPath()];
    }

    
    [Theory]
    [MemberData(nameof(SuccessTestUrls))]
    public async Task GivenRequest_WhenSuccess_ThenCallFlareSolverrApi(string url)
    {
        //Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Headers = { {HeaderNames.Authorization, Guid.NewGuid().ToString()}, {"blabla", Guid.NewGuid().ToString()} },
            Content = new StringContent(_faker.Lorem.Sentences())
        };

        var expectedResponse = JsonSerializer.Serialize(new { Value = _faker.Lorem.Sentences() });
        _receiver.Response = expectedResponse;
        
        //Act
        var response = await _sut.SendWrapperAsync(request, CancellationToken.None);

        //Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        
        _receiver.CallCount.Should().Be(1);

        var uriBuilder = new UriBuilder(request.RequestUri!);
        var query = string.IsNullOrWhiteSpace(uriBuilder.Query) ? "?" : uriBuilder.Query;
        foreach (var headerPair in request.Headers)
        {
            if (query != "?")
            {
                query += "&";
            }
            
            query += $"$$headers[]={headerPair.Key}:{UrlEncoder.Default.Encode(headerPair.Value.First())}";
        }
        
        uriBuilder.Query = query;
        var expectedUri = uriBuilder.Uri;
        
        _receiver.FlareSolverrRequest.Should().BeEquivalentTo(new HttpRequestMessage
        {
            RequestUri = new Uri(_configuration.FlareSolverrUrl, UriKind.Absolute),
            Method = HttpMethod.Post
        }, opt => opt.Excluding(x => x.Content));
        
        _receiver.FlareSolverrRequest!.Content.Should().BeEquivalentTo(JsonContent.Create(new Dto
        {
            Cmd = $"request.{request.Method.Method.ToLower()}",
            Url = expectedUri,
            Proxy = new DtoProxy(new Uri(_configuration.FlareSolverrMitmProxyUrl, UriKind.Absolute)),
        }));
        
        var actualResponseContent = await response.Content.ReadAsStringAsync();
        actualResponseContent.Should().Be(expectedResponse);
    }

    
    [Theory]
    [MemberData(nameof(SuccessTestUrls))]
    public async Task GivenRequest_WhenSuccessAndNoHeaders_ThenCallFlareSolverrApi(string url)
    {
        //Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(_faker.Lorem.Sentences())
        };

        var expectedResponse = JsonSerializer.Serialize(new { Value = _faker.Lorem.Sentences() });
        _receiver.Response = expectedResponse;
        
        //Act
        var response = await _sut.SendWrapperAsync(request, CancellationToken.None);

        //Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        
        _receiver.CallCount.Should().Be(1);
        
        _receiver.FlareSolverrRequest.Should().BeEquivalentTo(new HttpRequestMessage
        {
            RequestUri = new Uri(_configuration.FlareSolverrUrl, UriKind.Absolute),
            Method = HttpMethod.Post
        }, opt => opt.Excluding(x => x.Content));
        
        _receiver.FlareSolverrRequest!.Content.Should().BeEquivalentTo(JsonContent.Create(new Dto
        {
            Cmd = $"request.{request.Method.Method.ToLower()}",
            Url = request.RequestUri!,
            Proxy = new DtoProxy(new Uri(_configuration.FlareSolverrMitmProxyUrl, UriKind.Absolute)),
        }));
        
        var actualResponseContent = await response.Content.ReadAsStringAsync();
        actualResponseContent.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task GivenRequest_WhenIsPostFormDataRequest_ThenCallFlareSolverrApi()
    {
        //Arrange
        var formData = new Dictionary<string, string>
        {
            { "key1", Guid.NewGuid().ToString() },
            { "key2", Guid.NewGuid().ToString() },
            { "key3", Guid.NewGuid().ToString() }
        };
        
        var request = new HttpRequestMessage(HttpMethod.Post, _faker.Internet.UrlWithPath())
        {
            Headers = { {HeaderNames.Authorization, _faker.Random.Words()}, {"blabla", _faker.Random.Words()} },
            Content = new FormUrlEncodedContent(formData)
        };

        var expectedResponse = JsonSerializer.Serialize(new { Value = _faker.Lorem.Sentences() });
        _receiver.Response = expectedResponse;
        
        //Act
        var response = await _sut.SendWrapperAsync(request, CancellationToken.None);

        //Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        
        _receiver.CallCount.Should().Be(1);

        var uriBuilder = new UriBuilder(request.RequestUri!);
        var query = "?";
        foreach (var headerPair in request.Headers)
        {
            if (query != "?")
            {
                query += "&";
            }
            
            query += $"$$headers[]={headerPair.Key}:{UrlEncoder.Default.Encode(headerPair.Value.First())}";
        }
        
        uriBuilder.Query = query;
        var expectedUri = uriBuilder.Uri;
        
        _receiver.FlareSolverrRequest.Should().BeEquivalentTo(new HttpRequestMessage
        {
            RequestUri = new Uri(_configuration.FlareSolverrUrl, UriKind.Absolute),
            Method = HttpMethod.Post
        }, opt => opt.Excluding(x => x.Content));
        
        _receiver.FlareSolverrRequest!.Content.Should().BeEquivalentTo(JsonContent.Create(new PostDto
        {
            Cmd = "request.post",
            Url = expectedUri,
            Proxy = new DtoProxy(new Uri(_configuration.FlareSolverrMitmProxyUrl, UriKind.Absolute)),
            PostData = formData.Select(x => $"{x.Key}={x.Value}").Aggregate((a, b) => $"{a}&{b}")
        }));
        
        var actualResponseContent = await response.Content.ReadAsStringAsync();
        actualResponseContent.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task GivenRequest_WhenFalreSolverrUrlIsNotSet_ThenThrowException()
    {
        //Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, _faker.Internet.Url())
        {
            Headers = { {HeaderNames.Authorization, _faker.Random.Words()}, {"blabla", _faker.Random.Words()} },
            Content = new StringContent(_faker.Lorem.Sentences())
        };
        
        _configuration.FlareSolverrUrl = string.Empty;
        
        //Act
        var action = async () => await _sut.SendWrapperAsync(request, CancellationToken.None);

        //Assert
        await action.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenRequest_WhenFalreSolverrMitmProxyUrlIsNotSet_ThenThrowException()
    {
        //Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, _faker.Internet.UrlWithPath())
        {
            Headers = { {HeaderNames.Authorization, _faker.Random.Words()}, {"blabla", _faker.Random.Words()} },
            Content = new StringContent(_faker.Lorem.Sentences())
        };
        
        _configuration.FlareSolverrMitmProxyUrl = string.Empty;
        
        //Act
        var action = async () => await _sut.SendWrapperAsync(request, CancellationToken.None);

        //Assert
        await action.Should().ThrowAsync<ArgumentException>();
    }
}

public sealed class FlareSolverrMessageHandlerTestWrapper : FlareSolverrMessageHandler
{
    public FlareSolverrMessageHandlerTestWrapper(PluginConfiguration configuration) : base(configuration)
    {
    }

    public Task<HttpResponseMessage> SendWrapperAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return SendAsync(request, cancellationToken);
    }
}

public sealed class ReceiverMessageHandler : DelegatingHandler
{
    public int CallCount { get; private set; }
    public HttpRequestMessage? FlareSolverrRequest { get; private set; }
    
    public string Response { get; set; } = "{}";
    
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        CallCount++;
        FlareSolverrRequest = request;
        return Task.FromResult(new HttpResponseMessage()
        {
            StatusCode = HttpStatusCode.OK,
            Content = JsonContent.Create(new FlareSolverrResponse
            {
                Status = "ok",
                Solution = new FlareSolverrResponseSolution
                {
                    Response = $"<html><body><pre>{Response}</pre></body></html>"
                }
            })
        });
    }
}