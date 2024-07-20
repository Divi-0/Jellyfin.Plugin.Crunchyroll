using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using HtmlAgilityPack;
using Jellyfin.Plugin.ExternalComments.Configuration;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.GetReviews;
using Jellyfin.Plugin.ExternalComments.Features.WaybackMachine.Client;
using Jellyfin.Plugin.ExternalComments.Features.WaybackMachine.Client.Dto;
using Mediator;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

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

    private readonly DateTime _dateWhenReviewsWereDeleted = new DateTime(2024, 07, 10);
    
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
        
        var availabilityResult = await GetAvailabilityAsync(url, cancellationToken);

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

    private async Task<Result<AvailabilityResponse>> GetAvailabilityAsync(string url, CancellationToken cancellationToken)
    {
        //2024 07 01 because in july they disabled reviews and comments
        var timestamp = new DateTime(2024, 07, 01);
        
        var adjustTimestampPredicate = new PredicateBuilder<Result<AvailabilityResponse>>().HandleResult(result =>
        {
            if (result.IsFailed)
            {
                return false;
            }
            
            var resultTimeStamp = DateTime.ParseExact(result.Value.ArchivedSnapshots.Closest!.Timestamp, 
                "yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            
            if (resultTimeStamp >= _dateWhenReviewsWereDeleted)
            {
                timestamp = timestamp.AddMonths(-1);
                return true;
            }

            return false;
        });
        
        var pipeline = new ResiliencePipelineBuilder<Result<AvailabilityResponse>>()
            .AddRetry(new RetryStrategyOptions<Result<AvailabilityResponse>>()
            {
                ShouldHandle = adjustTimestampPredicate,
                MaxRetryAttempts = 12
            })
            .Build();
        
        return await pipeline.ExecuteAsync<Result<AvailabilityResponse>>(async (cancelToken) => 
            await _waybackMachineClient.GetAvailabilityAsync(url, timestamp, cancelToken), cancellationToken);
    }
}