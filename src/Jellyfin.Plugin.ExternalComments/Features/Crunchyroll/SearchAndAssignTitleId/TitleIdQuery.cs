using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.SearchAndAssignTitleId.Client;
using Mediator;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.SearchAndAssignTitleId;

public record TitleIdQuery(string Title) : IRequiresCrunchyrollLogin, IRequest<Result<SearchResponse?>>;

public class TitleIdQueryHandler : IRequestHandler<TitleIdQuery, Result<SearchResponse?>>
{
    private readonly ICrunchyrollTitleIdClient _client;

    public TitleIdQueryHandler(ICrunchyrollTitleIdClient client)
    {
        _client = client;
    }

    public async ValueTask<Result<SearchResponse?>> Handle(TitleIdQuery request, CancellationToken cancellationToken)
    {
        var titleIdResult = await _client.GetTitleIdAsync(request.Title, cancellationToken);

        if (titleIdResult.IsFailed)
        {
            return Result.Fail(titleIdResult.Errors);
        }

        return titleIdResult.Value;
    }
}