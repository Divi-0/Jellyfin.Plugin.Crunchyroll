using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Mediator;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.GetSeasonId;

public record SeasonIdQueryByName(string TitleId, string SeasonName) : IRequest<Result<string?>>;

public class SeasonIdQueryByNameHandler : IRequestHandler<SeasonIdQueryByName, Result<string?>>
{
    private readonly IGetSeasonSession _getSeasonSession;

    public SeasonIdQueryByNameHandler(IGetSeasonSession getSeasonSession)
    {
        _getSeasonSession = getSeasonSession;
    }
    
    public async ValueTask<Result<string?>> Handle(SeasonIdQueryByName request, CancellationToken cancellationToken)
    {
        return await _getSeasonSession.GetSeasonIdByNameAsync(request.TitleId, request.SeasonName);
    }
}