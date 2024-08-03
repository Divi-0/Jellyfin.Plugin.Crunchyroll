using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.ExternalComments.Contracts.Comments;
using Jellyfin.Plugin.ExternalComments.Domain.Constants;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Comments.GetComments.Client;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Login;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Comments.GetComments;

public record GetCommentsQuery(string Id, int PageNumber, int PageSize) : IRequest<Result<CommentsResponse>>;

public class GetCommentsQueryHandler : IRequestHandler<GetCommentsQuery, Result<CommentsResponse>>
{
    private readonly ILogger<GetCommentsQueryHandler> _logger;
    private readonly ICrunchyrollGetCommentsClient _crunchyrollClient;
    private readonly ILibraryManager _libraryManager;
    private readonly ILoginService _loginService;

    public GetCommentsQueryHandler(ILogger<GetCommentsQueryHandler> logger, ICrunchyrollGetCommentsClient crunchyrollClient,
        ILibraryManager libraryManager, ILoginService loginService)
    {
        _logger = logger;
        _crunchyrollClient = crunchyrollClient;
        _libraryManager = libraryManager;
        _loginService = loginService;
    }

    public async ValueTask<Result<CommentsResponse>> Handle(GetCommentsQuery request, CancellationToken cancellationToken)
    {
        var queryResult = _libraryManager.GetItemsResult(new InternalItemsQuery()
        {
            AncestorWithPresentationUniqueKey = request.Id
        });

        var item = queryResult.Items.FirstOrDefault(x => x.DisplayParent is Series);

        if (item is null)
        {
            return Result.Fail(ErrorCodes.ItemNotFound);
        }

        if (!item.DisplayParent.ProviderIds.TryGetValue(CrunchyrollExternalKeys.Id, out string? cruncyrollId))
        {
            return Result.Fail(ErrorCodes.ProviderIdNotSet);
        }

        var loginResult = await _loginService.LoginAnonymously(cancellationToken);

        if (loginResult.IsFailed)
        {
            return loginResult;
        }

        var commentsResult = await _crunchyrollClient.GetCommentsAsync(cruncyrollId!, request.PageNumber,
            request.PageSize, cancellationToken);

        if (commentsResult.IsFailed)
        {
            return Result.Fail(commentsResult.Errors);
        }

        return commentsResult;
    }
}