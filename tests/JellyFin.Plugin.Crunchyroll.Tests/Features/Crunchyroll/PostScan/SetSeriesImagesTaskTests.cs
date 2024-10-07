using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Bogus;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Series;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;
using NSubstitute.ExceptionExtensions;

namespace JellyFin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.PostScan;

public class SetSeriesImagesTaskTests
{
    private readonly SetSeriesImagesTask _sut;

    private readonly ILibraryManager _libraryManager;
    private readonly IGetTitleMetadata _getTitleMetadata;
    private readonly MockFileSystem _fileSystem;
    private readonly ICrunchyrollSeriesClient _crunchyrollSeriesClient;
    private readonly ILogger<SetSeriesImagesTask> _logger;
    
    private readonly Faker _faker;

    private readonly string _directory;

    public SetSeriesImagesTaskTests()
    {
        _faker = new Faker();
        
        _libraryManager = Substitute.For<ILibraryManager>();
        _getTitleMetadata = Substitute.For<IGetTitleMetadata>();
        _crunchyrollSeriesClient = Substitute.For<ICrunchyrollSeriesClient>();
        _fileSystem = new MockFileSystem();
        _logger = Substitute.For<ILogger<SetSeriesImagesTask>>();
        
        _directory = Path.Combine(Path.GetDirectoryName(typeof(SetSeriesImagesTask).Assembly.Location)!, "series-images");

        _sut = new SetSeriesImagesTask(_libraryManager, _getTitleMetadata, _fileSystem.File, _crunchyrollSeriesClient,
            _logger, _fileSystem.Directory);
    }

    [Fact]
    public async Task StoresImagesToFileSystemAndUpdatesJellyfinItem_WhenRunsTask_GivenSeriesWithTitleId()
    {
        //Arrange
        var series = SeriesFaker.GenerateWithTitleId();

        var titleMetadata = new Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata
        {
            TitleId = string.Empty,
            SlugTitle = string.Empty,
            Description = string.Empty,
            Title = string.Empty,
            Studio = string.Empty,
            PosterTallUri = _faker.Internet.UrlWithPath(fileExt: "jpg"),
            PosterWideUri = _faker.Internet.UrlWithPath(fileExt: "jpg")
        };
        
        _getTitleMetadata
            .GetTitleMetadataAsync(Arg.Any<string>())
            .Returns(titleMetadata);

        var posterTallBytes = _faker.Random.Bytes(1000);
        _crunchyrollSeriesClient
            .GetPosterImagesAsync(titleMetadata.PosterTallUri, Arg.Any<CancellationToken>())
            .Returns(new MemoryStream(posterTallBytes));
        
        var posterWideBytes = _faker.Random.Bytes(1000);
        _crunchyrollSeriesClient
            .GetPosterImagesAsync(titleMetadata.PosterWideUri, Arg.Any<CancellationToken>())
            .Returns(new MemoryStream(posterWideBytes));
        
        //Act
        await _sut.RunAsync(series, CancellationToken.None);
        
        //Assert
        await _getTitleMetadata
            .Received(1)
            .GetTitleMetadataAsync(series.ProviderIds[CrunchyrollExternalKeys.Id]);

        await _crunchyrollSeriesClient
            .Received(1)
            .GetPosterImagesAsync(titleMetadata.PosterTallUri, Arg.Any<CancellationToken>());

        await _crunchyrollSeriesClient
            .Received(1)
            .GetPosterImagesAsync(titleMetadata.PosterWideUri, Arg.Any<CancellationToken>());

        _fileSystem.AllDirectories.Should().Contain(path => path == _directory);

        var posterTallFilePath = Path.Combine(_directory, Path.GetFileName(titleMetadata.PosterTallUri));
        var posterWideFilePath = Path.Combine(_directory, Path.GetFileName(titleMetadata.PosterWideUri));

        (await _fileSystem.File.ReadAllBytesAsync(posterTallFilePath)).Should().BeEquivalentTo(posterTallBytes);
        (await _fileSystem.File.ReadAllBytesAsync(posterWideFilePath)).Should().BeEquivalentTo(posterWideBytes);
        
        await _libraryManager
            .Received(1)
            .UpdateImagesAsync(series, forceUpdate: true);
        
        series.ImageInfos.Should().Contain(x => 
            x.Path == posterTallFilePath &&
            x.Type == ImageType.Primary);
        
        series.ImageInfos.Should().Contain(x => 
            x.Path == posterWideFilePath &&
            x.Type == ImageType.Backdrop);
    }
    
    [Fact]
    public async Task IgnoresSeries_WhenNoTitleMetadataFound_GivenSeriesWithTitleId()
    {
        //Arrange
        var series = SeriesFaker.GenerateWithTitleId();
        
        _getTitleMetadata
            .GetTitleMetadataAsync(Arg.Any<string>())
            .Returns((Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata?)null);
        
        //Act
        await _sut.RunAsync(series, CancellationToken.None);
        
        //Assert
        await _getTitleMetadata
            .Received(1)
            .GetTitleMetadataAsync(series.ProviderIds[CrunchyrollExternalKeys.Id]);

        await _crunchyrollSeriesClient
            .DidNotReceive()
            .GetPosterImagesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());

        _fileSystem.AllFiles.Should().BeEmpty();
        
        await _libraryManager
            .DidNotReceive()
            .UpdateImagesAsync(series, forceUpdate: true);
    }
    
    [Fact]
    public async Task IgnoresSeries_WhenSeriesHasNoTitleId_GivenSeriesWithoutTitleId()
    {
        //Arrange
        var series = SeriesFaker.Generate();
        
        //Act
        await _sut.RunAsync(series, CancellationToken.None);
        
        //Assert
        await _getTitleMetadata
            .DidNotReceive()
            .GetTitleMetadataAsync(Arg.Any<string>());

        await _crunchyrollSeriesClient
            .DidNotReceive()
            .GetPosterImagesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());

        _fileSystem.AllFiles.Should().BeEmpty();
        
        await _libraryManager
            .DidNotReceive()
            .UpdateImagesAsync(series, forceUpdate: true);
    }

    [Fact]
    public async Task DoesNotAddImage_WhenGetPosterImageFails_GivenSeriesWithTitleId()
    {
        //Arrange
        var series = SeriesFaker.GenerateWithTitleId();

        var titleMetadata = new Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata
        {
            TitleId = string.Empty,
            SlugTitle = string.Empty,
            Description = string.Empty,
            Title = string.Empty,
            Studio = string.Empty,
            PosterTallUri = _faker.Internet.UrlWithPath(fileExt: "jpg"),
            PosterWideUri = _faker.Internet.UrlWithPath(fileExt: "jpg")
        };
        
        _getTitleMetadata
            .GetTitleMetadataAsync(Arg.Any<string>())
            .Returns(titleMetadata);
        
        _crunchyrollSeriesClient
            .GetPosterImagesAsync(titleMetadata.PosterTallUri, Arg.Any<CancellationToken>())
            .Returns(Result.Fail("error"));
        
        var posterWideBytes = _faker.Random.Bytes(1000);
        _crunchyrollSeriesClient
            .GetPosterImagesAsync(titleMetadata.PosterWideUri, Arg.Any<CancellationToken>())
            .Returns(new MemoryStream(posterWideBytes));
        
        //Act
        await _sut.RunAsync(series, CancellationToken.None);
        
        //Assert
        await _getTitleMetadata
            .Received(1)
            .GetTitleMetadataAsync(series.ProviderIds[CrunchyrollExternalKeys.Id]);

        await _crunchyrollSeriesClient
            .Received(1)
            .GetPosterImagesAsync(titleMetadata.PosterTallUri, Arg.Any<CancellationToken>());

        await _crunchyrollSeriesClient
            .Received(1)
            .GetPosterImagesAsync(titleMetadata.PosterWideUri, Arg.Any<CancellationToken>());

        var posterTallFilePath = Path.Combine(_directory, Path.GetFileName(titleMetadata.PosterTallUri));
        var posterWideFilePath = Path.Combine(_directory, Path.GetFileName(titleMetadata.PosterWideUri));

        _fileSystem.AllFiles.Should().NotContain(posterTallFilePath);
        (await _fileSystem.File.ReadAllBytesAsync(posterWideFilePath)).Should().BeEquivalentTo(posterWideBytes);
        
        await _libraryManager
            .Received(1)
            .UpdateImagesAsync(series, forceUpdate: true);
        
        series.ImageInfos.Should().NotContain(x => 
            x.Path == posterTallFilePath &&
            x.Type == ImageType.Primary);
        
        series.ImageInfos.Should().Contain(x => 
            x.Path == posterWideFilePath &&
            x.Type == ImageType.Backdrop);
    }

    [Fact]
    public async Task DoesNotUpdateJellyfinItem_WhenBothGetPosterImageFails_GivenSeriesWithTitleId()
    {
        //Arrange
        var series = SeriesFaker.GenerateWithTitleId();

        var titleMetadata = new Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata
        {
            TitleId = string.Empty,
            SlugTitle = string.Empty,
            Description = string.Empty,
            Title = string.Empty,
            Studio = string.Empty,
            PosterTallUri = _faker.Internet.UrlWithPath(fileExt: "jpg"),
            PosterWideUri = _faker.Internet.UrlWithPath(fileExt: "jpg")
        };
        
        _getTitleMetadata
            .GetTitleMetadataAsync(Arg.Any<string>())
            .Returns(titleMetadata);
        
        _crunchyrollSeriesClient
            .GetPosterImagesAsync(titleMetadata.PosterTallUri, Arg.Any<CancellationToken>())
            .Returns(Result.Fail("error"));
        
        _crunchyrollSeriesClient
            .GetPosterImagesAsync(titleMetadata.PosterWideUri, Arg.Any<CancellationToken>())
            .Returns(Result.Fail("error"));
        
        //Act
        await _sut.RunAsync(series, CancellationToken.None);
        
        //Assert
        await _getTitleMetadata
            .Received(1)
            .GetTitleMetadataAsync(series.ProviderIds[CrunchyrollExternalKeys.Id]);

        await _crunchyrollSeriesClient
            .Received(1)
            .GetPosterImagesAsync(titleMetadata.PosterTallUri, Arg.Any<CancellationToken>());

        await _crunchyrollSeriesClient
            .Received(1)
            .GetPosterImagesAsync(titleMetadata.PosterWideUri, Arg.Any<CancellationToken>());

        var posterTallFilePath = Path.Combine(_directory, Path.GetFileName(titleMetadata.PosterTallUri));
        var posterWideFilePath = Path.Combine(_directory, Path.GetFileName(titleMetadata.PosterWideUri));

        _fileSystem.AllFiles.Should().NotContain(posterTallFilePath);
        _fileSystem.AllFiles.Should().NotContain(posterWideFilePath);
        
        await _libraryManager
            .DidNotReceive()
            .UpdateImagesAsync(series, forceUpdate: true);
        
        series.ImageInfos.Should().NotContain(x => 
            x.Path == posterTallFilePath &&
            x.Type == ImageType.Primary);
        
        series.ImageInfos.Should().NotContain(x => 
            x.Path == posterWideFilePath &&
            x.Type == ImageType.Backdrop);
    }
    
    

    [Fact]
    public async Task DoesNotAddImage_WhenCreateFileThrows_GivenSeriesWithTitleId()
    {
        //Arrange
        var series = SeriesFaker.GenerateWithTitleId();

        var titleMetadata = new Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata
        {
            TitleId = string.Empty,
            SlugTitle = string.Empty,
            Description = string.Empty,
            Title = string.Empty,
            Studio = string.Empty,
            PosterTallUri = _faker.Internet.UrlWithPath(fileExt: "jpg"),
            PosterWideUri = _faker.Internet.UrlWithPath(fileExt: "jpg")
        };
        
        _getTitleMetadata
            .GetTitleMetadataAsync(Arg.Any<string>())
            .Returns(titleMetadata);
        
        var posterTallBytes = _faker.Random.Bytes(1000);
        _crunchyrollSeriesClient
            .GetPosterImagesAsync(titleMetadata.PosterTallUri, Arg.Any<CancellationToken>())
            .Returns(new MemoryStream(posterTallBytes));
        
        var posterWideBytes = _faker.Random.Bytes(1000);
        _crunchyrollSeriesClient
            .GetPosterImagesAsync(titleMetadata.PosterWideUri, Arg.Any<CancellationToken>())
            .Returns(new MemoryStream(posterWideBytes));
        
        var file = Substitute.For<IFile>();
        file
            .Create(Arg.Any<string>())
            .Throws(new Exception());
        
        var sut = new SetSeriesImagesTask(_libraryManager, _getTitleMetadata, file, _crunchyrollSeriesClient,
            _logger, _fileSystem.Directory);
        
        //Act
        await sut.RunAsync(series, CancellationToken.None);
        
        //Assert
        await _getTitleMetadata
            .Received(1)
            .GetTitleMetadataAsync(series.ProviderIds[CrunchyrollExternalKeys.Id]);

        await _crunchyrollSeriesClient
            .Received(1)
            .GetPosterImagesAsync(titleMetadata.PosterTallUri, Arg.Any<CancellationToken>());

        await _crunchyrollSeriesClient
            .Received(1)
            .GetPosterImagesAsync(titleMetadata.PosterWideUri, Arg.Any<CancellationToken>());

        var posterTallFilePath = Path.Combine(_directory, Path.GetFileName(titleMetadata.PosterTallUri));
        var posterWideFilePath = Path.Combine(_directory, Path.GetFileName(titleMetadata.PosterWideUri));

        _fileSystem.AllFiles.Should().NotContain(posterTallFilePath);
        _fileSystem.AllFiles.Should().NotContain(posterTallFilePath);
        
        await _libraryManager
            .DidNotReceive()
            .UpdateImagesAsync(series, forceUpdate: true);
        
        series.ImageInfos.Should().NotContain(x => 
            x.Path == posterTallFilePath &&
            x.Type == ImageType.Primary);
        
        series.ImageInfos.Should().NotContain(x => 
            x.Path == posterWideFilePath &&
            x.Type == ImageType.Backdrop);
    }
}