using System.Threading.Tasks;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata;

public interface IScrapTitleMetadataSession
{
    public ValueTask AddOrUpdateTitleMetadata(Entities.TitleMetadata titleMetadata);
    public ValueTask<Entities.TitleMetadata?> GetTitleMetadata(string titleId);
}