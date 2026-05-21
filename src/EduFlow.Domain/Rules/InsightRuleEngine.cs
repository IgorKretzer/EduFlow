namespace EduFlow.Domain.Rules;

public sealed class InsightRuleEngine
{
    private readonly IReadOnlyList<InsightRule> _rules;

    public InsightRuleEngine()
    {
        _rules = BuildDefaultRules();
    }

    public IReadOnlyList<GeneratedInsight> Evaluate(InsightRuleContext context)
    {
        var results = new List<GeneratedInsight>();

        foreach (var rule in _rules)
        {
            if (!rule.Condition(context))
                continue;

            results.Add(new GeneratedInsight
            {
                Code = rule.Code,
                Severity = rule.Severity,
                Title = rule.TitleTemplate,
                Message = rule.MessageFactory(context),
                RecommendedAction = rule.RecommendedActionFactory?.Invoke(context),
                DeepLink = rule.DeepLinkFactory?.Invoke(context),
                Category = rule.Category,
                GeneratedAt = DateTime.UtcNow
            });
        }

        return results;
    }

    private static IReadOnlyList<InsightRule> BuildDefaultRules() =>
    [
        new InsightRule
        {
            Code = "delinquency_increase",
            TitleTemplate = "Aumento de inadimplência",
            Severity = "warning",
            Condition = ctx =>
                ctx.MetricKey == "delinquency_rate"
                && ctx.PreviousValue > 0
                && ctx.CurrentValue > ctx.PreviousValue * 1.1m,
            MessageFactory = ctx =>
                $"{FormatUnitLabel(ctx.UnitName, ctx.UnitCode)} apresentou aumento de inadimplência " +
                $"({ctx.PreviousValue:P1} → {ctx.CurrentValue:P1}).",
            RecommendedActionFactory = ctx =>
                $"Priorize contato com famílias inadimplentes em {FormatUnitLabel(ctx.UnitName, ctx.UnitCode)} e revise planos de negociação.",
            DeepLinkFactory = _ => "/financeiro",
            Category = "financeiro"
        },
        new InsightRule
        {
            Code = "churn_spike",
            TitleTemplate = "Pico de evasão",
            Severity = "critical",
            Condition = ctx =>
                ctx.MetricKey == "churn_rate"
                && ctx.CurrentValue > ctx.PreviousValue * 1.2m
                && ctx.CurrentValue > 0.05m,
            MessageFactory = ctx =>
                $"{FormatUnitLabel(ctx.UnitName, ctx.UnitCode)}: taxa de evasão subiu para {ctx.CurrentValue:P1}.",
            RecommendedActionFactory = _ =>
                "Revise matrículas em risco e acione equipe pedagógica para retenção nas próximas duas semanas.",
            DeepLinkFactory = _ => "/risk-enrollments",
            Category = "evasao"
        },
        new InsightRule
        {
            Code = "revenue_drop",
            TitleTemplate = "Queda de receita",
            Severity = "warning",
            Condition = ctx =>
                ctx.MetricKey == "revenue"
                && ctx.PreviousValue > 0
                && ctx.CurrentValue < ctx.PreviousValue * 0.9m,
            MessageFactory = ctx =>
                $"Receita de {FormatUnitLabel(ctx.UnitName, ctx.UnitCode)} caiu mais de 10% no período.",
            RecommendedActionFactory = ctx =>
                $"Analise recebimentos e campanhas de cobrança em {FormatUnitLabel(ctx.UnitName, ctx.UnitCode)}.",
            DeepLinkFactory = _ => "/financeiro",
            Category = "financeiro"
        },
        new InsightRule
        {
            Code = "retention_improvement",
            TitleTemplate = "Melhoria de retenção",
            Severity = "success",
            Condition = ctx =>
                ctx.MetricKey == "retention_rate"
                && ctx.PreviousValue > 0
                && ctx.CurrentValue > ctx.PreviousValue * 1.02m,
            MessageFactory = ctx =>
                $"A retenção subiu para {ctx.CurrentValue:P1} (antes {ctx.PreviousValue:P1}).",
            RecommendedActionFactory = _ =>
                "Mantenha o engajamento com famílias e replique as práticas que estão funcionando nas unidades com melhor desempenho.",
            DeepLinkFactory = _ => "/retencao",
            Category = "retencao"
        },
        new InsightRule
        {
            Code = "evasion_risk",
            TitleTemplate = "Risco de evasão",
            Severity = "critical",
            Condition = ctx =>
                ctx.MetricKey == "delinquency_rate"
                && ctx.CurrentValue >= 0.5m,
            MessageFactory = ctx =>
                $"{FormatUnitLabel(ctx.UnitName, ctx.UnitCode)} com inadimplência elevada ({ctx.CurrentValue:P1}). " +
                "Há matrículas que precisam de acompanhamento imediato.",
            RecommendedActionFactory = _ =>
                "Revise a lista de matrículas em risco e acione a equipe pedagógica e financeira nesta semana.",
            DeepLinkFactory = _ => "/risk-enrollments",
            Category = "evasao"
        }
    ];

    private static string FormatUnitLabel(string? unitName, string? unitCode)
    {
        if (!string.IsNullOrWhiteSpace(unitName))
        {
            if (unitName.StartsWith("Unidade ", StringComparison.OrdinalIgnoreCase))
                return unitName;
            return $"Unidade {unitName}";
        }

        if (!string.IsNullOrWhiteSpace(unitCode))
        {
            if (unitCode.StartsWith("UN-", StringComparison.OrdinalIgnoreCase))
                return $"Unidade {unitCode[3..].Trim()}";
            return $"Unidade {unitCode}";
        }

        return "Unidade";
    }
}

public sealed class GeneratedInsight
{
    public required string Code { get; init; }
    public required string Title { get; init; }
    public required string Message { get; init; }
    public required string Severity { get; init; }
    public string? RecommendedAction { get; init; }
    public string? DeepLink { get; init; }
    public string? Category { get; init; }
    public DateTime GeneratedAt { get; init; }
}
