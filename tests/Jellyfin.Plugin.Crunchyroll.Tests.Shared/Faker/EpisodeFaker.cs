using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;

public static class EpisodeFaker
{
    public static Episode Generate(Season? parent = null)
    {
        var parentId = parent?.Id ?? Guid.NewGuid();
        return new Bogus.Faker<Episode>()
            .RuleFor(x => x.Id, Guid.NewGuid())
            .RuleFor(x => x.IndexNumber, f => f.Random.Number(99))
            .RuleFor(x => x.ParentId, parentId)
            .RuleFor(x => x.SeasonId, parentId)
            .RuleFor(x => x.Name, f => $"{f.Random.Words()}-{f.Random.Number(9999)}")
            .RuleFor(x => x.Path, f => $"/{f.Random.Words()}/E-{f.Random.Number(9999)}.mp4")
            .RuleFor(x => x.PreferredMetadataLanguage, "en")
            .RuleFor(x => x.PreferredMetadataCountryCode, "US")
            .Generate();
    }    
    
    public static Episode GenerateWithEpisodeId(Season? parent = null)
    {
        var episode = Generate(parent);

        episode.ProviderIds.Add(CrunchyrollExternalKeys.EpisodeId, CrunchyrollIdFaker.Generate());
        
        return episode;
    }
}