using System.Globalization;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Common.Persistence;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.DeleteTitleMetadata;

public interface IDeleteTitleMetadataRepository : ISaveChanges
{
    public Task<Result> DeleteTitleMetadataAsync(string crunchyrollSeriesId, CultureInfo language);
}