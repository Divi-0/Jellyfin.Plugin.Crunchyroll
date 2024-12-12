using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.ScrapEpisodeMetadata.Client.Dtos;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.ScrapEpisodeMetadata.Client;

public interface IScrapEpisodeCrunchyrollClient
{
    public Task<Result<CrunchyrollEpisodesResponse>> GetEpisodesAsync(string seasonId, CultureInfo language, 
        CancellationToken cancellationToken);
    public Task<Result<CrunchyrollEpisodeDataItem>> GetEpisodeAsync(string episodeId, CultureInfo language, 
        CancellationToken cancellationToken);
}