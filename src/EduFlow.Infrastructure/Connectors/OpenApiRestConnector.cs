using System.Net.Http.Headers;
using System.Text.Json;
using EduFlow.Application.Interfaces;
using EduFlow.Application.Options;
using EduFlow.Infrastructure.Connectors.OpenApi;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EduFlow.Infrastructure.Connectors;

/// <summary>
/// Connector REST/OpenAPI — GET em paths configuráveis por tenant (SearchParameters*).
/// Username: vazio = Bearer no token; "ApiKey" = header X-Api-Key; outro valor = nome do header customizado.
/// </summary>
public sealed class OpenApiRestConnector : IErpConnector
{
    public string ProviderKey => "openapi";

    private readonly HttpClient _http;
    private readonly ILogger<OpenApiRestConnector> _logger;
    private readonly ErpConnectorSettings _settings;

    public OpenApiRestConnector(
        HttpClient http,
        ILogger<OpenApiRestConnector> logger,
        IOptions<ErpConnectorSettings> settings)
    {
        _http = http;
        _logger = logger;
        _settings = settings.Value;
    }

    public Task<IReadOnlyList<RawErpRecord>> FetchStudentsAsync(ErpSyncContext context, CancellationToken ct) =>
        FetchAsync(context, "student", context.SearchParametersStudents ?? "/students", ct);

    public Task<IReadOnlyList<RawErpRecord>> FetchFinancialAsync(ErpSyncContext context, CancellationToken ct) =>
        FetchAsync(context, "financial", context.SearchParametersFinancial ?? "/financial", ct);

    public Task<IReadOnlyList<RawErpRecord>> FetchContractsAsync(ErpSyncContext context, CancellationToken ct) =>
        FetchAsync(context, "contract", context.SearchParametersContracts ?? "/contracts", ct);

    private async Task<IReadOnlyList<RawErpRecord>> FetchAsync(
        ErpSyncContext context,
        string entityType,
        string pathOrQuery,
        CancellationToken ct)
    {
        var baseUrl = (context.EndpointUrl ?? "").TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new InvalidOperationException("Informe a URL base da API REST (EndpointUrl).");

        var relative = pathOrQuery.Trim();
        if (!relative.StartsWith('/'))
            relative = "/" + relative;

        var url = baseUrl + relative;
        var limit = Math.Clamp(context.PageSize, 1, 500);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            ApplyAuth(request, context);

            _logger.LogInformation(
                "OpenAPI GET {EntityType} TenantId={TenantId} Url={Url}",
                entityType, context.TenantId, url);

            var response = await _http.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "OpenAPI HTTP {Status} para {EntityType}. Corpo: {Body}",
                    (int)response.StatusCode, entityType, Truncate(body, 400));

                return _settings.AllowDemoFallback
                    ? BuildDemoBatch(entityType, limit)
                    : [];
            }

            var records = ParseJsonPayload(entityType, body, limit);
            if (records.Count > 0)
                return records;

            _logger.LogWarning("OpenAPI {EntityType} sem registros em {Url}.", entityType, url);

            return _settings.AllowDemoFallback
                ? BuildDemoBatch(entityType, limit)
                : [];
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "OpenAPI indisponível ({EntityType}).", entityType);
            return _settings.AllowDemoFallback
                ? BuildDemoBatch(entityType, limit)
                : [];
        }
    }

    private static void ApplyAuth(HttpRequestMessage request, ErpSyncContext context)
    {
        var token = context.Password?.Trim();
        if (string.IsNullOrEmpty(token)) return;

        var user = context.Username?.Trim() ?? "";
        if (string.IsNullOrEmpty(user))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return;
        }

        if (user.Equals("ApiKey", StringComparison.OrdinalIgnoreCase))
        {
            request.Headers.TryAddWithoutValidation("X-Api-Key", token);
            return;
        }

        request.Headers.TryAddWithoutValidation(user, token);
    }

    private static IReadOnlyList<RawErpRecord> ParseJsonPayload(string entityType, string body, int limit)
    {
        if (string.IsNullOrWhiteSpace(body)) return [];

        using var doc = JsonDocument.Parse(body);
        var elements = ExtractItems(doc.RootElement).Take(limit).ToList();
        var now = DateTime.UtcNow;
        var list = new List<RawErpRecord>(elements.Count);

        foreach (var (el, index) in elements.Select((e, i) => (e, i)))
        {
            var id = TryGetId(el) ?? $"{entityType}-{index + 1}";
            list.Add(new RawErpRecord(
                entityType,
                id,
                OpenApiRecordEnvelope.Wrap(entityType, el.GetRawText()),
                now));
        }

        return list;
    }

    private static IEnumerable<JsonElement> ExtractItems(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
                yield return item;
            yield break;
        }

        foreach (var prop in new[] { "items", "data", "results", "value", "records" })
        {
            if (root.TryGetProperty(prop, out var arr) && arr.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in arr.EnumerateArray())
                    yield return item;
                yield break;
            }
        }

        if (root.ValueKind == JsonValueKind.Object)
            yield return root;
    }

    private static string? TryGetId(JsonElement el)
    {
        foreach (var name in new[] { "id", "externalId", "alunoId", "AlunoID", "contaReceberId", "contratoId" })
        {
            if (el.TryGetProperty(name, out var v))
            {
                var s = v.ValueKind == JsonValueKind.Number
                    ? v.GetRawText()
                    : v.GetString();
                if (!string.IsNullOrWhiteSpace(s)) return s;
            }
        }

        return null;
    }

    private static IReadOnlyList<RawErpRecord> BuildDemoBatch(string entityType, int count)
    {
        var now = DateTime.UtcNow;
        var list = new List<RawErpRecord>(count);

        for (var i = 1; i <= count; i++)
        {
            var json = entityType switch
            {
                "student" => $$"""
                    {"id":"{{1000 + i}}","enrollmentCode":"MAT-2024-{{i:D4}}","status":"active","enrolledAt":"2024-02-01"}
                    """,
                "financial" => $$"""
                    {"id":"{{5000 + i}}-1","enrollmentCode":"MAT-2024-{{i:D4}}","unitCode":"UN-RJ","debtAmount":{{(i % 4 == 0 ? 450.50 + i : 0)}},"paidAmount":{{(i % 3 == 0 ? 450.50 + i : 0)}},"paymentStatus":"{{(i % 4 == 0 ? "overdue" : "pending")}}","dueDate":"2025-{{(i % 12 + 1):D2}}-10"}
                    """,
                _ => $$"""
                    {"id":"{{7000 + i}}","enrollmentCode":"{{1000 + i}}","contractStatus":"active","startDate":"2024-01-15","endDate":"2025-12-15"}
                    """
            };

            var id = entityType switch
            {
                "student" => $"{1000 + i}",
                "financial" => $"{5000 + i}-1",
                _ => $"{7000 + i}"
            };

            list.Add(new RawErpRecord(entityType, id, OpenApiRecordEnvelope.Wrap(entityType, json), now));
        }

        return list;
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";
}
