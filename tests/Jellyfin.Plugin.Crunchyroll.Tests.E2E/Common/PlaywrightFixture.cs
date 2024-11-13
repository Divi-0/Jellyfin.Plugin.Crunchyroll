using Microsoft.Playwright;

namespace Jellyfin.Plugin.Crunchyroll.Tests.E2E.Common;

public sealed class PlaywrightFixture : IAsyncLifetime
{
    public IBrowser Browser { get; set; } = null!;
    private IPlaywright PlaywrightInstance { get; set; } = null!;
    
    public async Task InitializeAsync()
    {
        PlaywrightInstance = await Playwright.CreateAsync();
        Browser = await PlaywrightInstance.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
#if DEBUG
            Headless = false
#else
            Headless = true 
#endif
        });
    }

    public async Task DisposeAsync()
    {
        await Browser.DisposeAsync();
        PlaywrightInstance.Dispose();
    }
}