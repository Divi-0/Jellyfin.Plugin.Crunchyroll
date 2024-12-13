using System.Globalization;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Text.Json;
using Bogus;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Domain.Entities;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.GetMetadata.ScrapSeriesMetadata.Client;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.OverwriteSeriesJellyfinData;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata;
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
    private readonly IGetTitleMetadataRepository _repository;
    private readonly MockFileSystem _fileSystem;
    private readonly ICrunchyrollSeriesClient _crunchyrollSeriesClient;
    private readonly ILogger<OverwriteSeriesJellyfinDataTask> _logger;
    private readonly IFile _file;
    private readonly IDirectory _directory;
    private readonly PluginConfiguration _config;
    
    private readonly Faker _faker;

    private readonly string _directoryPath;

    public OverwriteSeriesJellyfinDataTaskTests()
    {
        _faker = new Faker();
        
        _libraryManager = MockHelper.LibraryManager;
        _repository = Substitute.For<IGetTitleMetadataRepository>();
        _crunchyrollSeriesClient = Substitute.For<ICrunchyrollSeriesClient>();
        _fileSystem = new MockFileSystem();
        _logger = Substitute.For<ILogger<OverwriteSeriesJellyfinDataTask>>();
        _file = _fileSystem.File;
        _directory = _fileSystem.Directory;
        _config = new PluginConfiguration
        {
            IsFeatureSeriesTitleEnabled = true,
            IsFeatureSeriesDescriptionEnabled = true,
            IsFeatureSeriesStudioEnabled = true,
            IsFeatureSeriesRatingsEnabled = true,
            IsFeatureSeriesCoverImageEnabled = true,
            IsFeatureSeriesBackgroundImageEnabled = true
        };
        
        _directoryPath = Path.Combine(Path.GetDirectoryName(typeof(OverwriteSeriesJellyfinDataTask).Assembly.Location)!, "series-images");

        _sut = new OverwriteSeriesJellyfinDataTask(_libraryManager, _repository, _file, _crunchyrollSeriesClient,
            _logger, _directory, _config);
    }

    [Fact]
    public async Task StoresImagesToFileSystemAndUpdatesJellyfinItem_WhenRunsTask_GivenSeriesWithTitleId()
    {
        //Arrange
        var series = SeriesFaker.GenerateWithTitleId();
        var language = new CultureInfo("en-US");

        var posterTall = new ImageSource
        {
            Uri = _faker.Internet.UrlWithPath(fileExt: "jpg"),
            Height = 1,
            Width = 1
        };
        
        var posterWide = new ImageSource
        {
            Uri = _faker.Internet.UrlWithPath(fileExt: "jpg"),
            Height = 1,
            Width = 1
        };

        var titleMetadata = new Domain.Entities.TitleMetadata
        {
            CrunchyrollId = string.Empty,
            SlugTitle = string.Empty,
            Description = _faker.Lorem.Sentences(),
            Title = _faker.Random.Words(),
            Studio = _faker.Random.Words(),
            Rating = _faker.Random.Float(),
            PosterTall = JsonSerializer.Serialize(posterTall),
            PosterWide = JsonSerializer.Serialize(posterWide),
            Language = language.Name
        };

        _libraryManager
            .GetItemById(series.ParentId)
            .Returns((BaseItem?)null);
        
        _repository
            .GetTitleMetadataAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(titleMetadata);

        var posterTallBytes = _faker.Random.Bytes(1000);
        _crunchyrollSeriesClient
            .GetPosterImagesAsync(posterTall.Uri, Arg.Any<CancellationToken>())
            .Returns(new MemoryStream(posterTallBytes));
        
        var posterWideBytes = _faker.Random.Bytes(1000);
        _crunchyrollSeriesClient
            .GetPosterImagesAsync(posterWide.Uri, Arg.Any<CancellationToken>())
            .Returns(new MemoryStream(posterWideBytes));
        
        //Act
        await _sut.RunAsync(series, CancellationToken.None);
        
        //Assert
        series.Name.Should().Be(titleMetadata.Title);
        series.Overview.Should().Be(titleMetadata.Description);
        series.Studios.Should().BeEquivalentTo([titleMetadata.Studio]);
        series.CommunityRating.Should().Be(titleMetadata.Rating);
        
        await _repository
            .Received(1)
            .GetTitleMetadataAsync(series.ProviderIds[CrunchyrollExternalKeys.SeriesId], Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        await _crunchyrollSeriesClient
            .Received(1)
            .GetPosterImagesAsync(posterTall.Uri, Arg.Any<CancellationToken>());

        await _crunchyrollSeriesClient
            .Received(1)
            .GetPosterImagesAsync(posterWide.Uri, Arg.Any<CancellationToken>());

        _fileSystem.AllDirectories.Should().Contain(path => path == _directoryPath);

        var posterTallFilePath = Path.Combine(_directoryPath, Path.GetFileName(posterTall.Uri));
        var posterWideFilePath = Path.Combine(_directoryPath, Path.GetFileName(posterWide.Uri));

        (await _fileSystem.File.ReadAllBytesAsync(posterTallFilePath)).Should().BeEquivalentTo(posterTallBytes);
        (await _fileSystem.File.ReadAllBytesAsync(posterWideFilePath)).Should().BeEquivalentTo(posterWideBytes);
        
        await _libraryManager
            .Received(1)
            .UpdateItemAsync(series, series.DisplayParent, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
        
        series.ImageInfos.Should().Contain(x => 
            x.Path == posterTallFilePath &&
            x.Type == ImageType.Primary &&
            x.Width == posterTall.Width &&
            x.Height == posterTall.Height);
        
        series.ImageInfos.Should().Contain(x => 
            x.Path == posterWideFilePath &&
            x.Type == ImageType.Backdrop &&
            x.Width == posterTall.Width &&
            x.Height == posterTall.Height);
    }

    [Fact]
    public async Task SetsImagesFromFileSystem_WhenImagesFoundOnFileSystem_GivenSeriesWithTitleId()
    {
        //Arrange
        var series = SeriesFaker.GenerateWithTitleId();
        var language = new CultureInfo("en-US");

        var posterTall = new ImageSource
        {
            Uri = _faker.Internet.UrlWithPath(fileExt: "jpg"),
            Height = 1,
            Width = 1
        };
        
        var posterWide = new ImageSource
        {
            Uri = _faker.Internet.UrlWithPath(fileExt: "jpg"),
            Height = 1,
            Width = 1
        };

        _directory.CreateDirectory(_directoryPath);
        var posterTallFilePath = Path.Combine(_directoryPath, Path.GetFileName(posterTall.Uri));
        var posterWideFilePath = Path.Combine(_directoryPath, Path.GetFileName(posterWide.Uri));

        _ = _file.Create(posterTallFilePath);
        _ = _file.Create(posterWideFilePath);


        var titleMetadata = new Domain.Entities.TitleMetadata
        {
            CrunchyrollId = string.Empty,
            SlugTitle = string.Empty,
            Description = _faker.Lorem.Sentences(),
            Title = _faker.Random.Words(),
            Studio = _faker.Random.Words(),
            Rating = _faker.Random.Float(),
            PosterTall = JsonSerializer.Serialize(posterTall),
            PosterWide = JsonSerializer.Serialize(posterWide),
            Language = language.Name
        };

        _libraryManager
            .GetItemById(series.ParentId)
            .Returns((BaseItem?)null);
        
        _repository
            .GetTitleMetadataAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(titleMetadata);
        
        //Act
        await _sut.RunAsync(series, CancellationToken.None);
        
        //Assert
        series.Name.Should().Be(titleMetadata.Title);
        series.Overview.Should().Be(titleMetadata.Description);
        series.Studios.Should().BeEquivalentTo([titleMetadata.Studio]);
        series.CommunityRating.Should().Be(titleMetadata.Rating);
        
        await _repository
            .Received(1)
            .GetTitleMetadataAsync(series.ProviderIds[CrunchyrollExternalKeys.SeriesId], Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        await _crunchyrollSeriesClient
            .DidNotReceive()
            .GetPosterImagesAsync(posterTall.Uri, Arg.Any<CancellationToken>());

        await _crunchyrollSeriesClient
            .DidNotReceive()
            .GetPosterImagesAsync(posterWide.Uri, Arg.Any<CancellationToken>());

        _fileSystem.AllDirectories.Should().Contain(path => path == _directoryPath);
        
        await _libraryManager
            .Received(1)
            .UpdateItemAsync(series, series.DisplayParent, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
        
        series.ImageInfos.Should().Contain(x => 
            x.Path == posterTallFilePath &&
            x.Type == ImageType.Primary &&
            x.Width == posterTall.Width &&
            x.Height == posterTall.Height);
        
        series.ImageInfos.Should().Contain(x => 
            x.Path == posterWideFilePath &&
            x.Type == ImageType.Backdrop &&
            x.Width == posterTall.Width &&
            x.Height == posterTall.Height);
    }

    [Fact]
    public async Task SkipsGetImage_WhenImageAlreadySetAndStillOnFileSystem_GivenSeriesWithTitleId()
    {
        //Arrange
        var series = SeriesFaker.GenerateWithTitleId();
        var language = new CultureInfo("en-US");
        
        var posterTall = new ImageSource
        {
            Uri = _faker.Internet.UrlWithPath(fileExt: "jpg"),
            Height = 1,
            Width = 1
        };
        
        var posterWide = new ImageSource
        {
            Uri = _faker.Internet.UrlWithPath(fileExt: "jpg"),
            Height = 1,
            Width = 1
        };

        var titleMetadata = new Domain.Entities.TitleMetadata
        {
            CrunchyrollId = string.Empty,
            SlugTitle = string.Empty,
            Description = _faker.Lorem.Sentences(),
            Title = _faker.Random.Words(),
            Studio = _faker.Random.Words(),
            Rating = _faker.Random.Float(),
            PosterTall = JsonSerializer.Serialize(posterTall),
            PosterWide = JsonSerializer.Serialize(posterWide),
            Language = language.Name
        };
        
        var posterTallFilePath = Path.Combine(_directoryPath, Path.GetFileName(posterTall.Uri));
        var posterWideFilePath = Path.Combine(_directoryPath, Path.GetFileName(posterWide.Uri));

        _directory.CreateDirectory(_directoryPath);
        _ = _file.Create(posterTallFilePath);
        _ = _file.Create(posterWideFilePath);
        
        series.SetImage(new ItemImageInfo()
        {
            Path = posterTallFilePath,
            Type = ImageType.Primary,
            Width = posterTall.Width,
            Height = posterTall.Height
        }, 0);        
        
        series.SetImage(new ItemImageInfo()
        {
            Path = posterWideFilePath,
            Type = ImageType.Backdrop,
            Width = posterWide.Width,
            Height = posterWide.Height
        }, 0);

        _libraryManager
            .GetItemById(series.ParentId)
            .Returns((BaseItem?)null);
        
        _repository
            .GetTitleMetadataAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns(titleMetadata);
        
        //Act
        await _sut.RunAsync(series, CancellationToken.None);
        
        //Assert
        series.Name.Should().Be(titleMetadata.Title);
        series.Overview.Should().Be(titleMetadata.Description);
        series.Studios.Should().BeEquivalentTo([titleMetadata.Studio]);
        series.CommunityRating.Should().Be(titleMetadata.Rating);
        
        await _repository
            .Received(1)
            .GetTitleMetadataAsync(series.ProviderIds[CrunchyrollExternalKeys.SeriesId], 
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        await _crunchyrollSeriesClient
            .DidNotReceive()
            .GetPosterImagesAsync(posterTall.Uri, Arg.Any<CancellationToken>());

        await _crunchyrollSeriesClient
            .DidNotReceive()
            .GetPosterImagesAsync(posterWide.Uri, Arg.Any<CancellationToken>());
        
        await _libraryManager
            .Received(1)
            .UpdateItemAsync(series, series.DisplayParent, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
        
        series.ImageInfos.Should().Contain(x => 
            x.Path == posterTallFilePath &&
            x.Type == ImageType.Primary &&
            x.Width == posterTall.Width &&
            x.Height == posterTall.Height);
        
        series.ImageInfos.Should().Contain(x => 
            x.Path == posterWideFilePath &&
            x.Type == ImageType.Backdrop &&
            x.Width == posterTall.Width &&
            x.Height == posterTall.Height);
    }

    [Fact]
    public async Task DownloadsImages_WhenImageAlreadySetButNotFoundOnFileSystem_GivenSeriesWithTitleId()
    {
        //Arrange
        var series = SeriesFaker.GenerateWithTitleId();
        var language = new CultureInfo("en-US");
        
        var posterTall = new ImageSource
        {
            Uri = _faker.Internet.UrlWithPath(fileExt: "jpg"),
            Height = 1,
            Width = 1
        };
        
        var posterWide = new ImageSource
        {
            Uri = _faker.Internet.UrlWithPath(fileExt: "jpg"),
            Height = 1,
            Width = 1
        };

        var titleMetadata = new Domain.Entities.TitleMetadata
        {
            CrunchyrollId = string.Empty,
            SlugTitle = string.Empty,
            Description = _faker.Lorem.Sentences(),
            Title = _faker.Random.Words(),
            Studio = _faker.Random.Words(),
            Rating = _faker.Random.Float(),
            PosterTall = JsonSerializer.Serialize(posterTall),
            PosterWide = JsonSerializer.Serialize(posterWide),
            Language = language.Name
        };
        
        var posterTallFilePath = Path.Combine(_directoryPath, Path.GetFileName(posterTall.Uri));
        var posterWideFilePath = Path.Combine(_directoryPath, Path.GetFileName(posterWide.Uri));
        
        series.SetImage(new ItemImageInfo()
        {
            Path = posterTallFilePath,
            Type = ImageType.Primary,
            Width = posterTall.Width,
            Height = posterTall.Height
        }, 0);        
        
        series.SetImage(new ItemImageInfo()
        {
            Path = posterWideFilePath,
            Type = ImageType.Backdrop,
            Width = posterWide.Width,
            Height = posterWide.Height
        }, 0);

        _libraryManager
            .GetItemById(series.ParentId)
            .Returns((BaseItem?)null);
        
        _repository
            .GetTitleMetadataAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns(titleMetadata);
        
        var posterTallBytes = _faker.Random.Bytes(1000);
        _crunchyrollSeriesClient
            .GetPosterImagesAsync(posterTall.Uri, Arg.Any<CancellationToken>())
            .Returns(new MemoryStream(posterTallBytes));
        
        var posterWideBytes = _faker.Random.Bytes(1000);
        _crunchyrollSeriesClient
            .GetPosterImagesAsync(posterWide.Uri, Arg.Any<CancellationToken>())
            .Returns(new MemoryStream(posterWideBytes));
        
        //Act
        await _sut.RunAsync(series, CancellationToken.None);
        
        //Assert
        series.Name.Should().Be(titleMetadata.Title);
        series.Overview.Should().Be(titleMetadata.Description);
        series.Studios.Should().BeEquivalentTo([titleMetadata.Studio]);
        series.CommunityRating.Should().Be(titleMetadata.Rating);
        
        await _repository
            .Received(1)
            .GetTitleMetadataAsync(series.ProviderIds[CrunchyrollExternalKeys.SeriesId], 
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        await _crunchyrollSeriesClient
            .Received(1)
            .GetPosterImagesAsync(posterTall.Uri, Arg.Any<CancellationToken>());

        await _crunchyrollSeriesClient
            .Received(1)
            .GetPosterImagesAsync(posterWide.Uri, Arg.Any<CancellationToken>());
        
        await _libraryManager
            .Received(1)
            .UpdateItemAsync(series, series.DisplayParent, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
        
        _fileSystem.AllDirectories.Should().Contain(path => path == _directoryPath);
        
        (await _fileSystem.File.ReadAllBytesAsync(posterTallFilePath)).Should().BeEquivalentTo(posterTallBytes);
        (await _fileSystem.File.ReadAllBytesAsync(posterWideFilePath)).Should().BeEquivalentTo(posterWideBytes);
        
        series.ImageInfos.Should().Contain(x => 
            x.Path == posterTallFilePath &&
            x.Type == ImageType.Primary &&
            x.Width == posterTall.Width &&
            x.Height == posterTall.Height);
        
        series.ImageInfos.Should().Contain(x => 
            x.Path == posterWideFilePath &&
            x.Type == ImageType.Backdrop &&
            x.Width == posterTall.Width &&
            x.Height == posterTall.Height);
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
        await _repository
            .DidNotReceive()
            .GetTitleMetadataAsync(Arg.Any<string>(),
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

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
        var language = new CultureInfo("en-US");
        
        _libraryManager
            .GetItemById(series.ParentId)
            .Returns((BaseItem?)null);
        
        var posterTall = new ImageSource
        {
            Uri = _faker.Internet.UrlWithPath(fileExt: "jpg"),
            Height = 1,
            Width = 1
        };
        
        var posterWide = new ImageSource
        {
            Uri = _faker.Internet.UrlWithPath(fileExt: "jpg"),
            Height = 1,
            Width = 1
        };

        var titleMetadata = new Domain.Entities.TitleMetadata
        {
            CrunchyrollId = string.Empty,
            SlugTitle = string.Empty,
            Description = _faker.Lorem.Sentences(),
            Title = _faker.Random.Words(),
            Studio = _faker.Random.Words(),
            Rating = _faker.Random.Float(),
            PosterTall = JsonSerializer.Serialize(posterTall),
            PosterWide = JsonSerializer.Serialize(posterWide),
            Language = language.Name
        };
        
        _repository
            .GetTitleMetadataAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(titleMetadata);
        
        _crunchyrollSeriesClient
            .GetPosterImagesAsync(posterTall.Uri, Arg.Any<CancellationToken>())
            .Returns(Result.Fail("error"));
        
        var posterWideBytes = _faker.Random.Bytes(1000);
        _crunchyrollSeriesClient
            .GetPosterImagesAsync(posterWide.Uri, Arg.Any<CancellationToken>())
            .Returns(new MemoryStream(posterWideBytes));
        
        //Act
        await _sut.RunAsync(series, CancellationToken.None);
        
        //Assert
        series.Name.Should().Be(titleMetadata.Title);
        series.Overview.Should().Be(titleMetadata.Description);
        series.Studios.Should().BeEquivalentTo([titleMetadata.Studio]);
        series.CommunityRating.Should().Be(titleMetadata.Rating);
        
        await _repository
            .Received(1)
            .GetTitleMetadataAsync(series.ProviderIds[CrunchyrollExternalKeys.SeriesId],
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        await _crunchyrollSeriesClient
            .Received(1)
            .GetPosterImagesAsync(posterTall.Uri, Arg.Any<CancellationToken>());

        await _crunchyrollSeriesClient
            .Received(1)
            .GetPosterImagesAsync(posterWide.Uri, Arg.Any<CancellationToken>());

        var posterTallFilePath = Path.Combine(_directoryPath, Path.GetFileName(posterTall.Uri));
        var posterWideFilePath = Path.Combine(_directoryPath, Path.GetFileName(posterWide.Uri));

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
        var language = new CultureInfo("en-US");
        
        _libraryManager
            .GetItemById(series.ParentId)
            .Returns((BaseItem?)null);
        
        var posterTall = new ImageSource
        {
            Uri = _faker.Internet.UrlWithPath(fileExt: "jpg"),
            Height = 1,
            Width = 1
        };
        
        var posterWide = new ImageSource
        {
            Uri = _faker.Internet.UrlWithPath(fileExt: "jpg"),
            Height = 1,
            Width = 1
        };

        var titleMetadata = new Domain.Entities.TitleMetadata
        {
            CrunchyrollId = string.Empty,
            SlugTitle = string.Empty,
            Description = _faker.Lorem.Sentences(),
            Title = _faker.Random.Words(),
            Studio = _faker.Random.Words(),
            Rating = _faker.Random.Float(),
            PosterTall = JsonSerializer.Serialize(posterTall),
            PosterWide = JsonSerializer.Serialize(posterWide),
            Language = language.Name
        };
        
        _repository
            .GetTitleMetadataAsync(Arg.Any<string>(),
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(titleMetadata);
        
        _crunchyrollSeriesClient
            .GetPosterImagesAsync(posterTall.Uri, Arg.Any<CancellationToken>())
            .Returns(Result.Fail("error"));
        
        _crunchyrollSeriesClient
            .GetPosterImagesAsync(posterWide.Uri, Arg.Any<CancellationToken>())
            .Returns(Result.Fail("error"));
        
        //Act
        await _sut.RunAsync(series, CancellationToken.None);
        
        //Assert
        series.Name.Should().Be(titleMetadata.Title);
        series.Overview.Should().Be(titleMetadata.Description);
        series.Studios.Should().BeEquivalentTo([titleMetadata.Studio]);
        series.CommunityRating.Should().Be(titleMetadata.Rating);
        
        await _repository
            .Received(1)
            .GetTitleMetadataAsync(series.ProviderIds[CrunchyrollExternalKeys.SeriesId],
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        await _crunchyrollSeriesClient
            .Received(1)
            .GetPosterImagesAsync(posterTall.Uri, Arg.Any<CancellationToken>());

        await _crunchyrollSeriesClient
            .Received(1)
            .GetPosterImagesAsync(posterWide.Uri, Arg.Any<CancellationToken>());

        var posterTallFilePath = Path.Combine(_directoryPath, Path.GetFileName(posterTall.Uri));
        var posterWideFilePath = Path.Combine(_directoryPath, Path.GetFileName(posterWide.Uri));

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
        var language = new CultureInfo("en-US");
        
        _libraryManager
            .GetItemById(series.ParentId)
            .Returns((BaseItem?)null);
        
        var posterTall = new ImageSource
        {
            Uri = _faker.Internet.UrlWithPath(fileExt: "jpg"),
            Height = 1,
            Width = 1
        };
        
        var posterWide = new ImageSource
        {
            Uri = _faker.Internet.UrlWithPath(fileExt: "jpg"),
            Height = 1,
            Width = 1
        };
        
        var titleMetadata = new Domain.Entities.TitleMetadata
        {
            CrunchyrollId = string.Empty,
            SlugTitle = string.Empty,
            Description = _faker.Lorem.Sentences(),
            Title = _faker.Random.Words(),
            Studio = _faker.Random.Words(),
            Rating = _faker.Random.Float(),
            PosterTall = JsonSerializer.Serialize(posterTall),
            PosterWide = JsonSerializer.Serialize(posterWide),
            Language = language.Name
        };
        
        _repository
            .GetTitleMetadataAsync(Arg.Any<string>(),
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(titleMetadata);
        
        var posterTallBytes = _faker.Random.Bytes(1000);
        _crunchyrollSeriesClient
            .GetPosterImagesAsync(posterTall.Uri, Arg.Any<CancellationToken>())
            .Returns(new MemoryStream(posterTallBytes));
        
        var posterWideBytes = _faker.Random.Bytes(1000);
        _crunchyrollSeriesClient
            .GetPosterImagesAsync(posterWide.Uri, Arg.Any<CancellationToken>())
            .Returns(new MemoryStream(posterWideBytes));
        
        var file = Substitute.For<IFile>();
        file
            .Create(Arg.Any<string>())
            .Throws(new Exception());
        
        var sut = new OverwriteSeriesJellyfinDataTask(_libraryManager, _repository, file, _crunchyrollSeriesClient,
            _logger, _fileSystem.Directory, _config);
        
        //Act
        await sut.RunAsync(series, CancellationToken.None);
        
        //Assert
        series.Name.Should().Be(titleMetadata.Title);
        series.Overview.Should().Be(titleMetadata.Description);
        series.Studios.Should().BeEquivalentTo([titleMetadata.Studio]);
        series.CommunityRating.Should().Be(titleMetadata.Rating);
        
        await _repository
            .Received(1)
            .GetTitleMetadataAsync(series.ProviderIds[CrunchyrollExternalKeys.SeriesId],
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        await _crunchyrollSeriesClient
            .Received(1)
            .GetPosterImagesAsync(posterTall.Uri, Arg.Any<CancellationToken>());

        await _crunchyrollSeriesClient
            .Received(1)
            .GetPosterImagesAsync(posterWide.Uri, Arg.Any<CancellationToken>());

        var posterTallFilePath = Path.Combine(_directoryPath, Path.GetFileName(posterTall.Uri));
        var posterWideFilePath = Path.Combine(_directoryPath, Path.GetFileName(posterWide.Uri));

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
        var language = new CultureInfo("en-US");
        
        var posterTall = new ImageSource
        {
            Uri = _faker.Internet.UrlWithPath(fileExt: "jpg"),
            Height = 1,
            Width = 1
        };
        
        var posterWide = new ImageSource
        {
            Uri = _faker.Internet.UrlWithPath(fileExt: "jpg"),
            Height = 1,
            Width = 1
        };

        var titleMetadata = new Domain.Entities.TitleMetadata
        {
            CrunchyrollId = string.Empty,
            SlugTitle = string.Empty,
            Description = _faker.Lorem.Sentences(),
            Title = _faker.Random.Words(),
            Studio = _faker.Random.Words(),
            Rating = _faker.Random.Float(),
            PosterTall = JsonSerializer.Serialize(posterTall),
            PosterWide = JsonSerializer.Serialize(posterWide),
            Language = language.Name
        };

        _libraryManager
            .GetItemById(series.ParentId)
            .Returns((BaseItem?)null);
        
        _repository
            .GetTitleMetadataAsync(Arg.Any<string>(),
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(titleMetadata);

        var posterTallBytes = _faker.Random.Bytes(1000);
        _crunchyrollSeriesClient
            .GetPosterImagesAsync(posterTall.Uri, Arg.Any<CancellationToken>())
            .Returns(new MemoryStream(posterTallBytes));
        
        var posterWideBytes = _faker.Random.Bytes(1000);
        _crunchyrollSeriesClient
            .GetPosterImagesAsync(posterWide.Uri, Arg.Any<CancellationToken>())
            .Returns(new MemoryStream(posterWideBytes));
        
        //Act
        await _sut.RunAsync(series, CancellationToken.None);
        
        //Assert
        series.Name.Should().Be(titleMetadata.Title);
        series.Overview.Should().Be(titleMetadata.Description);
        series.Studios.Should().BeEquivalentTo([titleMetadata.Studio]);
        series.CommunityRating.Should().Be(titleMetadata.Rating);
        
        await _repository
            .Received(1)
            .GetTitleMetadataAsync(series.ProviderIds[CrunchyrollExternalKeys.SeriesId],
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        await _crunchyrollSeriesClient
            .Received(1)
            .GetPosterImagesAsync(posterTall.Uri, Arg.Any<CancellationToken>());

        await _crunchyrollSeriesClient
            .Received(1)
            .GetPosterImagesAsync(posterWide.Uri, Arg.Any<CancellationToken>());

        _fileSystem.AllDirectories.Should().Contain(path => path == _directoryPath);

        var posterTallFilePath = Path.Combine(_directoryPath, Path.GetFileName(posterTall.Uri))
            .Replace(".jpe", ".jpeg");
        var posterWideFilePath = Path.Combine(_directoryPath, Path.GetFileName(posterWide.Uri))
            .Replace(".jpe", ".jpeg");

        (await _fileSystem.File.ReadAllBytesAsync(posterTallFilePath)).Should().BeEquivalentTo(posterTallBytes);
        (await _fileSystem.File.ReadAllBytesAsync(posterWideFilePath)).Should().BeEquivalentTo(posterWideBytes);
        
        await _libraryManager
            .Received(1)
            .UpdateItemAsync(series, series.DisplayParent, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
        
        series.ImageInfos.Should().Contain(x => 
            x.Path == posterTallFilePath &&
            x.Type == ImageType.Primary &&
            x.Width == posterTall.Width &&
            x.Height == posterTall.Height);
        
        series.ImageInfos.Should().Contain(x => 
            x.Path == posterWideFilePath &&
            x.Type == ImageType.Backdrop &&
            x.Width == posterTall.Width &&
            x.Height == posterTall.Height);
    }
    
    [Fact]
    public async Task DoesNotGetAndStoreCoverImage_WhenFeatureCoverImageIsDisabled_GivenSeriesWithTitleId()
    {
        //Arrange
        var series = SeriesFaker.GenerateWithTitleId();
        var language = new CultureInfo("en-US");

        _config.IsFeatureSeriesCoverImageEnabled = false;

        var posterTall = new ImageSource
        {
            Uri = _faker.Internet.UrlWithPath(fileExt: "jpg"),
            Height = 1,
            Width = 1
        };
        
        var posterWide = new ImageSource
        {
            Uri = _faker.Internet.UrlWithPath(fileExt: "jpg"),
            Height = 1,
            Width = 1
        };

        var titleMetadata = new Domain.Entities.TitleMetadata
        {
            CrunchyrollId = string.Empty,
            SlugTitle = string.Empty,
            Description = _faker.Lorem.Sentences(),
            Title = _faker.Random.Words(),
            Studio = _faker.Random.Words(),
            Rating = _faker.Random.Float(),
            PosterTall = JsonSerializer.Serialize(posterTall),
            PosterWide = JsonSerializer.Serialize(posterWide),
            Language = language.Name
        };

        _libraryManager
            .GetItemById(series.ParentId)
            .Returns((BaseItem?)null);
        
        _repository
            .GetTitleMetadataAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(titleMetadata);

        var posterTallBytes = _faker.Random.Bytes(1000);
        _crunchyrollSeriesClient
            .GetPosterImagesAsync(posterTall.Uri, Arg.Any<CancellationToken>())
            .Returns(new MemoryStream(posterTallBytes));
        
        var posterWideBytes = _faker.Random.Bytes(1000);
        _crunchyrollSeriesClient
            .GetPosterImagesAsync(posterWide.Uri, Arg.Any<CancellationToken>())
            .Returns(new MemoryStream(posterWideBytes));
        
        //Act
        await _sut.RunAsync(series, CancellationToken.None);
        
        //Assert
        series.Name.Should().Be(titleMetadata.Title);
        series.Overview.Should().Be(titleMetadata.Description);
        series.Studios.Should().BeEquivalentTo([titleMetadata.Studio]);
        series.CommunityRating.Should().Be(titleMetadata.Rating);
        
        await _repository
            .Received(1)
            .GetTitleMetadataAsync(series.ProviderIds[CrunchyrollExternalKeys.SeriesId], Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        await _crunchyrollSeriesClient
            .DidNotReceive()
            .GetPosterImagesAsync(posterTall.Uri, Arg.Any<CancellationToken>());

        await _crunchyrollSeriesClient
            .Received(1)
            .GetPosterImagesAsync(posterWide.Uri, Arg.Any<CancellationToken>());

        _fileSystem.AllDirectories.Should().Contain(path => path == _directoryPath);

        var posterTallFilePath = Path.Combine(_directoryPath, Path.GetFileName(posterTall.Uri));
        var posterWideFilePath = Path.Combine(_directoryPath, Path.GetFileName(posterWide.Uri));

        _fileSystem.File.Exists(posterTallFilePath).Should().BeFalse("feature is disabled");
        _fileSystem.File.Exists(posterWideFilePath).Should().BeTrue();
        
        (await _fileSystem.File.ReadAllBytesAsync(posterWideFilePath)).Should().BeEquivalentTo(posterWideBytes);
        
        await _libraryManager
            .Received(1)
            .UpdateItemAsync(series, series.DisplayParent, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
        
        series.ImageInfos.Should().NotContain(x => 
            x.Path == posterTallFilePath &&
            x.Type == ImageType.Primary &&
            x.Width == posterTall.Width &&
            x.Height == posterTall.Height,
            "feature is disabled");
        
        series.ImageInfos.Should().Contain(x => 
            x.Path == posterWideFilePath &&
            x.Type == ImageType.Backdrop &&
            x.Width == posterTall.Width &&
            x.Height == posterTall.Height);
    }
    
    [Fact]
    public async Task DoesNotGetAndStoreBackgroundImage_WhenFeatureBackgroundImageIsDisabled_GivenSeriesWithTitleId()
    {
        //Arrange
        var series = SeriesFaker.GenerateWithTitleId();
        var language = new CultureInfo("en-US");

        _config.IsFeatureSeriesBackgroundImageEnabled = false;

        var posterTall = new ImageSource
        {
            Uri = _faker.Internet.UrlWithPath(fileExt: "jpg"),
            Height = 1,
            Width = 1
        };
        
        var posterWide = new ImageSource
        {
            Uri = _faker.Internet.UrlWithPath(fileExt: "jpg"),
            Height = 1,
            Width = 1
        };

        var titleMetadata = new Domain.Entities.TitleMetadata
        {
            CrunchyrollId = string.Empty,
            SlugTitle = string.Empty,
            Description = _faker.Lorem.Sentences(),
            Title = _faker.Random.Words(),
            Studio = _faker.Random.Words(),
            Rating = _faker.Random.Float(),
            PosterTall = JsonSerializer.Serialize(posterTall),
            PosterWide = JsonSerializer.Serialize(posterWide),
            Language = language.Name
        };

        _libraryManager
            .GetItemById(series.ParentId)
            .Returns((BaseItem?)null);
        
        _repository
            .GetTitleMetadataAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(titleMetadata);

        var posterTallBytes = _faker.Random.Bytes(1000);
        _crunchyrollSeriesClient
            .GetPosterImagesAsync(posterTall.Uri, Arg.Any<CancellationToken>())
            .Returns(new MemoryStream(posterTallBytes));
        
        var posterWideBytes = _faker.Random.Bytes(1000);
        _crunchyrollSeriesClient
            .GetPosterImagesAsync(posterWide.Uri, Arg.Any<CancellationToken>())
            .Returns(new MemoryStream(posterWideBytes));
        
        //Act
        await _sut.RunAsync(series, CancellationToken.None);
        
        //Assert
        series.Name.Should().Be(titleMetadata.Title);
        series.Overview.Should().Be(titleMetadata.Description);
        series.Studios.Should().BeEquivalentTo([titleMetadata.Studio]);
        series.CommunityRating.Should().Be(titleMetadata.Rating);
        
        await _repository
            .Received(1)
            .GetTitleMetadataAsync(series.ProviderIds[CrunchyrollExternalKeys.SeriesId], Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        await _crunchyrollSeriesClient
            .Received(1)
            .GetPosterImagesAsync(posterTall.Uri, Arg.Any<CancellationToken>());

        await _crunchyrollSeriesClient
            .DidNotReceive()
            .GetPosterImagesAsync(posterWide.Uri, Arg.Any<CancellationToken>());

        _fileSystem.AllDirectories.Should().Contain(path => path == _directoryPath);

        var posterTallFilePath = Path.Combine(_directoryPath, Path.GetFileName(posterTall.Uri));
        var posterWideFilePath = Path.Combine(_directoryPath, Path.GetFileName(posterWide.Uri));
        
        _fileSystem.File.Exists(posterTallFilePath).Should().BeTrue();
        _fileSystem.File.Exists(posterWideFilePath).Should().BeFalse("feature is disabled");

        (await _fileSystem.File.ReadAllBytesAsync(posterTallFilePath)).Should().BeEquivalentTo(posterTallBytes);
        
        await _libraryManager
            .Received(1)
            .UpdateItemAsync(series, series.DisplayParent, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
        
        series.ImageInfos.Should().Contain(x => 
            x.Path == posterTallFilePath &&
            x.Type == ImageType.Primary &&
            x.Width == posterTall.Width &&
            x.Height == posterTall.Height);
        
        series.ImageInfos.Should().NotContain(x => 
            x.Path == posterWideFilePath &&
            x.Type == ImageType.Backdrop &&
            x.Width == posterTall.Width &&
            x.Height == posterTall.Height,
            "feature is disabled");
    }
}