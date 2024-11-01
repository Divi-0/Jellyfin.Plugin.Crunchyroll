using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using MediaBrowser.Controller.Entities.TV;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker
{
    public static class SeriesFaker
    {
        public static Series Generate()
        {
            var series = new Bogus.Faker<Series>()
                .RuleFor(x => x.Id, Guid.NewGuid())
                .RuleFor(x => x.Name, f => $"{f.Random.Words()}-{f.Random.Number(9999)}")
                .Generate();

            return series;
        }

        public static Series GenerateWithTitleId()
        {
            var series = Generate();

            series.ProviderIds.Add(CrunchyrollExternalKeys.Id, CrunchyrollIdFaker.Generate());
            series.ProviderIds.Add(CrunchyrollExternalKeys.SlugTitle, CrunchyrollSlugFaker.Generate(series.Name));

            return series;
        }
    }
}
