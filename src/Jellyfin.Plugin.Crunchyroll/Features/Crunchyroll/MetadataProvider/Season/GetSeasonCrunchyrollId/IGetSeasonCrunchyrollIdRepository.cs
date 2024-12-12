using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Domain;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Season.GetSeasonCrunchyrollId;

public interface IGetSeasonCrunchyrollIdRepository
{
    public Task<Result<CrunchyrollId?>> GetSeasonIdByNumberAsync(CrunchyrollId titleId, int seasonNumber,
        CultureInfo language, CancellationToken cancellationToken);
    
    public Task<Result<CrunchyrollId?>> GetSeasonIdByNameAsync(CrunchyrollId crunchyrollTitleId, string name, CultureInfo language,
        CancellationToken cancellationToken);
    
    public Task<Result<Domain.Entities.Season[]>> GetAllSeasonsAsync(CrunchyrollId crunchyrollTitleId, CultureInfo language,
        CancellationToken cancellationToken);
}