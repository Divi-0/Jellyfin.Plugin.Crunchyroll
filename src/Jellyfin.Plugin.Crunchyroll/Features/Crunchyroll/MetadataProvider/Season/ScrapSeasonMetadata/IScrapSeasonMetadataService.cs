using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Domain;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Season.ScrapSeasonMetadata;

public interface IScrapSeasonMetadataService
{
    public Task<Result> ScrapSeasonMetadataAsync(CrunchyrollId seriesId, CultureInfo language, 
        CancellationToken cancellationToken);
}