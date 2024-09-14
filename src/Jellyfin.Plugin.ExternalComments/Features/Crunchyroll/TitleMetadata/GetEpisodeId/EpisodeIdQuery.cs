using System.Threading;
using System.Threading.Tasks;
using Mediator;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.GetEpisodeId;

public record EpisodeIdQuery(string TitleId, string SeasonId, string EpisodeIdentifier) : IRequest<string?>;

public class EpisodeIdQueryHandler : IRequestHandler<EpisodeIdQuery, string?>
{
    private readonly IGetEpisodeSession _getEpisodeSession;

    public EpisodeIdQueryHandler(IGetEpisodeSession getEpisodeSession)
    {
        _getEpisodeSession = getEpisodeSession;
    }
    
    public async ValueTask<string?> Handle(EpisodeIdQuery request, CancellationToken cancellationToken)
    {
        return await _getEpisodeSession.GetEpisodeIdAsync(request.TitleId, request.SeasonId, request.EpisodeIdentifier);
    }
}

