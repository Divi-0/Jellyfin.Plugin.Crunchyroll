using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Domain;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.GetSeriesCrunchyrollId;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.ScrapSeriesMetadata;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.SetMetadataToSeries;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series;

public partial class SeriesGetMetadataService : ISeriesGetMetadataService
{
    private readonly IGetSeriesCrunchyrollIdService _crunchyrollIdService;
    private readonly IScrapSeriesMetadataService _scrapSeriesMetadataService;
    private readonly ISetMetadataToSeriesService _setMetadataToSeriesService;
    private readonly ILogger<SeriesGetMetadataService> _logger;
    
    public SeriesGetMetadataService(IGetSeriesCrunchyrollIdService crunchyrollIdService,
        IScrapSeriesMetadataService scrapSeriesMetadataService, ISetMetadataToSeriesService setMetadataToSeriesService,
        ILogger<SeriesGetMetadataService> logger)
    {
        _crunchyrollIdService = crunchyrollIdService;
        _scrapSeriesMetadataService = scrapSeriesMetadataService;
        _setMetadataToSeriesService = setMetadataToSeriesService;
        _logger = logger;
    }
    
    public async Task<MetadataResult<MediaBrowser.Controller.Entities.TV.Series>> GetMetadataAsync(SeriesInfo info, CancellationToken cancellationToken)
    {
        var crunchyrollIdResult = await GetCrunchyrollId(info, cancellationToken);

        if (crunchyrollIdResult.IsFailed)
        {
            return new MetadataResult<MediaBrowser.Controller.Entities.TV.Series>()
            {
                HasMetadata = false,
                Item = new MediaBrowser.Controller.Entities.TV.Series()
            };
        }

        var crunchyrollId = crunchyrollIdResult.Value;

        if (string.IsNullOrWhiteSpace(crunchyrollId?.ToString()))
        {
            return new MetadataResult<MediaBrowser.Controller.Entities.TV.Series>()
            {
                HasMetadata = false,
                Item = new MediaBrowser.Controller.Entities.TV.Series()
                {
                    ProviderIds =
                    {
                        {CrunchyrollExternalKeys.SeriesId, string.Empty} //TODO: Test if its working with real jellyfin server
                    }
                }
            };
        }

        var scrapSeriesMetadataResult = await _scrapSeriesMetadataService.ScrapSeriesMetadataAsync(crunchyrollId, 
            info.GetPreferredMetadataCultureInfo(), cancellationToken);

        if (scrapSeriesMetadataResult.IsFailed)
        {
            return new MetadataResult<MediaBrowser.Controller.Entities.TV.Series>()
            {
                HasMetadata = false,
                Item = new MediaBrowser.Controller.Entities.TV.Series()
            };
        }

        var setMetadataToSeriesResult = await _setMetadataToSeriesService.SetSeriesMetadataAsync(crunchyrollId,
            info.GetPreferredMetadataCultureInfo(), cancellationToken);
        
        if (setMetadataToSeriesResult.IsFailed)
        {
            return new MetadataResult<MediaBrowser.Controller.Entities.TV.Series>()
            {
                HasMetadata = false,
                Item = new MediaBrowser.Controller.Entities.TV.Series()
            };
        }

        var newSeries = setMetadataToSeriesResult.Value;
        newSeries.ProviderIds[CrunchyrollExternalKeys.SeriesId] = crunchyrollId;
        
        return new MetadataResult<MediaBrowser.Controller.Entities.TV.Series>()
        {
            HasMetadata = true,
            Item = newSeries
        };
    }
    
    private async Task<Result<CrunchyrollId?>> GetCrunchyrollId(SeriesInfo info, CancellationToken cancellationToken)
    {
        var folderName = Path.GetFileNameWithoutExtension(info.Path);
        var extractedResult = ExtractIdFromFileNameAndSetProviderId(folderName);
        
        if (extractedResult.IsFailed)
        {
            if (info.ProviderIds.TryGetValue(CrunchyrollExternalKeys.SeriesId, out var id) && !string.IsNullOrWhiteSpace(id))
            {
                _logger.LogDebug("TitleId for Item {Name} is already set", folderName);
                return Result.Ok<CrunchyrollId?>(id);
            }
                
            var titleIdResult = await SearchForTitleAndSetProviderIdAsync(folderName, 
                info.GetPreferredMetadataCultureInfo(), cancellationToken);

            if (titleIdResult.IsFailed)
            {
                return titleIdResult.ToResult();
            }

            return titleIdResult.Value;
        }
        
        return Result.Ok<CrunchyrollId?>(extractedResult.Value);
    }
    
    private async Task<Result<CrunchyrollId?>> SearchForTitleAndSetProviderIdAsync(string folderName, 
        CultureInfo language, CancellationToken cancellationToken)
    {
        var seriesFileNameExtraDataInNameRegex = SeriesFileNameExtraDataInNameRegex();
        var match = seriesFileNameExtraDataInNameRegex.Match(folderName);

        var title = match.Success
            ? match.Groups[1].Value
            : folderName;

        return await _crunchyrollIdService
            .GetSeriesCrunchyrollId(title, language, cancellationToken);
    } 
    
    private static Result<string> ExtractIdFromFileNameAndSetProviderId(string folderName)
    {
        var regex = FileNameAttributeCrunchyrollIdRegex();
        var match = regex.Match(folderName);

        return match.Success 
            ? match.Groups[1].Value
            : Result.Fail(Domain.Constants.ErrorCodes.NotFound);
    }
    
    [GeneratedRegex(@"^(.*) \(| \[")]
    private static partial Regex SeriesFileNameExtraDataInNameRegex();
    [GeneratedRegex(@"\[CrunchyrollId\-(.*)\]")]
    private static partial Regex FileNameAttributeCrunchyrollIdRegex();
}