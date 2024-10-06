using System.Threading.Tasks;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata;

public interface IScrapTitleMetadataSession : IGetTitleMetadata
{
    public ValueTask AddOrUpdateTitleMetadata(Entities.TitleMetadata titleMetadata);
}