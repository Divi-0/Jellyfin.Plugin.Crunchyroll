using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;

public static class CrunchyrollEpisodeFaker
{
    public static Episode Generate(MediaBrowser.Controller.Entities.TV.Episode? episode = null)
    {
        var title = new Bogus.Faker().Random.Words();
        return new Bogus.Faker<Episode>()
            .RuleFor(x => x.Id, f => episode is null ? CrunchyrollIdFaker.Generate() : episode.ProviderIds[CrunchyrollExternalKeys.EpisodeId])
            .RuleFor(x => x.Title, _ => title)
            .RuleFor(x => x.SlugTitle, _ => CrunchyrollSlugFaker.Generate(title))
            .RuleFor(x => x.Description, f => f.Lorem.Sentences())
            .RuleFor(x => x.EpisodeNumber, f => episode?.IndexNumber is null ? f.Random.Number(1, 2000).ToString() : episode.IndexNumber.Value.ToString())
            .RuleFor(x => x.ThumbnailUrl, f => f.Internet.Url())
            .Generate();
    }
}