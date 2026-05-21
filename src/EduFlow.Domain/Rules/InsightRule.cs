namespace EduFlow.Domain.Rules;

/// <summary>
/// Motor de regras (não-IA): avalia métricas e gera insights executivos.
/// </summary>
public sealed class InsightRule
{
    public required string Code { get; init; }
    public required string TitleTemplate { get; init; }
    public required Func<InsightRuleContext, bool> Condition { get; init; }
    public required Func<InsightRuleContext, string> MessageFactory { get; init; }
    public Func<InsightRuleContext, string>? RecommendedActionFactory { get; init; }
    public Func<InsightRuleContext, string>? DeepLinkFactory { get; init; }
    public string? Category { get; init; }
    public string Severity { get; init; } = "info";
}

public sealed class InsightRuleContext
{
    public Guid TenantId { get; init; }
    public string? UnitCode { get; init; }
    public string? UnitName { get; init; }
    public decimal CurrentValue { get; init; }
    public decimal PreviousValue { get; init; }
    public string MetricKey { get; init; } = string.Empty;
}
