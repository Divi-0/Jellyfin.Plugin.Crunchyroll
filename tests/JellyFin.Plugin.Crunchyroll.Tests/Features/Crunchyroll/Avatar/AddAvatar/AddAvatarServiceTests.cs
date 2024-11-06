using Bogus;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Avatar;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Avatar.AddAvatar;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Avatar.Client;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.Avatar.AddAvatar;

public class AddAvatarServiceTests
{
    private readonly AddAvatarService _sut;
    private readonly IAddAvatarSession _session;
    private readonly IAvatarClient _client;
    
    private readonly Faker _faker;

    public AddAvatarServiceTests()
    {
        _session = Substitute.For<IAddAvatarSession>();
        _client = Substitute.For<IAvatarClient>();
        _sut = new AddAvatarService(_session, _client);
        
        _faker = new Faker();
    }

    [Fact]
    public async Task ReturnsSuccess_WhenStreamFound_GivenValidUri()
    {
        //Arrange
        var uri = _faker.Internet.UrlWithPath(fileExt: "png");
        var stream = new MemoryStream();
        
        _client
            .GetAvatarStreamAsync(uri, Arg.Any<CancellationToken>())
            .Returns(Result.Ok<Stream>(stream));
        
        _session
            .AddAvatarImageAsync(uri, Arg.Any<Stream>())
            .Returns(Result.Ok());
        
        //Act
        var result = await _sut.AddAvatarIfNotExists(uri, CancellationToken.None);
        
        //Assert
        result.IsSuccess.Should().BeTrue();
        
        await _client
            .Received(1)
            .GetAvatarStreamAsync(uri, Arg.Any<CancellationToken>());
            
        await _session
            .Received(1)
            .AddAvatarImageAsync(uri, Arg.Is<Stream>(actualStream => actualStream == stream));
    }

    [Fact]
    public async Task ReturnsFailed_WhenGetStreamFails_GivenValidUri()
    {
        //Arrange
        var uri = _faker.Internet.UrlWithPath(fileExt: "png");
        var stream = new MemoryStream();
        
        _client
            .GetAvatarStreamAsync(uri, Arg.Any<CancellationToken>())
            .Returns(Result.Fail("error"));
        
        //Act
        var result = await _sut.AddAvatarIfNotExists(uri, CancellationToken.None);
        
        //Assert
        result.IsFailed.Should().BeTrue();
        
        await _client
            .Received(1)
            .GetAvatarStreamAsync(uri, Arg.Any<CancellationToken>());
            
        await _session
            .DidNotReceive()
            .AddAvatarImageAsync(uri, Arg.Any<Stream>());
    }

    [Fact]
    public async Task ReturnsFailed_WhenAddAvatarImageFails_GivenValidUri()
    {
        //Arrange
        var uri = _faker.Internet.UrlWithPath(fileExt: "png");
        var stream = new MemoryStream();
        
        _client
            .GetAvatarStreamAsync(uri, Arg.Any<CancellationToken>())
            .Returns(Result.Ok<Stream>(stream));
        
        _session
            .AddAvatarImageAsync(uri, Arg.Any<Stream>())
            .Returns(Result.Fail("error"));
        
        //Act
        var result = await _sut.AddAvatarIfNotExists(uri, CancellationToken.None);
        
        //Assert
        result.IsFailed.Should().BeTrue();
        
        await _client
            .Received(1)
            .GetAvatarStreamAsync(uri, Arg.Any<CancellationToken>());
            
        await _session
            .Received(1)
            .AddAvatarImageAsync(uri, Arg.Any<Stream>());
    }
}