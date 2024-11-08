using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using HtmlAgilityPack;
using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Contracts.Reviews;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews.ExtractReviews;

public partial class HtmlReviewsExtractor : IHtmlReviewsExtractor
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HtmlReviewsExtractor> _logger;
    private readonly PluginConfiguration _config;

    public HtmlReviewsExtractor(HttpClient httpClient, ILogger<HtmlReviewsExtractor> logger, 
        PluginConfiguration config)
    {
        _httpClient = httpClient;
        _logger = logger;
        _config = config;
    }

    public async Task<Result<IReadOnlyList<ReviewItem>>> GetReviewsAsync(string url, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await WaybackMachineRequestResiliencePipeline
                .Get(_logger)
                .ExecuteAsync(
                    async _ => await _httpClient.GetAsync(url, cancellationToken), 
                    cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Get request for url {Url}", url);
            return Result.Fail(ExtractReviewsErrorCodes.HtmlUrlRequestFailed);
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Get request for url {Url} failed with statuscode {StatusCode}", url, response.StatusCode);
            return Result.Fail(ExtractReviewsErrorCodes.HtmlUrlRequestFailed);
        }
    
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        var htmlDocument = new HtmlDocument();
        htmlDocument.LoadHtml(content);
    
        var reviewParentElement = htmlDocument.DocumentNode.SelectSingleNode("//div[@class='erc-reviews']");

        if (reviewParentElement == null)
        {
            return Result.Fail(ExtractReviewsErrorCodes.HtmlExtractorInvalidCrunchyrollReviewsPage);
        }

        var reviewElements = reviewParentElement.ChildNodes.Where(x => 
            x.GetClasses()
                .Any(y => y.Contains("review")));
    
        var reviewItems = new List<ReviewItem>();
    
        foreach (var reviewElement in reviewElements)
        {
            var imageUrl = reviewElement.SelectSingleNode(".//img")?.GetAttributeValue("src", string.Empty);
            var username = reviewElement.SelectSingleNode(".//h5")?.GetDirectInnerText();
            var title = reviewElement.SelectSingleNode(".//h3")?.GetDirectInnerText();
            var body = reviewElement.SelectSingleNode(".//p")?.GetDirectInnerText();
        
            var createdAtString = reviewElement.SelectSingleNode(".//div[contains(@class, 'review__meta')]")
                .ChildNodes[2].GetDirectInnerText();
        
            var starRatingParentElement = reviewElement.SelectSingleNode(".//div[contains(@class, 'star-rating-controls')]");
            var svgs = starRatingParentElement.SelectNodes(".//svg");
            var autorRating = svgs.Count(x => x.SelectSingleNode(".//path").GetAttributeValue("d", string.Empty) == FullStarSvg);

            var votingSectionElement = reviewElement.SelectSingleNode(".//div[contains(@class, 'review-votings__votes-section')]");
            var votingContent = votingSectionElement.SelectSingleNode(".//span").GetDirectInnerText();
        
            var votingNumbers = DigitsRegex().Matches(votingContent);

            var createdAt = TryParseDate(createdAtString);
            
            var item = new ReviewItem()
            {
                Author = new ReviewItemAuthor()
                {
                    Username = username!,
                    AvatarUri = imageUrl!,
                },
                AuthorRating = autorRating,
                Title = title!,
                Body = body!,
                Rating = new ReviewItemRating()
                {
                    Likes = Convert.ToInt32(votingNumbers[0].Value),
                    Dislikes = Convert.ToInt32(votingNumbers[1].Value) - Convert.ToInt32(votingNumbers[0].Value),
                    Total = Convert.ToInt32(votingNumbers[1].Value)
                },
                CreatedAt = createdAt
            };
        
            reviewItems.Add(item);
        }
    
        return reviewItems;
    }

    private DateTime TryParseDate(string dateString)
    {
        var hasParsed = DateTime.TryParse(dateString, new CultureInfo(_config.CrunchyrollLanguage), out var date);
        return hasParsed ? date : default;
    }

    [GeneratedRegex(@"\d+")]
    private static partial Regex DigitsRegex();

    private const string FullStarSvg = "M15.266 8.352L11.988 1.723 8.73 8.352 1.431 9.397 6.71 14.528 5.465 21.849 11.999 18.39 18.544 21.85 17.285 14.528 22.57 9.398z";
}