using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Net;
using Bogus;
using FluentAssertions;
using Jellyfin.Plugin.Crunchyroll.Domain.Entities;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.SetEpisodeThumbnail;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Logging;
using NSubstitute.ExceptionExtensions;
using RichardSzalay.MockHttp;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.PostScan;

public class SetEpisodeThumbnailTests
{
    private readonly SetEpisodeThumbnail _sut;
    private readonly MockHttpMessageHandler _mockHttpMessageHandler;
    private readonly MockFileSystem _fileSystem;
    private readonly IMediaSourceManager _mediaSourceManager;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SetEpisodeThumbnail> _logger;

    private readonly Faker _faker;
    private readonly string _directory;

    public SetEpisodeThumbnailTests()
    {
        _mediaSourceManager = MockHelper.MediaSourceManager;
        _mockHttpMessageHandler = new MockHttpMessageHandler();
        _fileSystem = new MockFileSystem();
        _logger = Substitute.For<ILogger<SetEpisodeThumbnail>>();
        _httpClientFactory = Substitute.For<IHttpClientFactory>();
        _sut = new SetEpisodeThumbnail(_httpClientFactory, _fileSystem.File, _fileSystem.Directory, _logger);

        _httpClientFactory
            .CreateClient("ThumbnailClient")
            .Returns(_mockHttpMessageHandler.ToHttpClient());
        
        _faker = new Faker();
        _directory = Path.Combine(Path.GetDirectoryName(typeof(SetEpisodeThumbnail).Assembly.Location)!, "episode-thumbnails");
    }

    [Fact]
    public async Task DonwloadsStoresAndSetsTheImageAsThumbnail_WhenSuccessful_GivenEpisodeAndImageSource()
    {
        //Arrange
        var episode = EpisodeFaker.Generate();
        var imageBytes = _faker.Random.Bytes(1024);
        var imageSource = new ImageSource
        {
            Uri = _faker.Internet.UrlWithPath(fileExt: "png"),
            Width = Random.Shared.Next(1, 99999),
            Height = Random.Shared.Next(1, 99999)
        };
        
        var mockedRequest = _mockHttpMessageHandler
            .When(imageSource.Uri)
            .Respond(new StreamContent(new MemoryStream(imageBytes)));
        
        //Act
        var result = await _sut.GetAndSetThumbnailAsync(episode, imageSource, CancellationToken.None);
        
        //Assert
        result.IsSuccess.Should().BeTrue();
        
        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().Be(1, "it should download the thumbnail");
        
        _fileSystem.AllDirectories.Should().Contain(path => path == _directory);
        var thumbnailFilePath = Path.Combine(_directory, Path.GetFileName(imageSource.Uri));
        (await _fileSystem.File.ReadAllBytesAsync(thumbnailFilePath)).Should().BeEquivalentTo(imageBytes);
        
        var imageInfoPrimary = episode.GetImageInfo(ImageType.Primary, 0);
        imageInfoPrimary.Should().NotBeNull();
        imageInfoPrimary.Path.Should().Be(thumbnailFilePath);
        imageInfoPrimary.Width.Should().Be(imageSource.Width);
        imageInfoPrimary.Height.Should().Be(imageSource.Height);
        
        var imageInfoThumb = episode.GetImageInfo(ImageType.Thumb, 0);
        imageInfoThumb.Should().NotBeNull();
        imageInfoThumb.Path.Should().Be(thumbnailFilePath);
        imageInfoThumb.Width.Should().Be(imageSource.Width);
        imageInfoThumb.Height.Should().Be(imageSource.Height);
    }

    [Fact]
    public async Task DonwloadsStoresAndSetsTheImageAsThumbnail_WhenSuccessful_GivenMovieAndValidImageSource()
    {
        //Arrange
        var movie = MovieFaker.Generate();
        var imageBytes = _faker.Random.Bytes(1024);
        var imageSource = new ImageSource
        {
            Uri = _faker.Internet.UrlWithPath(fileExt: "png"),
            Width = Random.Shared.Next(1, 99999),
            Height = Random.Shared.Next(1, 99999)
        };
        
        var mockedRequest = _mockHttpMessageHandler
            .When(imageSource.Uri)
            .Respond(new StreamContent(new MemoryStream(imageBytes)));
        
        //Act
        var result = await _sut.GetAndSetThumbnailAsync(movie, imageSource, CancellationToken.None);
        
        //Assert
        result.IsSuccess.Should().BeTrue();
        
        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().Be(1, "it should download the thumbnail");
        
        _fileSystem.AllDirectories.Should().Contain(path => path == _directory);
        var thumbnailFilePath = Path.Combine(_directory, Path.GetFileName(imageSource.Uri));
        (await _fileSystem.File.ReadAllBytesAsync(thumbnailFilePath)).Should().BeEquivalentTo(imageBytes);
        
        var imageInfoPrimary = movie.GetImageInfo(ImageType.Primary, 0);
        imageInfoPrimary.Should().NotBeNull();
        imageInfoPrimary.Path.Should().Be(thumbnailFilePath);
        imageInfoPrimary.Width.Should().Be(imageSource.Width);
        imageInfoPrimary.Height.Should().Be(imageSource.Height);
        
        var imageInfoThumb = movie.GetImageInfo(ImageType.Thumb, 0);
        imageInfoThumb.Should().NotBeNull();
        imageInfoThumb.Path.Should().Be(thumbnailFilePath);
        imageInfoThumb.Width.Should().Be(imageSource.Width);
        imageInfoThumb.Height.Should().Be(imageSource.Height);
    }

    [Fact]
    public async Task SkipsDownloadAndOnlySetsPrimaryImage_WhenTheSameThumbnailIsAlreadySet_GivenEpisodeAndImageSource()
    {
        //Arrange
        var episode = EpisodeFaker.Generate();
        var imageBytes = _faker.Random.Bytes(1024);
        var imageSource = new ImageSource
        {
            Uri = _faker.Internet.UrlWithPath(fileExt: "png"),
            Width = Random.Shared.Next(1, 99999),
            Height = Random.Shared.Next(1, 99999)
        };
        
        _mediaSourceManager
            .GetPathProtocol(episode.Path)
            .Returns(MediaProtocol.File);
        
        var thumbnailFilePath = Path.Combine(_directory, Path.GetFileName(imageSource.Uri));

        _fileSystem.Directory.CreateDirectory(_directory);
        _ = _fileSystem.File.Create(thumbnailFilePath);
        
        episode.SetImage(new ItemImageInfo()
        {
            Path = thumbnailFilePath,
            Type = ImageType.Thumb,
            Width = imageSource.Width,
            Height = imageSource.Height
        }, 0);
            
        episode.SetImage(new ItemImageInfo()
        {
            Path = "abc123",
            Type = ImageType.Primary,
            Width = 1,
            Height = 1
        }, 0);
        
        var mockedRequest = _mockHttpMessageHandler
            .When(imageSource.Uri)
            .Respond(new StreamContent(new MemoryStream(imageBytes)));
        
        //Act
        var result = await _sut.GetAndSetThumbnailAsync(episode, imageSource, CancellationToken.None);
        
        //Assert
        result.IsSuccess.Should().BeTrue();
        
        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().Be(0, "it should not download the thumbnail");
        
        _fileSystem.AllDirectories.Should().Contain(path => path == _directory);
        
        var imageInfoPrimary = episode.GetImageInfo(ImageType.Primary, 0);
        imageInfoPrimary.Should().NotBeNull();
        imageInfoPrimary.Path.Should().Be(thumbnailFilePath);
        imageInfoPrimary.Width.Should().Be(imageSource.Width);
        imageInfoPrimary.Height.Should().Be(imageSource.Height);
        
        var imageInfoThumb = episode.GetImageInfo(ImageType.Thumb, 0);
        imageInfoThumb.Should().NotBeNull();
        imageInfoThumb.Path.Should().Be(thumbnailFilePath);
        imageInfoThumb.Width.Should().Be(imageSource.Width);
        imageInfoThumb.Height.Should().Be(imageSource.Height);
    }

    [Fact]
    public async Task SkipsDownloadAndOnlySetsImagesFromFileSystem_WhenNoMatchingImageInfoOnEpisodeFoundButImageStoredInFileSystem_GivenEpisodeAndImageSource()
    {
        //Arrange
        var episode = EpisodeFaker.Generate();
        var imageBytes = _faker.Random.Bytes(1024);
        var imageSource = new ImageSource
        {
            Uri = _faker.Internet.UrlWithPath(fileExt: "png"),
            Width = Random.Shared.Next(1, 99999),
            Height = Random.Shared.Next(1, 99999)
        };
        
        _mediaSourceManager
            .GetPathProtocol(episode.Path)
            .Returns(MediaProtocol.File);
        
        var thumbnailFilePath = Path.Combine(_directory, Path.GetFileName(imageSource.Uri));

        _fileSystem.Directory.CreateDirectory(_directory);
        _ = _fileSystem.File.Create(thumbnailFilePath);
        
        var mockedRequest = _mockHttpMessageHandler
            .When(imageSource.Uri)
            .Respond(new StreamContent(new MemoryStream(imageBytes)));
        
        //Act
        var result = await _sut.GetAndSetThumbnailAsync(episode, imageSource, CancellationToken.None);
        
        //Assert
        result.IsSuccess.Should().BeTrue();
        
        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().Be(0, "it should not download the thumbnail");
        
        _fileSystem.AllDirectories.Should().Contain(path => path == _directory);
        
        var imageInfoPrimary = episode.GetImageInfo(ImageType.Primary, 0);
        imageInfoPrimary.Should().NotBeNull();
        imageInfoPrimary.Path.Should().Be(thumbnailFilePath);
        imageInfoPrimary.Width.Should().Be(imageSource.Width);
        imageInfoPrimary.Height.Should().Be(imageSource.Height);
        
        var imageInfoThumb = episode.GetImageInfo(ImageType.Thumb, 0);
        imageInfoThumb.Should().NotBeNull();
        imageInfoThumb.Path.Should().Be(thumbnailFilePath);
        imageInfoThumb.Width.Should().Be(imageSource.Width);
        imageInfoThumb.Height.Should().Be(imageSource.Height);
    }

    [Fact]
    public async Task SkipsSetImage_WhenTheSameThumbnailIsAlreadySet_GivenMovieAndValidImageSource()
    {
        //Arrange
        var movie = MovieFaker.Generate();
        var imageBytes = _faker.Random.Bytes(1024);
        var imageSource = new ImageSource
        {
            Uri = _faker.Internet.UrlWithPath(fileExt: "png"),
            Width = Random.Shared.Next(1, 99999),
            Height = Random.Shared.Next(1, 99999)
        };

        _mediaSourceManager
            .GetPathProtocol(movie.Path)
            .Returns(MediaProtocol.File);
        
        var thumbnailFilePath = Path.Combine(_directory, Path.GetFileName(imageSource.Uri));
        
        _fileSystem.Directory.CreateDirectory(_directory);
        _ = _fileSystem.File.Create(thumbnailFilePath);
        
        movie.SetImage(new ItemImageInfo()
        {
            Path = thumbnailFilePath,
            Type = ImageType.Thumb,
            Width = imageSource.Width,
            Height = imageSource.Height
        }, 0);
            
        movie.SetImage(new ItemImageInfo()
        {
            Path = thumbnailFilePath,
            Type = ImageType.Primary,
            Width = imageSource.Width,
            Height = imageSource.Height
        }, 0);
        
        var mockedRequest = _mockHttpMessageHandler
            .When(imageSource.Uri)
            .Respond(new StreamContent(new MemoryStream(imageBytes)));
        
        //Act
        var result = await _sut.GetAndSetThumbnailAsync(movie, imageSource, CancellationToken.None);
        
        //Assert
        result.IsSuccess.Should().BeTrue();
        
        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().Be(0, "it should not download the thumbnail");
        
        _fileSystem.AllDirectories.Should().Contain(path => path == _directory);
        
        var imageInfoPrimary = movie.GetImageInfo(ImageType.Primary, 0);
        imageInfoPrimary.Should().NotBeNull();
        imageInfoPrimary.Path.Should().Be(thumbnailFilePath);
        imageInfoPrimary.Width.Should().Be(imageSource.Width);
        imageInfoPrimary.Height.Should().Be(imageSource.Height);
        
        var imageInfoThumb = movie.GetImageInfo(ImageType.Thumb, 0);
        imageInfoThumb.Should().NotBeNull();
        imageInfoThumb.Path.Should().Be(thumbnailFilePath);
        imageInfoThumb.Width.Should().Be(imageSource.Width);
        imageInfoThumb.Height.Should().Be(imageSource.Height);
    }

    [Fact]
    public async Task ReturnsFailed_WhenThumbnailRequestThrows_GivenEpisodeAndImageSource()
    {
        //Arrange
        var episode = EpisodeFaker.Generate();
        var imageSource = new ImageSource
        {
            Uri = _faker.Internet.UrlWithPath(fileExt: "png"),
            Width = Random.Shared.Next(1, 99999),
            Height = Random.Shared.Next(1, 99999)
        };

        _mediaSourceManager
            .GetPathProtocol(episode.Path)
            .Returns(MediaProtocol.File);

        var mockedRequest = _mockHttpMessageHandler
            .When(imageSource.Uri)
            .Throw(new Exception());

        //Act
        var result = await _sut.GetAndSetThumbnailAsync(episode, imageSource, CancellationToken.None);

        //Assert
        result.IsFailed.Should().BeTrue();

        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().Be(1, "it should not download the thumbnail");

        var thumbnailFilePath = Path.Combine(_directory, Path.GetFileName(imageSource.Uri));
        _fileSystem.File.Exists(thumbnailFilePath).Should().BeFalse();

        episode.ImageInfos.Should().NotContain(x =>
            x.Path == thumbnailFilePath &&
            x.Type == ImageType.Thumb);
    }

    [Fact]
    public async Task ReturnsFailed_WhenThumbnailRequestThrows_GivenMovieAndValidImageSource()
    {
        //Arrange
        var movie = MovieFaker.Generate();
        var imageSource = new ImageSource
        {
            Uri = _faker.Internet.UrlWithPath(fileExt: "png"),
            Width = Random.Shared.Next(1, 99999),
            Height = Random.Shared.Next(1, 99999)
        };

        _mediaSourceManager
            .GetPathProtocol(movie.Path)
            .Returns(MediaProtocol.File);

        var mockedRequest = _mockHttpMessageHandler
            .When(imageSource.Uri)
            .Throw(new Exception());

        //Act
        var result = await _sut.GetAndSetThumbnailAsync(movie, imageSource, CancellationToken.None);

        //Assert
        result.IsFailed.Should().BeTrue();

        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().Be(1, "it should download the thumbnail");

        var thumbnailFilePath = Path.Combine(_directory, Path.GetFileName(imageSource.Uri));
        _fileSystem.File.Exists(thumbnailFilePath).Should().BeFalse();

        movie.ImageInfos.Should().NotContain(x =>
            x.Path == thumbnailFilePath &&
            x.Type == ImageType.Thumb);
    }

    [Fact]
    public async Task ReturnsFailed_WhenThumbnailRequestIsNotSuccessStatusCode_GivenEpisodeAndImageSource()
    {
        //Arrange
        var episode = EpisodeFaker.Generate();
        var imageSource = new ImageSource
        {
            Uri = _faker.Internet.UrlWithPath(fileExt: "png"),
            Width = Random.Shared.Next(1, 99999),
            Height = Random.Shared.Next(1, 99999)
        };

        _mediaSourceManager
            .GetPathProtocol(episode.Path)
            .Returns(MediaProtocol.File);

        var mockedRequest = _mockHttpMessageHandler
            .When(imageSource.Uri)
            .Respond(HttpStatusCode.BadRequest);

        //Act
        var result = await _sut.GetAndSetThumbnailAsync(episode, imageSource, CancellationToken.None);

        //Assert
        result.IsFailed.Should().BeTrue();

        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().Be(1, "it should download the thumbnail");

        var thumbnailFilePath = Path.Combine(_directory, Path.GetFileName(imageSource.Uri));
        _fileSystem.File.Exists(thumbnailFilePath).Should().BeFalse();

        episode.ImageInfos.Should().NotContain(x =>
            x.Path == thumbnailFilePath &&
            x.Type == ImageType.Thumb);
    }

    [Fact]
    public async Task ReturnsFailed_WhenThumbnailRequestIsNotSuccessStatusCode_GivenMovieAndValidImageSource()
    {
        //Arrange
        var movie = MovieFaker.Generate();
        var imageSource = new ImageSource
        {
            Uri = _faker.Internet.UrlWithPath(fileExt: "png"),
            Width = Random.Shared.Next(1, 99999),
            Height = Random.Shared.Next(1, 99999)
        };

        _mediaSourceManager
            .GetPathProtocol(movie.Path)
            .Returns(MediaProtocol.File);

        var mockedRequest = _mockHttpMessageHandler
            .When(imageSource.Uri)
            .Respond(HttpStatusCode.BadRequest);

        //Act
        var result = await _sut.GetAndSetThumbnailAsync(movie, imageSource, CancellationToken.None);

        //Assert
        result.IsFailed.Should().BeTrue();

        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().Be(1, "it should download the thumbnail");

        var thumbnailFilePath = Path.Combine(_directory, Path.GetFileName(imageSource.Uri));
        _fileSystem.File.Exists(thumbnailFilePath).Should().BeFalse();

        movie.ImageInfos.Should().NotContain(x =>
            x.Path == thumbnailFilePath &&
            x.Type == ImageType.Thumb);
    }

    [Fact]
    public async Task ReturnsFailed_WhenCreateFileThrows_GivenEpisodeAndImageSource()
    {
        //Arrange
        var file = Substitute.For<IFile>();
        var sut = new SetEpisodeThumbnail(_httpClientFactory, file, _fileSystem.Directory, _logger); 
        
        file
            .Create(Arg.Any<string>())
            .Throws(new Exception());
        
        var episode = EpisodeFaker.Generate();
        var imageBytes = new Faker().Random.Bytes(1024);
        var imageSource = new ImageSource
        {
            Uri = _faker.Internet.UrlWithPath(fileExt: "png"),
            Width = Random.Shared.Next(1, 99999),
            Height = Random.Shared.Next(1, 99999)
        };

        _mediaSourceManager
            .GetPathProtocol(episode.Path)
            .Returns(MediaProtocol.File);

        var mockedRequest = _mockHttpMessageHandler
            .When(imageSource.Uri)
            .Respond(new StreamContent(new MemoryStream(imageBytes)));

        //Act
        var result = await sut.GetAndSetThumbnailAsync(episode, imageSource, CancellationToken.None);

        //Assert
        result.IsFailed.Should().BeTrue();

        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().Be(1, "it should download the thumbnail");
        
        var thumbnailFilePath = Path.Combine(_directory, Path.GetFileName(imageSource.Uri));
        file
            .Received(1)
            .Create(thumbnailFilePath);
        
        episode.ImageInfos.Should().NotContain(x =>
            x.Path == thumbnailFilePath &&
            x.Type == ImageType.Thumb);
    }

    [Fact]
    public async Task ReturnsFailed_WhenCreateFileThrows_GivenMovieAndValidImageSource()
    {
        //Arrange
        var file = Substitute.For<IFile>();
        var sut = new SetEpisodeThumbnail(_httpClientFactory, file, _fileSystem.Directory, _logger); 
        
        file
            .Create(Arg.Any<string>())
            .Throws(new Exception());
        
        var movie = MovieFaker.Generate();
        var imageBytes = new Faker().Random.Bytes(1024);
        var imageSource = new ImageSource
        {
            Uri = _faker.Internet.UrlWithPath(fileExt: "png"),
            Width = Random.Shared.Next(1, 99999),
            Height = Random.Shared.Next(1, 99999)
        };

        _mediaSourceManager
            .GetPathProtocol(movie.Path)
            .Returns(MediaProtocol.File);

        var mockedRequest = _mockHttpMessageHandler
            .When(imageSource.Uri)
            .Respond(new StreamContent(new MemoryStream(imageBytes)));

        //Act
        var result = await sut.GetAndSetThumbnailAsync(movie, imageSource, CancellationToken.None);

        //Assert
        result.IsFailed.Should().BeTrue();

        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().Be(1, "it should download the thumbnail");
        
        var thumbnailFilePath = Path.Combine(_directory, Path.GetFileName(imageSource.Uri));
        file
            .Received(1)
            .Create(thumbnailFilePath);
        
        movie.ImageInfos.Should().NotContain(x =>
            x.Path == thumbnailFilePath &&
            x.Type == ImageType.Thumb);
    }

    [Fact]
    public async Task ReturnsFailed_WhenCreateDirectoryThrows_GivenEpisodeAndImageSource()
    {
        //Arrange
        var directory = Substitute.For<IDirectory>();
        var sut = new SetEpisodeThumbnail(_httpClientFactory, _fileSystem.File, directory, _logger); 
        
        directory
            .Exists(Arg.Any<string>())
            .Returns(false);
        
        directory
            .CreateDirectory(Arg.Any<string>())
            .Throws(new Exception());
        
        var episode = EpisodeFaker.Generate();
        var imageBytes = new Faker().Random.Bytes(1024);
        var imageSource = new ImageSource
        {
            Uri = _faker.Internet.UrlWithPath(fileExt: "png"),
            Width = Random.Shared.Next(1, 99999),
            Height = Random.Shared.Next(1, 99999)
        };

        _mediaSourceManager
            .GetPathProtocol(episode.Path)
            .Returns(MediaProtocol.File);

        var mockedRequest = _mockHttpMessageHandler
            .When(imageSource.Uri)
            .Respond(new StreamContent(new MemoryStream(imageBytes)));

        //Act
        var result = await sut.GetAndSetThumbnailAsync(episode, imageSource, CancellationToken.None);

        //Assert
        result.IsFailed.Should().BeTrue();
        directory
            .Received(1)
            .CreateDirectory(_directory);

        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().Be(0, "it should download the thumbnail");
        
        var thumbnailFilePath = Path.Combine(_directory, Path.GetFileName(imageSource.Uri));
        episode.ImageInfos.Should().NotContain(x =>
            x.Path == thumbnailFilePath &&
            x.Type == ImageType.Thumb);
    }

    [Fact]
    public async Task ReturnsFailed_WhenCreateDirectoryThrows_GivenMovieAndValidImageSource()
    {
        //Arrange
        var directory = Substitute.For<IDirectory>();
        var sut = new SetEpisodeThumbnail(_httpClientFactory, _fileSystem.File, directory, _logger);

        directory
            .Exists(Arg.Any<string>())
            .Returns(false);

        directory
            .CreateDirectory(Arg.Any<string>())
            .Throws(new Exception());

        var movie = MovieFaker.Generate();
        var imageBytes = new Faker().Random.Bytes(1024);
        var imageSource = new ImageSource
        {
            Uri = _faker.Internet.UrlWithPath(fileExt: "png"),
            Width = Random.Shared.Next(1, 99999),
            Height = Random.Shared.Next(1, 99999)
        };

        _mediaSourceManager
            .GetPathProtocol(movie.Path)
            .Returns(MediaProtocol.File);

        var mockedRequest = _mockHttpMessageHandler
            .When(imageSource.Uri)
            .Respond(new StreamContent(new MemoryStream(imageBytes)));

        //Act
        var result = await sut.GetAndSetThumbnailAsync(movie, imageSource, CancellationToken.None);

        //Assert
        result.IsFailed.Should().BeTrue();
        directory
            .Received(1)
            .CreateDirectory(_directory);

        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().Be(0, "it should download the thumbnail");

        var thumbnailFilePath = Path.Combine(_directory, Path.GetFileName(imageSource.Uri));
        movie.ImageInfos.Should().NotContain(x =>
            x.Path == thumbnailFilePath &&
            x.Type == ImageType.Thumb);
    }

    [Fact]
    public async Task SkipsCreateDirectory_WhenDirectoryAlreadyExists_GivenEpisodeAndImageSource()
    {
        //Arrange
        var directory = Substitute.For<IDirectory>();
        var sut = new SetEpisodeThumbnail(_httpClientFactory, _fileSystem.File, directory, _logger); 
        
        //create directory on mock filesystem to pass file creation
        _fileSystem.Directory.CreateDirectory(_directory);
        
        //use Substitute to check method calls
        directory
            .Exists(Arg.Any<string>())
            .Returns(true);
        
        var episode = EpisodeFaker.Generate();
        var imageBytes = new Faker().Random.Bytes(1024);
        var imageSource = new ImageSource
        {
            Uri = _faker.Internet.UrlWithPath(fileExt: "png"),
            Width = Random.Shared.Next(1, 99999),
            Height = Random.Shared.Next(1, 99999)
        };

        _mediaSourceManager
            .GetPathProtocol(episode.Path)
            .Returns(MediaProtocol.File);

        var mockedRequest = _mockHttpMessageHandler
            .When(imageSource.Uri)
            .Respond(new StreamContent(new MemoryStream(imageBytes)));

        //Act
        var result = await sut.GetAndSetThumbnailAsync(episode, imageSource, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        directory
            .DidNotReceive()
            .CreateDirectory(_directory);

        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().Be(1, "it should download the thumbnail");
        
        _fileSystem.AllDirectories.Should().Contain(path => path == _directory);
        var thumbnailFilePath = Path.Combine(_directory, Path.GetFileName(imageSource.Uri));
        (await _fileSystem.File.ReadAllBytesAsync(thumbnailFilePath)).Should().BeEquivalentTo(imageBytes);
        
        var imageInfoPrimary = episode.GetImageInfo(ImageType.Primary, 0);
        imageInfoPrimary.Should().NotBeNull();
        imageInfoPrimary.Path.Should().Be(thumbnailFilePath);
        imageInfoPrimary.Width.Should().Be(imageSource.Width);
        imageInfoPrimary.Height.Should().Be(imageSource.Height);
        
        var imageInfoThumb = episode.GetImageInfo(ImageType.Thumb, 0);
        imageInfoThumb.Should().NotBeNull();
        imageInfoThumb.Path.Should().Be(thumbnailFilePath);
        imageInfoThumb.Width.Should().Be(imageSource.Width);
        imageInfoThumb.Height.Should().Be(imageSource.Height);
    }

    [Fact]
    public async Task SkipsCreateDirectory_WhenDirectoryAlreadyExists_GivenMovieAndValidImageSource()
    {
        //Arrange
        var directory = Substitute.For<IDirectory>();
        var sut = new SetEpisodeThumbnail(_httpClientFactory, _fileSystem.File, directory, _logger);

        //create directory on mock filesystem to pass file creation
        _fileSystem.Directory.CreateDirectory(_directory);
        
        //use Substitute to check method calls
        directory
            .Exists(Arg.Any<string>())
            .Returns(true);

        var movie = MovieFaker.Generate();
        var imageBytes = new Faker().Random.Bytes(1024);
        var imageSource = new ImageSource
        {
            Uri = _faker.Internet.UrlWithPath(fileExt: "png"),
            Width = Random.Shared.Next(1, 99999),
            Height = Random.Shared.Next(1, 99999)
        };

        _mediaSourceManager
            .GetPathProtocol(movie.Path)
            .Returns(MediaProtocol.File);

        var mockedRequest = _mockHttpMessageHandler
            .When(imageSource.Uri)
            .Respond(new StreamContent(new MemoryStream(imageBytes)));

        //Act
        var result = await sut.GetAndSetThumbnailAsync(movie, imageSource, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        directory
            .DidNotReceive()
            .CreateDirectory(_directory);

        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().Be(1, "it should download the thumbnail");
        
        _fileSystem.AllDirectories.Should().Contain(path => path == _directory);
        var thumbnailFilePath = Path.Combine(_directory, Path.GetFileName(imageSource.Uri));
        (await _fileSystem.File.ReadAllBytesAsync(thumbnailFilePath)).Should().BeEquivalentTo(imageBytes);
        
        var imageInfoPrimary = movie.GetImageInfo(ImageType.Primary, 0);
        imageInfoPrimary.Should().NotBeNull();
        imageInfoPrimary.Path.Should().Be(thumbnailFilePath);
        imageInfoPrimary.Width.Should().Be(imageSource.Width);
        imageInfoPrimary.Height.Should().Be(imageSource.Height);
        
        var imageInfoThumb = movie.GetImageInfo(ImageType.Thumb, 0);
        imageInfoThumb.Should().NotBeNull();
        imageInfoThumb.Path.Should().Be(thumbnailFilePath);
        imageInfoThumb.Width.Should().Be(imageSource.Width);
        imageInfoThumb.Height.Should().Be(imageSource.Height);
    }

    [Fact]
    public async Task ReturnsFailed_WhenDirectoryCreateThrows_GivenEpisodeAndImageSource()
    {
        //Arrange
        var directory = Substitute.For<IDirectory>();
        var sut = new SetEpisodeThumbnail(_httpClientFactory, _fileSystem.File, directory, _logger); 
        
        directory
            .Exists(Arg.Any<string>())
            .Returns(false);
        
        directory
            .CreateDirectory(Arg.Any<string>())
            .Throws(new Exception());
        
        var episode = EpisodeFaker.Generate();
        var imageBytes = new Faker().Random.Bytes(1024);
        var imageSource = new ImageSource
        {
            Uri = _faker.Internet.UrlWithPath(fileExt: "png"),
            Width = Random.Shared.Next(1, 99999),
            Height = Random.Shared.Next(1, 99999)
        };

        _mediaSourceManager
            .GetPathProtocol(episode.Path)
            .Returns(MediaProtocol.File);

        var mockedRequest = _mockHttpMessageHandler
            .When(imageSource.Uri)
            .Respond(new StreamContent(new MemoryStream(imageBytes)));

        //Act
        var result = await sut.GetAndSetThumbnailAsync(episode, imageSource, CancellationToken.None);

        //Assert
        result.IsFailed.Should().BeTrue();
        directory
            .Received(1)
            .CreateDirectory(_directory);
        
        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().Be(0,
            "it should not download the thumbnail, if directory can not be created");
    }

    [Fact]
    public async Task ReturnsFailed_WhenDirectoryCreateThrows_GivenMovieAndValidImageSource()
    {
        //Arrange
        var directory = Substitute.For<IDirectory>();
        var sut = new SetEpisodeThumbnail(_httpClientFactory, _fileSystem.File, directory, _logger);

        directory
            .Exists(Arg.Any<string>())
            .Returns(false);
        
        directory
            .CreateDirectory(Arg.Any<string>())
            .Throws(new Exception());

        var movie = MovieFaker.Generate();
        var imageBytes = new Faker().Random.Bytes(1024);
        var imageSource = new ImageSource
        {
            Uri = _faker.Internet.UrlWithPath(fileExt: "png"),
            Width = Random.Shared.Next(1, 99999),
            Height = Random.Shared.Next(1, 99999)
        };

        _mediaSourceManager
            .GetPathProtocol(movie.Path)
            .Returns(MediaProtocol.File);

        var mockedRequest = _mockHttpMessageHandler
            .When(imageSource.Uri)
            .Respond(new StreamContent(new MemoryStream(imageBytes)));

        //Act
        var result = await sut.GetAndSetThumbnailAsync(movie, imageSource, CancellationToken.None);

        //Assert
        result.IsFailed.Should().BeTrue();
        directory
            .Received(1)
            .CreateDirectory(_directory);
        
        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().Be(0,
            "it should not download the thumbnail, if directory can not be created");
    }

    [Fact]
    public async Task ReturnsSuccess_WhenThumbnailUriIsEmpty_GivenEpisodeAndImageSource()
    {
        //Arrange
        var episode = EpisodeFaker.Generate();
        var imageSource = new ImageSource
        {
            Uri = string.Empty,
            Width = Random.Shared.Next(1, 99999),
            Height = Random.Shared.Next(1, 99999)
        };

        //Act
        var result = await _sut.GetAndSetThumbnailAsync(episode, imageSource, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        _httpClientFactory
            .DidNotReceive()
            .CreateClient(Arg.Any<string>());
        
        _fileSystem.AllFiles.Should().BeEmpty();
        
        var imageInfoPrimary = episode.GetImageInfo(ImageType.Primary, 0);
        imageInfoPrimary.Should().BeNull();
        
        var imageInfoThumb = episode.GetImageInfo(ImageType.Thumb, 0);
        imageInfoThumb.Should().BeNull();
    }

    [Fact]
    public async Task ReturnsSuccess_WhenThumbnailUriIsEmpty_GivenMovieAndValidImageSource()
    {
        //Arrange
        var movie = MovieFaker.Generate();
        var imageSource = new ImageSource
        {
            Uri = string.Empty,
            Width = Random.Shared.Next(1, 99999),
            Height = Random.Shared.Next(1, 99999)
        };
        
        //Act
        var result = await _sut.GetAndSetThumbnailAsync(movie, imageSource, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        _httpClientFactory
            .DidNotReceive()
            .CreateClient(Arg.Any<string>());
        
        _fileSystem.AllFiles.Should().BeEmpty();
        
        var imageInfoPrimary = movie.GetImageInfo(ImageType.Primary, 0);
        imageInfoPrimary.Should().BeNull();
        
        var imageInfoThumb = movie.GetImageInfo(ImageType.Thumb, 0);
        imageInfoThumb.Should().BeNull();
    }

    [Fact]
    public async Task OverwritesFileExtensionsToJpeg_WhenFileExtensionIsJpe_GivenEpisodeAndImageSource()
    {
        //Arrange
        var episode = EpisodeFaker.Generate();
        var imageBytes = _faker.Random.Bytes(1024);
        var imageSource = new ImageSource
        {
            Uri = _faker.Internet.UrlWithPath(fileExt: "jpe"),
            Width = Random.Shared.Next(1, 99999),
            Height = Random.Shared.Next(1, 99999)
        };
        
        var mockedRequest = _mockHttpMessageHandler
            .When(imageSource.Uri)
            .Respond(new StreamContent(new MemoryStream(imageBytes)));
        
        //Act
        var result = await _sut.GetAndSetThumbnailAsync(episode, imageSource, CancellationToken.None);
        
        //Assert
        result.IsSuccess.Should().BeTrue();
        
        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().Be(1, "it should download the thumbnail");
        
        _fileSystem.AllDirectories.Should().Contain(path => path == _directory);
        var thumbnailFilePath = Path.Combine(_directory, Path.GetFileName(imageSource.Uri))
            .Replace("jpe", "jpeg");
        (await _fileSystem.File.ReadAllBytesAsync(thumbnailFilePath)).Should().BeEquivalentTo(imageBytes);
        
        var imageInfoPrimary = episode.GetImageInfo(ImageType.Primary, 0);
        imageInfoPrimary.Should().NotBeNull();
        imageInfoPrimary.Path.Should().Be(thumbnailFilePath);
        imageInfoPrimary.Width.Should().Be(imageSource.Width);
        imageInfoPrimary.Height.Should().Be(imageSource.Height);
        
        var imageInfoThumb = episode.GetImageInfo(ImageType.Thumb, 0);
        imageInfoThumb.Should().NotBeNull();
        imageInfoThumb.Path.Should().Be(thumbnailFilePath);
        imageInfoThumb.Width.Should().Be(imageSource.Width);
        imageInfoThumb.Height.Should().Be(imageSource.Height);
    }

    [Fact]
    public async Task OverwritesFileExtensionsToJpeg_WhenFileExtensionIsJpe_GivenMovieAndValidImageSource()
    {
        //Arrange
        var movie = MovieFaker.Generate();
        var imageBytes = _faker.Random.Bytes(1024);
        var imageSource = new ImageSource
        {
            Uri = _faker.Internet.UrlWithPath(fileExt: "jpe"),
            Width = Random.Shared.Next(1, 99999),
            Height = Random.Shared.Next(1, 99999)
        };
        
        var mockedRequest = _mockHttpMessageHandler
            .When(imageSource.Uri)
            .Respond(new StreamContent(new MemoryStream(imageBytes)));
        
        //Act
        var result = await _sut.GetAndSetThumbnailAsync(movie, imageSource, CancellationToken.None);
        
        //Assert
        result.IsSuccess.Should().BeTrue();
        
        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().Be(1, "it should download the thumbnail");
        
        _fileSystem.AllDirectories.Should().Contain(path => path == _directory);
        var thumbnailFilePath = Path.Combine(_directory, Path.GetFileName(imageSource.Uri))
            .Replace("jpe", "jpeg");
        (await _fileSystem.File.ReadAllBytesAsync(thumbnailFilePath)).Should().BeEquivalentTo(imageBytes);
        
        var imageInfoPrimary = movie.GetImageInfo(ImageType.Primary, 0);
        imageInfoPrimary.Should().NotBeNull();
        imageInfoPrimary.Path.Should().Be(thumbnailFilePath);
        imageInfoPrimary.Width.Should().Be(imageSource.Width);
        imageInfoPrimary.Height.Should().Be(imageSource.Height);
        
        var imageInfoThumb = movie.GetImageInfo(ImageType.Thumb, 0);
        imageInfoThumb.Should().NotBeNull();
        imageInfoThumb.Path.Should().Be(thumbnailFilePath);
        imageInfoThumb.Width.Should().Be(imageSource.Width);
        imageInfoThumb.Height.Should().Be(imageSource.Height);
    }
}