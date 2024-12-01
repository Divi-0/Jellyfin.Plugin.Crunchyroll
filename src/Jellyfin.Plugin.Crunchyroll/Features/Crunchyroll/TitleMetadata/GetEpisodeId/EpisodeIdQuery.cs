using System;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.GetEpisodeId;

public record EpisodeIdQuery(string SeasonId, string EpisodeIdentifier) : IRequest<Result<EpisodeIdResult?>>;

public class EpisodeIdQueryHandler : IRequestHandler<EpisodeIdQuery, Result<EpisodeIdResult?>>
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<EpisodeIdQueryHandler> _logger;

    public EpisodeIdQueryHandler(IServiceScopeFactory serviceScopeFactory, ILogger<EpisodeIdQueryHandler> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }
    
    public async ValueTask<Result<EpisodeIdResult?>> Handle(EpisodeIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IGetEpisodeRepository>();
            return await repository.GetEpisodeIdAsync(request.SeasonId,
                request.EpisodeIdentifier, cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occured while getting episode Id");
            return Result.Fail(EpisodeIdQueryErrorCodes.Internal);
        }
    }
}

