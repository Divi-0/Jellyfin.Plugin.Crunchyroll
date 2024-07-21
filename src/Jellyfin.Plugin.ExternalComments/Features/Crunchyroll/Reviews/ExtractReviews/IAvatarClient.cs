using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Reviews.ExtractReviews;

public interface IAvatarClient
{
    public Task<Result<Stream>> GetAvatarStreamAsync(string uri, CancellationToken cancellationToken);
}