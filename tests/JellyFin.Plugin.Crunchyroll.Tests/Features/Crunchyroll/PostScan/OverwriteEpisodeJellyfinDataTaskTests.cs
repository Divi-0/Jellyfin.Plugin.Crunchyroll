using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Net;
using Bogus;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.OverwriteEpisodeJellyfinData;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;
using NSubstitute.ExceptionExtensions;
using RichardSzalay.MockHttp;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.PostScan;

public class OverwriteEpisodeJellyfinDataTaskTests
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MockHttpMessageHandler _mockHttpMessageHandler;
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<OverwriteEpisodeJellyfinDataTask> _logger;
    private readonly IOverwriteEpisodeJellyfinDataTaskSession _session;
    private readonly MockFileSystem _fileSystem;
    private readonly OverwriteEpisodeJellyfinDataTask _sut;
    
    private readonly string _directory;
    
    public OverwriteEpisodeJellyfinDataTaskTests()
    {
        _httpClientFactory = Substitute.For<IHttpClientFactory>();
        _libraryManager = MockHelper.LibraryManager;
        _session = Substitute.For<IOverwriteEpisodeJellyfinDataTaskSession>();
        _logger = Substitute.For<ILogger<OverwriteEpisodeJellyfinDataTask>>();
        _mockHttpMessageHandler = new MockHttpMessageHandler();
        _fileSystem = new MockFileSystem();
        
        _directory = Path.Combine(Path.GetDirectoryName(typeof(OverwriteEpisodeJellyfinDataTask).Assembly.Location)!, "episode-thumbnails");
        
        _httpClientFactory
            .CreateClient()
            .Returns(_mockHttpMessageHandler.ToHttpClient());
        
        _sut = new OverwriteEpisodeJellyfinDataTask(_httpClientFactory, _logger, _libraryManager, _session, 
            _fileSystem.File, _fileSystem.Directory);
    }

    [Fact]
    public async Task SetsTitleDescriptionAndThumbnail_WhenSuccessful_GivenEpisodeWithEpisodeId()
    {
        //Arrange
        var episode = EpisodeFaker.GenerateWithEpisodeId();
        var crunchyrollEpisode = CrunchyrollEpisodeFaker.Generate(episode);
        var imageBytes = new Faker().Random.Bytes(1024);
        
        _libraryManager
            .GetItemById(episode.ParentId)
            .Returns((BaseItem?)null);

        _session
            .GetEpisodeAsync(crunchyrollEpisode.Id)
            .Returns(crunchyrollEpisode);

        var mockedRequest = _mockHttpMessageHandler
            .When(crunchyrollEpisode.ThumbnailUrl)
            .Respond(new StreamContent(new MemoryStream(imageBytes)));

        //Act
        await _sut.RunAsync(episode, CancellationToken.None);

        //Assert
        await _session
            .Received(1)
            .GetEpisodeAsync(crunchyrollEpisode.Id);
        
        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().Be(1, "it should download the thumbnail");
        
        _fileSystem.AllDirectories.Should().Contain(path => path == _directory);
        var thumbnailFilePath = Path.Combine(_directory, Path.GetFileName(crunchyrollEpisode.ThumbnailUrl));
        (await _fileSystem.File.ReadAllBytesAsync(thumbnailFilePath)).Should().BeEquivalentTo(imageBytes);
        
        episode.Name.Should().Be(crunchyrollEpisode.Title);
        episode.Overview.Should().Be(crunchyrollEpisode.Description);
        
        var imageInfoPrimary = episode.GetImageInfo(ImageType.Primary, 0);
        imageInfoPrimary.Should().NotBeNull();
        imageInfoPrimary.Path.Should().Be(thumbnailFilePath);
        
        var imageInfoThumb = episode.GetImageInfo(ImageType.Thumb, 0);
        imageInfoThumb.Should().NotBeNull();
        imageInfoThumb.Path.Should().Be(thumbnailFilePath);
        
        await _libraryManager
            .Received(1)
            .UpdateItemAsync(episode, episode.DisplayParent, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DoesNotUpdateEpisode_WhenGetEpisodeFails_GivenEpisodeWithEpisodeId()
    {
        //Arrange
        var episode = EpisodeFaker.GenerateWithEpisodeId();
        var crunchyrollEpisode = CrunchyrollEpisodeFaker.Generate(episode);
        
        _libraryManager
            .GetItemById(episode.ParentId)
            .Returns((BaseItem?)null);

        _session
            .GetEpisodeAsync(crunchyrollEpisode.Id)
            .Returns(Result.Fail("error"));

        //Act
        await _sut.RunAsync(episode, CancellationToken.None);

        //Assert
        await _session
            .Received(1)
            .GetEpisodeAsync(crunchyrollEpisode.Id);
        
        var thumbnailFilePath = Path.Combine(_directory, Path.GetFileName(crunchyrollEpisode.ThumbnailUrl));
        _fileSystem.File.Exists(thumbnailFilePath).Should().BeFalse("it could not get the episode from database");
        
        await _libraryManager
            .DidNotReceive()
            .UpdateItemAsync(episode, episode.DisplayParent, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DoesNotUpdateEpisode_WhenThumbnailRequestThrows_GivenEpisodeWithEpisodeId()
    {
        //Arrange
        var episode = EpisodeFaker.GenerateWithEpisodeId();
        var crunchyrollEpisode = CrunchyrollEpisodeFaker.Generate(episode);
        
        _libraryManager
            .GetItemById(episode.ParentId)
            .Returns((BaseItem?)null);

        _session
            .GetEpisodeAsync(crunchyrollEpisode.Id)
            .Returns(crunchyrollEpisode);

        var mockedRequest = _mockHttpMessageHandler
            .When(crunchyrollEpisode.ThumbnailUrl)
            .Throw(new Exception());

        //Act
        await _sut.RunAsync(episode, CancellationToken.None);

        //Assert
        await _session
            .Received(1)
            .GetEpisodeAsync(crunchyrollEpisode.Id);
        
        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().Be(1, "it should try to download the thumbnail");
        
        var thumbnailFilePath = Path.Combine(_directory, Path.GetFileName(crunchyrollEpisode.ThumbnailUrl));
        _fileSystem.File.Exists(thumbnailFilePath).Should().BeFalse();
        
        episode.Name.Should().Be(crunchyrollEpisode.Title);
        episode.Overview.Should().Be(crunchyrollEpisode.Description);
        episode.ImageInfos.Should().NotContain(x => 
            x.Path == thumbnailFilePath &&
            x.Type == ImageType.Thumb);
        
        await _libraryManager
            .Received(1)
            .UpdateItemAsync(episode, episode.DisplayParent, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdatesOnlyTitleAndDescription_WhenThumbnailRequestNotSuccessStatusCode_GivenEpisodeWithEpisodeId()
    {
        //Arrange
        var episode = EpisodeFaker.GenerateWithEpisodeId();
        var crunchyrollEpisode = CrunchyrollEpisodeFaker.Generate(episode);
        
        _libraryManager
            .GetItemById(episode.ParentId)
            .Returns((BaseItem?)null);

        _session
            .GetEpisodeAsync(crunchyrollEpisode.Id)
            .Returns(crunchyrollEpisode);

        var mockedRequest = _mockHttpMessageHandler
            .When(crunchyrollEpisode.ThumbnailUrl)
            .Respond(HttpStatusCode.BadRequest);

        //Act
        await _sut.RunAsync(episode, CancellationToken.None);

        //Assert
        await _session
            .Received(1)
            .GetEpisodeAsync(crunchyrollEpisode.Id);
        
        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().Be(1, "it should try to download the thumbnail");
        
        var thumbnailFilePath = Path.Combine(_directory, Path.GetFileName(crunchyrollEpisode.ThumbnailUrl));
        _fileSystem.File.Exists(thumbnailFilePath).Should().BeFalse();
        
        episode.Name.Should().Be(crunchyrollEpisode.Title);
        episode.Overview.Should().Be(crunchyrollEpisode.Description);
        episode.ImageInfos.Should().NotContain(x => 
            x.Path == thumbnailFilePath &&
            x.Type == ImageType.Thumb);
        
        await _libraryManager
            .Received(1)
            .UpdateItemAsync(episode, episode.DisplayParent, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task DoesNotUpdateEpisode_WhenCreateFileThrows_GivenEpisodeWithEpisodeId()
    {
        //Arrange
        var episode = EpisodeFaker.GenerateWithEpisodeId();
        var crunchyrollEpisode = CrunchyrollEpisodeFaker.Generate(episode);
        var imageBytes = new Faker().Random.Bytes(1024);
        
        _libraryManager
            .GetItemById(episode.ParentId)
            .Returns((BaseItem?)null);

        _session
            .GetEpisodeAsync(crunchyrollEpisode.Id)
            .Returns(crunchyrollEpisode);

        var mockedRequest = _mockHttpMessageHandler
            .When(crunchyrollEpisode.ThumbnailUrl)
            .Respond(new StreamContent(new MemoryStream(imageBytes)));

        var file = Substitute.For<IFile>();
        file
            .Create(Arg.Any<string>())
            .Throws(new Exception());
        
        var sut = new OverwriteEpisodeJellyfinDataTask(_httpClientFactory, _logger, _libraryManager, _session, 
            file, _fileSystem.Directory);

        //Act
        await sut.RunAsync(episode, CancellationToken.None);

        //Assert
        await _session
            .Received(1)
            .GetEpisodeAsync(crunchyrollEpisode.Id);
        
        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().Be(1, "it should download the thumbnail");
        
        var thumbnailFilePath = Path.Combine(_directory, Path.GetFileName(crunchyrollEpisode.ThumbnailUrl));
        file
            .Received(1)
            .Create(thumbnailFilePath);
        
        await _libraryManager
            .DidNotReceive()
            .UpdateItemAsync(episode, episode.DisplayParent, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task DoesNotUpdateEpisode_WhenCreateDirectoryThrows_GivenEpisodeWithEpisodeId()
    {
        //Arrange
        var episode = EpisodeFaker.GenerateWithEpisodeId();
        var crunchyrollEpisode = CrunchyrollEpisodeFaker.Generate(episode);
        var imageBytes = new Faker().Random.Bytes(1024);
        
        _libraryManager
            .GetItemById(episode.ParentId)
            .Returns((BaseItem?)null);

        _session
            .GetEpisodeAsync(crunchyrollEpisode.Id)
            .Returns(crunchyrollEpisode);

        var mockedRequest = _mockHttpMessageHandler
            .When(crunchyrollEpisode.ThumbnailUrl)
            .Respond(new StreamContent(new MemoryStream(imageBytes)));

        var directory = Substitute.For<IDirectory>();
        directory
            .Exists(Arg.Any<string>())
            .Returns(false);
        
        directory
            .CreateDirectory(Arg.Any<string>())
            .Throws(new Exception());
        
        var sut = new OverwriteEpisodeJellyfinDataTask(_httpClientFactory, _logger, _libraryManager, _session, 
            _fileSystem.File, directory);

        //Act
        await sut.RunAsync(episode, CancellationToken.None);

        //Assert
        directory
            .Received(1)
            .CreateDirectory(_directory);
        
        await _session
            .DidNotReceive()
            .GetEpisodeAsync(crunchyrollEpisode.Id);
        
        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().Be(0,
            "it should not download the thumbnail, if directory can not be created");
        
        await _libraryManager
            .DidNotReceive()
            .UpdateItemAsync(episode, episode.DisplayParent, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task SkipsCreateDirectory_WhenDirectoryAlreadyExists_GivenEpisodeWithEpisodeId()
    {
        //Arrange
        var episode = EpisodeFaker.GenerateWithEpisodeId();
        var crunchyrollEpisode = CrunchyrollEpisodeFaker.Generate(episode);
        var imageBytes = new Faker().Random.Bytes(1024);
        
        _libraryManager
            .GetItemById(episode.ParentId)
            .Returns((BaseItem?)null);

        _session
            .GetEpisodeAsync(crunchyrollEpisode.Id)
            .Returns(crunchyrollEpisode);

        var mockedRequest = _mockHttpMessageHandler
            .When(crunchyrollEpisode.ThumbnailUrl)
            .Respond(new StreamContent(new MemoryStream(imageBytes)));

        //create directory on mock filesystem to pass file creation
        _fileSystem.Directory.CreateDirectory(_directory);
        
        //use Substitute to check method calls
        var directory = Substitute.For<IDirectory>();
        directory
            .Exists(Arg.Any<string>())
            .Returns(true);
        
        var sut = new OverwriteEpisodeJellyfinDataTask(_httpClientFactory, _logger, _libraryManager, _session, 
            _fileSystem.File, directory);

        //Act
        await sut.RunAsync(episode, CancellationToken.None);

        //Assert
        await _session
            .Received(1)
            .GetEpisodeAsync(crunchyrollEpisode.Id);
        
        directory
            .DidNotReceive()
            .CreateDirectory(_directory);
        
        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().Be(1);
        
        await _libraryManager
            .Received(1)
            .UpdateItemAsync(episode, episode.DisplayParent, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task SkipsCreateDirectory_WhenDirectoryCreateThrows_GivenEpisodeWithEpisodeId()
    {
        //Arrange
        var episode = EpisodeFaker.GenerateWithEpisodeId();
        var crunchyrollEpisode = CrunchyrollEpisodeFaker.Generate(episode);
        var imageBytes = new Faker().Random.Bytes(1024);
        
        _libraryManager
            .GetItemById(episode.ParentId)
            .Returns((BaseItem?)null);

        _session
            .GetEpisodeAsync(crunchyrollEpisode.Id)
            .Returns(crunchyrollEpisode);

        var mockedRequest = _mockHttpMessageHandler
            .When(crunchyrollEpisode.ThumbnailUrl)
            .Respond(new StreamContent(new MemoryStream(imageBytes)));
        
        //use Substitute to check method calls
        var directory = Substitute.For<IDirectory>();
        directory
            .Exists(Arg.Any<string>())
            .Returns(false);
        
        directory
            .CreateDirectory(Arg.Any<string>())
            .Throws(new Exception());
        
        var sut = new OverwriteEpisodeJellyfinDataTask(_httpClientFactory, _logger, _libraryManager, _session, 
            _fileSystem.File, directory);

        //Act
        await sut.RunAsync(episode, CancellationToken.None);

        //Assert
        await _session
            .DidNotReceive()
            .GetEpisodeAsync(crunchyrollEpisode.Id);
        
        directory
            .Received(1)
            .CreateDirectory(_directory);
        
        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().Be(0,
            "it should not download the thumbnail, if directory can not be created");
        
        await _libraryManager
            .DidNotReceive()
            .UpdateItemAsync(episode, episode.DisplayParent, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task SkipsDownloadThumbnail_WhenRequestRespondsNotFound_GivenEpisodeWithEpisodeId()
    {
        //Arrange
        var episode = EpisodeFaker.GenerateWithEpisodeId();
        var crunchyrollEpisode = CrunchyrollEpisodeFaker.Generate(episode);
        
        _libraryManager
            .GetItemById(episode.ParentId)
            .Returns((BaseItem?)null);

        _session
            .GetEpisodeAsync(crunchyrollEpisode.Id)
            .Returns(crunchyrollEpisode);

        var mockedRequest = _mockHttpMessageHandler
            .When(crunchyrollEpisode.ThumbnailUrl)
            .Respond(HttpStatusCode.NotFound);

        //Act
        await _sut.RunAsync(episode, CancellationToken.None);

        //Assert
        await _session
            .Received(1)
            .GetEpisodeAsync(crunchyrollEpisode.Id);
        
        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().Be(1, "it should try to download the thumbnail");
        
        var thumbnailFilePath = Path.Combine(_directory, Path.GetFileName(crunchyrollEpisode.ThumbnailUrl));
        _fileSystem.File.Exists(thumbnailFilePath).Should().BeFalse();
        
        episode.Name.Should().Be(crunchyrollEpisode.Title);
        episode.Overview.Should().Be(crunchyrollEpisode.Description);
        episode.ImageInfos.Should().NotContain(x => 
            x.Path == thumbnailFilePath &&
            x.Type == ImageType.Thumb);
        
        await _libraryManager
            .Received(1)
            .UpdateItemAsync(episode, episode.DisplayParent, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task SkipsItem_WhenItemHasNoEpisodeId_GivenEpisodeWithoutEpisodeId()
    {
        //Arrange
        var episode = EpisodeFaker.Generate();

        _libraryManager
            .GetItemById(episode.ParentId)
            .Returns((BaseItem?)null);

        //Act
        await _sut.RunAsync(episode, CancellationToken.None);

        //Assert
        await _session
            .DidNotReceive()
            .GetEpisodeAsync(Arg.Any<string>());
        
        await _libraryManager
            .DidNotReceive()
            .UpdateItemAsync(episode, episode.DisplayParent, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
    }
}