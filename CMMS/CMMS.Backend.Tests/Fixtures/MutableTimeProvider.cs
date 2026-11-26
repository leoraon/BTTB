namespace CMMS.Backend.Tests.Fixtures;

public sealed class MutableTimeProvider(DateTime initialLocalDateTime) : TimeProvider
{
    private DateTimeOffset _local = ToLocalOffset(initialLocalDateTime);

    public override DateTimeOffset GetUtcNow() => _local.ToUniversalTime();
    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Local;

    public void SetLocalTime(DateTime localDateTime) => _local = ToLocalOffset(localDateTime);
    public void AdvanceDays(int days) => SetLocalTime(_local.DateTime.AddDays(days));
    public void AdvanceHours(double hours) => SetLocalTime(_local.DateTime.AddHours(hours));
    public void AdvanceMinutes(int minutes) => SetLocalTime(_local.DateTime.AddMinutes(minutes));

    private static DateTimeOffset ToLocalOffset(DateTime dt)
    {
        var unspecified = DateTime.SpecifyKind(dt, DateTimeKind.Unspecified);
        return new DateTimeOffset(unspecified, TimeZoneInfo.Local.GetUtcOffset(unspecified));
    }
}
