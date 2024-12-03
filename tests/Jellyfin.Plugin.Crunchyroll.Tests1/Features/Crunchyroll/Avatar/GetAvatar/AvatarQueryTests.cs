using AutoFixture;
using FluentAssertions;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Avatar;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Avatar.GetAvatar;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.Avatar.GetAvatar;

public class AvatarQueryTests
{
    private readonly Fixture _fixture;
    
    private readonly IGetAvatarRepository _repository;
    
    private readonly AvatarQueryHandler _sut;
    
    public AvatarQueryTests()
    {
        _fixture = new Fixture();
        
        _repository = Substitute.For<IGetAvatarRepository>();
        
        _sut = new AvatarQueryHandler(_repository);
    }

    [Fact]
    public async Task ReturnsStream_WhenExistingUrlIsRequested_GivenUrl()
    {
        //Arrange
        var url = _fixture.Create<Uri>().AbsoluteUri;
        var fileName = Path.GetFileName(url);
        
        _repository
            .GetAvatarImageAsync(fileName, Arg.Any<CancellationToken>())
            .Returns(new MemoryStream(_fixture.Create<byte[]>()));
        
        //Act
        var query = new AvatarQuery { Url = url };
        var result = await _sut.Handle(query, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        
        await _repository
            .Received(1)
            .GetAvatarImageAsync(fileName, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnsStream_WhenExistingUrlIsRequested_GivenUrlWithJpeExtension()
    {
        //Arrange
        var url = _fixture.Create<Uri>().AbsoluteUri + "/abc.jpe";
        
        _repository
            .GetAvatarImageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new MemoryStream(_fixture.Create<byte[]>()));
        
        //Act
        var query = new AvatarQuery { Url = url };
        var result = await _sut.Handle(query, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        
        await _repository
            .Received(1)
            .GetAvatarImageAsync("abc.jpeg", Arg.Any<CancellationToken>());
    }
}