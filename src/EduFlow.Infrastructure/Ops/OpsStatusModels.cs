namespace EduFlow.Infrastructure.Ops;

public sealed record OpsStatusResponse(
    DateTime CheckedAtUtc,
    IReadOnlyList<OpsComponentStatus> Components,
    IReadOnlyList<OpsQueueStatus> Queues,
    OpsPipelineInfo? Pipeline,
    IReadOnlyList<OpsExternalLink> Links);

public sealed record OpsComponentStatus(
    string Id,
    string Name,
    string Status,
    string Detail);

public sealed record OpsQueueStatus(
    string Name,
    bool Exists,
    uint Messages,
    uint Consumers);

public sealed record OpsPipelineInfo(
    bool SchedulerEnabled,
    int PollIntervalSeconds,
    DateTime? LastSyncAtUtc,
    string? LastSyncStatus,
    string? LastSyncMessage);

public sealed record OpsExternalLink(string Label, string Url);
