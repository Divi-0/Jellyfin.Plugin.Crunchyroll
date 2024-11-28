using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Seasons.Dtos;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Seasons;

public interface ICrunchyrollSeasonsClient
{
    public Task<Result<CrunchyrollSeasonsResponse>> GetSeasonsAsync(string titleId, CultureInfo language, 
        CancellationToken cancellationToken);
}