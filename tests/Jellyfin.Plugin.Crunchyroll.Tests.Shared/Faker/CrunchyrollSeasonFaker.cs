using J2N;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;

public static class CrunchyrollSeasonFaker
{
    public static Season Generate(MediaBrowser.Controller.Entities.TV.Season? season = null)
    {
        var title = new Bogus.Faker().Random.Words();
        var seasonNumber = Random.Shared.Next(1, int.MaxValue);
        return new Bogus.Faker<Season>()
            .RuleFor(x => x.Id, season is null ? CrunchyrollIdFaker.Generate() : season.ProviderIds[CrunchyrollExternalKeys.SeasonId])
            .RuleFor(x => x.Title, _ => title)
            .RuleFor(x => x.SlugTitle, _ => CrunchyrollSlugFaker.Generate(title))
            .RuleFor(x => x.SeasonNumber, seasonNumber)
            .RuleFor(x => x.SeasonSequenceNumber, f => f.Random.Number())
            .RuleFor(x => x.SeasonDisplayNumber, seasonNumber.ToString())
            .RuleFor(x => x.Identifier, $"{CrunchyrollIdFaker.Generate()}|S{seasonNumber}")
            .Generate();
    }
}