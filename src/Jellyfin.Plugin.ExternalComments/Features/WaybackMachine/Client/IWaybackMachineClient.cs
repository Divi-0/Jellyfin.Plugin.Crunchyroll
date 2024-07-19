using System;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.ExternalComments.Features.WaybackMachine.Client.Dto;

namespace Jellyfin.Plugin.ExternalComments.Features.WaybackMachine.Client;

public interface IWaybackMachineClient
{
    public Task<Result<AvailabilityResponse>> GetAvailabilityAsync(string url, DateTime timestamp, CancellationToken cancellationToken = default);
}