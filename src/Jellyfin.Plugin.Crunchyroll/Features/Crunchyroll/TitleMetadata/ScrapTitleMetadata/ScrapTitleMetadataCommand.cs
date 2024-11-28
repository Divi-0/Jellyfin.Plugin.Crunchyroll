using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
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
    public required CultureInfo Language { get; init; }
    public string? MovieEpisodeId { get; init; }
    public string? MovieSeasonId { get; init; }
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
        
        var seasonsResult = await _seasonsClient.GetSeasonsAsync(request.TitleId, request.Language, cancellationToken);

        if (seasonsResult.IsFailed)
        {
            return seasonsResult.ToResult();
        }
        
        var crunchyrollSeasons = seasonsResult.Value.Data;
        
        var titleMetadata = await _unitOfWork.GetTitleMetadataAsync(request.TitleId);

        var seasonEpisodesDictionary = new ConcurrentDictionary<string, List<Episode>>();
        await Parallel.ForEachAsync(crunchyrollSeasons, cancellationToken, async (season, _) =>
        {
            var episodesResult =  await _episodesClient.GetEpisodesAsync(season.Id, request.Language, cancellationToken);
            
            seasonEpisodesDictionary[season.Id] = episodesResult.IsFailed ? 
                Array.Empty<Episode>().ToList() : 
                episodesResult.Value.Data
                    .Select(x => x.ToEpisodeEntity()).ToList();
        });

        var seasons = crunchyrollSeasons.Select(x =>
            x.ToSeasonEntity(seasonEpisodesDictionary[x.Id])).ToList();
        
        var seriesMetadataResult = await _crunchyrollSeriesClient.GetSeriesMetadataAsync(request.TitleId, request.Language,
            cancellationToken);

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

        if (!string.IsNullOrWhiteSpace(request.MovieEpisodeId) 
            && !string.IsNullOrWhiteSpace(request.MovieSeasonId))
        {
            await HandleMovieAsync(request.MovieEpisodeId!, request.MovieSeasonId, request.Language, titleMetadata, cancellationToken);
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

    private async Task HandleMovieAsync(string episodeId, string seasonId, CultureInfo language, Entities.TitleMetadata titleMetadata, 
        CancellationToken cancellationToken)
    {
        var isEpisodeAlreadyScraped = titleMetadata.Seasons
            .SelectMany(x => x.Episodes)
            .Any(x => x.Id == episodeId);
        
        if (isEpisodeAlreadyScraped)
        {
            return;
        }

        var episodeResult = await _episodesClient.GetEpisodeAsync(episodeId, language, cancellationToken);

        if (episodeResult.IsFailed)
        {
            return;
        }

        var season = titleMetadata.Seasons.FirstOrDefault(x => x.Id == seasonId);

        if (season is null)
        {
            season = new Season()
            {
                Id = seasonId,
                Title = episodeResult.Value.EpisodeMetadata.SeasonTitle,
                Identifier = string.Empty,
                SeasonNumber = episodeResult.Value.EpisodeMetadata.SeasonNumber,
                SeasonSequenceNumber = episodeResult.Value.EpisodeMetadata.SeasonSequenceNumber,
                SlugTitle = episodeResult.Value.EpisodeMetadata.SeriesSlugTitle,
                SeasonDisplayNumber = episodeResult.Value.EpisodeMetadata.SeasonDisplayNumber,
                Episodes = []
            };
            
            titleMetadata.Seasons.Add(season);
        }
        
        season.Episodes.Add(new Episode
        {
            Id = episodeId,
            Title = episodeResult.Value.Title,
            Description = episodeResult.Value.Description,
            Thumbnail = new ImageSource
            {
                Uri = episodeResult.Value.Images.Thumbnail.First().Last().Source,
                Height = episodeResult.Value.Images.Thumbnail.First().Last().Height,
                Width = episodeResult.Value.Images.Thumbnail.First().Last().Width
            },
            EpisodeNumber = episodeResult.Value.EpisodeMetadata.Episode,
            SequenceNumber = episodeResult.Value.EpisodeMetadata.SequenceNumber,
            SlugTitle = episodeResult.Value.SlugTitle
        });
    }
}