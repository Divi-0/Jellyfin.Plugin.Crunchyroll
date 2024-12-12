using System.IO;
using Jellyfin.Plugin.Crunchyroll.Domain.Entities;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Comments.Entites;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews.Entities;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Plugin.Crunchyroll.Common.Persistence;

public class CrunchyrollDbContext : DbContext
{
    public DbSet<TitleMetadata> TitleMetadata { get; set; }
    public DbSet<Season> Seasons { get; set; }
    public DbSet<Episode> Episodes { get; set; }
    public DbSet<EpisodeComments> Comments { get; set; }
    public DbSet<TitleReviews> Reviews { get; set; }

    public string DbPath { get; }

    public CrunchyrollDbContext()
    {
        var location = typeof(CrunchyrollDbContext).Assembly.Location;
        DbPath = Path.Combine(Path.GetDirectoryName(location)!, "Crunchyroll.db");
    }
    
    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={DbPath}");
}