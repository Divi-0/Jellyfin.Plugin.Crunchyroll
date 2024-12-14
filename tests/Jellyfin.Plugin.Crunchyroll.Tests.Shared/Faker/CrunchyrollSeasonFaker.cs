using J2N;
using Jellyfin.Plugin.Crunchyroll.Domain.Entities;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;

public static class CrunchyrollSeasonFaker
{
    public static Season Generate(MediaBrowser.Controller.Entities.TV.Season? season = null)
    {
        var title = new Bogus.Faker().Random.Words();
        var seasonNumber = Random.Shared.Next(1, int.MaxValue);
        return new Bogus.Faker<Season>()
            .RuleFor(x => x.CrunchyrollId, season is null ? CrunchyrollIdFaker.Generate() : season.ProviderIds[CrunchyrollExternalKeys.SeasonId])
            .RuleFor(x => x.Title, _ => title)
            .RuleFor(x => x.SlugTitle, _ => CrunchyrollSlugFaker.Generate(title))
            .RuleFor(x => x.SeasonNumber, seasonNumber)
            .RuleFor(x => x.SeasonSequenceNumber, Random.Shared.Next(1, 10))
            .RuleFor(x => x.SeasonDisplayNumber, seasonNumber.ToString())
            .RuleFor(x => x.Identifier, $"{CrunchyrollIdFaker.Generate()}|S{seasonNumber}")
            .RuleFor(x => x.Language, "en-US")
            .Generate();
    }
}