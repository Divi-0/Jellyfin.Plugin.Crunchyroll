using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Season.ScrapSeasonMetadata.Client.Dtos;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Season.ScrapSeasonMetadata.Client;

public interface ICrunchyrollSeasonsClient
{
    public Task<Result<CrunchyrollSeasonsResponse>> GetSeasonsAsync(string titleId, CultureInfo language, 
        CancellationToken cancellationToken);
}