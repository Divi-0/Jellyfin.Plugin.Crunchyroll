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

    public async Task<Result<IReadOnlyList<CommentItem>>> GetCommentsAsync(string url,
        CancellationToken cancellationToken)
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
            return Result.Fail(ExtractCommentsErrorCodes.HtmlUrlRequestFailed);
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Get request for url {Url} failed with statuscode {StatusCode}", url, response.StatusCode);
            return Result.Fail(ExtractCommentsErrorCodes.HtmlUrlRequestFailed);
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        var htmlDocument = new HtmlDocument();
        htmlDocument.LoadHtml(content);

        var commentsParentElement = htmlDocument.DocumentNode.SelectSingleNode("//div[@class='erc-comments']");

        if (commentsParentElement == null)
        {
            return Result.Fail(ExtractCommentsErrorCodes.HtmlExtractorInvalidCrunchyrollCommentsPage);
        }
        

        var comments = new List<CommentItem>();

        var commentElements = commentsParentElement.SelectNodes(".//div[contains(@class, 'comment--')]");
        foreach (var commentElement in commentElements)
        {
            var spoilerElement = commentElement.SelectSingleNode(".//div[contains(@class, 'comment-spoiler')]");
            if (spoilerElement is not null)
            {
                //ignore item, the message is not readable
                continue;
            }
            
            var imageUrl = commentElement.SelectSingleNode(".//img")!.GetAttributeValue("src", string.Empty);
            var username = commentElement.SelectSingleNode(".//h5")!.GetDirectInnerText();
            var body = commentElement.SelectSingleNode(".//p")!.GetDirectInnerText();

            var createdAtString = commentElement.SelectSingleNode(".//div[contains(@class, 'comment__signature')]")
                .ChildNodes[1].GetDirectInnerText();

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
                    .GetDirectInnerText();
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

    private int ConvertLikesToInt(string likes)
    {
        if (int.TryParse(likes, out var result))
        {
            return result;
        }
        
        var match = NumberAndUnitRegex().Match(likes);
        
        if (!match.Success)
        {
            throw new NotImplementedException("likes value is not implemented");
        }
        
        var number = match.Groups[1].Value;
        var unit = match.Groups[2].Value;

        if (!unit.Equals("k", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotImplementedException($"value '{unit}' is not implemented");
        }

        var decimalValue = Convert.ToDecimal(number, new CultureInfo(_config.CrunchyrollLanguage));
        return Convert.ToInt32(Math.Round(decimalValue * 1000));
    }

    [GeneratedRegex("([0-9]*(?:\\.[0-9]*)?)(.)")]
    private static partial Regex NumberAndUnitRegex();
}