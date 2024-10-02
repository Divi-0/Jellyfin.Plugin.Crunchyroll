using System;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.GetEpisodeId;

public record EpisodeIdQuery(string TitleId, string SeasonId, string EpisodeIdentifier) : IRequest<Result<EpisodeIdResult?>>;

public class EpisodeIdQueryHandler : IRequestHandler<EpisodeIdQuery, Result<EpisodeIdResult?>>
{
    private readonly IGetEpisodeSession _getEpisodeSession;
    private readonly ILogger<EpisodeIdQueryHandler> _logger;

    public EpisodeIdQueryHandler(IGetEpisodeSession getEpisodeSession, ILogger<EpisodeIdQueryHandler> logger)
    {
        _getEpisodeSession = getEpisodeSession;
        _logger = logger;
    }
    
    public async ValueTask<Result<EpisodeIdResult?>> Handle(EpisodeIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            return await _getEpisodeSession.GetEpisodeIdAsync(request.TitleId, request.SeasonId,
                request.EpisodeIdentifier);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occured while getting episode Id");
            return Result.Fail(EpisodeIdQueryErrorCodes.Internal);
        }
    }
}

