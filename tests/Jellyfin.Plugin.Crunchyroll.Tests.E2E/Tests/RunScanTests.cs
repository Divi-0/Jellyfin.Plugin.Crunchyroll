using Jellyfin.Plugin.Crunchyroll.Tests.E2E.Common;
using Jellyfin.Plugin.Crunchyroll.Tests.E2E.Pages;
using Microsoft.Playwright;

namespace Jellyfin.Plugin.Crunchyroll.Tests.E2E.Tests;

[Collection(CollectionNames.E2E)]
public class RunScanTests
{
    private readonly JellyfinFixture _jellyfinFixture;
    private readonly FlareSolverrFixture _flareSolverrFixture;
    private readonly IBrowser _browser;
    
    public RunScanTests(PlaywrightFixture playwrightFixture, JellyfinFixture jellyfinFixture, 
        FlareSolverrFixture flareSolverrFixture)
    {
        _jellyfinFixture = jellyfinFixture;
        _flareSolverrFixture = flareSolverrFixture;
        _browser = playwrightFixture.Browser;
    }

    [Fact]
    public async Task Test()
    {
        var page = await _browser.NewPageAsync();

        try
        {
            await page.FinishWizardAsync(_jellyfinFixture.Url, JellyfinFixture.VideoContainerPath);
            await page.LoginAsync();
            await page.GoToDashboardAsync();
            await page.SetCrunchyrollPluginConfigAsync(_jellyfinFixture.Url, FlareSolverrFixture.Url,
                JellyfinFixture.VideoContainerPath);
            await page.StartLibraryScan(_jellyfinFixture.Url);
        }
        catch
        {
            await File.WriteAllBytesAsync("error.png", await page.ScreenshotAsync());
        }
        finally
        {
            await File.WriteAllBytesAsync("test.png", await page.ScreenshotAsync());
        }
    }
}