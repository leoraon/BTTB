namespace CMMS.Backend.Helpers;

public class PmSchedulerSettings
{
    public bool Enabled { get; set; } = true;
    public bool RunOnStartup { get; set; } = true;
    public int IntervalMinutes { get; set; } = 60;
}
