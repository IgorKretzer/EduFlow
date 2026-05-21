namespace EduFlow.Infrastructure.Etl;

internal static class UnitDisplayNames
{
    /// <summary>
    /// Converte CodigoUnidade Sponte (ex.: UN-RJ) em rótulo legível para insights e DW.
    /// </summary>
    public static (string UnitCode, string UnitName) FromSponteCode(string? unitCode, Guid unitId)
    {
        var code = string.IsNullOrWhiteSpace(unitCode)
            ? unitId.ToString("N")[..8].ToUpperInvariant()
            : unitCode.Trim().ToUpperInvariant();

        if (code is "GERAL" or "00000000")
            return ("GERAL", "Unidade Geral");

        if (code.StartsWith("UN-", StringComparison.OrdinalIgnoreCase))
        {
            var suffix = code[3..].Trim();
            return (code, string.IsNullOrEmpty(suffix) ? code : $"Unidade {suffix}");
        }

        return (code, code);
    }
}
