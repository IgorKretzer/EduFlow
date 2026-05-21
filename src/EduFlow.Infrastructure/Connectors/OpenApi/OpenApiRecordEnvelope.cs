using System.Text;
using System.Xml.Linq;

namespace EduFlow.Infrastructure.Connectors.OpenApi;

/// <summary>
/// Envelope XML estável para transportar JSON do conector REST até o normalizador OpenAPI.
/// </summary>
internal static class OpenApiRecordEnvelope
{
    public const string Namespace = "http://eduflow.local/openapi";

    public static string Wrap(string entityType, string json) =>
        $"""
        <EduFlowOpenApi xmlns="{Namespace}">
          <EntityType>{System.Security.SecurityElement.Escape(entityType)}</EntityType>
          <Json><![CDATA[{json}]]></Json>
        </EduFlowOpenApi>
        """;

    public static bool TryGetJson(string rawXml, out string json)
    {
        json = "";
        try
        {
            var root = XDocument.Parse(rawXml).Root;
            if (root is null) return false;

            var jsonEl = root.Name.LocalName.Equals("EduFlowOpenApi", StringComparison.OrdinalIgnoreCase)
                ? root.Element(XName.Get("Json", Namespace)) ?? root.Element("Json")
                : root;

            if (jsonEl is null) return false;
            json = jsonEl.Value.Trim();
            return !string.IsNullOrWhiteSpace(json);
        }
        catch
        {
            return false;
        }
    }
}
