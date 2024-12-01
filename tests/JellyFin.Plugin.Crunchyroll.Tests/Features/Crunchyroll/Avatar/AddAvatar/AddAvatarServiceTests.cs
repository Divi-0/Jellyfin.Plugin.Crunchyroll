using Bogus;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Avatar.AddAvatar;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Avatar.Client;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.Avatar.AddAvatar;

public class AddAvatarServiceTests
{
    private readonly AddAvatarService _sut;
    private readonly IAddAvatarRepository _repository;
    private readonly IAvatarClient _client;
    
    private readonly Faker _faker;

    public AddAvatarServiceTests()
    {
        _repository = Substitute.For<IAddAvatarRepository>();
        _client = Substitute.For<IAvatarClient>();
        _sut = new AddAvatarService(_repository, _client);
        
        _faker = new Faker();
    }

    [Fact]
    public async Task ReturnsSuccess_WhenStreamFound_GivenValidUri()
    {
        //Arrange
        var uri = _faker.Internet.UrlWithPath(fileExt: "png");
        var fileName = Path.GetFileName(uri);
        var stream = new MemoryStream();
        
        _repository
            .AvatarExists(fileName)
            .Returns(Result.Ok(false));
        
        _client
            .GetAvatarStreamAsync(uri, Arg.Any<CancellationToken>())
            .Returns(Result.Ok<Stream>(stream));
        
        _repository
            .AddAvatarImageAsync(fileName, Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        //Act
        var result = await _sut.AddAvatarIfNotExists(uri, CancellationToken.None);
        
        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(uri);
        
        _repository
            .Received(1)
            .AvatarExists(fileName);
        
        await _client
            .Received(1)
            .GetAvatarStreamAsync(uri, Arg.Any<CancellationToken>());
            
        await _repository
            .Received(1)
            .AddAvatarImageAsync(fileName, Arg.Is<Stream>(actualStream => actualStream == stream), 
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnsSuccess_WhenAvatarAlreadyExists_GivenValidUri()
    {
        //Arrange
        var uri = _faker.Internet.UrlWithPath(fileExt: "png");
        var fileName = Path.GetFileName(uri);
        
        _repository
            .AvatarExists(fileName)
            .Returns(Result.Ok(true));
        
        //Act
        var result = await _sut.AddAvatarIfNotExists(uri, CancellationToken.None);
        
        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(uri);
        
        _repository
            .Received(1)
            .AvatarExists(fileName);
        
        await _client
            .DidNotReceive()
            .GetAvatarStreamAsync(uri, Arg.Any<CancellationToken>());
            
        await _repository
            .DidNotReceive()
            .AddAvatarImageAsync(fileName, Arg.Any<Stream>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnsFailed_WhenAvatarExistsFails_GivenValidUri()
    {
        //Arrange
        var uri = _faker.Internet.UrlWithPath(fileExt: "png");
        var fileName = Path.GetFileName(uri);
        
        _repository
            .AvatarExists(fileName)
            .Returns(Result.Fail("error"));
        
        //Act
        var result = await _sut.AddAvatarIfNotExists(uri, CancellationToken.None);
        
        //Assert
        result.IsFailed.Should().BeTrue();
        
        _repository
            .Received(1)
            .AvatarExists(fileName);
        
        await _client
            .DidNotReceive()
            .GetAvatarStreamAsync(uri, Arg.Any<CancellationToken>());
            
        await _repository
            .DidNotReceive()
            .AddAvatarImageAsync(fileName, Arg.Any<Stream>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnsFailed_WhenGetStreamFails_GivenValidUri()
    {
        //Arrange
        var uri = _faker.Internet.UrlWithPath(fileExt: "png");
        var fileName = Path.GetFileName(uri);
        
        _repository
            .AvatarExists(fileName)
            .Returns(Result.Ok(false));
        
        _client
            .GetAvatarStreamAsync(uri, Arg.Any<CancellationToken>())
            .Returns(Result.Fail("error"));
        
        //Act
        var result = await _sut.AddAvatarIfNotExists(uri, CancellationToken.None);
        
        //Assert
        result.IsFailed.Should().BeTrue();
        
        _repository
            .Received(1)
            .AvatarExists(fileName);
        
        await _client
            .Received(1)
            .GetAvatarStreamAsync(uri, Arg.Any<CancellationToken>());
            
        await _repository
            .DidNotReceive()
            .AddAvatarImageAsync(fileName, Arg.Any<Stream>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnsFailed_WhenAddAvatarImageFails_GivenValidUri()
    {
        //Arrange
        var uri = _faker.Internet.UrlWithPath(fileExt: "png");
        var stream = new MemoryStream();
        var fileName = Path.GetFileName(uri);
        
        _repository
            .AvatarExists(fileName)
            .Returns(Result.Ok(false));
        
        _client
            .GetAvatarStreamAsync(uri, Arg.Any<CancellationToken>())
            .Returns(Result.Ok<Stream>(stream));
        
        _repository
            .AddAvatarImageAsync(fileName, Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail("error"));
        
        //Act
        var result = await _sut.AddAvatarIfNotExists(uri, CancellationToken.None);
        
        //Assert
        result.IsFailed.Should().BeTrue();
        
        _repository
            .Received(1)
            .AvatarExists(fileName);
        
        await _client
            .Received(1)
            .GetAvatarStreamAsync(uri, Arg.Any<CancellationToken>());
            
        await _repository
            .Received(1)
            .AddAvatarImageAsync(fileName, Arg.Any<Stream>(), Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsSuccess_WhenStreamFound_GivenWebArchiveUri()
    {
        //Arrange
        var webArchiveOrgUri = new UriBuilder(Uri.UriSchemeHttp, "web.archive.org");
        var archivedImageUrl = _faker.Internet.UrlWithPath(fileExt: "png");
        var uri = $"{webArchiveOrgUri}im_/{archivedImageUrl}";
        var stream = new MemoryStream();
        var fileName = Path.GetFileName(archivedImageUrl);
        
        _repository
            .AvatarExists(fileName)
            .Returns(Result.Ok(false));
        
        _client
            .GetAvatarStreamAsync(uri, Arg.Any<CancellationToken>())
            .Returns(Result.Ok<Stream>(stream));
        
        _repository
            .AddAvatarImageAsync(fileName, Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        //Act
        var result = await _sut.AddAvatarIfNotExists(uri, CancellationToken.None);
        
        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(archivedImageUrl);
        
        _repository
            .Received(1)
            .AvatarExists(fileName);
        
        await _client
            .Received(1)
            .GetAvatarStreamAsync(uri, Arg.Any<CancellationToken>());
            
        await _repository
            .Received(1)
            .AddAvatarImageAsync(fileName, Arg.Is<Stream>(actualStream => actualStream == stream),
                Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task DoesNotFetchAvatar_WhenUriIsAlreadyBeingFetched_GivenDuplicateWebArchiveUri()
    {
        //Arrange
        var webArchiveOrgUri = new UriBuilder(Uri.UriSchemeHttp, "web.archive.org");
        var archivedImageUrl = _faker.Internet.UrlWithPath(fileExt: "png");
        var uri = $"{webArchiveOrgUri}im_/{archivedImageUrl}";
        var stream = new MemoryStream();
        var fileName = Path.GetFileName(archivedImageUrl);
        
        var cancellationTokenSource = new CancellationTokenSource();
        
        _repository
            .AvatarExists(fileName)
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
        
        _repository
            .AddAvatarImageAsync(fileName, Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        //Act
        var firstTask = _sut.AddAvatarIfNotExists(uri, CancellationToken.None);
        // ReSharper disable once MethodSupportsCancellation
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
        
        _repository
            .Received(1)
            .AvatarExists(fileName);
        
        await _client
            .Received(1)
            .GetAvatarStreamAsync(uri, Arg.Any<CancellationToken>());
            
        await _repository
            .Received(1)
            .AddAvatarImageAsync(fileName, Arg.Is<Stream>(actualStream => actualStream == stream),
                Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsSuccess_WhenStreamFound_GivenUriWithJpeExtension()
    {
        //Arrange
        var uri = _faker.Internet.UrlWithPath(fileExt: "jpe");
        var fileName = Path.GetFileName(uri).Replace("jpe", "jpeg");
        var stream = new MemoryStream();
        
        _repository
            .AvatarExists(fileName)
            .Returns(Result.Ok(false));
        
        _client
            .GetAvatarStreamAsync(uri, Arg.Any<CancellationToken>())
            .Returns(Result.Ok<Stream>(stream));
        
        _repository
            .AddAvatarImageAsync(fileName, Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        //Act
        var result = await _sut.AddAvatarIfNotExists(uri, CancellationToken.None);
        
        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(uri);
        
        _repository
            .Received(1)
            .AvatarExists(fileName);
        
        await _client
            .Received(1)
            .GetAvatarStreamAsync(uri, Arg.Any<CancellationToken>());
            
        await _repository
            .Received(1)
            .AddAvatarImageAsync(fileName, Arg.Is<Stream>(actualStream => actualStream == stream), 
                Arg.Any<CancellationToken>());
    }
}