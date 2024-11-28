using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Login;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.Interfaces;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.SetMovieEpisodeId.Client;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.SetMovieEpisodeId;

public class SetMovieEpisodeIdTask : IPostMovieScanTask
{
    private readonly ILogger<SetMovieEpisodeIdTask> _logger;
    private readonly IEnumerable<IPostMovieIdSetTask> _postmovieIdSetTasks;
    private readonly ICrunchyrollMovieEpisodeIdClient _client;
    private readonly ILoginService _loginService;

    public SetMovieEpisodeIdTask(ILogger<SetMovieEpisodeIdTask> logger, IEnumerable<IPostMovieIdSetTask> postmovieIdSetTasks,
        ICrunchyrollMovieEpisodeIdClient client, ILoginService loginService)
    {
        _logger = logger;
        _postmovieIdSetTasks = postmovieIdSetTasks;
        _client = client;
        _loginService = loginService;
    }
    
    public async Task RunAsync(BaseItem item, CancellationToken cancellationToken)
    {
        if (item is not Movie movie)
        {
            _logger.LogWarning("Item with path {Path} is not a Movie", item.Path);
            return;
        }
        
        var setTitleIdResult = await SetEpisodeIdAsync(movie, cancellationToken);

        if (setTitleIdResult.IsFailed)
        {
            return;
        }

        await RunPostTasksAsync(movie, cancellationToken);
    }

    //Movies have episode ids
    private async Task<Result> SetEpisodeIdAsync(Movie movie, CancellationToken cancellationToken)
    {
        var loginResult = await _loginService.LoginAnonymouslyAsync(cancellationToken);

        if (loginResult.IsFailed)
        {
            return loginResult;
        }
        
        var searchResponseResult = await _client.SearchTitleIdAsync(
            movie.FileNameWithoutExtension,
            movie.GetPreferredMetadataCultureInfo(),
            cancellationToken);

        if (searchResponseResult.IsFailed)
        {
            return searchResponseResult.ToResult();
        }
        
        movie.ProviderIds[CrunchyrollExternalKeys.SeriesId] = searchResponseResult.Value?.SeriesId ?? string.Empty;
        movie.ProviderIds[CrunchyrollExternalKeys.SeriesSlugTitle] = searchResponseResult.Value?.SeriesSlugTitle ?? string.Empty;
        
        movie.ProviderIds[CrunchyrollExternalKeys.EpisodeId] = searchResponseResult.Value?.EpisodeId ?? string.Empty;
        movie.ProviderIds[CrunchyrollExternalKeys.EpisodeSlugTitle] = searchResponseResult.Value?.EpisodeSlugTitle ?? string.Empty;
        
        movie.ProviderIds[CrunchyrollExternalKeys.SeasonId] = searchResponseResult.Value?.SeasonId ?? string.Empty;
        
        return Result.Ok();
    }

    private async Task RunPostTasksAsync(Movie movie, CancellationToken cancellationToken)
    {
        foreach (var task in _postmovieIdSetTasks)
        {
            await task.RunAsync(movie, cancellationToken);
        }
    }
}