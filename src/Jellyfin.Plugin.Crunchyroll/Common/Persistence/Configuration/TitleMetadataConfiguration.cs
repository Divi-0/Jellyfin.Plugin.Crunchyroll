using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Plugin.Crunchyroll.Common.Persistence.Configuration;

public class TitleMetadataConfiguration : IEntityTypeConfiguration<TitleMetadata>
{
    public void Configure(EntityTypeBuilder<TitleMetadata> builder)
    {
        builder.ToTable("TitleMetadata");

        builder.Property(x => x.Id)
            .IsRequired();
        
        builder.Property(x => x.PosterTall)
            .HasColumnType("jsonb");
        
        builder.Property(x => x.PosterWide)
            .HasColumnType("jsonb");
        
        builder
            .HasMany(t => t.Seasons)
            .WithOne(s => s.Series)
            .HasForeignKey(e => e.SeriesId);
        
        builder.HasIndex(x => new { x.CrunchyrollId, x.Language });
    }
}