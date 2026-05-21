namespace EduFlow.Application.Options;

public sealed class SyncSchedulerSettings
{
    public const string SectionName = "SyncScheduler";

    public bool Enabled { get; set; } = true;
    public int PollIntervalSeconds { get; set; } = 60;
    public int DefaultIntervalMinutes { get; set; } = 15;
    public string[] EntityTypes { get; set; } = ["students", "financial", "contracts"];
}
