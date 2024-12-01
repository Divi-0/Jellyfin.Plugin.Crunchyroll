using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Comments.Entites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Plugin.Crunchyroll.Common.Persistence.Configuration;

public class EpisodeCommentsConfiguration : IEntityTypeConfiguration<EpisodeComments>
{
    public void Configure(EntityTypeBuilder<EpisodeComments> builder)
    {
        builder.ToTable("EpisodeComments");

        builder.Property(x => x.Id)
            .IsRequired();

        builder.Property(x => x.Comments)
            .HasColumnType("jsonb");

        builder.HasIndex(x => new { x.CrunchyrollEpisodeId, x.Language });
    }
}