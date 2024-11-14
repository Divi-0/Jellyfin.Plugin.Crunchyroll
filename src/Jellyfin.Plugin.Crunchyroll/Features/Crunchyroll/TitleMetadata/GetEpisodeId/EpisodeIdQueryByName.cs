using System;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.GetEpisodeId;

public record EpisodeIdQueryByName(string TitleId, string SeasonId, string EpisodeName) : IRequest<Result<EpisodeIdResult?>>;

public class EpisodeIdQueryByNameHandler : IRequestHandler<EpisodeIdQueryByName, Result<EpisodeIdResult?>>
{
    private readonly IGetEpisodeSession _getEpisodeSession;
    private readonly ILogger<EpisodeIdQueryHandler> _logger;

    public EpisodeIdQueryByNameHandler(IGetEpisodeSession getEpisodeSession, ILogger<EpisodeIdQueryHandler> logger)
    {
        _getEpisodeSession = getEpisodeSession;
        _logger = logger;
    }
    
    public async ValueTask<Result<EpisodeIdResult?>> Handle(EpisodeIdQueryByName request, CancellationToken cancellationToken)
    {
        try
        {
            return await _getEpisodeSession.GetEpisodeIdByNameAsync(request.TitleId, request.SeasonId,
                request.EpisodeName);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occured while getting episode Id");
            return Result.Fail(EpisodeIdQueryErrorCodes.Internal);
        }
    }
}

