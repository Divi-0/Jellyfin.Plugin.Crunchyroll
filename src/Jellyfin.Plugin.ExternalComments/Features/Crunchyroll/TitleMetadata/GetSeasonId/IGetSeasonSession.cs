using System.Threading.Tasks;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.GetSeasonId;

public interface IGetSeasonSession
{
    /// <param name="titleId">crunchyroll titleId</param>
    /// <param name="seasonNumber">eg. 1,2,3</param>
    /// <param name="duplicateCounter">When multiple seasons have the same season number this param identifies which duplicate season to choose;
    /// 1 = take the first season you find, 2 = take the second the season of the seasons with identical season-numbers, ...</param>
    public ValueTask<string?> GetSeasonIdByNumberAsync(string titleId, int seasonNumber, int duplicateCounter);
    public ValueTask<string?> GetSeasonIdByNameAsync(string titleId, string name);
}