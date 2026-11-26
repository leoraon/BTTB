using CMMS.Backend.Helpers;
using CMMS.Backend.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace CMMS.Backend.Services.Background;

/// <summary>
/// Dịch vụ chạy nền tự động rà soát các kế hoạch bảo trì định kỳ đến hạn khi khởi động
/// và định kỳ theo cấu hình PmSchedulerSettings.
/// </summary>
public sealed class PmAutoSyncBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<PmSchedulerSettings> options,
    TimeProvider timeProvider,
    ILogger<PmAutoSyncBackgroundService> logger) : BackgroundService
{
    private readonly PmSchedulerSettings _settings = options.Value;

    public async Task<int> RunOnceAsync(CancellationToken ct = default)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var keHoachSvc = scope.ServiceProvider.GetRequiredService<IKeHoachService>();
            var res = await keHoachSvc.SyncWorkOrdersAsync(null);
            int created = res.Data?.SoLenhMoiSinh ?? 0;
            if (created > 0)
            {
                logger.LogInformation(
                    "PM Auto-Sync đã tự động sinh {Count} lệnh bảo trì định kỳ: {Ids}",
                    created,
                    string.Join(", ", res.Data!.DanhSachMaLenhMoi));
            }
            return created;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi khi chạy PM Auto-Sync nền.");
            return 0;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled) return;

        await Task.Yield();

        if (_settings.RunOnStartup)
            await RunOnceAsync(stoppingToken);

        var interval = TimeSpan.FromMinutes(Math.Max(1, _settings.IntervalMinutes));
        using var timer = new PeriodicTimer(interval, timeProvider);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunOnceAsync(stoppingToken);
        }
    }
}
