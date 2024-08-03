using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Login;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.Entities;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Episodes;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Seasons;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata;

public record ScrapTitleMetadataCommand : IRequest<Result>
{
    public required string TitleId { get; init; }
    public required string SlugTitle { get; init; }
}

public class ScrapTitleMetadataCommandHandler : IRequestHandler<ScrapTitleMetadataCommand, Result>
{
    private readonly IScrapTitleMetadataSession _unitOfWork;
    private readonly ICrunchyrollSeasonsClient _seasonsClient;
    private readonly ICrunchyrollEpisodesClient _episodesClient;
    private readonly ILogger<ScrapTitleMetadataCommandHandler> _logger;
    private readonly ILoginService _loginService;

    public ScrapTitleMetadataCommandHandler(IScrapTitleMetadataSession unitOfWork,
        ICrunchyrollSeasonsClient seasonsClient, ICrunchyrollEpisodesClient episodesClient, 
        ILogger<ScrapTitleMetadataCommandHandler> logger, ILoginService loginService)
    {
        _unitOfWork = unitOfWork;
        _seasonsClient = seasonsClient;
        _episodesClient = episodesClient;
        _logger = logger;
        _loginService = loginService;
    }
    
    public async ValueTask<Result> Handle(ScrapTitleMetadataCommand request, CancellationToken cancellationToken)
    {
        var loginResult = await _loginService.LoginAnonymously(cancellationToken);

        if (loginResult.IsFailed)
        {
            return loginResult;
        }
        
        var seasonsResult = await _seasonsClient.GetSeasonsAsync(request.TitleId, cancellationToken);

        if (seasonsResult.IsFailed)
        {
            return seasonsResult.ToResult();
        }
        
        var crunchyrollSeasons = seasonsResult.Value.Data;
        
        var titleMetadata = await _unitOfWork.GetTitleMetadata(request.TitleId);
        
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount / 2
        };

        var seasonEpisodesDictionary = new ConcurrentDictionary<string, List<Episode>>();
        await Parallel.ForEachAsync(crunchyrollSeasons, parallelOptions, async (item, _) =>
        {
            var episodesResult =  await _episodesClient.GetEpisodesAsync(item.Id, cancellationToken);
            
            seasonEpisodesDictionary[item.Id] = episodesResult.IsFailed ? 
                Array.Empty<Episode>().ToList() : 
                episodesResult.Value.Data
                    .Select(x => x.ToEpisodeEntity()).ToList();
        });

        var seasons = crunchyrollSeasons.Select(x =>
            x.ToSeasonEntity(seasonEpisodesDictionary[x.Id])).ToList();
        
        if (titleMetadata is null)
        {
            titleMetadata = new Entities.TitleMetadata
            {
                TitleId = request.TitleId,
                SlugTitle = request.SlugTitle,
                Description = "",
                Seasons = seasons
            };
        }
        else
        {
            ApplyNewSeasonsToExistingSeasons(titleMetadata, seasons);
            ApplyNewEpisodesToExistingEpisodes(titleMetadata, seasonEpisodesDictionary);
        }
        
        await _unitOfWork.AddOrUpdateTitleMetadata(titleMetadata);
        
        return Result.Ok();
    }

    private static void ApplyNewEpisodesToExistingEpisodes(Entities.TitleMetadata titleMetadata, ConcurrentDictionary<string, List<Episode>> seasonEpisodesDictionary)
    {
        foreach (var season in titleMetadata.Seasons)
        {
            seasonEpisodesDictionary.TryGetValue(season.Id, out var episodes);
            foreach (var currentListedCrunchyrollEpisode in episodes ?? Array.Empty<Episode>().ToList())
            {
                if (season.Episodes.All(x => x.Id != currentListedCrunchyrollEpisode.Id))
                {
                    season.Episodes.Add(currentListedCrunchyrollEpisode);
                }
            }
        }
    }

    private static void ApplyNewSeasonsToExistingSeasons(Entities.TitleMetadata titleMetadata, List<Season> seasons)
    {
        foreach (var season in seasons)
        {
            if (titleMetadata.Seasons.All(x => x.Id != season.Id))
            {
                titleMetadata.Seasons.Add(season);
            }
        }
    }
}