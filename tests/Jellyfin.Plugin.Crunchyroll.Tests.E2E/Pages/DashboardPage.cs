using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Jellyfin.Plugin.Crunchyroll.Tests.E2E.Pages;

public static class DashboardPage
{
    public static async Task GoToDashboardAsync(this IPage page)
    {
        await page.Locator(".mainDrawerButton").ClickAsync();
        var adminMenuOptions = page.Locator(".adminMenuOptions");
        await adminMenuOptions.Locator("[data-itemid='dashboard']").ClickAsync();
    }
    
    public static async Task SetCrunchyrollPluginConfigAsync(this IPage page, string jellyfinUrl, string animeCollectionName)
    {
        const string crunchyrollPluginGuid = "c6f8461a9a6f4c658bb9825866cabc91";
        
        await page.GotoAsync($"{jellyfinUrl}web/#/dashboard/plugins");
        var crunchyrollPluginCard = page.Locator($"[data-id='{crunchyrollPluginGuid}']");
        await crunchyrollPluginCard.WaitForAsync(new LocatorWaitForOptions() { State = WaitForSelectorState.Visible });
        await crunchyrollPluginCard.ClickAsync();

        var crunchyrollPluginConfigForm = page.Locator("form#CrunchyrollPluginConfigForm");
        var x = await crunchyrollPluginConfigForm.AllAsync();

        await page.ConfigEnableFeatureAsync("IsFeatureSeriesTitleEnabled");
        await page.ConfigEnableFeatureAsync("IsFeatureSeriesDescriptionEnabled");
        await page.ConfigEnableFeatureAsync("IsFeatureSeriesStudioEnabled");
        await page.ConfigEnableFeatureAsync("IsFeatureSeriesRatingsEnabled");
        await page.ConfigEnableFeatureAsync("IsFeatureSeriesRatingsEnabled");
        
        await page.ConfigEnableFeatureAsync("IsFeatureSeasonTitleEnabled");
        await page.ConfigEnableFeatureAsync("IsFeatureSeasonOrderByCrunchyrollOrderEnabled");
        
        await page.ConfigEnableFeatureAsync("IsFeatureEpisodeTitleEnabled");
        await page.ConfigEnableFeatureAsync("IsFeatureEpisodeDescriptionEnabled");
        await page.ConfigEnableFeatureAsync("IsFeatureEpisodeIncludeSpecialsInNormalSeasonsEnabled");
        
        await page.ConfigEnableFeatureAsync("IsFeatureMovieTitleEnabled");
        await page.ConfigEnableFeatureAsync("IsFeatureMovieDescriptionEnabled");
        await page.ConfigEnableFeatureAsync("IsFeatureMovieStudioEnabled");
        
        await crunchyrollPluginConfigForm.Locator(".button-submit").ClickAsync();
    }

    private static async Task ConfigEnableFeatureAsync(this IPage locator, string checkboxId)
    {
        await locator.Locator($"#{checkboxId}-span").CheckAsync();
    }
    
    public static async Task RestartServerAsync(this IPage page, string jellyfinUrl)
    {
        await page.GotoAsync($"{jellyfinUrl}web/#/dashboard");
        
        var dashboardActionsContainer = page.Locator(".dashboardActionsContainer");
        await dashboardActionsContainer.WaitForAsync(new LocatorWaitForOptions() { State = WaitForSelectorState.Visible });
        await dashboardActionsContainer.Locator("button#btnRestartServer").ClickAsync();

        var deleteButton = page.Locator(".button-delete").Locator("visible=true");
        await deleteButton.ClickAsync();

        IResponse response;
        do
        {
            response = await page.WaitForResponseAsync(new Regex(@"ScheduledTasks\?IsEnabled=true"), new PageWaitForResponseOptions{Timeout = 60000});
        } while (!response.Ok);

        await page.ReloadAsync();
    }
    
    public static async Task StartLibraryScan(this IPage page, string jellyfinUrl)
    {
        await page.GotoAsync($"{jellyfinUrl}web/#/dashboard");
        
        var dashboardActionsContainer = page.Locator(".dashboardActionsContainer");
        await dashboardActionsContainer.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await dashboardActionsContainer.Locator("button[data-taskid]").ClickAsync();

        var divRunningTasks = page.Locator("#divRunningTasks");
        await divRunningTasks.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await divRunningTasks.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden, Timeout = 3_600_000});
    }

    public static async Task SetupLibraryAsync(this IPage page, string videoPath)
    {
        await page.Locator("[data-testid='LibraryAddIcon']").ClickAsync();
        await page.Locator("[href='#/dashboard/libraries']").ClickAsync();
        var librarySetupForm = page.Locator("div#divVirtualFolders").Locator("visible=true");
        await librarySetupForm.Locator("div#addLibrary").ClickAsync();
        
        var addLibraryForm = page.Locator(".addLibraryForm");
        await addLibraryForm.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await addLibraryForm.Locator("select#selectCollectionType").SelectOptionAsync("mixed");
        await addLibraryForm.Locator("input#txtValue").FillAsync("Anime 123");
        await addLibraryForm.Locator(".btnAddFolder").ClickAsync();
        
        var directoryPicker = page.Locator(".directoryPicker");
        await directoryPicker.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        var directoryPickerInputElement = directoryPicker.Locator("input#txtDirectoryPickerPath");
        await directoryPickerInputElement.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await directoryPickerInputElement.FillAsync(videoPath);
        await directoryPicker.Locator(".button-submit").ClickAsync();

        await page.MoveMetadataProviderCrunchyrollToTop("Series");
        await page.MoveMetadataProviderCrunchyrollToTop("Season");
        await page.MoveMetadataProviderCrunchyrollToTop("Episode");
        await page.MoveMetadataProviderCrunchyrollToTop("Movie");
        
        await page.MoveImageProviderCrunchyrollToTop("Series");
        await page.MoveImageProviderCrunchyrollToTop("Episode");
        await page.MoveImageProviderCrunchyrollToTop("Movie");

        var addLibraryFormSubmitButton = addLibraryForm.Locator(".button-submit");
        await addLibraryFormSubmitButton.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await addLibraryFormSubmitButton.Locator("span").ClickAsync();
    }
    
    private static async Task MoveMetadataProviderCrunchyrollToTop(this IPage page, string type)
    {
        var metadataFetcherList = page.Locator($".metadataFetcher[data-type='{type}']");

        while (await metadataFetcherList.Locator("[data-pluginname]").First.GetAttributeAsync("data-pluginname") != "Crunchyroll")
        {
            var crunchyrollElement = metadataFetcherList.Locator("[data-pluginname='Crunchyroll']");
            await crunchyrollElement.Locator("button.btnSortableMoveUp").ClickAsync();
        }
    }

    private static async Task MoveImageProviderCrunchyrollToTop(this IPage page, string type)
    {
        var metadataFetcherList = page.Locator($".imageFetcher[data-type='{type}']");

        while (await metadataFetcherList.Locator("[data-pluginname]").First.GetAttributeAsync("data-pluginname") != "Crunchyroll")
        {
            var crunchyrollElement = metadataFetcherList.Locator("[data-pluginname='Crunchyroll']");
            await crunchyrollElement.Locator("button.btnSortableMoveUp").ClickAsync();
        }
    }
}