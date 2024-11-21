using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Bogus;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.OverwriteSeriesJellyfinData;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Image.Entites;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Series;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;
using NSubstitute.ExceptionExtensions;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.PostScan;

public class OverwriteSeriesJellyfinDataTaskTests
{
    private readonly OverwriteSeriesJellyfinDataTask _sut;

    private readonly ILibraryManager _libraryManager;
    private readonly IGetTitleMetadata _getTitleMetadata;
    private readonly MockFileSystem _fileSystem;
    private readonly ICrunchyrollSeriesClient _crunchyrollSeriesClient;
    private readonly ILogger<OverwriteSeriesJellyfinDataTask> _logger;
    
    private readonly Faker _faker;

    private readonly string _directory;

    public OverwriteSeriesJellyfinDataTaskTests()
    {
        _faker = new Faker();
        
        _libraryManager = MockHelper.LibraryManager;
        _getTitleMetadata = Substitute.For<IGetTitleMetadata>();
        _crunchyrollSeriesClient = Substitute.For<ICrunchyrollSeriesClient>();
        _fileSystem = new MockFileSystem();
        _logger = Substitute.For<ILogger<OverwriteSeriesJellyfinDataTask>>();
        
        _directory = Path.Combine(Path.GetDirectoryName(typeof(OverwriteSeriesJellyfinDataTask).Assembly.Location)!, "series-images");

        _sut = new OverwriteSeriesJellyfinDataTask(_libraryManager, _getTitleMetadata, _fileSystem.File, _crunchyrollSeriesClient,
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
            Description = _faker.Lorem.Sentences(),
            Title = _faker.Random.Words(),
            Studio = _faker.Random.Words(),
            PosterTall = new ImageSource
            {
                Uri = _faker.Internet.UrlWithPath(fileExt: "jpg"),
                Height = 1,
                Width = 1
            },
            PosterWide = new ImageSource
            {
                Uri = _faker.Internet.UrlWithPath(fileExt: "jpg"),
                Height = 1,
                Width = 1
            }
        };

        _libraryManager
            .GetItemById(series.ParentId)
            .Returns((BaseItem?)null);
        
        _getTitleMetadata
            .GetTitleMetadataAsync(Arg.Any<string>())
            .Returns(titleMetadata);

        var posterTallBytes = _faker.Random.Bytes(1000);
        _crunchyrollSeriesClient
            .GetPosterImagesAsync(titleMetadata.PosterTall.Uri, Arg.Any<CancellationToken>())
            .Returns(new MemoryStream(posterTallBytes));
        
        var posterWideBytes = _faker.Random.Bytes(1000);
        _crunchyrollSeriesClient
            .GetPosterImagesAsync(titleMetadata.PosterWide.Uri, Arg.Any<CancellationToken>())
            .Returns(new MemoryStream(posterWideBytes));
        
        //Act
        await _sut.RunAsync(series, CancellationToken.None);
        
        //Assert
        series.Name.Should().Be(titleMetadata.Title);
        series.Overview.Should().Be(titleMetadata.Description);
        series.Studios.Should().BeEquivalentTo([titleMetadata.Studio]);
        
        await _getTitleMetadata
            .Received(1)
            .GetTitleMetadataAsync(series.ProviderIds[CrunchyrollExternalKeys.SeriesId]);

        await _crunchyrollSeriesClient
            .Received(1)
            .GetPosterImagesAsync(titleMetadata.PosterTall.Uri, Arg.Any<CancellationToken>());

        await _crunchyrollSeriesClient
            .Received(1)
            .GetPosterImagesAsync(titleMetadata.PosterWide.Uri, Arg.Any<CancellationToken>());

        _fileSystem.AllDirectories.Should().Contain(path => path == _directory);

        var posterTallFilePath = Path.Combine(_directory, Path.GetFileName(titleMetadata.PosterTall.Uri));
        var posterWideFilePath = Path.Combine(_directory, Path.GetFileName(titleMetadata.PosterWide.Uri));

        (await _fileSystem.File.ReadAllBytesAsync(posterTallFilePath)).Should().BeEquivalentTo(posterTallBytes);
        (await _fileSystem.File.ReadAllBytesAsync(posterWideFilePath)).Should().BeEquivalentTo(posterWideBytes);
        
        await _libraryManager
            .Received(1)
            .UpdateItemAsync(series, series.DisplayParent, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
        
        series.ImageInfos.Should().Contain(x => 
            x.Path == posterTallFilePath &&
            x.Type == ImageType.Primary &&
            x.Width == titleMetadata.PosterTall.Width &&
            x.Height == titleMetadata.PosterTall.Height);
        
        series.ImageInfos.Should().Contain(x => 
            x.Path == posterWideFilePath &&
            x.Type == ImageType.Backdrop &&
            x.Width == titleMetadata.PosterTall.Width &&
            x.Height == titleMetadata.PosterTall.Height);
    }

    [Fact]
    public async Task SkipsGetImage_WhenImageAlreadySet_GivenSeriesWithTitleId()
    {
        //Arrange
        var series = SeriesFaker.GenerateWithTitleId();

        var titleMetadata = new Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata
        {
            TitleId = string.Empty,
            SlugTitle = string.Empty,
            Description = _faker.Lorem.Sentences(),
            Title = _faker.Random.Words(),
            Studio = _faker.Random.Words(),
            PosterTall = new ImageSource
            {
                Uri = _faker.Internet.UrlWithPath(fileExt: "jpg"),
                Height = 1,
                Width = 1
            },
            PosterWide = new ImageSource
            {
                Uri = _faker.Internet.UrlWithPath(fileExt: "jpg"),
                Height = 1,
                Width = 1
            }
        };
        
        var posterTallFilePath = Path.Combine(_directory, Path.GetFileName(titleMetadata.PosterTall.Uri));
        var posterWideFilePath = Path.Combine(_directory, Path.GetFileName(titleMetadata.PosterWide.Uri));
        
        series.SetImage(new ItemImageInfo()
        {
            Path = posterTallFilePath,
            Type = ImageType.Primary,
            Width = titleMetadata.PosterTall.Width,
            Height = titleMetadata.PosterTall.Height
        }, 0);        
        
        series.SetImage(new ItemImageInfo()
        {
            Path = posterWideFilePath,
            Type = ImageType.Backdrop,
            Width = titleMetadata.PosterWide.Width,
            Height = titleMetadata.PosterWide.Height
        }, 0);

        _libraryManager
            .GetItemById(series.ParentId)
            .Returns((BaseItem?)null);
        
        _getTitleMetadata
            .GetTitleMetadataAsync(Arg.Any<string>())
            .Returns(titleMetadata);
        
        //Act
        await _sut.RunAsync(series, CancellationToken.None);
        
        //Assert
        series.Name.Should().Be(titleMetadata.Title);
        series.Overview.Should().Be(titleMetadata.Description);
        series.Studios.Should().BeEquivalentTo([titleMetadata.Studio]);
        
        await _getTitleMetadata
            .Received(1)
            .GetTitleMetadataAsync(series.ProviderIds[CrunchyrollExternalKeys.SeriesId]);

        await _crunchyrollSeriesClient
            .DidNotReceive()
            .GetPosterImagesAsync(titleMetadata.PosterTall.Uri, Arg.Any<CancellationToken>());

        await _crunchyrollSeriesClient
            .DidNotReceive()
            .GetPosterImagesAsync(titleMetadata.PosterWide.Uri, Arg.Any<CancellationToken>());
        
        await _libraryManager
            .Received(1)
            .UpdateItemAsync(series, series.DisplayParent, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
        
        series.ImageInfos.Should().Contain(x => 
            x.Path == posterTallFilePath &&
            x.Type == ImageType.Primary &&
            x.Width == titleMetadata.PosterTall.Width &&
            x.Height == titleMetadata.PosterTall.Height);
        
        series.ImageInfos.Should().Contain(x => 
            x.Path == posterWideFilePath &&
            x.Type == ImageType.Backdrop &&
            x.Width == titleMetadata.PosterTall.Width &&
            x.Height == titleMetadata.PosterTall.Height);
    }
    
    [Fact]
    public async Task IgnoresSeries_WhenNoTitleMetadataFound_GivenSeriesWithTitleId()
    {
        //Arrange
        var series = SeriesFaker.GenerateWithTitleId();
        
        _libraryManager
            .GetItemById(series.ParentId)
            .Returns((BaseItem?)null);
        
        _getTitleMetadata
            .GetTitleMetadataAsync(Arg.Any<string>())
            .Returns((Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata?)null);
        
        //Act
        await _sut.RunAsync(series, CancellationToken.None);
        
        //Assert
        await _getTitleMetadata
            .Received(1)
            .GetTitleMetadataAsync(series.ProviderIds[CrunchyrollExternalKeys.SeriesId]);

        await _crunchyrollSeriesClient
            .DidNotReceive()
            .GetPosterImagesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());

        _fileSystem.AllFiles.Should().BeEmpty();
        
        await _libraryManager
            .DidNotReceive()
            .UpdateItemAsync(series, series.DisplayParent, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task IgnoresSeries_WhenSeriesHasNoTitleId_GivenSeriesWithoutTitleId()
    {
        //Arrange
        var series = SeriesFaker.Generate();
        
        _libraryManager
            .GetItemById(series.ParentId)
            .Returns((BaseItem?)null);
        
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
            .UpdateItemAsync(series, series.DisplayParent, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DoesNotAddImage_WhenGetPosterImageFails_GivenSeriesWithTitleId()
    {
        //Arrange
        var series = SeriesFaker.GenerateWithTitleId();
        
        _libraryManager
            .GetItemById(series.ParentId)
            .Returns((BaseItem?)null);

        var titleMetadata = new Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata
        {
            TitleId = string.Empty,
            SlugTitle = string.Empty,
            Description = _faker.Lorem.Sentences(),
            Title = _faker.Random.Words(),
            Studio = _faker.Random.Words(),
            PosterTall = new ImageSource
            {
                Uri = _faker.Internet.UrlWithPath(fileExt: "jpg"),
                Height = 1,
                Width = 1
            },
            PosterWide = new ImageSource
            {
                Uri = _faker.Internet.UrlWithPath(fileExt: "jpg"),
                Height = 1,
                Width = 1
            }
        };
        
        _getTitleMetadata
            .GetTitleMetadataAsync(Arg.Any<string>())
            .Returns(titleMetadata);
        
        _crunchyrollSeriesClient
            .GetPosterImagesAsync(titleMetadata.PosterTall.Uri, Arg.Any<CancellationToken>())
            .Returns(Result.Fail("error"));
        
        var posterWideBytes = _faker.Random.Bytes(1000);
        _crunchyrollSeriesClient
            .GetPosterImagesAsync(titleMetadata.PosterWide.Uri, Arg.Any<CancellationToken>())
            .Returns(new MemoryStream(posterWideBytes));
        
        //Act
        await _sut.RunAsync(series, CancellationToken.None);
        
        //Assert
        series.Name.Should().Be(titleMetadata.Title);
        series.Overview.Should().Be(titleMetadata.Description);
        series.Studios.Should().BeEquivalentTo([titleMetadata.Studio]);
        
        await _getTitleMetadata
            .Received(1)
            .GetTitleMetadataAsync(series.ProviderIds[CrunchyrollExternalKeys.SeriesId]);

        await _crunchyrollSeriesClient
            .Received(1)
            .GetPosterImagesAsync(titleMetadata.PosterTall.Uri, Arg.Any<CancellationToken>());

        await _crunchyrollSeriesClient
            .Received(1)
            .GetPosterImagesAsync(titleMetadata.PosterWide.Uri, Arg.Any<CancellationToken>());

        var posterTallFilePath = Path.Combine(_directory, Path.GetFileName(titleMetadata.PosterTall.Uri));
        var posterWideFilePath = Path.Combine(_directory, Path.GetFileName(titleMetadata.PosterWide.Uri));

        _fileSystem.AllFiles.Should().NotContain(posterTallFilePath);
        (await _fileSystem.File.ReadAllBytesAsync(posterWideFilePath)).Should().BeEquivalentTo(posterWideBytes);
        
        await _libraryManager
            .Received(1)
            .UpdateItemAsync(series, series.DisplayParent, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
        
        series.ImageInfos.Should().NotContain(x => 
            x.Path == posterTallFilePath &&
            x.Type == ImageType.Primary);
        
        series.ImageInfos.Should().Contain(x => 
            x.Path == posterWideFilePath &&
            x.Type == ImageType.Backdrop);
    }

    [Fact]
    public async Task OnlyUpdatesMetadata_WhenBothGetPosterImageFails_GivenSeriesWithTitleId()
    {
        //Arrange
        var series = SeriesFaker.GenerateWithTitleId();
        
        _libraryManager
            .GetItemById(series.ParentId)
            .Returns((BaseItem?)null);

        var titleMetadata = new Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata
        {
            TitleId = string.Empty,
            SlugTitle = string.Empty,
            Description = _faker.Lorem.Sentences(),
            Title = _faker.Random.Words(),
            Studio = _faker.Random.Words(),
            PosterTall = new ImageSource
            {
                Uri = _faker.Internet.UrlWithPath(fileExt: "jpg"),
                Height = 1,
                Width = 1
            },
            PosterWide = new ImageSource
            {
                Uri = _faker.Internet.UrlWithPath(fileExt: "jpg"),
                Height = 1,
                Width = 1
            }
        };
        
        _getTitleMetadata
            .GetTitleMetadataAsync(Arg.Any<string>())
            .Returns(titleMetadata);
        
        _crunchyrollSeriesClient
            .GetPosterImagesAsync(titleMetadata.PosterTall.Uri, Arg.Any<CancellationToken>())
            .Returns(Result.Fail("error"));
        
        _crunchyrollSeriesClient
            .GetPosterImagesAsync(titleMetadata.PosterWide.Uri, Arg.Any<CancellationToken>())
            .Returns(Result.Fail("error"));
        
        //Act
        await _sut.RunAsync(series, CancellationToken.None);
        
        //Assert
        series.Name.Should().Be(titleMetadata.Title);
        series.Overview.Should().Be(titleMetadata.Description);
        series.Studios.Should().BeEquivalentTo([titleMetadata.Studio]);
        
        await _getTitleMetadata
            .Received(1)
            .GetTitleMetadataAsync(series.ProviderIds[CrunchyrollExternalKeys.SeriesId]);

        await _crunchyrollSeriesClient
            .Received(1)
            .GetPosterImagesAsync(titleMetadata.PosterTall.Uri, Arg.Any<CancellationToken>());

        await _crunchyrollSeriesClient
            .Received(1)
            .GetPosterImagesAsync(titleMetadata.PosterWide.Uri, Arg.Any<CancellationToken>());

        var posterTallFilePath = Path.Combine(_directory, Path.GetFileName(titleMetadata.PosterTall.Uri));
        var posterWideFilePath = Path.Combine(_directory, Path.GetFileName(titleMetadata.PosterWide.Uri));

        _fileSystem.AllFiles.Should().NotContain(posterTallFilePath);
        _fileSystem.AllFiles.Should().NotContain(posterWideFilePath);
        
        await _libraryManager
            .Received(1)
            .UpdateItemAsync(series, series.DisplayParent, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
        
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
        
        _libraryManager
            .GetItemById(series.ParentId)
            .Returns((BaseItem?)null);
        
        var titleMetadata = new Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata
        {
            TitleId = string.Empty,
            SlugTitle = string.Empty,
            Description = _faker.Lorem.Sentences(),
            Title = _faker.Random.Words(),
            Studio = _faker.Random.Words(),
            PosterTall = new ImageSource
            {
                Uri = _faker.Internet.UrlWithPath(fileExt: "jpg"),
                Height = 1,
                Width = 1
            },
            PosterWide = new ImageSource
            {
                Uri = _faker.Internet.UrlWithPath(fileExt: "jpg"),
                Height = 1,
                Width = 1
            }
        };
        
        _getTitleMetadata
            .GetTitleMetadataAsync(Arg.Any<string>())
            .Returns(titleMetadata);
        
        var posterTallBytes = _faker.Random.Bytes(1000);
        _crunchyrollSeriesClient
            .GetPosterImagesAsync(titleMetadata.PosterTall.Uri, Arg.Any<CancellationToken>())
            .Returns(new MemoryStream(posterTallBytes));
        
        var posterWideBytes = _faker.Random.Bytes(1000);
        _crunchyrollSeriesClient
            .GetPosterImagesAsync(titleMetadata.PosterWide.Uri, Arg.Any<CancellationToken>())
            .Returns(new MemoryStream(posterWideBytes));
        
        var file = Substitute.For<IFile>();
        file
            .Create(Arg.Any<string>())
            .Throws(new Exception());
        
        var sut = new OverwriteSeriesJellyfinDataTask(_libraryManager, _getTitleMetadata, file, _crunchyrollSeriesClient,
            _logger, _fileSystem.Directory);
        
        //Act
        await sut.RunAsync(series, CancellationToken.None);
        
        //Assert
        series.Name.Should().Be(titleMetadata.Title);
        series.Overview.Should().Be(titleMetadata.Description);
        series.Studios.Should().BeEquivalentTo([titleMetadata.Studio]);
        
        await _getTitleMetadata
            .Received(1)
            .GetTitleMetadataAsync(series.ProviderIds[CrunchyrollExternalKeys.SeriesId]);

        await _crunchyrollSeriesClient
            .Received(1)
            .GetPosterImagesAsync(titleMetadata.PosterTall.Uri, Arg.Any<CancellationToken>());

        await _crunchyrollSeriesClient
            .Received(1)
            .GetPosterImagesAsync(titleMetadata.PosterWide.Uri, Arg.Any<CancellationToken>());

        var posterTallFilePath = Path.Combine(_directory, Path.GetFileName(titleMetadata.PosterTall.Uri));
        var posterWideFilePath = Path.Combine(_directory, Path.GetFileName(titleMetadata.PosterWide.Uri));

        _fileSystem.AllFiles.Should().NotContain(posterTallFilePath);
        _fileSystem.AllFiles.Should().NotContain(posterTallFilePath);
        
        series.ImageInfos.Should().NotContain(x => 
            x.Path == posterTallFilePath &&
            x.Type == ImageType.Primary);
        
        series.ImageInfos.Should().NotContain(x => 
            x.Path == posterWideFilePath &&
            x.Type == ImageType.Backdrop);
        
        await _libraryManager
            .Received(1)
            .UpdateItemAsync(series, series.DisplayParent, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task OverwritesFileExtensionsToJpeg_WhenFileExtensionIsJpe_GivenSeriesWithTitleId()
    {
        //Arrange
        var series = SeriesFaker.GenerateWithTitleId();

        var titleMetadata = new Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata
        {
            TitleId = string.Empty,
            SlugTitle = string.Empty,
            Description = _faker.Lorem.Sentences(),
            Title = _faker.Random.Words(),
            Studio = _faker.Random.Words(),
            PosterTall = new ImageSource
            {
                Uri = _faker.Internet.UrlWithPath(fileExt: "jpe"),
                Height = 1,
                Width = 1
            },
            PosterWide = new ImageSource
            {
                Uri = _faker.Internet.UrlWithPath(fileExt: "jpe"),
                Height = 1,
                Width = 1
            }
        };

        _libraryManager
            .GetItemById(series.ParentId)
            .Returns((BaseItem?)null);
        
        _getTitleMetadata
            .GetTitleMetadataAsync(Arg.Any<string>())
            .Returns(titleMetadata);

        var posterTallBytes = _faker.Random.Bytes(1000);
        _crunchyrollSeriesClient
            .GetPosterImagesAsync(titleMetadata.PosterTall.Uri, Arg.Any<CancellationToken>())
            .Returns(new MemoryStream(posterTallBytes));
        
        var posterWideBytes = _faker.Random.Bytes(1000);
        _crunchyrollSeriesClient
            .GetPosterImagesAsync(titleMetadata.PosterWide.Uri, Arg.Any<CancellationToken>())
            .Returns(new MemoryStream(posterWideBytes));
        
        //Act
        await _sut.RunAsync(series, CancellationToken.None);
        
        //Assert
        series.Name.Should().Be(titleMetadata.Title);
        series.Overview.Should().Be(titleMetadata.Description);
        series.Studios.Should().BeEquivalentTo([titleMetadata.Studio]);
        
        await _getTitleMetadata
            .Received(1)
            .GetTitleMetadataAsync(series.ProviderIds[CrunchyrollExternalKeys.SeriesId]);

        await _crunchyrollSeriesClient
            .Received(1)
            .GetPosterImagesAsync(titleMetadata.PosterTall.Uri, Arg.Any<CancellationToken>());

        await _crunchyrollSeriesClient
            .Received(1)
            .GetPosterImagesAsync(titleMetadata.PosterWide.Uri, Arg.Any<CancellationToken>());

        _fileSystem.AllDirectories.Should().Contain(path => path == _directory);

        var posterTallFilePath = Path.Combine(_directory, Path.GetFileName(titleMetadata.PosterTall.Uri))
            .Replace(".jpe", ".jpeg");
        var posterWideFilePath = Path.Combine(_directory, Path.GetFileName(titleMetadata.PosterWide.Uri))
            .Replace(".jpe", ".jpeg");

        (await _fileSystem.File.ReadAllBytesAsync(posterTallFilePath)).Should().BeEquivalentTo(posterTallBytes);
        (await _fileSystem.File.ReadAllBytesAsync(posterWideFilePath)).Should().BeEquivalentTo(posterWideBytes);
        
        await _libraryManager
            .Received(1)
            .UpdateItemAsync(series, series.DisplayParent, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
        
        series.ImageInfos.Should().Contain(x => 
            x.Path == posterTallFilePath &&
            x.Type == ImageType.Primary &&
            x.Width == titleMetadata.PosterTall.Width &&
            x.Height == titleMetadata.PosterTall.Height);
        
        series.ImageInfos.Should().Contain(x => 
            x.Path == posterWideFilePath &&
            x.Type == ImageType.Backdrop &&
            x.Width == titleMetadata.PosterTall.Width &&
            x.Height == titleMetadata.PosterTall.Height);
    }
}