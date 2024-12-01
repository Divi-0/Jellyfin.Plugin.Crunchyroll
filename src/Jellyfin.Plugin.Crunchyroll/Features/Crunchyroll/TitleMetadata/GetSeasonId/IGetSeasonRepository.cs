using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.GetSeasonId;

public interface IGetSeasonRepository
{
    /// <param name="titleId">crunchyroll titleId</param>
    /// <param name="seasonNumber">eg. 1,2,3</param>
    /// <param name="duplicateCounter">When multiple seasons have the same season number this param identifies which duplicate season to choose;
    /// 1 = take the first season you find, 2 = take the second the season of the seasons with identical season-numbers, ...</param>
    /// <param name="language"></param>
    /// <param name="cancellationToken"></param>
    public Task<Result<string?>> GetSeasonIdByNumberAsync(string titleId, int seasonNumber, int duplicateCounter,
        CultureInfo language, CancellationToken cancellationToken);
    public Task<Result<IReadOnlyList<Season>>> GetAllSeasonsAsync(string crunchyrollTitleId, CultureInfo language,
        CancellationToken cancellationToken);
    public Task<Result<string?>> GetSeasonIdByNameAsync(string crunchyrollTitleId, string name, CultureInfo language,
        CancellationToken cancellationToken);
}