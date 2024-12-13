using System.Globalization;
using System.Text.Json;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Domain.Entities;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.OverwriteMovieJellyfinData;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.SetEpisodeThumbnail;
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
}