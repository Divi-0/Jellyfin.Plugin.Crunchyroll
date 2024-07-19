using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using HtmlAgilityPack;
using Jellyfin.Plugin.ExternalComments.Configuration;
using Jellyfin.Plugin.ExternalComments.Features.WaybackMachine.Client;
using Mediator;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.ExtractReviews;

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

    public ExtractReviewsCommandHandler(IWaybackMachineClient waybackMachineClient, PluginConfiguration config, 
        IHtmlReviewsExtractor htmlReviewsExtractor, IAddReviewsSession addReviewsSession)
    {
        _waybackMachineClient = waybackMachineClient;
        _config = config;
        _htmlReviewsExtractor = htmlReviewsExtractor;
        _addReviewsSession = addReviewsSession;
    }
    
    public async ValueTask<Result> Handle(ExtractReviewsCommand request, CancellationToken cancellationToken)
    {
        string url = Path.Combine(
                _config.CrunchyrollUrl.Contains("www") ? _config.CrunchyrollUrl.Split("www.")[1] : _config.CrunchyrollUrl.Split("//")[1], 
            new CultureInfo(_config.CrunchyrollLanguage).TwoLetterISOLanguageName,
            "series",
            request.TitleId,
            request.SlugTitle)
            .Replace('\\', '/');
        
        //Date before comments have been disabled
        var availabilityResult = await _waybackMachineClient.GetAvailabilityAsync(url, new DateTime(2024, 07, 07), cancellationToken);

        if (availabilityResult.IsFailed)
        {
            return availabilityResult.ToResult();
        }
        
        var availability = availabilityResult.Value;

        var reviewsResult = await _htmlReviewsExtractor.GetReviewsAsync(availability.ArchivedSnapshots.Closest.Url, cancellationToken);

        if (reviewsResult.IsFailed)
        {
            return reviewsResult.ToResult();
        }
        
        await _addReviewsSession.AddReviewsForTitleIdAsync(request.TitleId, reviewsResult.Value);

        return Result.Ok();
    }
}