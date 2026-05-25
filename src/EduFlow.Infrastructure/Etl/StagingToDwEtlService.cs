using Dapper;
using EduFlow.Analytics.Queries;
using EduFlow.Application.Interfaces;
using EduFlow.Domain.Entities;
using EduFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduFlow.Infrastructure.Etl;

/// <summary>
/// ETL staging (EF) → Data Warehouse (Dapper). Fecha o pipeline analítico.
/// </summary>
public sealed class StagingToDwEtlService : IEtlService
{
    private readonly StagingDbContext _staging;
    private readonly IDwConnectionFactory _dw;
    private readonly ILogger<StagingToDwEtlService> _logger;

    public StagingToDwEtlService(
        StagingDbContext staging,
        IDwConnectionFactory dw,
        ILogger<StagingToDwEtlService> logger)
    {
        _staging = staging;
        _dw = dw;
        _logger = logger;
    }

    public async Task UpsertStudentAsync(Guid tenantId, CanonicalStudent student, CancellationToken ct)
    {
        var existing = await _staging.Students
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.ExternalId == student.ExternalId, ct);

        var entity = student with
        {
            Id = existing?.Id ?? Guid.NewGuid(),
            TenantId = tenantId,
            SyncedAt = DateTime.UtcNow
        };

        if (existing is not null)
            _staging.Students.Remove(existing);
        _staging.Students.Add(entity);

        _staging.StudentSnapshots.Add(new StudentSnapshot
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ExternalId = student.ExternalId,
            EnrollmentCode = student.EnrollmentCode,
            Status = student.Status,
            CapturedAt = DateTime.UtcNow
        });

        await _staging.SaveChangesAsync(ct);
    }

    public async Task UpsertFinancialAsync(Guid tenantId, CanonicalFinancial financial, CancellationToken ct)
    {
        var existing = await _staging.Financials
            .FirstOrDefaultAsync(f => f.TenantId == tenantId && f.ExternalId == financial.ExternalId, ct);

        var entity = financial with { TenantId = tenantId, SyncedAt = DateTime.UtcNow };
        if (existing is null)
        {
            _staging.Financials.Add(entity);
        }
        else
        {
            _staging.Financials.Remove(existing);
            _staging.Financials.Add(entity with { Id = existing.Id });
        }

        _staging.FinancialSnapshots.Add(new FinancialSnapshot
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ExternalId = financial.ExternalId,
            EnrollmentCode = financial.EnrollmentCode,
            DebtAmount = financial.DebtAmount,
            PaidAmount = financial.PaidAmount,
            PaymentStatus = financial.PaymentStatus,
            DueDate = financial.DueDate,
            CapturedAt = DateTime.UtcNow
        });

        await _staging.SaveChangesAsync(ct);
    }

    public async Task UpsertCategoryAsync(Guid tenantId, CanonicalCategory category, CancellationToken ct)
    {
        var existing = await _staging.Categories
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.ExternalId == category.ExternalId, ct);

        var entity = category with { TenantId = tenantId, SyncedAt = DateTime.UtcNow };
        if (existing is null)
            _staging.Categories.Add(entity);
        else
        {
            _staging.Categories.Remove(existing);
            _staging.Categories.Add(entity with { Id = existing.Id });
        }

        await _staging.SaveChangesAsync(ct);
    }

    public async Task UpsertContractAsync(Guid tenantId, CanonicalContract contract, CancellationToken ct)
    {
        var existing = await _staging.Contracts
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.ExternalId == contract.ExternalId, ct);

        var entity = contract with { TenantId = tenantId, SyncedAt = DateTime.UtcNow };
        if (existing is null)
            _staging.Contracts.Add(entity);
        else
        {
            _staging.Contracts.Remove(existing);
            _staging.Contracts.Add(entity with { Id = existing.Id });
        }

        await _staging.SaveChangesAsync(ct);
    }

    public async Task RefreshAnalyticsAsync(Guid tenantId, CancellationToken ct)
    {
        var students = await _staging.Students.AsNoTracking()
            .Where(s => s.TenantId == tenantId).ToListAsync(ct);
        var financials = await _staging.Financials.AsNoTracking()
            .Where(f => f.TenantId == tenantId).ToListAsync(ct);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var dateKey = today.Year * 10000 + today.Month * 100 + today.Day;

        using var conn = _dw.Create();
        await conn.OpenAsync(ct);

        using var tx = conn.BeginTransaction();

        await conn.ExecuteAsync("""
            IF NOT EXISTS (SELECT 1 FROM dw.DimTempo WHERE DateKey = @DateKey)
                INSERT INTO dw.DimTempo (DateKey, [Date], [Year], [Month], [Quarter])
                VALUES (@DateKey, @Date, @Year, @Month, @Quarter)
            """, new
        {
            DateKey = dateKey,
            Date = today.ToDateTime(TimeOnly.MinValue),
            Year = today.Year,
            Month = today.Month,
            Quarter = (today.Month - 1) / 3 + 1
        }, tx);

        var unitCodeById = financials
            .Where(f => f.UnitId != Guid.Empty)
            .GroupBy(f => f.UnitId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.UnitCode).FirstOrDefault(c => !string.IsNullOrWhiteSpace(c)) ?? "");

        var unitIds = unitCodeById.Keys
            .Union(students.Select(s => s.UnitId))
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (unitIds.Count == 0)
            unitIds.Add(Guid.Empty);

        foreach (var unitId in unitIds)
        {
            if (unitId == Guid.Empty)
            {
                await conn.ExecuteAsync("""
                    IF NOT EXISTS (SELECT 1 FROM dw.DimUnidade WHERE TenantId = @TenantId AND UnitCode = 'GERAL')
                        INSERT INTO dw.DimUnidade (TenantId, UnitId, UnitCode, UnitName)
                        VALUES (@TenantId, '00000000-0000-0000-0000-000000000001', 'GERAL', N'Unidade Geral')
                    ELSE
                        UPDATE dw.DimUnidade SET UnitCode = 'GERAL', UnitName = N'Unidade Geral'
                        WHERE TenantId = @TenantId AND UnitCode = 'GERAL'
                    """, new { TenantId = tenantId }, tx);
                continue;
            }

            unitCodeById.TryGetValue(unitId, out var sponteCode);
            var (code, name) = UnitDisplayNames.FromSponteCode(sponteCode, unitId);

            await conn.ExecuteAsync("""
                IF NOT EXISTS (SELECT 1 FROM dw.DimUnidade WHERE TenantId = @TenantId AND UnitId = @UnitId)
                    INSERT INTO dw.DimUnidade (TenantId, UnitId, UnitCode, UnitName)
                    VALUES (@TenantId, @UnitId, @Code, @Name)
                ELSE
                    UPDATE dw.DimUnidade SET UnitCode = @Code, UnitName = @Name
                    WHERE TenantId = @TenantId AND UnitId = @UnitId
                """, new
            {
                TenantId = tenantId,
                UnitId = unitId,
                Code = code,
                Name = name
            }, tx);
        }

        var unitKeys = (await conn.QueryAsync<(Guid UnitId, int UnitKey)>(
            "SELECT UnitId, UnitKey FROM dw.DimUnidade WHERE TenantId = @TenantId",
            new { TenantId = tenantId }, tx)).ToDictionary(x => x.UnitId, x => x.UnitKey);

        foreach (var s in students)
        {
            var unitKey = unitKeys.GetValueOrDefault(s.UnitId, unitKeys.Values.FirstOrDefault());
            if (unitKey == 0) continue;

            await conn.ExecuteAsync("""
                IF NOT EXISTS (
                    SELECT 1 FROM dw.DimAluno
                    WHERE TenantId = @TenantId AND EnrollmentCode = @EnrollmentCode)
                    INSERT INTO dw.DimAluno (TenantId, EnrollmentCode, UnitKey, CourseKey, Status)
                    VALUES (@TenantId, @EnrollmentCode, @UnitKey, NULL, @Status)
                ELSE
                    UPDATE dw.DimAluno SET Status = @Status, UnitKey = @UnitKey
                    WHERE TenantId = @TenantId AND EnrollmentCode = @EnrollmentCode
                """, new
            {
                TenantId = tenantId,
                s.EnrollmentCode,
                UnitKey = unitKey,
                s.Status
            }, tx);
        }

        await conn.ExecuteAsync(
            "DELETE FROM dw.FactFinanceiro WHERE TenantId = @TenantId AND DateKey = @DateKey",
            new { TenantId = tenantId, DateKey = dateKey }, tx);

        foreach (var f in financials)
        {
            if (!unitKeys.TryGetValue(f.UnitId, out var unitKey)) continue;
            var isOverdue = f.PaymentStatus == "overdue" || (f.DebtAmount > 0 && f.DueDate < today);

            await conn.ExecuteAsync("""
                INSERT INTO dw.FactFinanceiro
                    (TenantId, DateKey, UnitKey, EnrollmentCode, DebtAmount, PaidAmount, InterestAmount, PaymentStatus, IsOverdue)
                VALUES
                    (@TenantId, @DateKey, @UnitKey, @EnrollmentCode, @DebtAmount, @PaidAmount, @InterestAmount, @PaymentStatus, @IsOverdue)
                """, new
            {
                TenantId = tenantId,
                DateKey = dateKey,
                UnitKey = unitKey,
                f.EnrollmentCode,
                f.DebtAmount,
                f.PaidAmount,
                f.InterestAmount,
                f.PaymentStatus,
                IsOverdue = isOverdue ? 1 : 0
            }, tx);
        }

        await conn.ExecuteAsync(
            "DELETE FROM dw.FactInadimplencia WHERE TenantId = @TenantId AND DateKey = @DateKey",
            new { TenantId = tenantId, DateKey = dateKey }, tx);

        await conn.ExecuteAsync("""
            INSERT INTO dw.FactInadimplencia (TenantId, DateKey, UnitKey, DelinquencyRate, DebtAmount, OverdueCount)
            SELECT
                fi.TenantId,
                fi.DateKey,
                fi.UnitKey,
                CAST(SUM(CASE WHEN fi.IsOverdue = 1 THEN 1.0 ELSE 0 END) / NULLIF(COUNT(*), 0) AS DECIMAL(9,4)),
                SUM(fi.DebtAmount),
                SUM(CASE WHEN fi.IsOverdue = 1 THEN 1 ELSE 0 END)
            FROM dw.FactFinanceiro fi
            WHERE fi.TenantId = @TenantId AND fi.DateKey = @DateKey
            GROUP BY fi.TenantId, fi.DateKey, fi.UnitKey
            """, new { TenantId = tenantId, DateKey = dateKey }, tx);

        var active = students.Count(s => s.Status is "active" or "delinquent");
        var churned = students.Count(s => s.Status is "inactive");
        var churnRate = active + churned == 0 ? 0m : (decimal)churned / (active + churned);

        await conn.ExecuteAsync(
            "DELETE FROM dw.FactEvasao WHERE TenantId = @TenantId AND DateKey = @DateKey",
            new { TenantId = tenantId, DateKey = dateKey }, tx);

        foreach (var unitKey in unitKeys.Values.Distinct())
        {
            await conn.ExecuteAsync("""
                INSERT INTO dw.FactEvasao (TenantId, DateKey, UnitKey, ChurnRate, ChurnedStudents, ActiveStudents)
                VALUES (@TenantId, @DateKey, @UnitKey, @ChurnRate, @Churned, @Active)
                """, new
            {
                TenantId = tenantId,
                DateKey = dateKey,
                UnitKey = unitKey,
                ChurnRate = churnRate,
                Churned = churned,
                Active = active
            }, tx);
        }

        tx.Commit();

        _logger.LogInformation(
            "ETL concluído TenantId={TenantId} Students={Students} Financials={Financials} DateKey={DateKey}",
            tenantId, students.Count, financials.Count, dateKey);
    }
}
