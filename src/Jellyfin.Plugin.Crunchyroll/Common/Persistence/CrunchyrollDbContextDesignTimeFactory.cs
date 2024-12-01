using Microsoft.EntityFrameworkCore.Design;

namespace Jellyfin.Plugin.Crunchyroll.Common.Persistence;

public class CrunchyrollDbContextDesignTimeFactory : IDesignTimeDbContextFactory<CrunchyrollDbContext>
{
    public CrunchyrollDbContext CreateDbContext(string[] args)
    {
        return new CrunchyrollDbContext();
    }
}