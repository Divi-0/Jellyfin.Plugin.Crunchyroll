using System.Globalization;
using System.Text.Json;
using AutoFixture;
using FluentAssertions;
using Jellyfin.Plugin.Crunchyroll.Common.Persistence;
using Jellyfin.Plugin.Crunchyroll.Contracts.Comments;
using Jellyfin.Plugin.Crunchyroll.Contracts.Reviews;
using Jellyfin.Plugin.Crunchyroll.Domain.Entities;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Avatar;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Comments.Entites;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.GetMetadata.ScrapSeriesMetadata.Client.Dtos;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews.Entities;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Integration.Shared.MockData;

public static class DatabaseMockHelper
{
    public static async Task<IReadOnlyList<ReviewItem>> InsertRandomReviews(string titleId)
    {
        var fixture = new Fixture();

        await using var dbContext = new CrunchyrollDbContext();

        var reviews = fixture.CreateMany<ReviewItem>().ToList();

        var entity = new TitleReviews
        {
            CrunchyrollSeriesId = titleId,
            Reviews = JsonSerializer.Serialize(reviews),
            Language = "en-US"
        };

        await dbContext.Reviews.AddAsync(entity);
        await dbContext.SaveChangesAsync();

        return reviews;
    }
    
    public static async Task<IReadOnlyList<CommentItem>> InsertRandomComments(string episodeId)
    {
        var fixture = new Fixture();

        await using var dbContext = new CrunchyrollDbContext();

        var comments = fixture.CreateMany<CommentItem>().ToArray();

        var entity = new EpisodeComments
        {
            CrunchyrollEpisodeId = episodeId,
            Comments = JsonSerializer.Serialize(comments),
            Language = "en-US"
        };

        await dbContext.Comments.AddAsync(entity);
        await dbContext.SaveChangesAsync();

        return comments;
    }
    
    public static void ShouldHaveReviews(string titleId)
    {
        using var dbContext = new CrunchyrollDbContext();
        
        var hasReviews = dbContext.Reviews.Any(x => x.CrunchyrollSeriesId == titleId);

        hasReviews.Should().BeTrue();
    }
    
    public static void ShouldHaveComments(string episodeId)
    {
        using var dbContext = new CrunchyrollDbContext();
        
        var hasComments = dbContext.Comments.Any(x => x.CrunchyrollEpisodeId == episodeId);

        hasComments.Should().BeTrue();
    }
    
    public static void ShouldHaveAvatarUri(string uri)
    {
        var directoryPath = Path.Combine(
            Path.GetDirectoryName(typeof(AvatarRepository).Assembly.Location)!,
            "avatar-images");

        var fileName = Path.GetFileName(uri);
        var filePath = Path.Combine(directoryPath, fileName);
        var hasAvatarUri = File.Exists(filePath);
        
        hasAvatarUri.Should().BeTrue();
    }
    
    public static void InsertAvatarImage(string imageUrl, Stream imageStream)
    {
        var directoryPath = Path.Combine(
            Path.GetDirectoryName(typeof(AvatarRepository).Assembly.Location)!,
            "avatar-images");

        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        var fileName = Path.GetFileName(imageUrl);
        var filePath = Path.Combine(directoryPath, fileName);
        using var fileStream = File.Create(filePath);
        imageStream.CopyTo(fileStream);
    }
    
    public static void ShouldHaveMetadata(string titleId, CrunchyrollSeriesContentItem seriesContentResponse, 
        CrunchyrollSeriesRatingResponse ratingResponse)
    {
        using var dbContext = new CrunchyrollDbContext();

        var metadata = dbContext.TitleMetadata
            .Include(x => x.Seasons)
            .ThenInclude(x => x.Episodes)
            .Single(x => x.CrunchyrollId == titleId);

        metadata.Should().NotBeNull();
        metadata.CrunchyrollId.Should().NotBeEmpty();
        metadata.SlugTitle.Should().Be(seriesContentResponse.SlugTitle);
        metadata.Title.Should().Be(seriesContentResponse.Title);
        metadata.Description.Should().Be(seriesContentResponse.Description);
        metadata.Studio.Should().Be(seriesContentResponse.ContentProvider);
        metadata.Rating.Should().Be(float.Parse(ratingResponse.Average, CultureInfo.InvariantCulture));
        var posteTall = JsonSerializer.Deserialize<ImageSource>(metadata.PosterTall)!;
        var posteWide = JsonSerializer.Deserialize<ImageSource>(metadata.PosterWide)!;
        posteTall.Uri.Should().Be(seriesContentResponse.Images.PosterTall.First().Last().Source);
        posteWide.Uri.Should().Be(seriesContentResponse.Images.PosterWide.First().Last().Source);
        posteTall.Height.Should().Be(seriesContentResponse.Images.PosterTall.First().Last().Height);
        posteWide.Height.Should().Be(seriesContentResponse.Images.PosterWide.First().Last().Height);
        posteTall.Width.Should().Be(seriesContentResponse.Images.PosterTall.First().Last().Width);
        posteWide.Width.Should().Be(seriesContentResponse.Images.PosterWide.First().Last().Width);
        metadata.Seasons.Should().NotBeEmpty();
        metadata.Seasons.Should().AllSatisfy(x =>
        {
            x.Id.Should().NotBeEmpty();
            x.Title.Should().NotBeEmpty();
            x.SlugTitle.Should().NotBeEmpty();
            x.Episodes.Should().NotBeEmpty();
            x.Episodes.Should().AllSatisfy(episode =>
            {
               episode.Id.Should().NotBeEmpty(); 
               episode.Title.Should().NotBeEmpty(); 
               episode.SlugTitle.Should().NotBeEmpty(); 
               episode.Description.Should().NotBeEmpty(); 
            });
        });
    }
}