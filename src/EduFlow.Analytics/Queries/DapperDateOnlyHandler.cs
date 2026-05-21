using System.Data;
using Dapper;

namespace EduFlow.Analytics.Queries;

/// <summary>
/// Dapper não mapeia DateOnly nativamente em versões usadas pelo projeto.
/// </summary>
internal sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override void SetValue(IDbDataParameter parameter, DateOnly value) =>
        parameter.Value = value.ToDateTime(TimeOnly.MinValue);

    public override DateOnly Parse(object value) =>
        value switch
        {
            DateOnly d => d,
            DateTime dt => DateOnly.FromDateTime(dt),
            _ => DateOnly.FromDateTime(Convert.ToDateTime(value))
        };
}

internal sealed class NullableDateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly?>
{
    public override void SetValue(IDbDataParameter parameter, DateOnly? value) =>
        parameter.Value = value.HasValue ? value.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value;

    public override DateOnly? Parse(object value) =>
        value is null or DBNull ? null : new DateOnlyTypeHandler().Parse(value);
}

public static class DapperConfiguration
{
    private static bool _configured;

    public static void EnsureDateOnlyHandlers()
    {
        if (_configured) return;
        SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
        SqlMapper.AddTypeHandler(new NullableDateOnlyTypeHandler());
        _configured = true;
    }
}
