using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.OverwriteSeasonJellyfinData;

public interface IOverwriteSeasonJellyfinDataSession
{
    public ValueTask<Result<Season>> GetSeasonAsync(string seasonId);
}