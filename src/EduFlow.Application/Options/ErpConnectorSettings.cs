namespace EduFlow.Application.Options;

/// <summary>
/// Opções globais dos conectores ERP (Sponte SOAP, OpenAPI REST, etc.).
/// </summary>
public sealed class ErpConnectorSettings
{
    public const string SectionName = "ErpConnectors";

    /// <summary>
    /// Em Development, permite dados de demonstração quando a API REST falha ou retorna vazio.
    /// Desligue em produção.
    /// </summary>
    public bool AllowDemoFallback { get; set; } = true;
}
