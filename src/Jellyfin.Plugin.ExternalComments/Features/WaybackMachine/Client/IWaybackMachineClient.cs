using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.ExternalComments.Features.WaybackMachine.Client.Dto;

namespace Jellyfin.Plugin.ExternalComments.Features.WaybackMachine.Client;

public interface IWaybackMachineClient
{
    /// <summary>
    /// Searches for the newest snapshot before the provided timestamp
    /// </summary>
    /// <param name="url">url to serach for</param>
    /// <param name="timestamp">result will be filtered to the first snapshot that is before this timestamp</param>
    /// <param name="cancellationToken">cancellationToken</param>
    /// <returns></returns>
    public Task<Result<IReadOnlyList<SearchResponse>>> SearchAsync(string url, DateTime timestamp, CancellationToken cancellationToken = default);
}