using System.Globalization;
using System.Text.RegularExpressions;
using FluentAssertions;
using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.GetMetadata.ScrapSeriesMetadata.Client.Dtos;
using Jellyfin.Plugin.Crunchyroll.Tests.E2E.Common.Helpers;
using Microsoft.Playwright;

namespace Jellyfin.Plugin.Crunchyroll.Tests.E2E.Pages;

public static class SeriesPage
{
    /// <returns>Series details page</returns>
    public static async Task<IPage> SeriesShouldHaveDataFromCrunchyrollAsync(this IPage page, IBrowser browser, string jellyfinUrl, 
        string seriesCrunchyrollId, CultureInfo language, int expectedReviewsCount)
    {
        var seriesClient = await CrunchyrollClientHelper.GetSeriesClientAsync();
        var seriesMetadataResult = await seriesClient.GetSeriesMetadataAsync(seriesCrunchyrollId, language, CancellationToken.None);
        seriesMetadataResult.IsSuccess.Should().BeTrue();
        var seriesMetadata = seriesMetadataResult.Value;
        
        await page.GoToSeriesPageAsync(jellyfinUrl, seriesMetadata.Title);
        await page.SeriesShouldHaveMetadataAsync(seriesMetadata);
        await page.SeriesShouldHaveReviewsAsync(expectedReviewsCount);
        
        await page.ShouldHaveSeasonsWithMetadataAsync(seriesCrunchyrollId, browser);

        return page;
    }
    
    public static async Task GoToSeriesPageAsync(this IPage page, string jellyfinUrl, string seriesTitle)
    {
        //Go to collection page
        await page.GotoAsync($"{jellyfinUrl}web/#/home.html");
        var animeCollectionDiv = page.Locator("div[data-type='CollectionFolder']").First;
        await animeCollectionDiv.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        var animeCollectionHref = animeCollectionDiv.Locator("a[title='Anime']");
        await animeCollectionHref.ClickAsync();
        
        //click on series to go to series details page
        await page.WaitForURLAsync(new Regex(@"web/#/list\.html"));
        var queryString = new Uri(page.Url.Replace("/#", string.Empty), UriKind.Absolute).Query;
        var queryDictionary = System.Web.HttpUtility.ParseQueryString(queryString);
        var itemsContainer = page.Locator($"div[data-parentId='{queryDictionary["parentId"]}']");
        await itemsContainer.Locator($"a[title='{seriesTitle}']").First.ClickAsync();
        await page.WaitForURLAsync(new Regex("details"));
    }
    
    private static async Task SeriesShouldHaveMetadataAsync(this IPage page, CrunchyrollSeriesContentItem seriesMetadata)
    {
        var nameContainer = page.Locator("div.nameContainer");
        var actualSeriesName = await nameContainer.Locator("bdi").InnerTextAsync();
        actualSeriesName.Should().Be(seriesMetadata.Title);
        
        var studioElement = page.Locator("div.studios");
        var studioText = await studioElement.Locator("a").InnerTextAsync();
        studioText.Should().Be(seriesMetadata.ContentProvider);
        
        var description = page.Locator("p.detail-clamp-text");
        foreach (var pElement in await description.Locator("p").AllAsync())
        {
            var descriptionPartText = await pElement.InnerTextAsync();
            seriesMetadata.Description.Should().Contain(descriptionPartText);
        }
    }
    
    private static async Task SeriesShouldHaveReviewsAsync(this IPage page, int expectedReviewsCount)
    {
        var crunchyrollReviewsWrapperDiv = page.Locator("div#crunchyroll-reviews-wrapper");
        await crunchyrollReviewsWrapperDiv.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        
        var crunchyrollReviewsDiv = crunchyrollReviewsWrapperDiv.Locator("div#crunchyroll-reviews");
        var crunchyrollReviewElements = crunchyrollReviewsDiv.Locator("div.crunchyroll-review");
        var crunchyrollReviewsCount = await crunchyrollReviewElements.CountAsync();
        crunchyrollReviewsCount.Should().Be(expectedReviewsCount);

        foreach (var reviewDiv in await crunchyrollReviewElements.AllAsync())
        {
            //avatar img
            var avatarImgElement = reviewDiv.Locator("img");
            var isAvatarImgVisible = await avatarImgElement.IsVisibleAsync();
            isAvatarImgVisible.Should().BeTrue();
            
            //username
            var usernameElement = reviewDiv.Locator("h5");
            var isUsernameVisible = await usernameElement.IsVisibleAsync();
            isUsernameVisible.Should().BeTrue();
            var usernameText = await usernameElement.InnerTextAsync();
            usernameText.Should().NotBeEmpty();
            
            //stars
            var starsDiv = reviewDiv.Locator("div.stars");
            var isStarDivVisible = await starsDiv.IsVisibleAsync();
            isStarDivVisible.Should().BeTrue();
            var starsCount = await starsDiv.Locator("svg").CountAsync();
            starsCount.Should().Be(5);
            
            //title
            var titleElement = reviewDiv.Locator("h3");
            var isTitleVisible = await titleElement.IsVisibleAsync();
            isTitleVisible.Should().BeTrue();
            var titleText = await titleElement.InnerTextAsync();
            titleText.Should().NotBeEmpty();
            
            //body
            var bodyElement = reviewDiv.Locator("p");
            var isBodyVisible = await bodyElement.IsVisibleAsync();
            isBodyVisible.Should().BeTrue();
            var bodyText = await bodyElement.InnerTextAsync();
            bodyText.Should().NotBeEmpty();
            
            //rating
            var ratingElement = reviewDiv.Locator("div.rating-section");
            var isRatingVisible = await ratingElement.IsVisibleAsync();
            isRatingVisible.Should().BeTrue();
            var ratingText = await ratingElement.Locator("span").InnerTextAsync();
            ratingText.Should().NotBeEmpty();
        }
    }
    
    
}