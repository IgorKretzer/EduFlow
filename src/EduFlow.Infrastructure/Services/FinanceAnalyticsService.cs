using EduFlow.Application.DTOs;
using EduFlow.Application.Interfaces;
using EduFlow.Domain.Entities;
using EduFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EduFlow.Infrastructure.Services;

public sealed class FinanceAnalyticsService : IFinanceAnalyticsService
{
    private readonly StagingDbContext _db;
    private const string BasisCash = "cash";
    private const string BasisDue = "due";
    private const string BasisCompetence = "competence";

    public FinanceAnalyticsService(StagingDbContext db) => _db = db;

    public async Task<FinanceReceivablesDto> GetReceivablesAsync(
        Guid tenantId,
        DashboardFilterDto filter,
        CancellationToken ct = default)
    {
        var lines = await LoadReceivableLinesAsync(tenantId, filter, ct);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var open = lines.Where(l => l.DebtAmount > 0).ToList();
        var overdue = open.Where(l => l.DueDate < today).ToList();
        var toMature = open.Where(l => l.DueDate >= today).ToList();

        var overdueAmount = overdue.Sum(l => l.DebtAmount);
        var toMatureAmount = toMature.Sum(l => l.DebtAmount);
        var totalReceivable = overdueAmount + toMatureAmount;
        var totalPaid = lines.Sum(l => l.PaidAmount);
        var totalInvoice = lines.Sum(l => l.DebtAmount + l.PaidAmount);
        var totalInterest = lines.Sum(l => l.InterestAmount);

        var people = open
            .Select(l => l.EnrollmentCode)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        var overdueShare = totalReceivable > 0 ? overdueAmount / totalReceivable * 100m : 0m;

        var summary = new FinanceReceivablesSummaryDto(
            totalReceivable,
            overdueAmount,
            toMatureAmount,
            totalPaid,
            totalInvoice,
            totalInterest,
            people,
            overdueShare,
            100m - overdueShare);

        return new FinanceReceivablesDto(
            summary,
            BuildSlices(overdue),
            BuildSituations(overdue),
            BuildSlices(toMature),
            BuildSituations(toMature));
    }

    public async Task<FinanceCashFlowDto> GetCashFlowAsync(
        Guid tenantId,
        int year,
        int month,
        DashboardFilterDto filter,
        CancellationToken ct = default)
    {
        if (month is < 1 or > 12)
            throw new ArgumentOutOfRangeException(nameof(month), "Mês inválido.");

        var all = await LoadAllFinancialLinesAsync(tenantId, filter, ct);
        var periodStart = new DateOnly(year, month, 1);
        var periodEnd = periodStart.AddMonths(1).AddDays(-1);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var receivables = all.Where(IsReceivable).ToList();
        var payables = all.Where(IsPayable).ToList();

        decimal NetBefore(DateOnly date) =>
            receivables.Where(l => l.CashFlowDate < date && l.PaidAmount > 0).Sum(l => l.PaidAmount)
            - payables.Where(l => l.CashFlowDate < date && l.PaidAmount > 0).Sum(l => l.PaidAmount);

        var openingBalance = NetBefore(periodStart);

        var totalInflow = receivables
            .Where(l => l.CashFlowDate >= periodStart && l.CashFlowDate <= periodEnd && l.PaidAmount > 0)
            .Sum(l => l.PaidAmount);

        var totalOutflow = payables
            .Where(l => l.CashFlowDate >= periodStart && l.CashFlowDate <= periodEnd && l.PaidAmount > 0)
            .Sum(l => l.PaidAmount);

        var closingBalance = openingBalance + totalInflow - totalOutflow;

        var openDebt = receivables.Where(l => l.DebtAmount > 0).ToList();
        var totalOpen = openDebt.Sum(l => l.DebtAmount);
        var overdueOpen = openDebt.Where(l => l.DueDate < today).Sum(l => l.DebtAmount);
        var delinquencyPct = totalOpen > 0 ? overdueOpen / totalOpen * 100m : 0m;
        var totalInterest = receivables.Sum(l => l.InterestAmount);

        var inflowShare = totalInflow + totalOutflow > 0
            ? totalInflow / (totalInflow + totalOutflow) * 100m
            : 100m;

        var daysInMonth = DateTime.DaysInMonth(year, month);
        var days = new List<FinanceCashFlowDayDto>(daysInMonth);
        var running = openingBalance;

        for (var day = 1; day <= daysInMonth; day++)
        {
            var date = new DateOnly(year, month, day);
            var openingDay = running;

            var inflow = receivables
                .Where(l => l.CashFlowDate == date && l.PaidAmount > 0)
                .Sum(l => l.PaidAmount);

            var outflow = payables
                .Where(l => l.CashFlowDate == date && l.PaidAmount > 0)
                .Sum(l => l.PaidAmount);

            var operational = inflow - outflow;
            running = openingDay + operational;

            var scheduled = receivables
                .Where(l => l.DueDate == date && l.DebtAmount > 0)
                .Sum(l => l.DebtAmount);

            days.Add(new FinanceCashFlowDayDto(
                date.ToString("yyyy-MM-dd"),
                day,
                openingDay,
                inflow,
                outflow,
                operational,
                running,
                scheduled));
        }

        var inflowBreakdown = receivables
            .Where(l => l.CashFlowDate >= periodStart && l.CashFlowDate <= periodEnd && l.PaidAmount > 0)
            .GroupBy(l => SliceGroupKey(l), StringComparer.OrdinalIgnoreCase)
            .Select(g => new FinanceBreakdownDto(g.Key, g.Sum(x => x.PaidAmount)))
            .OrderByDescending(x => x.Amount)
            .ToList();

        var outflowBreakdown = payables
            .Where(l => l.CashFlowDate >= periodStart && l.CashFlowDate <= periodEnd && l.PaidAmount > 0)
            .GroupBy(l => SliceGroupKey(l), StringComparer.OrdinalIgnoreCase)
            .Select(g => new FinanceBreakdownDto(g.Key, g.Sum(x => x.PaidAmount)))
            .OrderByDescending(x => x.Amount)
            .ToList();

        var summary = new FinanceCashFlowSummaryDto(
            openingBalance,
            totalInflow,
            totalOutflow,
            closingBalance,
            delinquencyPct,
            totalInterest,
            inflowShare,
            100m - inflowShare,
            inflowBreakdown,
            outflowBreakdown);

        return new FinanceCashFlowDto(summary, days);
    }

    public async Task<FinancePulseDto> GetPulseAsync(
        Guid tenantId,
        int year,
        int month,
        string dataBasis,
        DashboardFilterDto filter,
        CancellationToken ct = default)
    {
        if (month is < 1 or > 12)
            throw new ArgumentOutOfRangeException(nameof(month), "Mês inválido.");

        var basis = NormalizeDataBasis(dataBasis);
        var gaps = new List<string>();
        if (basis == BasisCompetence)
        {
            gaps.Add(
                "GAP IDENTIFICADO: o banco atual ainda não possui campo de competência financeira. A visão de competência está usando vencimento como aproximação até a modelagem ser criada.");
        }

        var periodStart = new DateOnly(year, month, 1);
        var periodEnd = periodStart.AddMonths(1).AddDays(-1);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var period = await LoadFinancialLinesForPeriodAsync(
            tenantId,
            periodStart,
            periodEnd,
            basis,
            filter.UnitId,
            ct);
        var receivables = period.Where(IsReceivable).ToList();
        var payables = period.Where(IsPayable).ToList();

        var grossRevenue = receivables.Sum(l => l.DebtAmount + l.PaidAmount);
        var receivedRevenue = receivables.Sum(l => l.PaidAmount);
        var openReceivable = receivables.Sum(l => l.DebtAmount);
        var overdue = receivables
            .Where(l => l.DebtAmount > 0 && l.DueDate < today)
            .Sum(l => l.DebtAmount);
        var expectedOutflow = payables.Sum(l => l.DebtAmount + l.PaidAmount);
        var realizedOutflow = payables.Sum(l => l.PaidAmount);
        var interest = receivables.Sum(l => l.InterestAmount);
        var netRevenue = receivedRevenue - realizedOutflow;
        var projectedBalance = receivedRevenue + openReceivable - expectedOutflow;
        var delinquencyPct = openReceivable > 0 ? overdue / openReceivable * 100m : 0m;
        var receivedShare = grossRevenue > 0 ? receivedRevenue / grossRevenue * 100m : 0m;

        var health = BuildHealthStatus(delinquencyPct, projectedBalance);
        var metrics = new List<FinancePulseMetricDto>
        {
            Metric("gross_revenue", "Faturamento bruto", grossRevenue, "currency", "stable",
                "Total de parcelas a receber no período, somando recebido e em aberto."),
            Metric("received_revenue", "Receita recebida", receivedRevenue, "currency", "positive",
                $"{receivedShare:N1}% do faturamento bruto sincronizado já foi recebido."),
            Metric("net_revenue", "Faturamento líquido", netRevenue, "currency",
                netRevenue >= 0 ? "positive" : "critical",
                "Receita recebida menos saídas pagas identificadas."),
            Metric("open_receivable", "Em aberto", openReceivable, "currency",
                openReceivable > 0 ? "attention" : "positive",
                "Valor ainda pendente no período selecionado."),
            Metric("overdue", "Inadimplência", overdue, "currency",
                delinquencyPct >= 30 ? "critical" : delinquencyPct >= 15 ? "attention" : "positive",
                $"{delinquencyPct:N1}% da carteira aberta está vencida."),
            Metric("interest", "Juros atribuídos", interest, "currency",
                interest > 0 ? "attention" : "stable",
                "Juros/multa identificados na integração do ERP."),
            Metric("projected_balance", "Saldo projetado", projectedBalance, "currency",
                projectedBalance < 0 ? "critical" : "stable",
                "Recebido mais aberto, menos saídas previstas.")
        };

        var configuredGoals = await GetGoalsAsync(tenantId, year, month, filter.UnitId, ct);
        var revenueGoal = configuredGoals.FirstOrDefault(g => g.Key.Equals("monthly_revenue", StringComparison.OrdinalIgnoreCase));
        var goals = new List<FinancePulseGoalDto>
        {
            BuildPulseGoal(
                "monthly_revenue",
                revenueGoal?.Label ?? "Meta de receita mensal",
                revenueGoal?.TargetAmount,
                receivedRevenue)
        };
        var signals = BuildPulseSignals(
            overdue,
            delinquencyPct,
            projectedBalance,
            receivedShare,
            revenueGoal?.TargetAmount,
            receivedRevenue,
            periodStart,
            periodEnd);
        var calendar = BuildPulseCalendar(period, basis, year, month);
        var statement = new List<FinanceStatementLineDto>
        {
            new("gross_revenue", "Receita bruta", grossRevenue, "revenue", 10),
            new("received_revenue", "Receita recebida", receivedRevenue, "revenue", 20),
            new("revenue_goal", "Meta de receita", revenueGoal?.TargetAmount ?? 0m, "target", 25),
            new("open_receivable", "Receita em aberto", openReceivable, "neutral", 30),
            new("overdue", "Inadimplência vencida", overdue, "deduction", 40),
            new("interest", "Juros atribuídos", interest, "neutral", 45),
            new("expected_outflow", "Despesas previstas", expectedOutflow, "expense", 50),
            new("realized_outflow", "Despesas pagas", realizedOutflow, "expense", 60),
            new("net_revenue", "Resultado líquido", netRevenue, netRevenue >= 0 ? "result" : "loss", 70)
        };

        if (payables.Count == 0)
        {
            gaps.Add(
                "GAP IDENTIFICADO: não há contas a pagar no período. Se a Sponte não entregar despesas reais, o calendário e o balancete exibirão saídas zeradas até a integração retornar esses dados.");
        }

        return new FinancePulseDto(
            year,
            month,
            basis,
            health.Status,
            health.Message,
            metrics,
            signals,
            calendar,
            statement.OrderBy(s => s.Order).ToList(),
            goals,
            gaps);
    }

    public async Task<IReadOnlyList<FinanceGoalDto>> GetGoalsAsync(
        Guid tenantId,
        int year,
        int month,
        Guid? unitId,
        CancellationToken ct = default)
    {
        var query = _db.FinanceGoals.AsNoTracking()
            .Where(g => g.TenantId == tenantId && g.Year == year && g.Month == month);

        query = unitId is Guid id
            ? query.Where(g => g.UnitId == id)
            : query.Where(g => g.UnitId == null);

        return await query
            .OrderBy(g => g.Key)
            .Select(g => new FinanceGoalDto(
                g.Id,
                g.UnitId,
                g.Year,
                g.Month,
                g.Key,
                g.Label,
                g.TargetAmount,
                g.UpdatedAtUtc))
            .ToListAsync(ct);
    }

    public async Task<FinanceGoalDto> UpsertGoalAsync(
        Guid tenantId,
        string key,
        UpsertFinanceGoalRequest request,
        CancellationToken ct = default)
    {
        if (request.Month is < 1 or > 12)
            throw new ArgumentOutOfRangeException(nameof(request.Month), "Mês inválido.");
        if (request.Year is < 2000 or > 2100)
            throw new ArgumentOutOfRangeException(nameof(request.Year), "Ano inválido.");
        if (request.TargetAmount < 0)
            throw new ArgumentOutOfRangeException(nameof(request.TargetAmount), "Meta não pode ser negativa.");

        var normalizedKey = NormalizeGoalKey(key);
        var label = string.IsNullOrWhiteSpace(request.Label)
            ? DefaultGoalLabel(normalizedKey)
            : request.Label.Trim();

        var existing = await _db.FinanceGoals
            .FirstOrDefaultAsync(g =>
                g.TenantId == tenantId &&
                g.UnitId == request.UnitId &&
                g.Year == request.Year &&
                g.Month == request.Month &&
                g.Key == normalizedKey, ct);

        var now = DateTime.UtcNow;
        if (existing is null)
        {
            existing = new FinanceGoalEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UnitId = request.UnitId,
                Year = request.Year,
                Month = request.Month,
                Key = normalizedKey,
                Label = label,
                TargetAmount = request.TargetAmount,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            _db.FinanceGoals.Add(existing);
        }
        else
        {
            existing.Label = label;
            existing.TargetAmount = request.TargetAmount;
            existing.UpdatedAtUtc = now;
        }

        await _db.SaveChangesAsync(ct);
        return ToGoalDto(existing);
    }

    private async Task<List<CanonicalFinancial>> LoadReceivableLinesAsync(
        Guid tenantId,
        DashboardFilterDto filter,
        CancellationToken ct)
    {
        var all = await LoadAllFinancialLinesAsync(tenantId, filter, ct);
        return all.Where(IsReceivable).ToList();
    }

    private async Task<List<CanonicalFinancial>> LoadAllFinancialLinesAsync(
        Guid tenantId,
        DashboardFilterDto filter,
        CancellationToken ct)
    {
        var query = _db.Financials.AsNoTracking().Where(f => f.TenantId == tenantId);

        if (filter.UnitId is Guid unitId)
            query = query.Where(f => f.UnitId == unitId);

        if (filter.From is DateOnly from)
            query = query.Where(f => f.DueDate >= from);

        if (filter.To is DateOnly to)
            query = query.Where(f => f.DueDate <= to);

        var rows = await query.ToListAsync(ct);
        var categories = await _db.Categories.AsNoTracking()
            .Where(c => c.TenantId == tenantId)
            .ToListAsync(ct);
        var categoryByName = categories
            .Where(c => !string.IsNullOrWhiteSpace(c.Name))
            .GroupBy(c => c.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().ExternalId, StringComparer.OrdinalIgnoreCase);

        return rows.Select(r => EnrichCategoryMetadata(r, categoryByName)).ToList();
    }

    private async Task<List<CanonicalFinancial>> LoadFinancialLinesForPeriodAsync(
        Guid tenantId,
        DateOnly periodStart,
        DateOnly periodEnd,
        string basis,
        Guid? unitId,
        CancellationToken ct)
    {
        var query = _db.Financials.AsNoTracking().Where(f => f.TenantId == tenantId);

        if (unitId is Guid id)
            query = query.Where(f => f.UnitId == id);

        query = basis == BasisCash
            ? query.Where(f => f.CashFlowDate >= periodStart && f.CashFlowDate <= periodEnd)
            : query.Where(f => f.DueDate >= periodStart && f.DueDate <= periodEnd);

        var rows = await query.ToListAsync(ct);
        var categories = await _db.Categories.AsNoTracking()
            .Where(c => c.TenantId == tenantId)
            .ToListAsync(ct);
        var categoryByName = categories
            .Where(c => !string.IsNullOrWhiteSpace(c.Name))
            .GroupBy(c => c.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().ExternalId, StringComparer.OrdinalIgnoreCase);

        return rows.Select(r => EnrichCategoryMetadata(r, categoryByName)).ToList();
    }

    private static CanonicalFinancial EnrichCategoryMetadata(
        CanonicalFinancial row,
        IReadOnlyDictionary<string, string> categoryByName)
    {
        var flow = string.IsNullOrWhiteSpace(row.FlowDirection)
            ? FinancialFlowDirection.Receivable
            : row.FlowDirection;
        var cashFlowDate = row.CashFlowDate == default ? row.DueDate : row.CashFlowDate;

        var categoryName = row.CategoryName;
        var categoryId = row.CategoryId;

        if (string.IsNullOrWhiteSpace(categoryName) && !string.IsNullOrWhiteSpace(row.PaymentMethodLabel))
            categoryName = row.PaymentMethodLabel;

        if (!string.IsNullOrWhiteSpace(categoryName) && string.IsNullOrWhiteSpace(categoryId)
            && categoryByName.TryGetValue(categoryName.Trim(), out var mappedId))
            categoryId = mappedId;

        return row with
        {
            FlowDirection = flow,
            CashFlowDate = cashFlowDate,
            CategoryName = categoryName ?? "",
            CategoryId = categoryId ?? ""
        };
    }

    private static bool IsReceivable(CanonicalFinancial l) =>
        string.IsNullOrWhiteSpace(l.FlowDirection)
        || l.FlowDirection.Equals(FinancialFlowDirection.Receivable, StringComparison.OrdinalIgnoreCase);

    private static bool IsPayable(CanonicalFinancial l) =>
        l.FlowDirection.Equals(FinancialFlowDirection.Payable, StringComparison.OrdinalIgnoreCase);

    private static string NormalizeDataBasis(string? basis)
    {
        var normalized = (basis ?? BasisCash).Trim().ToLowerInvariant();
        return normalized switch
        {
            BasisCash => BasisCash,
            BasisDue => BasisDue,
            BasisCompetence => BasisCompetence,
            "vencimento" => BasisDue,
            "caixa" => BasisCash,
            "competencia" => BasisCompetence,
            _ => BasisCash
        };
    }

    private static DateOnly ResolveDateForBasis(CanonicalFinancial line, string basis) =>
        basis switch
        {
            BasisCash => line.CashFlowDate == default ? line.DueDate : line.CashFlowDate,
            BasisDue => line.DueDate,
            // Competência ainda não existe no staging; queda intencional para vencimento.
            BasisCompetence => line.DueDate,
            _ => line.CashFlowDate == default ? line.DueDate : line.CashFlowDate
        };

    private static FinancePulseMetricDto Metric(
        string key,
        string label,
        decimal value,
        string format,
        string tone,
        string hint) =>
        new(key, label, value, format, null, null, tone, hint);

    private static FinancePulseGoalDto BuildPulseGoal(
        string key,
        string label,
        decimal? target,
        decimal actual)
    {
        decimal? progress = target is > 0 ? actual / target.Value * 100m : null;
        return new FinancePulseGoalDto(
            key,
            label,
            target,
            actual,
            progress,
            target is > 0 ? "EduFlow" : "GAP: meta ainda não cadastrada no EduFlow");
    }

    private static string NormalizeGoalKey(string key)
    {
        var normalized = (key ?? "").Trim().ToLowerInvariant();
        return normalized switch
        {
            "monthly_revenue" or "receita_mensal" => "monthly_revenue",
            _ => throw new ArgumentException("Meta financeira não reconhecida.")
        };
    }

    private static string DefaultGoalLabel(string key) =>
        key == "monthly_revenue" ? "Meta de receita mensal" : key;

    private static FinanceGoalDto ToGoalDto(FinanceGoalEntity goal) =>
        new(
            goal.Id,
            goal.UnitId,
            goal.Year,
            goal.Month,
            goal.Key,
            goal.Label,
            goal.TargetAmount,
            goal.UpdatedAtUtc);

    private static (string Status, string Message) BuildHealthStatus(
        decimal delinquencyPct,
        decimal projectedBalance)
    {
        if (projectedBalance < 0)
            return ("critical", "Saldo projetado negativo. O gestor precisa revisar entradas e saídas previstas.");
        if (delinquencyPct >= 30)
            return ("critical", "Inadimplência elevada para o período. Priorize cobrança e negociação.");
        if (delinquencyPct >= 15)
            return ("attention", "Inadimplência pede atenção, mas o caixa ainda pode ser administrado.");
        return ("stable", "Saúde financeira controlada com os dados sincronizados até agora.");
    }

    private static IReadOnlyList<FinancePulseSignalDto> BuildPulseSignals(
        decimal overdue,
        decimal delinquencyPct,
        decimal projectedBalance,
        decimal receivedShare,
        decimal? revenueTarget,
        decimal receivedRevenue,
        DateOnly periodStart,
        DateOnly periodEnd)
    {
        var signals = new List<FinancePulseSignalDto>();

        if (overdue > 0)
        {
            signals.Add(new FinancePulseSignalDto(
                "overdue_pressure",
                "Pressão de inadimplência",
                $"Há {overdue:C} vencidos no período ({delinquencyPct:N1}% da carteira aberta).",
                delinquencyPct >= 30 ? "critical" : "warning",
                "overdue",
                "Priorize cobrança das parcelas vencidas e acompanhe o impacto no caixa semanal.",
                "/financeiro?aba=recebiveis"));
        }

        if (projectedBalance < 0)
        {
            signals.Add(new FinancePulseSignalDto(
                "negative_projected_balance",
                "Risco de caixa negativo",
                $"O saldo projetado fecha o período em {projectedBalance:C}.",
                "critical",
                "projected_balance",
                "Revise despesas previstas e antecipe ações sobre recebíveis em aberto.",
                "/financeiro?aba=fluxo"));
        }

        if (receivedShare < 60)
        {
            signals.Add(new FinancePulseSignalDto(
                "collection_pace",
                "Ritmo de recebimento abaixo do ideal",
                $"A receita recebida representa {receivedShare:N1}% do faturamento bruto entre {periodStart:dd/MM} e {periodEnd:dd/MM}.",
                "warning",
                "received_revenue",
                "Compare com a meta mensal e monitore a recuperação dos próximos 7 dias.",
                "/financeiro"));
        }

        if (revenueTarget is > 0)
        {
            var progress = receivedRevenue / revenueTarget.Value * 100m;
            if (progress < 70m)
            {
                signals.Add(new FinancePulseSignalDto(
                    "goal_gap",
                    "Receita abaixo da meta",
                    $"A escola realizou {progress:N1}% da meta de receita mensal ({receivedRevenue:C} de {revenueTarget.Value:C}).",
                    "warning",
                    "monthly_revenue",
                    "Acompanhe recebíveis em aberto e priorize parcelas vencidas com maior impacto.",
                    "/financeiro"));
            }
            else if (progress >= 100m)
            {
                signals.Add(new FinancePulseSignalDto(
                    "goal_reached",
                    "Meta mensal atingida",
                    $"A receita recebida chegou a {progress:N1}% da meta cadastrada.",
                    "success",
                    "monthly_revenue",
                    "Use o excedente para avaliar margem, inadimplência residual e previsibilidade do próximo mês.",
                    "/financeiro"));
            }
        }
        else
        {
            signals.Add(new FinancePulseSignalDto(
                "missing_revenue_goal",
                "Meta de receita ausente",
                "Cadastre a meta mensal para comparar automaticamente realizado, previsto e desvio.",
                "info",
                "monthly_revenue",
                "Defina a meta no painel do Pulso Financeiro.",
                "/financeiro"));
        }

        if (signals.Count == 0)
        {
            signals.Add(new FinancePulseSignalDto(
                "stable_financial_pulse",
                "Pulso financeiro controlado",
                "Não há sinais críticos com os dados financeiros sincronizados para o período.",
                "success",
                "health",
                "Mantenha a rotina de acompanhamento e compare o realizado com a meta do gestor.",
                "/financeiro"));
        }

        return signals;
    }

    private static IReadOnlyList<FinancePulseCalendarDayDto> BuildPulseCalendar(
        IReadOnlyList<CanonicalFinancial> period,
        string basis,
        int year,
        int month)
    {
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var running = 0m;
        var result = new List<FinancePulseCalendarDayDto>(daysInMonth);

        for (var day = 1; day <= daysInMonth; day++)
        {
            var date = new DateOnly(year, month, day);
            var lines = period.Where(l => ResolveDateForBasis(l, basis) == date).ToList();
            var receivables = lines.Where(IsReceivable).ToList();
            var payables = lines.Where(IsPayable).ToList();

            var expectedInflow = receivables.Sum(l => l.DebtAmount + l.PaidAmount);
            var realizedInflow = receivables.Sum(l => l.PaidAmount);
            var expectedOutflow = payables.Sum(l => l.DebtAmount + l.PaidAmount);
            var realizedOutflow = payables.Sum(l => l.PaidAmount);
            var overdue = receivables
                .Where(l => l.PaymentStatus.Equals("overdue", StringComparison.OrdinalIgnoreCase))
                .Sum(l => l.DebtAmount);

            running += realizedInflow - realizedOutflow;
            var status =
                overdue > 0 ? "critical" :
                expectedOutflow > realizedInflow && expectedOutflow > 0 ? "attention" :
                expectedInflow > 0 || realizedInflow > 0 ? "positive" :
                "neutral";

            result.Add(new FinancePulseCalendarDayDto(
                date.ToString("yyyy-MM-dd"),
                day,
                expectedInflow,
                realizedInflow,
                expectedOutflow,
                realizedOutflow,
                running,
                overdue,
                status));
        }

        return result;
    }

    private static string SliceGroupKey(CanonicalFinancial l)
    {
        if (!string.IsNullOrWhiteSpace(l.CategoryName)) return l.CategoryName.Trim();
        if (!string.IsNullOrWhiteSpace(l.PaymentMethodLabel)) return l.PaymentMethodLabel.Trim();
        if (!string.IsNullOrWhiteSpace(l.CategoryId)) return $"Categoria {l.CategoryId}";
        return StatusLabel(l.PaymentStatus);
    }

    private static IReadOnlyList<FinanceAmountSliceDto> BuildSlices(IReadOnlyList<CanonicalFinancial> lines)
    {
        return lines
            .GroupBy(SliceGroupKey, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var open = g.Sum(x => x.DebtAmount);
                var invoice = g.Sum(x => x.DebtAmount + x.PaidAmount);
                var id = g.Key.ToLowerInvariant().Replace(' ', '-');
                return new FinanceAmountSliceDto(id, g.Key, invoice, open);
            })
            .OrderByDescending(s => s.OpenAmount)
            .ToList();
    }

    private static IReadOnlyList<FinanceSituationDto> BuildSituations(IReadOnlyList<CanonicalFinancial> lines)
    {
        return lines
            .GroupBy(SliceGroupKey, StringComparer.OrdinalIgnoreCase)
            .Select(g => new FinanceSituationDto(
                g.Key.ToLowerInvariant().Replace(' ', '-'),
                g.Key,
                g.Sum(x => x.DebtAmount)))
            .OrderByDescending(s => s.Amount)
            .ToList();
    }

    private static string StatusLabel(string status) =>
        status.ToLowerInvariant() switch
        {
            "overdue" => "Vencido",
            "pending" => "Em aberto",
            "paid" => "Pago",
            _ => string.IsNullOrWhiteSpace(status) ? "Sem status" : status
        };
}
