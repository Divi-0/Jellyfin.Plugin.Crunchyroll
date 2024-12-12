using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Domain;
using Jellyfin.Plugin.Crunchyroll.Domain.Constants;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.SetMetadataToSeries;

public class SetMetadataToSeriesService : ISetMetadataToSeriesService
{
    private readonly PluginConfiguration _config;
    private readonly ISetMetadataToSeriesRepository _repository;

    public SetMetadataToSeriesService(PluginConfiguration config, ISetMetadataToSeriesRepository repository)
    {
        _config = config;
        _repository = repository;
    }
    
    public async Task<Result<MediaBrowser.Controller.Entities.TV.Series>> SetSeriesMetadataAsync(CrunchyrollId crunchyrollId, 
        CultureInfo language, CancellationToken cancellationToken)
    {
        var titleMetadataResult = await _repository.GetTitleMetadataAsync(crunchyrollId, language, cancellationToken);

        if (titleMetadataResult.IsFailed)
        {
            return titleMetadataResult.ToResult();
        }

        if (titleMetadataResult.Value is null)
        {
            return Result.Fail(ErrorCodes.NotFound);
        }

        var titleMetadata = titleMetadataResult.Value;
        var newSeriesMetadata = new MediaBrowser.Controller.Entities.TV.Series();
        
        SetSeriesItemName(newSeriesMetadata, titleMetadata);
        SetSeriesItemOverview(newSeriesMetadata, titleMetadata);
        SetSeriesItemStudios(newSeriesMetadata, titleMetadata);
        SetSeriesItemCommunityRating(newSeriesMetadata, titleMetadata);
        
        return Result.Ok(newSeriesMetadata);
    }
    
    private void SetSeriesItemName(MediaBrowser.Controller.Entities.TV.Series series, Domain.Entities.TitleMetadata titleMetadata)
    {
        if (!_config.IsFeatureSeriesTitleEnabled)
        {
            return;
        }
        
        series.Name = titleMetadata.Title;
    }

    private void SetSeriesItemOverview(MediaBrowser.Controller.Entities.TV.Series series, Domain.Entities.TitleMetadata titleMetadata)
    {
        if (!_config.IsFeatureSeriesDescriptionEnabled)
        {
            return;
        }
        
        series.Overview = titleMetadata.Description;
    }

    private void SetSeriesItemStudios(MediaBrowser.Controller.Entities.TV.Series series, Domain.Entities.TitleMetadata titleMetadata)
    {
        if (!_config.IsFeatureSeriesStudioEnabled)
        {
            return;
        }
        
        series.SetStudios([titleMetadata.Studio]);
    }

    private void SetSeriesItemCommunityRating(MediaBrowser.Controller.Entities.TV.Series series, Domain.Entities.TitleMetadata titleMetadata)
    {
        if (!_config.IsFeatureSeriesRatingsEnabled)
        {
            return;
        }
        
        series.CommunityRating = titleMetadata.Rating;
    }
}