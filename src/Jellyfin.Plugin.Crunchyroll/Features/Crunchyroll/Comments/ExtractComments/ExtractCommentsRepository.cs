using System;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Common.Persistence;
using Jellyfin.Plugin.Crunchyroll.Domain.Constants;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Comments.Entites;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Comments.ExtractComments;

public class ExtractCommentsRepository : IExtractCommentsRepository
{
    private readonly CrunchyrollDbContext _dbContext;
    private readonly ILogger<ExtractCommentsRepository> _logger;

    public ExtractCommentsRepository(CrunchyrollDbContext dbContext, ILogger<ExtractCommentsRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result> AddCommentsAsync(EpisodeComments comments, CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.Comments.AddAsync(comments, cancellationToken);
            return Result.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to add comments {@Comments}", comments);
            return Result.Fail(ErrorCodes.Internal);
        }
    }

    public async Task<Result<bool>> CommentsForEpisodeExistsAsync(string crunchyrollEpisodeId, CancellationToken cancellationToken)
    {
        try
        {
            return await _dbContext.Comments
                .AnyAsync(x => x.CrunchyrollEpisodeId == crunchyrollEpisodeId, 
                    cancellationToken: cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to check existing comments for crunchyroll episode id {CrunchyrollEpisodeId}", 
                crunchyrollEpisodeId);
            return Result.Fail(ErrorCodes.Internal);
        }
    }
    
    public async Task<Result> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Result.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to save changes for comments");
            return Result.Fail(ErrorCodes.Internal);
        }
    }
}