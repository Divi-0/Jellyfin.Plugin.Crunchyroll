using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.ExternalComments.Configuration;
using Jellyfin.Plugin.ExternalComments.Features.WaybackMachine.Client;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Reviews.ExtractReviews;

public record ExtractReviewsCommand() : IRequest<Result>
{
    public required string TitleId { get; init; }
    public required string SlugTitle { get; init; }
}

public class ExtractReviewsCommandHandler : IRequestHandler<ExtractReviewsCommand, Result>
{
    private readonly IWaybackMachineClient _waybackMachineClient;
    private readonly PluginConfiguration _config;
    private readonly IHtmlReviewsExtractor _htmlReviewsExtractor;
    private readonly IAddReviewsSession _addReviewsSession;
    private readonly IGetReviewsSession _getReviewsSession;
    private readonly ILogger<ExtractReviewsCommandHandler> _logger;

    private readonly DateTime _dateWhenReviewsWereDeleted = new DateTime(2024, 07, 10);
    
    public ExtractReviewsCommandHandler(IWaybackMachineClient waybackMachineClient, PluginConfiguration config, 
        IHtmlReviewsExtractor htmlReviewsExtractor, IAddReviewsSession addReviewsSession, IGetReviewsSession getReviewsSession,
        ILogger<ExtractReviewsCommandHandler> logger)
    {
        _waybackMachineClient = waybackMachineClient;
        _config = config;
        _htmlReviewsExtractor = htmlReviewsExtractor;
        _addReviewsSession = addReviewsSession;
        _getReviewsSession = getReviewsSession;
        _logger = logger;
    }
    
    public async ValueTask<Result> Handle(ExtractReviewsCommand request, CancellationToken cancellationToken)
    {
        var hasReviewsResult = await HasTitleAnyReviews(request.TitleId);

        if (hasReviewsResult.IsFailed)
        {
            return hasReviewsResult.ToResult();
        }

        var hasTitleAnyReviews = hasReviewsResult.Value;

        if (hasTitleAnyReviews)
        {
            _logger.LogDebug("Title with id {TitleId} already has reviews", request.TitleId);
            return Result.Fail(ExtractReviewsErrorCodes.TitleAlreadyHasReviews);
        }
        
        var crunchyrollUrl = Path.Combine(
                _config.CrunchyrollUrl.Contains("www") ? _config.CrunchyrollUrl.Split("www.")[1] : _config.CrunchyrollUrl.Split("//")[1], 
            new CultureInfo(_config.CrunchyrollLanguage).TwoLetterISOLanguageName,
            "series",
            request.TitleId,
            request.SlugTitle)
            .Replace('\\', '/');
        
        var searchResult = await _waybackMachineClient.SearchAsync(crunchyrollUrl, _dateWhenReviewsWereDeleted, cancellationToken);

        if (searchResult.IsFailed)
        {
            return searchResult.ToResult();
        }

        var snapshotUrl = Path.Combine(
                _config.ArchiveOrgUrl,
                "web",
                searchResult.Value.Timestamp.ToString("yyyyMMddHHmmss"),
                crunchyrollUrl)
            .Replace('\\', '/');

        var reviewsResult = await _htmlReviewsExtractor.GetReviewsAsync(snapshotUrl, cancellationToken);

        if (reviewsResult.IsFailed)
        {
            return reviewsResult.ToResult();
        }
        
        await _addReviewsSession.AddReviewsForTitleIdAsync(request.TitleId, reviewsResult.Value);

        return Result.Ok();
    }

    private async ValueTask<Result<bool>> HasTitleAnyReviews(string titleId)
    {
        var result = await _getReviewsSession.GetReviewsForTitleIdAsync(titleId);

        if (result.IsFailed)
        {
            return result.ToResult();
        }
        
        return result.Value?.Any() ?? false;
    }
}