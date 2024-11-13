using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Image.Entites;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;

public static class CrunchyrollEpisodeFaker
{
    public static Episode Generate(MediaBrowser.Controller.Entities.TV.Episode? episode = null)
    {
        var title = new Bogus.Faker().Random.Words();
        var episodeNumber = episode?.IndexNumber is null 
            ? Random.Shared.Next(1, 2000).ToString() 
            : episode.IndexNumber.Value.ToString();
        
        return new Bogus.Faker<Episode>()
            .RuleFor(x => x.Id, f => episode is null ? CrunchyrollIdFaker.Generate() : episode.ProviderIds[CrunchyrollExternalKeys.EpisodeId])
            .RuleFor(x => x.Title, _ => title)
            .RuleFor(x => x.SlugTitle, _ => CrunchyrollSlugFaker.Generate(title))
            .RuleFor(x => x.Description, f => f.Lorem.Sentences())
            .RuleFor(x => x.EpisodeNumber, f => episodeNumber)
            .RuleFor(x => x.Thumbnail, f => new ImageSource
            {
                Uri = f.Internet.Url(), 
                Height = f.Random.Number(), 
                Width = f.Random.Number()
            })
            .RuleFor(x => x.SequenceNumber, Convert.ToDouble(episodeNumber))
            .Generate();
    }
}