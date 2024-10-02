using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;

namespace Jellyfin.Plugin.ExternalComments.Tests.Shared.Faker;

public static class EpisodeFaker
{
    public static Episode Generate(BaseItem? parent = null)
    {
        var parentId = parent?.Id ?? Guid.NewGuid();
        return new Bogus.Faker<Episode>()
            .RuleFor(x => x.Id, Guid.NewGuid())
            .RuleFor(x => x.IndexNumber, f => f.Random.Number(99))
            .RuleFor(x => x.ParentId, parentId)
            .RuleFor(x => x.SeasonId, parentId)
            .RuleFor(x => x.Name, f => f.Random.Word())
            .Generate();
    }    
    
    public static Episode GenerateWithEpisodeId(BaseItem? parent = null)
    {
        var episode = Generate(parent);

        episode.ProviderIds.Add(CrunchyrollExternalKeys.EpisodeId, CrunchyrollIdFaker.Generate());
        episode.ProviderIds.Add(CrunchyrollExternalKeys.EpisodeSlugTitle, CrunchyrollSlugFaker.Generate(episode.Name));
        
        return episode;
    }
}