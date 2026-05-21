namespace EduFlow.Application.DTOs;

public sealed record TriggerSyncRequest(
    string EntityType,
    DateOnly? FromDate);

public sealed record SyncJobStatusDto(
    Guid JobId,
    string EntityType,
    string Status,
    int RecordsProcessed,
    DateTime StartedAt,
    DateTime? CompletedAt,
    string? ErrorMessage);
