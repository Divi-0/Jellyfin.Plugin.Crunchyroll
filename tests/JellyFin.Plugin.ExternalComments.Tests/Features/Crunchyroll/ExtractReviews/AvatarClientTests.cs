using System.Net;
using System.Text;
using AutoFixture;
using FluentAssertions;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Reviews.ExtractReviews;
using JellyFin.Plugin.ExternalComments.Tests.Features.Crunchyroll.ExtractReviews.MockHelper;
using Microsoft.Extensions.Logging;
using RichardSzalay.MockHttp;

namespace JellyFin.Plugin.ExternalComments.Tests.Features.Crunchyroll.ExtractReviews;

public class AvatarClientTests
{
    private readonly IFixture _fixture;
    
    private readonly MockHttpMessageHandler _messageHandlerMock;
    private readonly ILogger<AvatarClient> _logger;
    
    private readonly AvatarClient _sut;

    public AvatarClientTests()
    {
        _fixture = new Fixture();
        
        _messageHandlerMock = new MockHttpMessageHandler();
        _logger = Substitute.For<ILogger<AvatarClient>>();
        
        _sut = new AvatarClient(_messageHandlerMock.ToHttpClient(), _logger);
    }

    [Fact]
    private async Task ReturnsSuccess_WhenFetchingImage_GivenUri()
    {
        //Arrange
        var uri = _fixture.Create<Uri>().AbsoluteUri;

        var (mockedRequest, bytesConten) = _messageHandlerMock.MockAvatarUriRequest(uri);
        
        //Act
        var result = await _sut.GetAvatarStreamAsync(uri, CancellationToken.None);
        
        //Assert
        result.IsSuccess.Should().BeTrue();
        
        using var ms = new MemoryStream();
        await result.Value.CopyToAsync(ms);
        var content = Encoding.Default.GetString(ms.ToArray());
        content.Should().Be(bytesConten);
        
        
        _messageHandlerMock.GetMatchCount(mockedRequest).Should().BePositive();
    }

    [Fact]
    private async Task ReturnsFailed_WhenHttpClientThrows_GivenUri()
    {
        //Arrange
        var uri = _fixture.Create<Uri>().AbsoluteUri;

        var mockedRequest = _messageHandlerMock.MockAvatarUriRequestThrows(uri, new Exception());
        
        //Act
        var result = await _sut.GetAvatarStreamAsync(uri, CancellationToken.None);
        
        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Message.Equals(GetAvatarImageErrorCodes.GetAvatarImageRequestFailed));
        
        _messageHandlerMock.GetMatchCount(mockedRequest).Should().BePositive();
    }

    [Fact]
    private async Task ReturnsFailed_WhenHttpClientStatusCodeIsNotOk_GivenUri()
    {
        //Arrange
        var uri = _fixture.Create<Uri>().AbsoluteUri;

        var mockedRequest = _messageHandlerMock.MockAvatarUriRequest(uri, HttpStatusCode.BadRequest);
        
        //Act
        var result = await _sut.GetAvatarStreamAsync(uri, CancellationToken.None);
        
        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Message.Equals(GetAvatarImageErrorCodes.GetAvatarImageRequestFailed));
        
        _messageHandlerMock.GetMatchCount(mockedRequest).Should().BePositive();
    }
}