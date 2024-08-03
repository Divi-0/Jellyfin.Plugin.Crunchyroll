using System.Threading.Tasks;
using Jellyfin.Plugin.ExternalComments.Entities;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.ScrapTitleMetadata;

public interface IScrapTitleMetadataSession
{
    public ValueTask AddOrUpdateTitleMetadata(TitleMetadata titleMetadata);
    public ValueTask<TitleMetadata?> GetTitleMetadata(string titleId);
}