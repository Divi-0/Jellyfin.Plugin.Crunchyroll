using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Jellyfin.Plugin.Crunchyroll.Tests.E2E.Pages;

public static class WizardPage
{
    public static async Task<IPage> FinishWizardAsync(this IPage page, string jellyfinUrl, string videoPath, 
        string animeCollectionName)
    {
        await page.GoToWizardAsync(jellyfinUrl);
        await page.FinishStepStartAsync();
        await page.FinishStepUserAsync();
        await page.FinishStepLibrariesAsync(videoPath, animeCollectionName);
        await page.FinishStepMetadataLanguageAsync();
        await page.FinishStepMiscOptionsAsync();
        await page.FinishStepFinishPageAsync();
        
        return page;
    }

    private static async Task GoToWizardAsync(this IPage page, string jellyfinUrl)
    {
        await page.GotoAsync($"{jellyfinUrl}web/#/wizardstart.html");
        await page.WaitForURLAsync(new Regex("wizardstart"));
    }

    private static async Task FinishStepStartAsync(this IPage page)
    {
        await page.Locator("//form[contains(@class,'wizardStartForm')]").WaitForAsync(new LocatorWaitForOptions{State = WaitForSelectorState.Visible});
        await page.ClickAsync("button.button-submit");
    }

    private static async Task FinishStepUserAsync(this IPage page)
    {
        var wizardUserForm = page.Locator(".wizardUserForm");
        await wizardUserForm.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

        await page.ReloadAsync(); //Refresh, otherwise username is empty
        
        var passwordInputLocator = wizardUserForm.Locator("input#txtManualPassword");
        await passwordInputLocator.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await passwordInputLocator.FillAsync("root");
        
        var passwordConfirmInputLocator = wizardUserForm.Locator("input#txtPasswordConfirm");
        await passwordConfirmInputLocator.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await passwordConfirmInputLocator.FillAsync("root");
        
        await wizardUserForm.Locator(".button-submit").ClickAsync();
    }

    private static async Task FinishStepLibrariesAsync(this IPage page, string videoPath, string animeCollectionName)
    {
        var librarySetupForm = page.Locator("div#wizardLibraryPage");
        await librarySetupForm.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await librarySetupForm.Locator("div#addLibrary").ClickAsync();
        
        var addLibraryForm = page.Locator(".addLibraryForm");
        await addLibraryForm.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await addLibraryForm.Locator("select#selectCollectionType").SelectOptionAsync("mixed");
        await addLibraryForm.Locator("input#txtValue").FillAsync(animeCollectionName);
        await addLibraryForm.Locator(".btnAddFolder").ClickAsync();
        
        var directoryPicker = page.Locator(".directoryPicker");
        await directoryPicker.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        var directoryPickerInputElement = directoryPicker.Locator("input#txtDirectoryPickerPath");
        await directoryPickerInputElement.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await directoryPickerInputElement.FillAsync(videoPath);
        await directoryPicker.Locator(".button-submit").ClickAsync();

        var addLibraryFormSubmitButton = addLibraryForm.Locator(".button-submit");
        await addLibraryFormSubmitButton.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await addLibraryFormSubmitButton.Locator("span").ClickAsync();
        
        await librarySetupForm.Locator(".button-submit").ClickAsync();
    }

    private static async Task FinishStepMetadataLanguageAsync(this IPage page)
    {
        var metadataSettingsForm = page.Locator(".wizardSettingsForm");
        await metadataSettingsForm.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await metadataSettingsForm.Locator(".button-submit").ClickAsync();
    }

    private static async Task FinishStepMiscOptionsAsync(this IPage page)
    {
        var enableExternalAccess = page.Locator("input#chkRemoteAccess");
        await enableExternalAccess.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        var optionsForm = page.Locator("div#wizardSettingsPage:visible");
        await optionsForm.Locator("label").Filter(new LocatorFilterOptions{Has = enableExternalAccess}).ClickAsync();
        await optionsForm.Locator(".button-submit").ClickAsync();
    }

    private static async Task FinishStepFinishPageAsync(this IPage page)
    {
        var finishForm = page.Locator("div#wizardFinishPage");
        await finishForm.Locator(".button-submit").ClickAsync();
    }
}