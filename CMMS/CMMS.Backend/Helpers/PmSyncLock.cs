namespace CMMS.Backend.Helpers;

public sealed class PmSyncLock
{
    public SemaphoreSlim Gate { get; } = new(1, 1);
}
