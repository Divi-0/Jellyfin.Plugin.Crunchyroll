using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using FluentResults.Extensions;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Login;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Episodes;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Image.Entites;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Seasons;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Series;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Series.Dtos;
using Mediator;
using JsonSerializer = System.Text.Json.JsonSerializer;

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
    private readonly IScrapTitleMetadataRepository _repository;
    private readonly ICrunchyrollSeasonsClient _seasonsClient;
    private readonly ICrunchyrollEpisodesClient _episodesClient;
    private readonly ILoginService _loginService;
    private readonly ICrunchyrollSeriesClient _crunchyrollSeriesClient;

    public ScrapTitleMetadataCommandHandler(IScrapTitleMetadataRepository repository,
        ICrunchyrollSeasonsClient seasonsClient, ICrunchyrollEpisodesClient episodesClient, ILoginService loginService, 
        ICrunchyrollSeriesClient crunchyrollSeriesClient)
    {
        _repository = repository;
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
        
        var titleMetadataResult = await _repository.GetTitleMetadataAsync(request.TitleId, 
            request.Language, cancellationToken);

        if (titleMetadataResult.IsFailed)
        {
            return titleMetadataResult.ToResult();
        }

        var titleMetadata = titleMetadataResult.Value;
        
        var seriesMetadataResult = await _crunchyrollSeriesClient.GetSeriesMetadataAsync(request.TitleId, request.Language,
            cancellationToken);

        if (seriesMetadataResult.IsFailed)
        {
            return seriesMetadataResult.ToResult();
        }

        var seriesRating = await _crunchyrollSeriesClient.GetRatingAsync(request.TitleId,
            cancellationToken);
        
        if (titleMetadata is null)
        {
            var crunchyrollPosterTall = seriesMetadataResult.Value.Images.PosterTall.First().Last();
            var crunchyrollPosterWide = seriesMetadataResult.Value.Images.PosterWide.First().Last();
            titleMetadata = new Entities.TitleMetadata
            {
                Id = Guid.NewGuid(),
                CrunchyrollId = request.TitleId,
                SlugTitle = seriesMetadataResult.Value.SlugTitle,
                Description = seriesMetadataResult.Value.Description,
                Title = seriesMetadataResult.Value.Title,
                Studio = seriesMetadataResult.Value.ContentProvider,
                Rating = seriesRating.ValueOrDefault, //ignore if failed
                PosterTall = JsonSerializer.Serialize(new ImageSource
                {
                    Uri = crunchyrollPosterTall.Source,
                    Width = crunchyrollPosterTall.Width,
                    Height = crunchyrollPosterTall.Height,
                }),
                PosterWide = JsonSerializer.Serialize(new ImageSource
                {
                    Uri = crunchyrollPosterWide.Source,
                    Width = crunchyrollPosterWide.Width,
                    Height = crunchyrollPosterWide.Height,
                }),
                Seasons = [],
                Language = request.Language.Name
            };
        }
        else
        {
            ApplyNewSeriesMetadataToTitleMetadata(titleMetadata, seriesMetadataResult.Value, seriesRating.ValueOrDefault);
        }

        var seasons = crunchyrollSeasons.Select(x =>
            x.ToSeasonEntity(titleMetadata.Id, request.Language)).ToList();
        
        if (titleMetadata.Seasons.Count == 0)
        {
            titleMetadata.Seasons.AddRange(seasons);
        }
        else
        {
            ApplyNewSeasonsToExistingSeasons(titleMetadata, seasons);
        }
        
        await Parallel.ForEachAsync(titleMetadata.Seasons, cancellationToken, async (season, _) =>
        {
            var episodesResult =  await _episodesClient.GetEpisodesAsync(season.CrunchyrollId, request.Language, cancellationToken);

            if (episodesResult.IsFailed)
            {
                return;
            }

            var episodes = episodesResult.Value.Data
                .Select(x => x.ToEpisodeEntity(season.Id, request.Language)).ToList();
            
            if (season.Episodes.Count == 0)
            {
                season.Episodes.AddRange(episodes);
            }
            else
            {
                ApplyNewEpisodesToExistingEpisodes(season, episodes);
            }
        });

        if (!string.IsNullOrWhiteSpace(request.MovieEpisodeId) 
            && !string.IsNullOrWhiteSpace(request.MovieSeasonId))
        {
            await HandleMovieAsync(request.MovieEpisodeId!, request.MovieSeasonId, request.Language, titleMetadata, cancellationToken);
        }
        
        var dbResult = await _repository.AddOrUpdateTitleMetadata(titleMetadata, cancellationToken)
            .Bind(async () => await _repository.SaveChangesAsync(cancellationToken));
        
        return dbResult.IsFailed
            ? dbResult
            : Result.Ok();
    }

    private static void ApplyNewEpisodesToExistingEpisodes(Season season, List<Episode> episodes)
    {
        foreach (var currentListedCrunchyrollEpisode in episodes ?? [])
        {
            if (season.Episodes.All(x => x.CrunchyrollId != currentListedCrunchyrollEpisode.CrunchyrollId))
            {
                season.Episodes.Add(currentListedCrunchyrollEpisode);
            }
        }
    }

    private static void ApplyNewSeasonsToExistingSeasons(Entities.TitleMetadata titleMetadata, List<Season> seasons)
    {
        foreach (var season in seasons)
        {
            if (titleMetadata.Seasons.All(x => x.CrunchyrollId != season.CrunchyrollId))
            {
                titleMetadata.Seasons.Add(season);
            }
        }
    }

    private static void ApplyNewSeriesMetadataToTitleMetadata(Entities.TitleMetadata titleMetadata, 
        CrunchyrollSeriesContentItem seriesContentResponse, float rating)
    {
        titleMetadata.Title = seriesContentResponse.Title;
        titleMetadata.SlugTitle = seriesContentResponse.SlugTitle;
        titleMetadata.Description = seriesContentResponse.Description;
        titleMetadata.Studio = seriesContentResponse.ContentProvider;
        titleMetadata.Rating = rating;
        
        var crunchyrollPosterTall = seriesContentResponse.Images.PosterTall.First().Last();
        var crunchyrollPosterWide = seriesContentResponse.Images.PosterWide.First().Last();
        titleMetadata.PosterTall = JsonSerializer.Serialize(new ImageSource
        {
            Uri = crunchyrollPosterTall.Source,
            Width = crunchyrollPosterTall.Width,
            Height = crunchyrollPosterTall.Height,
        });
        titleMetadata.PosterWide = JsonSerializer.Serialize(new ImageSource
        {
            Uri = crunchyrollPosterWide.Source,
            Width = crunchyrollPosterWide.Width,
            Height = crunchyrollPosterWide.Height,
        });
    }

    private async Task HandleMovieAsync(string crunchyrollEpisodeId, string crunchyrollSeasonId, CultureInfo language, Entities.TitleMetadata titleMetadata, 
        CancellationToken cancellationToken)
    {
        var isEpisodeAlreadyScraped = titleMetadata.Seasons
            .SelectMany(x => x.Episodes)
            .Any(x => x.CrunchyrollId == crunchyrollEpisodeId);
        
        if (isEpisodeAlreadyScraped)
        {
            return;
        }

        var episodeResult = await _episodesClient.GetEpisodeAsync(crunchyrollEpisodeId, language, cancellationToken);

        if (episodeResult.IsFailed)
        {
            return;
        }

        var season = titleMetadata.Seasons.FirstOrDefault(x => x.CrunchyrollId == crunchyrollSeasonId);

        if (season is null)
        {
            season = new Season
            {
                CrunchyrollId = crunchyrollSeasonId,
                Title = episodeResult.Value.EpisodeMetadata.SeasonTitle,
                Identifier = string.Empty,
                SeasonNumber = episodeResult.Value.EpisodeMetadata.SeasonNumber,
                SeasonSequenceNumber = episodeResult.Value.EpisodeMetadata.SeasonSequenceNumber,
                SlugTitle = episodeResult.Value.EpisodeMetadata.SeriesSlugTitle,
                SeasonDisplayNumber = episodeResult.Value.EpisodeMetadata.SeasonDisplayNumber,
                Episodes = [],
                SeriesId = titleMetadata.Id,
                Language = language.Name
            };
            
            titleMetadata.Seasons.Add(season);
        }
        
        season.Episodes.Add(new Episode
        {
            CrunchyrollId = crunchyrollEpisodeId,
            Title = episodeResult.Value.Title,
            Description = episodeResult.Value.Description,
            Thumbnail = JsonSerializer.Serialize(new ImageSource
            {
                Uri = episodeResult.Value.Images.Thumbnail.First().Last().Source,
                Height = episodeResult.Value.Images.Thumbnail.First().Last().Height,
                Width = episodeResult.Value.Images.Thumbnail.First().Last().Width
            }),
            EpisodeNumber = episodeResult.Value.EpisodeMetadata.Episode,
            SequenceNumber = episodeResult.Value.EpisodeMetadata.SequenceNumber,
            SlugTitle = episodeResult.Value.SlugTitle,
            SeasonId = season.Id,
            Language = language.Name
        });
    }
}