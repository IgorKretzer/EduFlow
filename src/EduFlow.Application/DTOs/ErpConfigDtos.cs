namespace EduFlow.Application.DTOs;

public sealed record ErpConfigDto(
    string ProviderKey,
    string EndpointUrl,
    string Username,
    bool HasPassword,
    int PageSize,
    bool SyncEnabled,
    int SyncIntervalMinutes,
    DateTime? LastSyncAtUtc,
    string? LastSyncStatus,
    string? LastSyncMessage,
    string? SearchParametersStudents,
    string? SearchParametersFinancial,
    string? SearchParametersContracts);

public sealed record UpdateErpConfigRequest(
    string? ProviderKey,
    string EndpointUrl,
    string Username,
    string? Password,
    int PageSize,
    bool SyncEnabled,
    int SyncIntervalMinutes,
    string? SearchParametersStudents,
    string? SearchParametersFinancial,
    string? SearchParametersContracts);

public sealed record SyncSchedulerStatusDto(
    bool SchedulerEnabled,
    int PollIntervalSeconds,
    int DefaultIntervalMinutes);
