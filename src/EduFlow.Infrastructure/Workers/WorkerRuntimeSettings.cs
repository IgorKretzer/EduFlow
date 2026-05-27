namespace EduFlow.Infrastructure.Workers;

public sealed class WorkerRuntimeSettings
{
    public const string SectionName = "Workers";

    public bool EnableStudents { get; set; } = true;
    public bool EnableFinancial { get; set; } = true;
    public bool EnableContracts { get; set; } = true;
    public bool EnableAnalytics { get; set; } = true;
    public bool EnableScheduler { get; set; } = true;
}
