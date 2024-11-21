using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Login;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Episodes;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Image.Entites;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Seasons;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Series;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Series.Dtos;
using Mediator;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata;

public record ScrapTitleMetadataCommand : IRequest<Result>
{
    public required string TitleId { get; init; }
}

public class ScrapTitleMetadataCommandHandler : IRequestHandler<ScrapTitleMetadataCommand, Result>
{
    private readonly IScrapTitleMetadataSession _unitOfWork;
    private readonly ICrunchyrollSeasonsClient _seasonsClient;
    private readonly ICrunchyrollEpisodesClient _episodesClient;
    private readonly ILoginService _loginService;
    private readonly ICrunchyrollSeriesClient _crunchyrollSeriesClient;

    public ScrapTitleMetadataCommandHandler(IScrapTitleMetadataSession unitOfWork,
        ICrunchyrollSeasonsClient seasonsClient, ICrunchyrollEpisodesClient episodesClient, ILoginService loginService, 
        ICrunchyrollSeriesClient crunchyrollSeriesClient)
    {
        _unitOfWork = unitOfWork;
        _seasonsClient = seasonsClient;
        _episodesClient = episodesClient;
        _loginService = loginService;
        _crunchyrollSeriesClient = crunchyrollSeriesClient;
    }
    
    public async ValueTask<Result> Handle(ScrapTitleMetadataCommand request, CancellationToken cancellationToken)
    {
        var loginResult = await _loginService.LoginAnonymouslyAsync(cancellationToken);

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
        
        var titleMetadata = await _unitOfWork.GetTitleMetadataAsync(request.TitleId);
        
        var parallelOptions = new ParallelOptions
        {
            CancellationToken = cancellationToken
        };

        var seasonEpisodesDictionary = new ConcurrentDictionary<string, List<Episode>>();
        await Parallel.ForEachAsync(crunchyrollSeasons, parallelOptions, async (season, token) =>
        {
            var episodesResult =  await _episodesClient.GetEpisodesAsync(season.Id, token);
            
            seasonEpisodesDictionary[season.Id] = episodesResult.IsFailed ? 
                Array.Empty<Episode>().ToList() : 
                episodesResult.Value.Data
                    .Select(x => x.ToEpisodeEntity()).ToList();
        });

        var seasons = crunchyrollSeasons.Select(x =>
            x.ToSeasonEntity(seasonEpisodesDictionary[x.Id])).ToList();
        
        var seriesMetadataResult = await _crunchyrollSeriesClient.GetSeriesMetadataAsync(request.TitleId, cancellationToken);

        if (seriesMetadataResult.IsFailed)
        {
            return seriesMetadataResult.ToResult();
        }
        
        if (titleMetadata is null)
        {
            var crunchyrollPosterTall = seriesMetadataResult.Value.Images.PosterTall.First().Last();
            var crunchyrollPosterWide = seriesMetadataResult.Value.Images.PosterWide.First().Last();
            titleMetadata = new Entities.TitleMetadata
            {
                TitleId = request.TitleId,
                SlugTitle = seriesMetadataResult.Value.SlugTitle,
                Description = seriesMetadataResult.Value.Description,
                Title = seriesMetadataResult.Value.Title,
                Studio = seriesMetadataResult.Value.ContentProvider,
                PosterTall = new ImageSource
                {
                    Uri = crunchyrollPosterTall.Source,
                    Width = crunchyrollPosterTall.Width,
                    Height = crunchyrollPosterTall.Height,
                },
                PosterWide = new ImageSource
                {
                    Uri = crunchyrollPosterWide.Source,
                    Width = crunchyrollPosterWide.Width,
                    Height = crunchyrollPosterWide.Height,
                },
                Seasons = seasons
            };
        }
        else
        {
            ApplyNewSeriesMetadataToTitleMetadata(titleMetadata, seriesMetadataResult.Value);
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

    private static void ApplyNewSeriesMetadataToTitleMetadata(Entities.TitleMetadata titleMetadata, CrunchyrollSeriesContentItem seriesContentResponse)
    {
        titleMetadata.Title = seriesContentResponse.Title;
        titleMetadata.SlugTitle = seriesContentResponse.SlugTitle;
        titleMetadata.Description = seriesContentResponse.Description;
        titleMetadata.Studio = seriesContentResponse.ContentProvider;
        
        var crunchyrollPosterTall = seriesContentResponse.Images.PosterTall.First().Last();
        var crunchyrollPosterWide = seriesContentResponse.Images.PosterWide.First().Last();
        titleMetadata.PosterTall = new ImageSource
        {
            Uri = crunchyrollPosterTall.Source,
            Width = crunchyrollPosterTall.Width,
            Height = crunchyrollPosterTall.Height,
        };
        titleMetadata.PosterWide = new ImageSource
        {
            Uri = crunchyrollPosterWide.Source,
            Width = crunchyrollPosterWide.Width,
            Height = crunchyrollPosterWide.Height,
        };
    }
}