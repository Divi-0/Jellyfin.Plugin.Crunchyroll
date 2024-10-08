using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Login;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.SearchTitleId.Client;
using Mediator;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.SearchTitleId;

public record TitleIdQuery(string Title) : IRequest<Result<SearchResponse?>>;

public class TitleIdQueryHandler : IRequestHandler<TitleIdQuery, Result<SearchResponse?>>
{
    private readonly ICrunchyrollTitleIdClient _client;
    private readonly ILoginService _loginService;

    public TitleIdQueryHandler(ICrunchyrollTitleIdClient client, ILoginService loginService)
    {
        _client = client;
        _loginService = loginService;
    }

    public async ValueTask<Result<SearchResponse?>> Handle(TitleIdQuery request, CancellationToken cancellationToken)
    {
        var loginResult = await _loginService.LoginAnonymously(cancellationToken);

        if (loginResult.IsFailed)
        {
            return loginResult;
        }
        
        var titleIdResult = await _client.GetTitleIdAsync(request.Title, cancellationToken);

        if (titleIdResult.IsFailed)
        {
            return Result.Fail(titleIdResult.Errors);
        }

        return titleIdResult.Value;
    }
}