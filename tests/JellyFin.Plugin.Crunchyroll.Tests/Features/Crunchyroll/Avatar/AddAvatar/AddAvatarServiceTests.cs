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
        
        _session
            .AvatarExistsAsync(uri)
            .Returns(Result.Ok(false));
        
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
        
        await _session
            .Received(1)
            .AvatarExistsAsync(uri);
        
        await _client
            .Received(1)
            .GetAvatarStreamAsync(uri, Arg.Any<CancellationToken>());
            
        await _session
            .Received(1)
            .AddAvatarImageAsync(uri, Arg.Is<Stream>(actualStream => actualStream == stream));
    }

    [Fact]
    public async Task ReturnsSuccess_WhenAvatarAlreadyExists_GivenValidUri()
    {
        //Arrange
        var uri = _faker.Internet.UrlWithPath(fileExt: "png");
        
        _session
            .AvatarExistsAsync(uri)
            .Returns(Result.Ok(true));
        
        //Act
        var result = await _sut.AddAvatarIfNotExists(uri, CancellationToken.None);
        
        //Assert
        result.IsSuccess.Should().BeTrue();
        
        await _session
            .Received(1)
            .AvatarExistsAsync(uri);
        
        await _client
            .DidNotReceive()
            .GetAvatarStreamAsync(uri, Arg.Any<CancellationToken>());
            
        await _session
            .DidNotReceive()
            .AddAvatarImageAsync(uri, Arg.Any<Stream>());
    }

    [Fact]
    public async Task ReturnsFailed_WhenAvatarExistsFails_GivenValidUri()
    {
        //Arrange
        var uri = _faker.Internet.UrlWithPath(fileExt: "png");
        
        _session
            .AvatarExistsAsync(uri)
            .Returns(Result.Fail("error"));
        
        //Act
        var result = await _sut.AddAvatarIfNotExists(uri, CancellationToken.None);
        
        //Assert
        result.IsFailed.Should().BeTrue();
        
        await _session
            .Received(1)
            .AvatarExistsAsync(uri);
        
        await _client
            .DidNotReceive()
            .GetAvatarStreamAsync(uri, Arg.Any<CancellationToken>());
            
        await _session
            .DidNotReceive()
            .AddAvatarImageAsync(uri, Arg.Any<Stream>());
    }

    [Fact]
    public async Task ReturnsFailed_WhenGetStreamFails_GivenValidUri()
    {
        //Arrange
        var uri = _faker.Internet.UrlWithPath(fileExt: "png");
        
        _session
            .AvatarExistsAsync(uri)
            .Returns(Result.Ok(false));
        
        _client
            .GetAvatarStreamAsync(uri, Arg.Any<CancellationToken>())
            .Returns(Result.Fail("error"));
        
        //Act
        var result = await _sut.AddAvatarIfNotExists(uri, CancellationToken.None);
        
        //Assert
        result.IsFailed.Should().BeTrue();
        
        await _session
            .Received(1)
            .AvatarExistsAsync(uri);
        
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
        
        _session
            .AvatarExistsAsync(uri)
            .Returns(Result.Ok(false));
        
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
        
        await _session
            .Received(1)
            .AvatarExistsAsync(uri);
        
        await _client
            .Received(1)
            .GetAvatarStreamAsync(uri, Arg.Any<CancellationToken>());
            
        await _session
            .Received(1)
            .AddAvatarImageAsync(uri, Arg.Any<Stream>());
    }
    
    [Fact]
    public async Task ReturnsSuccess_WhenStreamFound_GivenWebArchiveUri()
    {
        //Arrange
        var webArchiveOrgUri = new UriBuilder(Uri.UriSchemeHttp, "web.archive.org");
        var archivedImageUrl = _faker.Internet.UrlWithPath(fileExt: "png");
        var uri = $"{webArchiveOrgUri}im_/{archivedImageUrl}";
        var stream = new MemoryStream();
        
        _session
            .AvatarExistsAsync(archivedImageUrl)
            .Returns(Result.Ok(false));
        
        _client
            .GetAvatarStreamAsync(uri, Arg.Any<CancellationToken>())
            .Returns(Result.Ok<Stream>(stream));
        
        _session
            .AddAvatarImageAsync(archivedImageUrl, Arg.Any<Stream>())
            .Returns(Result.Ok());
        
        //Act
        var result = await _sut.AddAvatarIfNotExists(uri, CancellationToken.None);
        
        //Assert
        result.IsSuccess.Should().BeTrue();
        
        await _session
            .Received(1)
            .AvatarExistsAsync(archivedImageUrl);
        
        await _client
            .Received(1)
            .GetAvatarStreamAsync(uri, Arg.Any<CancellationToken>());
            
        await _session
            .Received(1)
            .AddAvatarImageAsync(archivedImageUrl, Arg.Is<Stream>(actualStream => actualStream == stream));
    }
    
    [Fact]
    public async Task DoesNotFetchAvatar_WhenUriIsAlreadyBeingFetched_GivenDuplicateWebArchiveUri()
    {
        //Arrange
        var webArchiveOrgUri = new UriBuilder(Uri.UriSchemeHttp, "web.archive.org");
        var archivedImageUrl = _faker.Internet.UrlWithPath(fileExt: "png");
        var uri = $"{webArchiveOrgUri}im_/{archivedImageUrl}";
        var stream = new MemoryStream();
        
        var cancellationTokenSource = new CancellationTokenSource();
        
        _session
            .AvatarExistsAsync(archivedImageUrl)
            .Returns(Result.Ok(false));
        
        _client
            .GetAvatarStreamAsync(uri, Arg.Any<CancellationToken>())
            .Returns(Task.Run(async () =>
            {
                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    await Task.Delay(500);
                }
                
                return Result.Ok<Stream>(stream);
            }));
        
        _session
            .AddAvatarImageAsync(archivedImageUrl, Arg.Any<Stream>())
            .Returns(Result.Ok());
        
        //Act
        var firstTask = _sut.AddAvatarIfNotExists(uri, CancellationToken.None);
        var secondTask = Task.Run(async () =>
        {
            var result = await _sut.AddAvatarIfNotExists(uri, CancellationToken.None);
            await cancellationTokenSource.CancelAsync();
            return result;
        });
        
        var results = await Task.WhenAll(firstTask.AsTask(), secondTask);

        //Assert
        results.Should().AllSatisfy(x =>
        {
            x.IsSuccess.Should()
                .BeTrue("first task adds image, second task sees uri is already being fetched");
        });
        
        await _session
            .Received(1)
            .AvatarExistsAsync(archivedImageUrl);
        
        await _client
            .Received(1)
            .GetAvatarStreamAsync(uri, Arg.Any<CancellationToken>());
            
        await _session
            .Received(1)
            .AddAvatarImageAsync(archivedImageUrl, Arg.Is<Stream>(actualStream => actualStream == stream));
    }
}