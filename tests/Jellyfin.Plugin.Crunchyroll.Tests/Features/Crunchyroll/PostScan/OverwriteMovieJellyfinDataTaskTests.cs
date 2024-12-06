using System.Globalization;
using System.Text.Json;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.OverwriteMovieJellyfinData;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.SetEpisodeThumbnail;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Image.Entites;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.PostScan;

public class OverwriteMovieJellyfinDataTaskTests
{
    private readonly OverwriteMovieJellyfinDataTask _sut;
    private readonly IOverwriteMovieJellyfinDataRepository _repository;
    private readonly ILibraryManager _libraryManager;
    private readonly IMediaSourceManager _mediaSourceManager;
    private readonly ISetEpisodeThumbnail _setEpisodeThumbnail;
    private readonly PluginConfiguration _config;

    public OverwriteMovieJellyfinDataTaskTests()
    {
        _repository = Substitute.For<IOverwriteMovieJellyfinDataRepository>();
        _setEpisodeThumbnail = Substitute.For<ISetEpisodeThumbnail>();
        _libraryManager = MockHelper.LibraryManager;
        var logger = Substitute.For<ILogger<OverwriteMovieJellyfinDataTask>>();
        _mediaSourceManager = MockHelper.MediaSourceManager;
        _config = new PluginConfiguration
        {
            IsFeatureMovieTitleEnabled = true,
            IsFeatureMovieDescriptionEnabled = true,
            IsFeatureMovieStudioEnabled = true,
            IsFeatureMovieThumbnailImageEnabled = true
        };
        
        _sut = new OverwriteMovieJellyfinDataTask(
            _repository, 
            logger,
            _setEpisodeThumbnail,
            _libraryManager,
            _config);
    }

    [Fact]
    public async Task SetsMetadataAndThumbnail_WhenSuccessful_GivenMovieWithIds()
    {
        //Arrange
        var movie = MovieFaker.GenerateWithCrunchyrollIds();
        var episode = CrunchyrollEpisodeFaker.Generate();
        episode = episode with { CrunchyrollId = movie.ProviderIds[CrunchyrollExternalKeys.EpisodeId] };
        var thumbnailImageSource = JsonSerializer.Deserialize<ImageSource>(episode.Thumbnail)!;
        
        var season = CrunchyrollSeasonFaker.Generate();
        season.Episodes.Add(episode);
        var titleMetadata = CrunchyrollTitleMetadataFaker.Generate(movie, [season]);
        
        _libraryManager
            .GetItemById(movie.ParentId)
            .Returns((BaseItem?)null);

        _repository
            .GetTitleMetadataAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(titleMetadata);
        
        _setEpisodeThumbnail
            .GetAndSetThumbnailAsync(movie, thumbnailImageSource, Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        //Act
        await _sut.RunAsync(movie, CancellationToken.None);

        //Assert
        movie.Name.Should().Be(episode.Title);
        movie.Overview.Should().Be(episode.Description);
        movie.Studios.Should().BeEquivalentTo([titleMetadata.Studio]);

        await _setEpisodeThumbnail
            .Received(1)
            .GetAndSetThumbnailAsync(movie, Arg.Is<ImageSource>(x => 
                x.Uri == thumbnailImageSource.Uri &&
                x.Height == thumbnailImageSource.Height &&
                x.Width == thumbnailImageSource.Width), Arg.Any<CancellationToken>());
        
        await _libraryManager
            .Received(1)
            .UpdateItemAsync(movie, movie.DisplayParent, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetsTitleNameWithoutBrackets_WhenTitleHasBracketsAtStart_GivenMovieWithIds()
    {
        //Arrange
        var movie = MovieFaker.GenerateWithCrunchyrollIds();
        var episode = CrunchyrollEpisodeFaker.Generate();
        var title = episode.Title;
        episode = episode with
        {
            CrunchyrollId = movie.ProviderIds[CrunchyrollExternalKeys.EpisodeId],
            Title = $"(OMU) {episode.Title}"
        };
        var thumbnailImageSource = JsonSerializer.Deserialize<ImageSource>(episode.Thumbnail)!;
        
        var season = CrunchyrollSeasonFaker.Generate();
        season.Episodes.Add(episode);
        var titleMetadata = CrunchyrollTitleMetadataFaker.Generate(movie, [season]);
        
        _libraryManager
            .GetItemById(movie.ParentId)
            .Returns((BaseItem?)null);

        _repository
            .GetTitleMetadataAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(titleMetadata);
        
        _setEpisodeThumbnail
            .GetAndSetThumbnailAsync(movie, thumbnailImageSource, Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        //Act
        await _sut.RunAsync(movie, CancellationToken.None);

        //Assert
        movie.Name.Should().Be(title);
        movie.Overview.Should().Be(episode.Description);
        movie.Studios.Should().BeEquivalentTo([titleMetadata.Studio]);

        await _setEpisodeThumbnail
            .Received(1)
            .GetAndSetThumbnailAsync(movie, Arg.Is<ImageSource>(x => 
                x.Uri == thumbnailImageSource.Uri &&
                x.Height == thumbnailImageSource.Height &&
                x.Width == thumbnailImageSource.Width), Arg.Any<CancellationToken>());
        
        await _libraryManager
            .Received(1)
            .UpdateItemAsync(movie, movie.DisplayParent, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetsMetadataAndThumbnail_WhenSetEpisodeThumbnailFails_GivenMovieWithIds()
    {
        //Arrange
        var movie = MovieFaker.GenerateWithCrunchyrollIds();
        var episode = CrunchyrollEpisodeFaker.Generate();
        episode = episode with { CrunchyrollId = movie.ProviderIds[CrunchyrollExternalKeys.EpisodeId] };
        var thumbnailImageSource = JsonSerializer.Deserialize<ImageSource>(episode.Thumbnail)!;
        
        var season = CrunchyrollSeasonFaker.Generate();
        season.Episodes.Add(episode);
        var titleMetadata = CrunchyrollTitleMetadataFaker.Generate(movie, [season]);
        
        _libraryManager
            .GetItemById(movie.ParentId)
            .Returns((BaseItem?)null);

        _repository
            .GetTitleMetadataAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(titleMetadata);
        
        _setEpisodeThumbnail
            .GetAndSetThumbnailAsync(movie, thumbnailImageSource, Arg.Any<CancellationToken>())
            .Returns(Result.Fail("error123"));

        //Act
        await _sut.RunAsync(movie, CancellationToken.None);

        //Assert
        movie.Name.Should().Be(episode.Title);
        movie.Overview.Should().Be(episode.Description);
        movie.Studios.Should().BeEquivalentTo([titleMetadata.Studio]);

        await _setEpisodeThumbnail
            .Received(1)
            .GetAndSetThumbnailAsync(movie, Arg.Is<ImageSource>(x => 
                x.Uri == thumbnailImageSource.Uri &&
                x.Height == thumbnailImageSource.Height &&
                x.Width == thumbnailImageSource.Width), Arg.Any<CancellationToken>());
        
        await _libraryManager
            .Received(1)
            .UpdateItemAsync(movie, movie.DisplayParent, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SkipsMovie_WhenNoTitleMetadataFound_GivenMovieWithIds()
    {
        //Arrange
        var movie = MovieFaker.GenerateWithCrunchyrollIds();

        _repository
            .GetTitleMetadataAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns((Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata?)null);
        
        //Act
        await _sut.RunAsync(movie, CancellationToken.None);

        //Assert
        await _repository
            .Received(1)
            .GetTitleMetadataAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
        
        await _libraryManager
            .DidNotReceive()
            .UpdateItemAsync(movie, movie.DisplayParent, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SkipsMovie_WhenGetTitleMetadataFailed_GivenMovieWithIds()
    {
        //Arrange
        var movie = MovieFaker.GenerateWithCrunchyrollIds();

        _repository
            .GetTitleMetadataAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail("error"));
        
        //Act
        await _sut.RunAsync(movie, CancellationToken.None);

        //Assert
        await _repository
            .Received(1)
            .GetTitleMetadataAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
        
        await _libraryManager
            .DidNotReceive()
            .UpdateItemAsync(movie, movie.DisplayParent, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SkipsMovie_WhenItemHasNoIds_GivenMovieWithNoIds()
    {
        //Arrange
        var movie = MovieFaker.Generate();

        _mediaSourceManager
            .GetPathProtocol(movie.Path)
            .Returns(MediaProtocol.File);

        //Act
        await _sut.RunAsync(movie, CancellationToken.None);

        //Assert
        await _repository
            .DidNotReceive()
            .GetTitleMetadataAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
    }
    
    

    [Fact]
    public async Task SkipsMovie_WhenEpisodeIdCanNotBeFound_GivenMovieWithIds()
    {
        //Arrange
        var movie = MovieFaker.GenerateWithCrunchyrollIds();
        var episode = CrunchyrollEpisodeFaker.Generate();
        var season = CrunchyrollSeasonFaker.Generate();
        season.Episodes.Add(episode);
        var titleMetadata = CrunchyrollTitleMetadataFaker.Generate(movie, [season]);
        var thumbnailImageSource = JsonSerializer.Deserialize<ImageSource>(episode.Thumbnail)!;
        
        _libraryManager
            .GetItemById(movie.ParentId)
            .Returns((BaseItem?)null);

        _repository
            .GetTitleMetadataAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(titleMetadata);

        //Act
        await _sut.RunAsync(movie, CancellationToken.None);

        //Assert
        await _setEpisodeThumbnail
            .DidNotReceive()
            .GetAndSetThumbnailAsync(movie, thumbnailImageSource, Arg.Any<CancellationToken>());
        
        await _libraryManager
            .DidNotReceive()
            .UpdateItemAsync(movie, movie.DisplayParent, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task DoesNotDownloadAndSetImage_WhenFeatureMovieThumbnailImageIsDisabled_GivenMovieWithIds()
    {
        //Arrange
        var movie = MovieFaker.GenerateWithCrunchyrollIds();
        var episode = CrunchyrollEpisodeFaker.Generate();
        episode = episode with { CrunchyrollId = movie.ProviderIds[CrunchyrollExternalKeys.EpisodeId] };
        var thumbnailImageSource = JsonSerializer.Deserialize<ImageSource>(episode.Thumbnail)!;

        _config.IsFeatureMovieThumbnailImageEnabled = false;
        
        var season = CrunchyrollSeasonFaker.Generate();
        season.Episodes.Add(episode);
        var titleMetadata = CrunchyrollTitleMetadataFaker.Generate(movie, [season]);
        
        _libraryManager
            .GetItemById(movie.ParentId)
            .Returns((BaseItem?)null);

        _repository
            .GetTitleMetadataAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(titleMetadata);

        //Act
        await _sut.RunAsync(movie, CancellationToken.None);

        //Assert
        movie.Name.Should().Be(episode.Title);
        movie.Overview.Should().Be(episode.Description);
        movie.Studios.Should().BeEquivalentTo([titleMetadata.Studio]);

        await _setEpisodeThumbnail
            .DidNotReceive()
            .GetAndSetThumbnailAsync(Arg.Any<Movie>(), Arg.Any<ImageSource>(), 
                Arg.Any<CancellationToken>());
        
        await _libraryManager
            .Received(1)
            .UpdateItemAsync(movie, movie.DisplayParent, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task DoesNotUpdateTitle_WhenFeatureMovieTitleIsDisabled_GivenMovieWithIds()
    {
        //Arrange
        var movie = MovieFaker.GenerateWithCrunchyrollIds();
        var episode = CrunchyrollEpisodeFaker.Generate();
        episode = episode with { CrunchyrollId = movie.ProviderIds[CrunchyrollExternalKeys.EpisodeId] };
        var thumbnailImageSource = JsonSerializer.Deserialize<ImageSource>(episode.Thumbnail)!;

        _config.IsFeatureMovieTitleEnabled = false;
        
        var season = CrunchyrollSeasonFaker.Generate();
        season.Episodes.Add(episode);
        var titleMetadata = CrunchyrollTitleMetadataFaker.Generate(movie, [season]);
        
        _libraryManager
            .GetItemById(movie.ParentId)
            .Returns((BaseItem?)null);

        _repository
            .GetTitleMetadataAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(titleMetadata);

        //Act
        await _sut.RunAsync(movie, CancellationToken.None);

        //Assert
        movie.Name.Should().NotBe(episode.Title, "feature is disabled");
        movie.Overview.Should().Be(episode.Description);
        movie.Studios.Should().BeEquivalentTo([titleMetadata.Studio]);

        await _setEpisodeThumbnail
            .Received(1)
            .GetAndSetThumbnailAsync(movie, Arg.Is<ImageSource>(x => 
                x.Uri == thumbnailImageSource.Uri &&
                x.Height == thumbnailImageSource.Height &&
                x.Width == thumbnailImageSource.Width), Arg.Any<CancellationToken>());
        
        await _libraryManager
            .Received(1)
            .UpdateItemAsync(movie, movie.DisplayParent, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task DoesNotUpdateOverview_WhenFeatureMovieDescriptionIsDisabled_GivenMovieWithIds()
    {
        //Arrange
        var movie = MovieFaker.GenerateWithCrunchyrollIds();
        var episode = CrunchyrollEpisodeFaker.Generate();
        episode = episode with { CrunchyrollId = movie.ProviderIds[CrunchyrollExternalKeys.EpisodeId] };
        var thumbnailImageSource = JsonSerializer.Deserialize<ImageSource>(episode.Thumbnail)!;

        _config.IsFeatureMovieDescriptionEnabled = false;
        
        var season = CrunchyrollSeasonFaker.Generate();
        season.Episodes.Add(episode);
        var titleMetadata = CrunchyrollTitleMetadataFaker.Generate(movie, [season]);
        
        _libraryManager
            .GetItemById(movie.ParentId)
            .Returns((BaseItem?)null);

        _repository
            .GetTitleMetadataAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(titleMetadata);

        //Act
        await _sut.RunAsync(movie, CancellationToken.None);

        //Assert
        movie.Name.Should().Be(episode.Title);
        movie.Overview.Should().NotBe(episode.Description, "feature is disabled");
        movie.Studios.Should().BeEquivalentTo([titleMetadata.Studio]);

        await _setEpisodeThumbnail
            .Received(1)
            .GetAndSetThumbnailAsync(movie, Arg.Is<ImageSource>(x => 
                x.Uri == thumbnailImageSource.Uri &&
                x.Height == thumbnailImageSource.Height &&
                x.Width == thumbnailImageSource.Width), Arg.Any<CancellationToken>());
        
        await _libraryManager
            .Received(1)
            .UpdateItemAsync(movie, movie.DisplayParent, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task DoesNotUpdateStudios_WhenFeatureMovieStudioIsDisabled_GivenMovieWithIds()
    {
        //Arrange
        var movie = MovieFaker.GenerateWithCrunchyrollIds();
        var episode = CrunchyrollEpisodeFaker.Generate();
        episode = episode with { CrunchyrollId = movie.ProviderIds[CrunchyrollExternalKeys.EpisodeId] };
        var thumbnailImageSource = JsonSerializer.Deserialize<ImageSource>(episode.Thumbnail)!;

        _config.IsFeatureMovieStudioEnabled = false;
        
        var season = CrunchyrollSeasonFaker.Generate();
        season.Episodes.Add(episode);
        var titleMetadata = CrunchyrollTitleMetadataFaker.Generate(movie, [season]);
        
        _libraryManager
            .GetItemById(movie.ParentId)
            .Returns((BaseItem?)null);

        _repository
            .GetTitleMetadataAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(titleMetadata);

        //Act
        await _sut.RunAsync(movie, CancellationToken.None);

        //Assert
        movie.Name.Should().Be(episode.Title);
        movie.Overview.Should().Be(episode.Description);
        movie.Studios.Should().NotContain(titleMetadata.Studio, "feature is disabled");

        await _setEpisodeThumbnail
            .Received(1)
            .GetAndSetThumbnailAsync(movie, Arg.Is<ImageSource>(x => 
                x.Uri == thumbnailImageSource.Uri &&
                x.Height == thumbnailImageSource.Height &&
                x.Width == thumbnailImageSource.Width), Arg.Any<CancellationToken>());
        
        await _libraryManager
            .Received(1)
            .UpdateItemAsync(movie, movie.DisplayParent, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
    }
}