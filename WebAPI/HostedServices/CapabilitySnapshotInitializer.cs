using Core.Utilities.Authorization;

namespace WebAPI.HostedServices;

public sealed class CapabilitySnapshotInitializer : IHostedService
{
    private readonly ICapabilitySnapshot _snapshot;
    private readonly ILogger<CapabilitySnapshotInitializer> _logger;

    public CapabilitySnapshotInitializer(
        ICapabilitySnapshot snapshot,
        ILogger<CapabilitySnapshotInitializer> logger)
    {
        _snapshot = snapshot;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _snapshot.InitializeAsync(cancellationToken);
            _logger.LogInformation(
                "CapabilitySnapshot ready: {Count} entries, loaded at {LoadedAt:O}",
                _snapshot.Count, _snapshot.LoadedAt);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "CapabilitySnapshot initialization failed — app cannot start safely");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
