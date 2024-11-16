using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Mediator;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.GetSeasonId;

/// <param name="SeasonNumber">eg. 1,2,3</param>
/// <param name="DuplicateCounter">When multiple seasons have the same season number this param identifies which duplicate season to choose;
/// 1 = take the first season you find, 2 = take the second the season of the seasons with identical season-numbers, ...</param>
public record SeasonIdQueryByNumber(string TitleId, int SeasonNumber, int DuplicateCounter) : IRequest<Result<string?>>;

public class SeasonIdQueryByNumberHandler : IRequestHandler<SeasonIdQueryByNumber, Result<string?>>
{
    private readonly IGetSeasonSession _getSeasonSession;

    public SeasonIdQueryByNumberHandler(IGetSeasonSession getSeasonSession)
    {
        _getSeasonSession = getSeasonSession;
    }
    
    public async ValueTask<Result<string?>> Handle(SeasonIdQueryByNumber request, CancellationToken cancellationToken)
    {
        var seasonId = await _getSeasonSession
            .GetSeasonIdByNumberAsync(request.TitleId, request.SeasonNumber, request.DuplicateCounter);

        if (!string.IsNullOrWhiteSpace(seasonId))
        {
            return seasonId;
        }

        var seasons = await _getSeasonSession.GetAllSeasonsAsync(request.TitleId);

        foreach (var season in seasons)
        {
            var regex = new Regex($@"S{request.SeasonNumber}(DC)?$");
            var match = regex.Match(season.Identifier);

            if (match.Success)
            {
                return season.Id;
            }
        }

        return Result.Ok<string?>(null);
    }
}