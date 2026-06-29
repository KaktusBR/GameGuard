using System.Runtime.Versioning;
using GameGuard.Core;

namespace GameGuard.Service;

[SupportedOSPlatform("windows")]
public class Worker : BackgroundService
{
    private static readonly Func<DateTimeOffset> Clock = () => DateTimeOffset.Now;

    private readonly ILogger<Worker> _logger;
    private readonly LockManager _lockManager;
    private readonly ProcessScanner _scanner;
    private readonly HostsFileApplier _hosts;
    private readonly PipeServer _pipeServer;
    private bool _hostsBlocked = true; // assume locked → blocked on start

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
        _lockManager = new LockManager(Clock);
        var handler = new UnlockHandler(_lockManager, LoadConfig, Clock);
        _scanner = new ProcessScanner(_lockManager, LoadConfig,
            new SystemProcessProvider(), new SystemProcessKiller());
        _hosts = new HostsFileApplier(Constants.HostsPath);
        _pipeServer = new PipeServer(handler, Constants.PipeName);
    }

    private GameGuardConfig LoadConfig() => ConfigStore.Load(Constants.ConfigPath);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _ = _pipeServer.RunAsync(stoppingToken)
            .ContinueWith(t => _logger.LogError(t.Exception, "GameGuard pipe server faulted"),
                TaskContinuationOptions.OnlyOnFaulted);
        SyncHosts(forced: true);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _scanner.ScanAndEnforce();
                SyncHosts(forced: false);
            }
            catch (Exception ex) { _logger.LogError(ex, "scan loop error"); }
            await Task.Delay(Constants.ScanInterval, stoppingToken);
        }
    }

    private void SyncHosts(bool forced)
    {
        var shouldBlock = _lockManager.IsLocked;
        if (!forced && shouldBlock == _hostsBlocked) return;
        try
        {
            if (shouldBlock) _hosts.ApplyBlock(LoadConfig().BlockedDomains);
            else _hosts.RemoveBlock();
            _hostsBlocked = shouldBlock;
        }
        catch (Exception ex) { _logger.LogError(ex, "hosts sync error"); }
    }
}
