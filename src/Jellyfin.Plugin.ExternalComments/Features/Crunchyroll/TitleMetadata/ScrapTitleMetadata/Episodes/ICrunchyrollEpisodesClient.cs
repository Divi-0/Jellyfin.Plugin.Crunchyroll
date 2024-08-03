using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Episodes.Dtos;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Episodes;

public interface ICrunchyrollEpisodesClient
{
    public Task<Result<CrunchyrollEpisodesResponse>> GetEpisodesAsync(string seasonId, CancellationToken cancellationToken);
}