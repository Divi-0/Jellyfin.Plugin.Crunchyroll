using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Common.Persistence;
using Jellyfin.Plugin.Crunchyroll.Domain;
using Jellyfin.Plugin.Crunchyroll.Domain.Constants;
using Jellyfin.Plugin.Crunchyroll.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.ImageProvider.Movie.GetMovieImageInfos;

public class GetMovieImageInfosRepository : IGetMovieImageInfosRepository
{
    private readonly CrunchyrollDbContext _dbContext;
    private readonly ILogger<GetMovieImageInfosRepository> _logger;

    private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions(); 

    public GetMovieImageInfosRepository(CrunchyrollDbContext dbContext,
        ILogger<GetMovieImageInfosRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }
    
    public async Task<Result<ImageSource?>> GetEpisodeThumbnailAsync(CrunchyrollId episodeId, CancellationToken cancellationToken)
    {
        try
        {
            return await _dbContext.Episodes
                .AsNoTracking()
                .Where(x => x.CrunchyrollId == episodeId.ToString())
                .Select(x => JsonSerializer.Deserialize<ImageSource>(x.Thumbnail, _jsonSerializerOptions))
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "db error, failed to get episode thumbnail");
            return Result.Fail(ErrorCodes.Internal);
        }
    }
}