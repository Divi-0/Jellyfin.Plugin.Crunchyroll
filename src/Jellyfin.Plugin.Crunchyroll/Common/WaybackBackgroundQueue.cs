using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Common;

/// <summary>
/// Runs Wayback Machine review and comment scraping in a bounded background queue so library scans stay fast.
/// </summary>
public sealed class WaybackBackgroundQueue : IDisposable
{
    private const int MaxConcurrency = 2;
    public static readonly DateTime CrunchyrollCommentsDeletedDate = new(2024, 7, 10, 0, 0, 0, DateTimeKind.Utc);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WaybackBackgroundQueue> _logger;
    private readonly Channel<Func<IServiceProvider, CancellationToken, Task>> _channel =
        Channel.CreateUnbounded<Func<IServiceProvider, CancellationToken, Task>>();
    private readonly CancellationTokenSource _shutdownCts = new();
    private readonly object _idleLock = new();
    private int _activeCount;
    private TaskCompletionSource _idleTcs;

    public WaybackBackgroundQueue(IServiceScopeFactory scopeFactory, ILogger<WaybackBackgroundQueue> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _idleTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _idleTcs.SetResult();

        for (var i = 0; i < MaxConcurrency; i++)
        {
            _ = Task.Run(ProcessQueueAsync);
        }
    }

    public void EnqueueIfEligible(
        bool isFeatureEnabled,
        DateTime? premiereDate,
        Func<IServiceProvider, CancellationToken, Task> work)
    {
        if (!isFeatureEnabled)
        {
            return;
        }

        if (premiereDate.HasValue && premiereDate.Value > CrunchyrollCommentsDeletedDate)
        {
            return;
        }

        Enqueue(work);
    }

    public void Enqueue(Func<IServiceProvider, CancellationToken, Task> work)
    {
        lock (_idleLock)
        {
            if (_activeCount == 0)
            {
                _idleTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            _activeCount++;
        }

        if (!_channel.Writer.TryWrite(work))
        {
            MarkItemCompleted();
        }
    }

    public Task WaitForIdleAsync(CancellationToken cancellationToken = default)
    {
        Task idleTask;
        lock (_idleLock)
        {
            idleTask = _activeCount == 0 ? Task.CompletedTask : _idleTcs.Task;
        }

        return idleTask.WaitAsync(cancellationToken);
    }

    private async Task ProcessQueueAsync()
    {
        await foreach (var work in _channel.Reader.ReadAllAsync(_shutdownCts.Token))
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                await work(scope.ServiceProvider, _shutdownCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (_shutdownCts.IsCancellationRequested)
            {
                break;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Background Wayback Machine extraction failed");
            }
            finally
            {
                MarkItemCompleted();
            }
        }
    }

    private void MarkItemCompleted()
    {
        lock (_idleLock)
        {
            if (_activeCount > 0 && --_activeCount == 0)
            {
                _idleTcs.TrySetResult();
            }
        }
    }

    public void Dispose()
    {
        _channel.Writer.TryComplete();
        _shutdownCts.Cancel();
        _shutdownCts.Dispose();
    }
}
