using AutoFixture;
using FluentAssertions;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Avatar;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Avatar.GetAvatar;

namespace JellyFin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.Avatar.GetAvatar;

public class AvatarQueryTests
{
    private readonly Fixture _fixture;
    
    private readonly IGetAvatarSession _getAvatarSession;
    
    private readonly AvatarQueryHandler _sut;
    
    public AvatarQueryTests()
    {
        _fixture = new Fixture();
        
        _getAvatarSession = Substitute.For<IGetAvatarSession>();
        
        _sut = new AvatarQueryHandler(_getAvatarSession);
    }

    [Fact]
    public async Task ReturnsStream_WhenExistingUrlIsRequested_GivenUrl()
    {
        //Arrange
        var url = _fixture.Create<Uri>().AbsoluteUri;
        
        _getAvatarSession
            .GetAvatarImageAsync(url)
            .Returns(new MemoryStream(_fixture.Create<byte[]>()));
        
        //Act
        var query = new AvatarQuery { Url = url };
        var result = await _sut.Handle(query, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        
        await _getAvatarSession
            .Received(1)
            .GetAvatarImageAsync(url);
    }
}