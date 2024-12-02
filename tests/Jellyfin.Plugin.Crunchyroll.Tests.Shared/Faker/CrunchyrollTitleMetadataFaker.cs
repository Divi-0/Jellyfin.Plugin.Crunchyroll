using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Image.Entites;
using MediaBrowser.Controller.Entities;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;

public static class CrunchyrollTitleMetadataFaker
{
    public static TitleMetadata Generate(BaseItem? seasonOrMovie = null, ICollection<Season>? seasons = null)
    {
        var faker = new Bogus.Faker();
        var title = seasonOrMovie?.Name ?? faker.Random.Words();
        return new TitleMetadata
        {
            Title = title,
            Description = faker.Lorem.Sentences(),
            Studio = faker.Company.CompanyName(),
            SlugTitle = CrunchyrollSlugFaker.Generate(title),
            Rating = faker.Random.Float(),
            CrunchyrollId = seasonOrMovie?.ProviderIds.TryGetValue(CrunchyrollExternalKeys.SeriesId, out var id) ?? false 
                ? id 
                : CrunchyrollIdFaker.Generate(),
            Id = Guid.NewGuid(),
            Seasons = seasons?.ToList() ?? [],
            PosterTall = JsonSerializer.Serialize(new ImageSource
            {
                Uri = faker.Internet.UrlWithPath(fileExt: "jpg"),
                Height = 1080,
                Width = 1920
            }),
            PosterWide = JsonSerializer.Serialize(new ImageSource
            {
                Uri = faker.Internet.UrlWithPath(fileExt: "jpg"),
                Height = 1080,
                Width = 1920
            }),
            Language = "en-US"
        };
    }
}