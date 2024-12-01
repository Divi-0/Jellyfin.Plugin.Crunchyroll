using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Plugin.Crunchyroll.Common.Persistence.Configuration;

public class SeasonConfiguration : IEntityTypeConfiguration<Season>
{
    public void Configure(EntityTypeBuilder<Season> builder)
    {
        builder.ToTable("Seasons");

        builder.Property(x => x.Id)
            .IsRequired();

        builder.Property(x => x.SeasonDisplayNumber)
            .HasDefaultValue(string.Empty);
        
        builder
            .HasMany(t => t.Episodes)
            .WithOne(s => s.Season)
            .HasForeignKey(e => e.SeasonId);
        
        builder.HasIndex(x => new { x.CrunchyrollId, x.Language });
    }
}