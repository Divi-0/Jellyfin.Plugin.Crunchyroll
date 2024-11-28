using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Episodes.Dtos;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Episodes;

public interface ICrunchyrollEpisodesClient
{
    public Task<Result<CrunchyrollEpisodesResponse>> GetEpisodesAsync(string seasonId, CultureInfo language, 
        CancellationToken cancellationToken);
    public Task<Result<CrunchyrollEpisodeDataItem>> GetEpisodeAsync(string episodeId, CultureInfo language, 
        CancellationToken cancellationToken);
}