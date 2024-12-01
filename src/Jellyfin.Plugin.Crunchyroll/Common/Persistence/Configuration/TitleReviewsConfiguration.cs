using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Plugin.Crunchyroll.Common.Persistence.Configuration;

public class TitleReviewsConfiguration : IEntityTypeConfiguration<TitleReviews>
{
    public void Configure(EntityTypeBuilder<TitleReviews> builder)
    {
        builder.Property(x => x.Id)
            .IsRequired();
        
        builder.Property(x => x.Reviews)
            .HasColumnType("jsonb");
        
        builder.HasIndex(x => new { x.CrunchyrollSeriesId, x.Language });
    }
}