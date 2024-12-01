using System.Threading;
using System.Threading.Tasks;
using FluentResults;

namespace Jellyfin.Plugin.Crunchyroll.Common.Persistence;

public interface ISaveChanges
{
    public Task<Result> SaveChangesAsync(CancellationToken cancellationToken);
}