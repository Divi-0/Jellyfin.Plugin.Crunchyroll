using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Common.Persistence;
using Jellyfin.Plugin.Crunchyroll.Contracts.Comments;
using Jellyfin.Plugin.Crunchyroll.Domain.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Comments.GetComments;

public class GetCommentsRepository : IGetCommentsRepository
{
    private readonly CrunchyrollDbContext _dbContext;
    private readonly ILogger<GetCommentsRepository> _logger;

    public GetCommentsRepository(CrunchyrollDbContext dbContext, ILogger<GetCommentsRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }
    
    public async Task<Result<IReadOnlyList<CommentItem>?>> GetCommentsAsync(string crunchyrollEpisodeId, int pageSize, 
        int pageNumber, CultureInfo language, CancellationToken cancellationToken)
    {
        try
        {
            var comments = await _dbContext.Comments
                .Where(x => 
                    x.CrunchyrollEpisodeId == crunchyrollEpisodeId &&
                    x.Language == language.Name)
                .Select(x => x.Comments)
                .FirstOrDefaultAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(comments))
            {
                return Result.Ok<IReadOnlyList<CommentItem>?>(null);
            }

            var array = JsonSerializer.Deserialize<IReadOnlyList<CommentItem>>(comments)!;
            
            return array
                .Skip(pageSize * (pageNumber - 1))
                .Take(pageSize)
                .ToList() ?? [];

        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to get comments for crunchyroll episodeId {EpisodeId}", 
                crunchyrollEpisodeId);
            return Result.Fail(ErrorCodes.Internal);
        }
    }
}