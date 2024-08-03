using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.ScrapTitleMetadata.Seasons.Dtos;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.ScrapTitleMetadata.Seasons;

public interface ICrunchyrollSeasonsClient
{
    public Task<Result<CrunchyrollSeasonsResponse>> GetSeasonsAsync(string titleId, CancellationToken cancellationToken);
}