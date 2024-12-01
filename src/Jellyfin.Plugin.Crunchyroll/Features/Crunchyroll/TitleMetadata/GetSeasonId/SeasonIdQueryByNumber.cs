using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Mediator;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.GetSeasonId;

/// <param name="SeasonNumber">eg. 1,2,3</param>
/// <param name="DuplicateCounter">When multiple seasons have the same season number this param identifies which duplicate season to choose;
/// 1 = take the first season you find, 2 = take the second the season of the seasons with identical season-numbers, ...</param>
public record SeasonIdQueryByNumber(string TitleId, int SeasonNumber, int DuplicateCounter, CultureInfo Language) : IRequest<Result<string?>>;

public class SeasonIdQueryByNumberHandler : IRequestHandler<SeasonIdQueryByNumber, Result<string?>>
{
    private readonly IGetSeasonRepository _repository;

    public SeasonIdQueryByNumberHandler(IGetSeasonRepository repository)
    {
        _repository = repository;
    }
    
    public async ValueTask<Result<string?>> Handle(SeasonIdQueryByNumber request, CancellationToken cancellationToken)
    {
        var seasonIdResult = await _repository
            .GetSeasonIdByNumberAsync(request.TitleId, request.SeasonNumber, request.DuplicateCounter, request.Language,
                cancellationToken);

        if (seasonIdResult.IsFailed)
        {
            return seasonIdResult;
        }
        
        var seasonId = seasonIdResult.Value;

        if (!string.IsNullOrWhiteSpace(seasonId))
        {
            return seasonId;
        }

        var seasonsResult = await _repository.GetAllSeasonsAsync(request.TitleId, request.Language,
            cancellationToken);

        foreach (var season in seasonsResult.Value)
        {
            var regex = new Regex($@"S{request.SeasonNumber}(DC)?$");
            var match = regex.Match(season.Identifier);

            if (match.Success)
            {
                return season.CrunchyrollId;
            }
        }

        return Result.Ok<string?>(null);
    }
}