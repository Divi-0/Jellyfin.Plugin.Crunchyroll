using System;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using FluentResults.Extensions;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Domain;
using Jellyfin.Plugin.Crunchyroll.Domain.Entities;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Login;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.GetMetadata.ScrapSeriesMetadata.Client;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.GetMetadata.ScrapSeriesMetadata.Client.Dtos;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.GetMetadata.ScrapSeriesMetadata;

public class ScrapSeriesMetadataService : IScrapSeriesMetadataService
{
    private readonly IScrapSeriesMetadataRepository _repository;
    private readonly ILoginService _loginService;
    private readonly ICrunchyrollSeriesClient _crunchyrollSeriesClient;
    private readonly TimeProvider _timeProvider;
    private readonly PluginConfiguration _config;
    private readonly ILogger<ScrapSeriesMetadataService> _logger;

    public ScrapSeriesMetadataService(IScrapSeriesMetadataRepository repository, ILoginService loginService,
        ICrunchyrollSeriesClient crunchyrollSeriesClient, TimeProvider timeProvider, PluginConfiguration config,
        ILogger<ScrapSeriesMetadataService> logger)
    {
        _repository = repository;
        _loginService = loginService;
        _crunchyrollSeriesClient = crunchyrollSeriesClient;
        _timeProvider = timeProvider;
        _config = config;
        _logger = logger;
    }
    
    public async Task<Result> ScrapSeriesMetadataAsync(CrunchyrollId crunchyrollId, CultureInfo language,
        CancellationToken cancellationToken)
    {
        var titleMetadataResult = await _repository
            .GetTitleMetadataAsync(crunchyrollId, language, cancellationToken);

        if (titleMetadataResult.IsFailed)
        {
            return titleMetadataResult.ToResult();
        }
        
        if (titleMetadataResult.Value is not null &&
            titleMetadataResult.Value.LastUpdatedAt.AddDays(_config.CrunchyrollUpdateThresholdInDays) > _timeProvider.GetUtcNow())
        {
            _logger.LogInformation("Not updating metadata for series {SeriesId}, threshold is not reached", crunchyrollId);
            return Result.Ok();
        }
        
        var loginResult = await _loginService.LoginAnonymouslyAsync(cancellationToken);

        if (loginResult.IsFailed)
        {
            return loginResult;
        }
        
        var crunchyrollSeriesMetadataResult = await _crunchyrollSeriesClient
            .GetSeriesMetadataAsync(crunchyrollId, language, cancellationToken);

        if (crunchyrollSeriesMetadataResult.IsFailed)
        {
            return crunchyrollSeriesMetadataResult.ToResult();
        }
        
        var seriesRatingResult = await _crunchyrollSeriesClient.GetRatingAsync(crunchyrollId,
            cancellationToken);

        var titleMetadata = titleMetadataResult.Value;
        
        if (titleMetadata is null)
        {
            var crunchyrollPosterTall = crunchyrollSeriesMetadataResult.Value.Images.PosterTall.First().Last();
            var crunchyrollPosterWide = crunchyrollSeriesMetadataResult.Value.Images.PosterWide.First().Last();
            titleMetadata = new Domain.Entities.TitleMetadata
            {
                Id = Guid.NewGuid(),
                CrunchyrollId = crunchyrollId,
                SlugTitle = crunchyrollSeriesMetadataResult.Value.SlugTitle,
                Description = crunchyrollSeriesMetadataResult.Value.Description,
                Title = crunchyrollSeriesMetadataResult.Value.Title,
                Studio = crunchyrollSeriesMetadataResult.Value.ContentProvider,
                Rating = seriesRatingResult.ValueOrDefault, //ignore if failed
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
                Language = language.Name
            };
        }
        else
        {
            ApplyNewSeriesMetadataToTitleMetadata(titleMetadata, crunchyrollSeriesMetadataResult.Value, 
                seriesRatingResult.ValueOrDefault);
        }
        
        var dbResult = await _repository.AddOrUpdateTitleMetadata(titleMetadata, cancellationToken)
            .Bind(async () => await _repository.SaveChangesAsync(cancellationToken));
        
        return dbResult.IsSuccess
            ? Result.Ok()
            : dbResult;
    }
    
    private static void ApplyNewSeriesMetadataToTitleMetadata(Domain.Entities.TitleMetadata titleMetadata, 
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
}