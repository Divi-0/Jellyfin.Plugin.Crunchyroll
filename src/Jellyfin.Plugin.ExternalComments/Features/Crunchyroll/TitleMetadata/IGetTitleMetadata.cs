using System.Threading.Tasks;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata;

public interface IGetTitleMetadata
{
    public ValueTask<Entities.TitleMetadata?> GetTitleMetadataAsync(string titleId);
}