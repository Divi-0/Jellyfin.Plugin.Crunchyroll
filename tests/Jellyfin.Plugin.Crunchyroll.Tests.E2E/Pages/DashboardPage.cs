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
    
    public static async Task SetCrunchyrollPluginConfigAsync(this IPage page, string jellyfinUrl, string animeVideoPath)
    {
        const string crunchyrollPluginGuid = "c6f8461a9a6f4c658bb9825866cabc91";
        
        await page.GotoAsync($"{jellyfinUrl}web/#/dashboard/plugins");
        var crunchyrollPluginCard = page.Locator($"[data-id='{crunchyrollPluginGuid}']");
        await crunchyrollPluginCard.WaitForAsync(new LocatorWaitForOptions() { State = WaitForSelectorState.Visible });
        await crunchyrollPluginCard.ClickAsync();

        var crunchyrollPluginConfigForm = page.Locator("form#CrunchyrollPluginConfigForm");
        
        var libraryPathInputElement = crunchyrollPluginConfigForm.Locator("input#LibraryPath");
        await libraryPathInputElement.WaitForAsync(new LocatorWaitForOptions() { State = WaitForSelectorState.Visible });
        await libraryPathInputElement.FillAsync(animeVideoPath);
        
        await crunchyrollPluginConfigForm.Locator(".button-submit").ClickAsync();

        await page.RestartServerAsync(jellyfinUrl);
    }
    
    public static async Task RestartServerAsync(this IPage page, string jellyfinUrl)
    {
        await page.GotoAsync($"{jellyfinUrl}web/#/dashboard");
        
        var dashboardActionsContainer = page.Locator(".dashboardActionsContainer");
        await dashboardActionsContainer.WaitForAsync(new LocatorWaitForOptions() { State = WaitForSelectorState.Visible });
        await dashboardActionsContainer.Locator("button#btnRestartServer").ClickAsync();

        var allDeleteButtons = await page.Locator(".button-delete").AllAsync();

        foreach (var deleteButton in allDeleteButtons)
        {
            //Find visibile delete button to restart the server
            if (await deleteButton.IsVisibleAsync())
            {
                await deleteButton.ClickAsync();
                break;
            }
        }

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
        await divRunningTasks.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden, Timeout = 0});
    }
}