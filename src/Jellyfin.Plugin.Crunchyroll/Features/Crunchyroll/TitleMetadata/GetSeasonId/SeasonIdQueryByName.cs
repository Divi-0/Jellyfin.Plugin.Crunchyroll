using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Mediator;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.GetSeasonId;

public record SeasonIdQueryByName(string TitleId, string SeasonName, CultureInfo Language) : IRequest<Result<string?>>;

public class SeasonIdQueryByNameHandler : IRequestHandler<SeasonIdQueryByName, Result<string?>>
{
    private readonly IGetSeasonRepository _repository;

    public SeasonIdQueryByNameHandler(IGetSeasonRepository repository)
    {
        _repository = repository;
    }
    
    public async ValueTask<Result<string?>> Handle(SeasonIdQueryByName request, CancellationToken cancellationToken)
    {
        return await _repository.GetSeasonIdByNameAsync(request.TitleId, request.SeasonName, request.Language,
            cancellationToken);
    }
}