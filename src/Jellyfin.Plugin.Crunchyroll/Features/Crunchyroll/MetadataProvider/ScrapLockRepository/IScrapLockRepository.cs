using System.Threading.Tasks;
using Jellyfin.Plugin.Crunchyroll.Domain;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.ScrapLockRepository;

public interface IScrapLockRepository
{
    /// <returns>true if added, false if entry already exists</returns>
    public ValueTask<bool> AddLockAsync(CrunchyrollId id);
}