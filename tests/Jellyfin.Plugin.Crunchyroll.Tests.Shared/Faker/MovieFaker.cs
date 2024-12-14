using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using MediaBrowser.Controller.Entities.Movies;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;

public class MovieFaker
{
    public static Movie Generate()
    {
        var faker = new Bogus.Faker();
        var name = $"{faker.Random.Words()}-{faker.Random.Number(9999)}";
        var movie = new Bogus.Faker<Movie>()
            .RuleFor(x => x.Id, Guid.NewGuid())
            .RuleFor(x => x.Name, name)
            .RuleFor(x => x.Path, f => $"videos/{f.Random.Words()}/{name}.mp4")
            .RuleFor(x => x.ParentId, Guid.NewGuid)
            .RuleFor(x => x.PreferredMetadataLanguage, "en")
            .RuleFor(x => x.PreferredMetadataCountryCode, "US")
            .Generate();

        return movie;
    }

    public static Movie GenerateWithCrunchyrollIds()
    {
        var movie = Generate();

        movie.ProviderIds.Add(CrunchyrollExternalKeys.SeriesId, CrunchyrollIdFaker.Generate());
        movie.ProviderIds.Add(CrunchyrollExternalKeys.EpisodeId, CrunchyrollIdFaker.Generate());
        movie.ProviderIds.Add(CrunchyrollExternalKeys.SeasonId, CrunchyrollIdFaker.Generate());

        return movie;
    }
}