using FluentAssertions;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.ScrapEpisodeMetadata.Client.Dtos;
using Microsoft.Playwright;

namespace Jellyfin.Plugin.Crunchyroll.Tests.E2E.Pages;

public static class EpisodePage
{
    public static async Task ShouldHaveEpisodeMetadataAsync(this IPage page, CrunchyrollEpisodeItem episodeItem)
    {
        var infoWrapper = page.Locator("div.infoWrapper").Locator("visible=true");
        var header = infoWrapper.Locator("h3.itemName");
        var bdi = header.Locator("bdi");
        var actualName = await bdi.InnerTextAsync();
        actualName.Should().Contain(episodeItem.Title);
        
        var descriptionWrapper = page.Locator("p.detail-clamp-text").Locator("visible=true");
        var descriptionBdi = descriptionWrapper.Locator("bdi");
        var descriptionElement = descriptionBdi.Locator("p");
        var actualDescription = await descriptionElement.InnerTextAsync();
        actualDescription.Should().Be(episodeItem.Description);
        
        
    }
    
    public static async Task ShouldHaveCommentsAsync(this IPage page, List<IResponse> pageResponses)
    {
        var commentsWrapper = page.Locator("div#crunchyroll-comments-wrapper");
        await commentsWrapper.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Attached });
        
        var crunchyrollCommentElements = commentsWrapper.Locator("div.crunchyroll-comment");
        var crunchyrollCommentsCount = await crunchyrollCommentElements.CountAsync();

        if (crunchyrollCommentsCount == 0)
        {
            var infoWrapper = page.Locator("div.infoWrapper").Locator("visible=true");
            var header = infoWrapper.Locator("h3.itemName");
            var bdi = header.Locator("bdi");
            var actualName = await bdi.InnerTextAsync();
            Console.WriteLine($"'{actualName}' has no comments");
        }

        foreach (var commentDiv in await crunchyrollCommentElements.AllAsync())
        {
            //avatar img
            var avatarImgElement = commentDiv.Locator("img");
            var isAvatarImgVisible = await avatarImgElement.IsVisibleAsync();
            isAvatarImgVisible.Should().BeTrue();

            var imageUrl = await avatarImgElement.GetAttributeAsync("src");
            var imageResponse = pageResponses.FirstOrDefault(x => x.Url.Contains(imageUrl!));
            imageResponse.Should().NotBeNull();
            imageResponse!.Ok.Should().BeTrue();

            //username
            var usernameElement = commentDiv.Locator("h5");
            var username = await usernameElement.InnerTextAsync();
            username.Should().NotBeEmpty();

            //body
            var bodyElement = commentDiv.Locator("p");
            var body = await bodyElement.InnerTextAsync();
            body.Should().NotBeEmpty();

            //likes
            var likesElement = commentDiv.Locator("div.likes");
            var likes = await likesElement.InnerTextAsync();
            likes.Should().NotBeEmpty();
        }
    }
}