using System;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Contracts.Comments;
using Jellyfin.Plugin.Crunchyroll.Domain.Constants;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Avatar;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Comments.GetComments.Client;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Login;
using MediaBrowser.Controller.Library;
using Mediator;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Comments.GetComments;

public record GetCommentsQuery(string Id, int PageNumber, int PageSize) : IRequest<Result<CommentsResponse>>;

public class GetCommentsQueryHandler : IRequestHandler<GetCommentsQuery, Result<CommentsResponse>>
{
    private readonly ICrunchyrollGetCommentsClient _crunchyrollClient;
    private readonly ILibraryManager _libraryManager;
    private readonly ILoginService _loginService;
    private readonly PluginConfiguration _config;
    private readonly IGetCommentsSession _session;

    public GetCommentsQueryHandler(ICrunchyrollGetCommentsClient crunchyrollClient,
        ILibraryManager libraryManager, ILoginService loginService, PluginConfiguration config,
        IGetCommentsSession session)
    {
        _crunchyrollClient = crunchyrollClient;
        _libraryManager = libraryManager;
        _loginService = loginService;
        _config = config;
        _session = session;
    }

    public async ValueTask<Result<CommentsResponse>> Handle(GetCommentsQuery request, CancellationToken cancellationToken)
    {
        var item = _libraryManager.RetrieveItem(Guid.Parse(request.Id));

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (item is null)
        {
            return Result.Fail(ErrorCodes.ItemNotFound);
        }

        if (!item.ProviderIds.TryGetValue(CrunchyrollExternalKeys.EpisodeId, out var episodeId) ||
            string.IsNullOrWhiteSpace(episodeId))
        {
            return Result.Fail(ErrorCodes.ProviderIdNotSet);
        }

        Result<CommentsResponse> commentsResult;
        if (_config.IsWaybackMachineEnabled)
        {
            commentsResult = await GetCommentsFromDatabase(episodeId, request.PageSize, request.PageNumber);
        }
        else
        {
            commentsResult = await GetCommentsFromApi(episodeId, request.PageSize, request.PageNumber, 
                cancellationToken);
        }

        return commentsResult;
    }
    
    private async ValueTask<Result<CommentsResponse>> GetCommentsFromApi(string episodeId, int pageSize, int pageNumber, 
        CancellationToken cancellationToken)
    {
        var loginResult = await _loginService.LoginAnonymously(cancellationToken);

        if (loginResult.IsFailed)
        {
            return loginResult;
        }

        return await _crunchyrollClient.GetCommentsAsync(episodeId, pageNumber,
            pageSize, cancellationToken);
    }
    
    private async ValueTask<Result<CommentsResponse>> GetCommentsFromDatabase(string episodeId, int pageSize, int pageNumber)
    {
        var comments = await _session.GetCommentsAsync(episodeId, pageSize, pageNumber);
        
        foreach (var commentItem in comments)
        {
            if (string.IsNullOrWhiteSpace(commentItem.AvatarIconUri))
            {
                continue;
            }
            
            commentItem.AvatarIconUri = $"/{Routes.Root}/{AvatarConstants.GetAvatarSubRoute}/{UrlEncoder.Default.Encode(commentItem.AvatarIconUri)}";
        }
        
        return new CommentsResponse
        {
            Comments = comments,
            Total = comments.Count
        };
    }
}