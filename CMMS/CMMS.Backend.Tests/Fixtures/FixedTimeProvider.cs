namespace CMMS.Backend.Tests.Fixtures;

public sealed class FixedTimeProvider(DateTime localDateTime) : TimeProvider
{
    private readonly DateTimeOffset _local = new(
        DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified),
        TimeZoneInfo.Local.GetUtcOffset(localDateTime));

    public override DateTimeOffset GetUtcNow() => _local.ToUniversalTime();
    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Local;
}
