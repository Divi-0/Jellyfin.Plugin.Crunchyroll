using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker
{
    public static class SeasonFaker
    {
        public static Season Generate(BaseItem? parent = null)
        {
            var parentId = parent?.Id ?? Guid.NewGuid();
            return new Bogus.Faker<Season>()
                .RuleFor(x => x.Id, Guid.NewGuid())
                .RuleFor(x => x.IndexNumber, f => f.Random.Number(99))
                .RuleFor(x => x.ParentId, parentId)
                .RuleFor(x => x.SeriesId, parentId)
                .RuleFor(x => x.Name, f => $"{f.Random.Words(3)}-{f.Random.Number(9999)}")
                .RuleFor(x => x.PresentationUniqueKey, new Season().CreatePresentationUniqueKey())
                .RuleFor(x => x.Path, f => $"/{f.Random.Words(3)}/{f.Random.Words(3)}-{f.Random.Number(9999)}")
                .RuleFor(x => x.PreferredMetadataLanguage, "en")
                .RuleFor(x => x.PreferredMetadataCountryCode, "US")
                .Generate();
        }
        
        public static Season GenerateWithSeasonId(BaseItem? parent = null)
        {
            var season = Generate(parent);

            season.ProviderIds.Add(CrunchyrollExternalKeys.SeasonId, CrunchyrollIdFaker.Generate());
            
            return season;
        }
    }
}
