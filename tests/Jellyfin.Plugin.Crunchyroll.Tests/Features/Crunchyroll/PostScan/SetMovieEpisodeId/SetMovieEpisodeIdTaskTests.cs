using System.Globalization;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Login;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.GetMetadata.GetMovieCrunchyrollId.Client;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.Interfaces;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.SetMovieEpisodeId;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.PostScan.SetMovieEpisodeId;

public class SetMovieEpisodeIdTaskTests
{
    private readonly SetMovieEpisodeIdTask _sut;
    private readonly IPostMovieIdSetTask[] _postMovieIdSetTasks;
    private readonly ICrunchyrollMovieEpisodeIdClient _client;
    private readonly ILoginService _loginService;
    private readonly IMediaSourceManager _mediaSourceManager;

    public SetMovieEpisodeIdTaskTests()
    {
        _postMovieIdSetTasks = Enumerable.Range(0, Random.Shared.Next(1, 10))
            .Select(_ => Substitute.For<IPostMovieIdSetTask>())
            .ToArray();
        
        var logger = Substitute.For<ILogger<SetMovieEpisodeIdTask>>();
        _client = Substitute.For<ICrunchyrollMovieEpisodeIdClient>();
        _loginService = Substitute.For<ILoginService>();
        _mediaSourceManager = MockHelper.MediaSourceManager;
        
        _sut = new SetMovieEpisodeIdTask(logger, _postMovieIdSetTasks, _client, _loginService);
    }
}