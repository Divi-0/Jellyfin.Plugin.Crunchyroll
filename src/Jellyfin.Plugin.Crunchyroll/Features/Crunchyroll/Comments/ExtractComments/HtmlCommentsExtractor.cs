using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using HtmlAgilityPack;
using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Contracts.Comments;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Comments.ExtractComments;

public partial class HtmlCommentsExtractor : IHtmlCommentsExtractor
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HtmlCommentsExtractor> _logger;
    private readonly PluginConfiguration _config;

    public HtmlCommentsExtractor(HttpClient httpClient, ILogger<HtmlCommentsExtractor> logger, PluginConfiguration config)
    {
        _httpClient = httpClient;
        _logger = logger;
        _config = config;
    }

    public async Task<Result<IReadOnlyList<CommentItem>>> GetCommentsAsync(string url, CultureInfo language,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await WaybackMachineRequestResiliencePipeline
                .Get(_logger, _config.WaybackMachineWaitTimeoutInSeconds)
                .ExecuteAsync(
                    async _ => await _httpClient.GetAsync(url, cancellationToken), 
                    cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Get request for url {Url}", url);
            return Result.Fail(ExtractCommentsErrorCodes.HtmlUrlRequestFailed);
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Get request for url {Url} failed with statuscode {StatusCode}", url, response.StatusCode);
            return Result.Fail(ExtractCommentsErrorCodes.HtmlUrlRequestFailed);
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return ExtractCommentsFromHtml(content, language);
    }

    private Result<IReadOnlyList<CommentItem>> ExtractCommentsFromHtml(string html, CultureInfo language)
    {
        var htmlDocument = new HtmlDocument();
        htmlDocument.LoadHtml(html);

        var commentsParentElement = htmlDocument.DocumentNode.SelectSingleNode("//div[@class='erc-comments']");

        if (commentsParentElement == null)
        {
            return Result.Fail(ExtractCommentsErrorCodes.HtmlExtractorInvalidCrunchyrollCommentsPage);
        }

        var comments = new List<CommentItem>();

        var commentElements = commentsParentElement.SelectNodes(".//div[contains(@class, 'comment--')]");
        if (commentElements is null)
        {
            return comments;
        }

        foreach (var commentElement in commentElements)
        {
            var spoilerElement = commentElement.SelectSingleNode(".//div[contains(@class, 'comment-spoiler')]");
            if (spoilerElement is not null)
            {
                //ignore item, the message is not readable
                continue;
            }
            
            var imageUrl = commentElement.SelectSingleNode(".//img")?.GetAttributeValue("src", string.Empty) ?? string.Empty;
            var username = commentElement.SelectSingleNode(".//h5")?.GetDirectInnerText();
            var body = commentElement.SelectSingleNode(".//p")?.GetDirectInnerText();
            if (string.IsNullOrWhiteSpace(username) || body is null)
            {
                continue;
            }

            var commentActionElement = commentElement.SelectSingleNode(".//ul[contains(@class, 'comment-actions--')]");

            var likes = string.Empty;
            if (commentActionElement is null)
            {
                var deletedBody = commentElement.SelectSingleNode(".//p[contains(@class, 'comment-deleted')]");
                if (deletedBody is not null)
                {
                    likes = "0";
                }
            }
            else
            {
                likes = commentActionElement.SelectSingleNode(".//button[@data-t='comment-like-btn']")
                    ?.GetDirectInnerText() ?? string.Empty;
            }

            var item = new CommentItem()
            {
                Author = username,
                Message = body,
                AvatarIconUri = imageUrl,
                Likes = ConvertLikesToInt(likes),
                RepliesCount = 0
            };

            comments.Add(item);
        }

        return comments;
    }

    private static int ConvertLikesToInt(string likes)
    {
        var trimmed = likes.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return 0;
        }

        if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        var match = NumberAndUnitRegex().Match(trimmed);
        if (!match.Success)
        {
            return 0;
        }

        var normalizedNumber = match.Groups[1].Value.Replace(',', '.');
        return decimal.TryParse(normalizedNumber, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var decimalValue)
            ? Convert.ToInt32(Math.Round(decimalValue * 1000m))
            : 0;
    }

    [GeneratedRegex(@"([0-9]+(?:[.,][0-9]+)?)\s*(mil|k)", RegexOptions.IgnoreCase)]
    private static partial Regex NumberAndUnitRegex();
}