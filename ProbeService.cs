using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FastSTRM;

/// <summary>
/// Background service that provides library-wide .strm pre-probing
/// and automatic probing of newly added items.
/// </summary>
public sealed class ProbeService : IHostedService, IDisposable
{
    private readonly ILibraryManager _libraryManager;
    private readonly IDirectoryService _directoryService;
    private readonly ILogger<ProbeService> _logger;

    private CancellationTokenSource? _cts;
    private Task? _probeTask;
    private readonly object _lock = new();

    /// <summary>Gets the current task state: Idle, Running, or Stopping.</summary>
    public string State { get; private set; } = "Idle";

    /// <summary>Gets the total number of items in the current probe batch.</summary>
    public int TotalItems { get; private set; }

    /// <summary>Gets the number of items already processed.</summary>
    public int ProcessedItems { get; private set; }

    /// <summary>Gets the name of the item currently being probed.</summary>
    public string CurrentItemName { get; private set; } = string.Empty;

    public ProbeService(
        ILibraryManager libraryManager,
        IDirectoryService directoryService,
        ILogger<ProbeService> logger)
    {
        _libraryManager = libraryManager;
        _directoryService = directoryService;
        _logger = logger;
    }

    // ── IHostedService lifecycle ──

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _libraryManager.ItemAdded += OnItemAdded;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _libraryManager.ItemAdded -= OnItemAdded;
        StopProbe();
        return Task.CompletedTask;
    }

    // ── Auto-probe on item added ──

    private void OnItemAdded(object? sender, ItemChangeEventArgs e)
    {
        var config = Plugin.Instance?.Configuration;
        if (config is not { EnableAutoProbe: true })
        {
            return;
        }

        var item = e.Item;
        if (!IsStrmItem(item) || HasStreamInfo(item))
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                _logger.LogInformation("FastSTRM: Auto-probing new item {Name}", item.Name);
                await ProbeItemAsync(item, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FastSTRM: Auto-probe failed for {Name}", item.Name);
            }
        });
    }

    // ── Public API ──

    /// <summary>
    /// Returns a summary of .strm items in the library.
    /// </summary>
    public (int Total, int Probed, int Unprobed) GetSummary()
    {
        var items = GetStrmItems();
        int probed = items.Count(HasStreamInfo);
        return (items.Count, probed, items.Count - probed);
    }

    /// <summary>
    /// Starts a probe task. Returns false if one is already running.
    /// </summary>
    /// <param name="mode">"Incremental" or "Full".</param>
    public bool StartProbe(string mode)
    {
        lock (_lock)
        {
            if (State == "Running")
            {
                return false;
            }

            var items = GetStrmItems();
            if (string.Equals(mode, "Incremental", StringComparison.OrdinalIgnoreCase))
            {
                items = items.Where(i => !HasStreamInfo(i)).ToList();
            }

            TotalItems = items.Count;
            ProcessedItems = 0;
            CurrentItemName = string.Empty;
            State = "Running";

            _cts = new CancellationTokenSource();
            var ct = _cts.Token;
            _probeTask = Task.Run(() => RunProbeAsync(items, ct));
            return true;
        }
    }

    /// <summary>
    /// Requests cancellation of the running probe task.
    /// </summary>
    public void StopProbe()
    {
        lock (_lock)
        {
            if (_cts is { IsCancellationRequested: false })
            {
                State = "Stopping";
                _cts.Cancel();
            }
        }
    }

    // ── Internal ──

    private async Task RunProbeAsync(List<BaseItem> items, CancellationToken ct)
    {
        try
        {
            foreach (var item in items)
            {
                if (ct.IsCancellationRequested)
                {
                    break;
                }

                CurrentItemName = item.Name ?? item.Path ?? string.Empty;
                _logger.LogInformation("FastSTRM: Probing [{Index}/{Total}] {Name}",
                    ProcessedItems + 1, TotalItems, CurrentItemName);

                try
                {
                    await ProbeItemAsync(item, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "FastSTRM: Probe failed for {Name}, skipping", CurrentItemName);
                }

                ProcessedItems++;
            }
        }
        finally
        {
            lock (_lock)
            {
                State = "Idle";
                CurrentItemName = string.Empty;
                _cts?.Dispose();
                _cts = null;
            }
        }
    }

    /// <summary>
    /// Triggers the official Jellyfin metadata refresh with remote content probing
    /// for a single item. This writes real probe data into the database.
    /// </summary>
    private async Task ProbeItemAsync(BaseItem item, CancellationToken ct)
    {
        await item.RefreshMetadata(
            new MetadataRefreshOptions(_directoryService)
            {
                EnableRemoteContentProbe = true,
                MetadataRefreshMode = MetadataRefreshMode.FullRefresh
            },
            ct).ConfigureAwait(false);
    }

    private List<BaseItem> GetStrmItems()
    {
        return _libraryManager
            .GetItemList(new InternalItemsQuery
            {
                MediaTypes = new[] { MediaType.Video },
                Recursive = true
            })
            .Where(IsStrmItem)
            .ToList();
    }

    private static bool IsStrmItem(BaseItem item)
        => item.Path is not null
           && item.Path.EndsWith(".strm", StringComparison.OrdinalIgnoreCase);

    private static bool HasStreamInfo(BaseItem item)
        => item.RunTimeTicks.HasValue && item.RunTimeTicks.Value > 0;

    public void Dispose()
    {
        _cts?.Dispose();
    }
}
