using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Common.Persistence;
using Jellyfin.Plugin.Crunchyroll.Domain;
using Jellyfin.Plugin.Crunchyroll.Domain.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.GetMetadata.SetMetadataToMovie;

public class SetMetadataToMovieRepository : ISetMetadataToMovieRepository
{
    private readonly CrunchyrollDbContext _dbContext;
    private readonly ILogger<SetMetadataToMovieRepository> _logger;

    public SetMetadataToMovieRepository(CrunchyrollDbContext dbContext,
        ILogger<SetMetadataToMovieRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }
    
    public async Task<Result<Domain.Entities.TitleMetadata?>> GetTitleMetadataAsync(CrunchyrollId seriesId, 
        CultureInfo language, CancellationToken cancellationToken)
    {
        try
        {
            return await _dbContext.TitleMetadata
                .AsNoTracking()
                .Include(x => x.Seasons)
                .ThenInclude(x => x.Episodes)
                .FirstOrDefaultAsync(x => 
                        x.CrunchyrollId == seriesId.ToString() &&
                        x.Language == language.Name,
                    cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "unknown database error");
            return Result.Fail(ErrorCodes.Internal);
        }
    }
}