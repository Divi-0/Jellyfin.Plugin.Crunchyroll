using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using MediaBrowser.Controller.Entities.TV;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker
{
    public static class SeriesFaker
    {
        public static Series Generate()
        {
            var faker = new Bogus.Faker();
            var name = $"{faker.Random.Words()}-{faker.Random.Number(9999)}";
            var series = new Bogus.Faker<Series>()
                .RuleFor(x => x.Id, Guid.NewGuid())
                .RuleFor(x => x.Name, name)
                .RuleFor(x => x.Path, f => $"videos/{f.Random.Words()}/{name}")
                .RuleFor(x => x.PreferredMetadataLanguage, "en")
                .RuleFor(x => x.PreferredMetadataCountryCode, "US")
                .Generate();

            return series;
        }

        public static Series GenerateWithTitleId()
        {
            var series = Generate();

            series.ProviderIds.Add(CrunchyrollExternalKeys.SeriesId, CrunchyrollIdFaker.Generate());

            return series;
        }
    }
}
