using System.Globalization;
using FluentAssertions;
using Jellyfin.Plugin.Crunchyroll.Tests.E2E.Common.Helpers;
using Microsoft.Playwright;

namespace Jellyfin.Plugin.Crunchyroll.Tests.E2E.Pages;

public static class SeasonPage
{
    public static async Task ShouldHaveSeasonsWithMetadataAsync(this IPage seriesPage, string titleId,
        CultureInfo language, IBrowser browser)
    {
        var seasonsClient = await CrunchyrollClientHelper.GetSeasonsClientAsync();
        var seasonsResult = await seasonsClient.GetSeasonsAsync(titleId, language, CancellationToken.None);
        seasonsResult.IsSuccess.Should().BeTrue();
        var seasonsResponse = seasonsResult.Value;

        foreach (var season in seasonsResponse.Data)
        {
            var seasonsPage = await browser.NewPageAsync();
            await seasonsPage.GotoAsync(seriesPage.Url);
            await seasonsPage.LoginAsync();
            
            var nameContainer = seasonsPage.Locator("div.nameContainer");
            await nameContainer.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            
            await seasonsPage.GoToSeasonAsync(season.Title);
            
            await seasonsPage.ShouldHaveSeasonMetadataAsync(season.Title);
            await seasonsPage.ShouldHaveEpisodesAsync(season.Id, language, browser);
            await seasonsPage.CloseAsync();
        }
    }
    
    public static async Task GoToSeasonAsync(this IPage seriesPage, string seasonName)
    {
        var seasonsListElement = seriesPage.Locator(".childrenItemsContainer");
        await seasonsListElement.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        var seasonLinks = await seasonsListElement.Locator("a[title]").AllAsync();

        foreach (var seasonLink in seasonLinks)
        {
            var title = await seasonLink.GetAttributeAsync("title");
            if (title is not null && title.Contains(seasonName))
            {
                await seasonLink.ClickAsync();
            }
        }
    }
    
    public static async Task ShouldHaveSeasonMetadataAsync(this IPage seasonsPage, string expectedSeasonName)
    {
        var infoWrapperElement = seasonsPage.Locator("div.infoWrapper");
        var nameContainerElement = infoWrapperElement.Locator("div.nameContainer");
        var itemNameElement = nameContainerElement.Locator("h3.itemName");
        var actualSeasonName = await itemNameElement.Locator("bdi").InnerTextAsync();
        actualSeasonName.Should().Be(expectedSeasonName);
    }
    
    public static async Task ShouldHaveEpisodesAsync(this IPage seasonsPage, string seasonId,
        CultureInfo language, IBrowser browser)
    {
        var episodeClient = await CrunchyrollClientHelper.GetEpisodesClientAsync();
        var episodesResult = await episodeClient.GetEpisodesAsync(seasonId, language, CancellationToken.None);
        episodesResult.IsSuccess.Should().BeTrue();
        var episodes = episodesResult.Value.Data;
        
        var episodeElements = await GetAllEpisodeElements(seasonsPage);
        for (var index = 0; index < episodeElements.Count; index++)
        {
            var responses = new List<IResponse>();
            var episodePage = await browser.NewPageAsync();
            episodePage.Response += (_, e) =>
            {
                responses.Add(e);
            };
            await episodePage.GotoAsync(seasonsPage.Url);
            await episodePage.LoginAsync();
            
            var episodePageEpisodeElements = await GetAllEpisodeElements(episodePage);
            var infoButton = episodePageEpisodeElements[index].Locator("button[is='paper-icon-button-light'][data-action='link']");
            await infoButton.ClickAsync();
            
            await episodePage.ShouldHaveEpisodeMetadataAsync(episodes[index]);
            await episodePage.ShouldHaveCommentsAsync(responses);
            await episodePage.CloseAsync();
        }

        return;

        async Task<IReadOnlyList<ILocator>> GetAllEpisodeElements(IPage page)
        {
            var childrenContent = page.Locator("div#childrenContent").Locator("visible=true");
            await childrenContent.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            var videoElements = childrenContent.Locator("div[data-type='Episode']");
            return await videoElements.AllAsync();
        }
    }
}