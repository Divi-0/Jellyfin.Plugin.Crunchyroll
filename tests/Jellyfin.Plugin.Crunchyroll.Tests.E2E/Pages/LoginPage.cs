using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Jellyfin.Plugin.Crunchyroll.Tests.E2E.Pages;

public static class LoginPage
{
    public static async Task<IPage> LoginAsync(this IPage page)
    {
        await page.WaitForURLAsync(new Regex(@"index\.html"));
        var loginForm = page.Locator(".manualLoginForm");
        await loginForm.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        
        var usernameLoginInputLocator = loginForm.Locator("input#txtManualName");
        await usernameLoginInputLocator.FillAsync("root");
        
        var passwordLoginInputLocator = loginForm.Locator("input#txtManualPassword");
        await passwordLoginInputLocator.FillAsync("root");
        
        //login-button
        await loginForm.Locator(".button-submit").ClickAsync();
        
        return page;
    }
}