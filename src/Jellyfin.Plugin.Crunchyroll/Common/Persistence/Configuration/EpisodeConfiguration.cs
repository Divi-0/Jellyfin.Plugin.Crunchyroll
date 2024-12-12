using Jellyfin.Plugin.Crunchyroll.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Plugin.Crunchyroll.Common.Persistence.Configuration;

public class EpisodeConfiguration : IEntityTypeConfiguration<Episode>
{
    public void Configure(EntityTypeBuilder<Episode> builder)
    {
        builder.ToTable("Episodes");

        builder.Property(x => x.Id)
            .IsRequired();
        
        builder.HasIndex(x => new { x.CrunchyrollId, x.Language });
    }
}