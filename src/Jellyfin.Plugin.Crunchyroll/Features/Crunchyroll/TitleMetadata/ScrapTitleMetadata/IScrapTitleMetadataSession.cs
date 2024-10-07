using System.Threading.Tasks;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata;

public interface IScrapTitleMetadataSession : IGetTitleMetadata
{
    public ValueTask AddOrUpdateTitleMetadata(Entities.TitleMetadata titleMetadata);
}