namespace EduFlow.Domain.Entities;

/// <summary>
/// Modelo canônico financeiro. debtAmount unifica ValorDevedor, OpenAmount, etc.
/// </summary>
public sealed record CanonicalFinancial
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public required string ExternalId { get; init; }
    public required string EnrollmentCode { get; init; }
    public Guid UnitId { get; init; }
    /// <summary>CodigoUnidade Sponte (ex.: UN-RJ).</summary>
    public string UnitCode { get; init; } = "";
    public decimal DebtAmount { get; init; }
    public decimal PaidAmount { get; init; }
    public decimal InterestAmount { get; init; }
    public string PaymentStatus { get; init; } = "pending";
    public DateOnly DueDate { get; init; }
    /// <summary>receivable (Contas a receber) ou payable (Contas a pagar).</summary>
    public string FlowDirection { get; init; } = FinancialFlowDirection.Receivable;
    public string CategoryId { get; init; } = "";
    public string CategoryName { get; init; } = "";
    public string PaymentMethodLabel { get; init; } = "";
    public string CounterpartyName { get; init; } = "";
    /// <summary>Data usada no fluxo de caixa (pagamento se houver, senão vencimento).</summary>
    public DateOnly CashFlowDate { get; init; }
    public DateTime SyncedAt { get; init; } = DateTime.UtcNow;
}
