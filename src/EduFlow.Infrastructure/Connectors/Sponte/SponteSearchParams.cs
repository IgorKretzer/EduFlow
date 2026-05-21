namespace EduFlow.Infrastructure.Connectors.Sponte;

internal static class SponteSearchParams
{
    /// <summary>Extrai TOP=NNN de sParametrosBusca (ex.: Situacao=2|TOP=150).</summary>
    public static int ParseTop(string? searchParams, int fallback = 0)
    {
        if (string.IsNullOrWhiteSpace(searchParams)) return fallback;

        foreach (var part in searchParams.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!part.StartsWith("TOP=", StringComparison.OrdinalIgnoreCase)) continue;
            var value = part[4..].Trim();
            if (int.TryParse(value, out var top) && top > 0)
                return Math.Min(top, 500);
        }

        return fallback;
    }

    public static int ResolveRecordLimit(string searchParams, int pageSize, int demoCap = 500)
    {
        var fromTop = ParseTop(searchParams);
        if (fromTop > 0) return fromTop;

        var fromPage = pageSize > 0 ? pageSize : 100;
        return Math.Min(fromPage, demoCap);
    }
}
