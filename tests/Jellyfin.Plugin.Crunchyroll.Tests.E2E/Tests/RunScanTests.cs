using System.Globalization;
using Jellyfin.Plugin.Crunchyroll.Tests.E2E.Common;
using Jellyfin.Plugin.Crunchyroll.Tests.E2E.Pages;
using Microsoft.Playwright;

namespace Jellyfin.Plugin.Crunchyroll.Tests.E2E.Tests;

[Collection(CollectionNames.E2E)]
public class RunScanTests
{
    private readonly JellyfinFixture _jellyfinFixture;
    private readonly IBrowser _browser;
    
    public RunScanTests(PlaywrightFixture playwrightFixture, JellyfinFixture jellyfinFixture)
    {
        _jellyfinFixture = jellyfinFixture;
        _browser = playwrightFixture.Browser;
    }

    [Fact]
    public async Task Test()
    {
        var page = await _browser.NewPageAsync();

        try
        {
            var animeCollectioName = "Anime 123";
            await page.FinishWizardAsync(_jellyfinFixture.Url, JellyfinFixture.VideoContainerPath, animeCollectioName);
            await page.LoginAsync();
            await page.GoToDashboardAsync();
            await page.SetCrunchyrollPluginConfigAsync(_jellyfinFixture.Url, animeCollectioName);
            await page.SetupLibraryAsync(JellyfinFixture.VideoContainerPath);

            //One Piece
            // await page.SeriesShouldHaveDataFromCrunchyrollAsync(_jellyfinFixture.Url, "GRMG8ZQZR", 
            //     new CultureInfo("en-US"), 5);
        }
        catch(Exception)
        {
            await File.WriteAllBytesAsync("error.png", await page.ScreenshotAsync());
            throw;
        }
        finally
        {
            await File.WriteAllBytesAsync("test.png", await page.ScreenshotAsync());
        }
    }
}