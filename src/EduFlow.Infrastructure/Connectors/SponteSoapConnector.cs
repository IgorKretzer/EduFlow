using System.Text;

using System.Xml.Linq;

using EduFlow.Application.Interfaces;
using EduFlow.Application.Options;
using EduFlow.Infrastructure.Connectors.Sponte;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;



namespace EduFlow.Infrastructure.Connectors;



/// <summary>

/// Connector Sponte — SOAP conforme WSDL (nCodigoCliente, sToken, sParametrosBusca).

/// </summary>

public sealed class SponteSoapConnector : IErpExtendedConnector

{

    public string ProviderKey => "sponte";



    public const string DefaultServiceUrl = "https://api.sponteeducacional.net.br/WSAPIEdu.asmx";



    private readonly HttpClient _http;
    private readonly ILogger<SponteSoapConnector> _logger;
    private readonly ErpConnectorSettings _settings;

    public SponteSoapConnector(
        HttpClient http,
        ILogger<SponteSoapConnector> logger,
        IOptions<ErpConnectorSettings> settings)
    {
        _http = http;
        _logger = logger;
        _settings = settings.Value;
    }



    public Task<IReadOnlyList<RawErpRecord>> FetchStudentsAsync(ErpSyncContext context, CancellationToken ct) =>

        FetchAsync(context, "GetAlunos", "student", BuildStudentDemoXml, ct);



    public Task<IReadOnlyList<RawErpRecord>> FetchFinancialAsync(ErpSyncContext context, CancellationToken ct) =>

        FetchAsync(context, "GetFinanceiro", "financial", BuildFinancialDemoXml, ct);



    public Task<IReadOnlyList<RawErpRecord>> FetchPayablesAsync(ErpSyncContext context, CancellationToken ct) =>

        FetchAsync(context, "GetParcelasPagar", "payable", BuildPayableDemoXml, ct);



    public Task<IReadOnlyList<RawErpRecord>> FetchCategoriesAsync(ErpSyncContext context, CancellationToken ct) =>

        FetchCategoriesInternalAsync(context, ct);



    public Task<IReadOnlyList<RawErpRecord>> FetchContractsAsync(ErpSyncContext context, CancellationToken ct) =>

        FetchAsync(context, "GetMatriculas", "contract", BuildMatriculaDemoXml, ct);



    private async Task<IReadOnlyList<RawErpRecord>> FetchAsync(

        ErpSyncContext context,

        string soapAction,

        string entityType,

        Func<int, string> demoFactory,

        CancellationToken ct)

    {

        var endpoint = string.IsNullOrWhiteSpace(context.EndpointUrl)

            ? DefaultServiceUrl

            : context.EndpointUrl;



        var searchParams = ResolveSearchParameters(soapAction, context);



        try

        {

            var envelope = BuildSoapEnvelope(soapAction, context, searchParams);

            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)

            {

                Content = new StringContent(envelope, Encoding.UTF8, "text/xml")

            };

            request.Headers.TryAddWithoutValidation("SOAPAction",

                $"http://api.sponteeducacional.net.br/{soapAction}");



            _logger.LogInformation(
                "Sponte SOAP {Action} Cliente={Cliente} ParametrosBusca=[redigido]",
                soapAction, context.Username);



            var response = await _http.SendAsync(request, ct);

            var xml = await response.Content.ReadAsStringAsync(ct);



            if (!response.IsSuccessStatusCode)

            {

                _logger.LogWarning(

                    "SOAP {Action} HTTP {Status}. Corpo: {Body}",

                    soapAction, (int)response.StatusCode,

                    Truncate(xml, 500));

                if (_settings.AllowDemoFallback)
                {
                    var demoCount = SponteSearchParams.ResolveRecordLimit(searchParams, context.PageSize);
                    return BuildDemoBatch(entityType, demoFactory, demoCount);
                }

                throw new InvalidOperationException(
                    $"Sponte {soapAction} retornou HTTP {(int)response.StatusCode}. Verifique token, código cliente e parâmetros de busca.");
            }



            var items = SponteXmlParser.ParseResponse(xml, entityType);

            if (items.Count > 0)

            {

                _logger.LogInformation(

                    "Sponte {Action} retornou {Count} registro(s) válido(s).",

                    soapAction, items.Count);

                return items;

            }



            var fault = SponteXmlParser.ExtractOperationMessage(xml);

            _logger.LogWarning(

                "Sponte {Action} sem registros. Mensagem ERP: {Message}",

                soapAction, fault ?? "(vazio)");



            throw new InvalidOperationException("Sponte indisponível em GetCategorias. Tente novamente em instantes.");

        }

        catch (HttpRequestException ex)

        {

            _logger.LogWarning(ex, "Sponte indisponível ({Action}).", soapAction);

            if (!_settings.AllowDemoFallback)
                throw new InvalidOperationException(
                    $"Sponte indisponível em {soapAction}. Tente novamente em instantes.");

            var demoCount = SponteSearchParams.ResolveRecordLimit(searchParams, context.PageSize);
            return BuildDemoBatch(entityType, demoFactory, demoCount);

        }

    }



    private static string ResolveSearchParameters(string soapAction, ErpSyncContext ctx) =>

        soapAction switch

        {

            "GetAlunos" => ctx.SearchParametersStudents

                               ?? $"Situacao=2|TOP={Math.Max(1, ctx.PageSize)}",

            "GetFinanceiro" => ctx.SearchParametersFinancial

                                 ?? $"Situacao=2|TOP={Math.Max(1, ctx.PageSize)}",

            "GetParcelasPagar" => ctx.SearchParametersPayables

                                   ?? ctx.SearchParametersFinancial

                                   ?? $"TOP={Math.Max(1, ctx.PageSize)}",

            "GetMatriculas" => ctx.SearchParametersContracts

                                 ?? $"Situacao=2|TOP={Math.Max(1, ctx.PageSize)}",

            _ => $"TOP={Math.Max(1, ctx.PageSize)}"

        };



    private static string BuildSoapEnvelope(string action, ErpSyncContext ctx, string searchParams)

    {

        if (!int.TryParse(ctx.Username.Trim(), out var clientCode))

            throw new InvalidOperationException(

                $"nCodigoCliente inválido: '{ctx.Username}'. Use apenas o código numérico Sponte.");



        var token = System.Security.SecurityElement.Escape(ctx.Password);

        var search = System.Security.SecurityElement.Escape(searchParams);



        return $"""

            <?xml version="1.0" encoding="utf-8"?>

            <soap:Envelope xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"

                           xmlns:xsd="http://www.w3.org/2001/XMLSchema"

                           xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">

              <soap:Body>

                <{action} xmlns="http://api.sponteeducacional.net.br/">

                  <nCodigoCliente>{clientCode}</nCodigoCliente>

                  <sToken>{token}</sToken>

                  <sParametrosBusca>{search}</sParametrosBusca>

                </{action}>

              </soap:Body>

            </soap:Envelope>

            """;

    }



    private static string Truncate(string value, int max) =>

        value.Length <= max ? value : value[..max] + "…";



    private static IReadOnlyList<RawErpRecord> BuildDemoBatch(

        string entityType,

        Func<int, string> factory,

        int count)

    {

        var body = string.Concat(Enumerable.Range(1, count).Select(factory));

        var wrapper = entityType switch

        {

            "student" => $"<ArrayOfWsAluno xmlns=\"{SponteXmlParser.SponteNamespace}\">{body}</ArrayOfWsAluno>",

            "financial" => $"<ArrayOfWsFinanceiro xmlns=\"{SponteXmlParser.SponteNamespace}\">{body}</ArrayOfWsFinanceiro>",

            "payable" => $"<ArrayOfWsParcelaPagar xmlns=\"{SponteXmlParser.SponteNamespace}\">{body}</ArrayOfWsParcelaPagar>",

            _ => $"<ArrayOfWsMatricula xmlns=\"{SponteXmlParser.SponteNamespace}\">{body}</ArrayOfWsMatricula>"

        };

        return SponteXmlParser.ParseResponse(wrapper, entityType);

    }



    private static string BuildStudentDemoXml(int i) => $"""

        <wsAluno xmlns="{SponteXmlParser.SponteNamespace}">

          <AlunoID>{1000 + i}</AlunoID>

          <NumeroMatricula>MAT-2024-{i:D4}</NumeroMatricula>

          <Situacao>Ativo</Situacao>

          <Inadimplente>Não</Inadimplente>

          <DataCadastro>2024-02-01</DataCadastro>

        </wsAluno>

        """;



    private static string BuildFinancialDemoXml(int i)

    {

        var matricula = $"MAT-2024-{i:D4}";

        return $"""

            <wsFinanceiro xmlns="{SponteXmlParser.SponteNamespace}">

              <CodigoUnidade>UN-RJ</CodigoUnidade>

              <ContaReceberID>{5000 + i}</ContaReceberID>

              <NumeroContrato>CNT-{i}</NumeroContrato>

              <Categoria>{(i % 3 == 0 ? "Mensalidade" : i % 3 == 1 ? "Material didático" : "Taxa de matrícula")}</Categoria>

              <TipoPlano>Plano {(i % 2) + 1}</TipoPlano>

              <Aluno>

                <wsInfoAluno>

                  <AlunoID>{1000 + i}</AlunoID>

                  <NumeroMatricula>{matricula}</NumeroMatricula>

                </wsInfoAluno>

              </Aluno>

              <Parcelas>

                <wsParcela>

                  <ContaReceberID>{5000 + i}</ContaReceberID>

                  <NumeroParcela>1</NumeroParcela>

                  <SituacaoParcela>{(i % 4 == 0 ? "Vencida" : "Aberta")}</SituacaoParcela>

                  <ValorParcela>{450.50m + i}</ValorParcela>

                  <ValorPago>{(i % 3 == 0 ? 450.50m + i : 0)}</ValorPago>

                  <Vencimento>2025-{(i % 12 + 1):D2}-10</Vencimento>

                  <Categoria>{(i % 3 == 0 ? "Mensalidade" : i % 3 == 1 ? "Material didático" : "Taxa de matrícula")}</Categoria>

                  <FormaCobranca>{(i % 2 == 0 ? "Boleto" : "Cartão")}</FormaCobranca>

                  <TipoRecebimento>{(i % 2 == 0 ? "Boleto" : "Cartão")}</TipoRecebimento>

                  <AlunoID>{1000 + i}</AlunoID>

                </wsParcela>

              </Parcelas>

            </wsFinanceiro>

            """;

    }



    private async Task<IReadOnlyList<RawErpRecord>> FetchCategoriesInternalAsync(
        ErpSyncContext context,
        CancellationToken ct)
    {
        var endpoint = string.IsNullOrWhiteSpace(context.EndpointUrl)
            ? DefaultServiceUrl
            : context.EndpointUrl;

        if (!int.TryParse(context.Username.Trim(), out var clientCode))
            throw new InvalidOperationException(
                $"nCodigoCliente inválido: '{context.Username}'.");

        var token = System.Security.SecurityElement.Escape(context.Password);
        var envelope = $"""
            <?xml version="1.0" encoding="utf-8"?>
            <soap:Envelope xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
                           xmlns:xsd="http://www.w3.org/2001/XMLSchema"
                           xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
              <soap:Body>
                <GetCategorias xmlns="http://api.sponteeducacional.net.br/">
                  <nCodigoCliente>{clientCode}</nCodigoCliente>
                  <sToken>{token}</sToken>
                </GetCategorias>
              </soap:Body>
            </soap:Envelope>
            """;

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(envelope, Encoding.UTF8, "text/xml")
            };
            request.Headers.TryAddWithoutValidation("SOAPAction",
                "http://api.sponteeducacional.net.br/GetCategorias");

            var response = await _http.SendAsync(request, ct);
            var xml = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("GetCategorias HTTP {Status}", (int)response.StatusCode);
                throw new InvalidOperationException(
                    $"Sponte GetCategorias retornou HTTP {(int)response.StatusCode}.");
            }

            var items = SponteXmlParser.ParseResponse(xml, "category");
            return items.Count > 0 ? items : [];
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "GetCategorias indisponível.");
            return [];
        }
    }

    private static string BuildPayableDemoXml(int i) => $"""
        <wsParcelaPagar xmlns="{SponteXmlParser.SponteNamespace}">
          <ContaPagarID>{8000 + i}</ContaPagarID>
          <NumeroParcela>1</NumeroParcela>
          <Sacado>Fornecedor {i}</Sacado>
          <SituacaoParcela>{(i % 5 == 0 ? "Pago" : "Aberta")}</SituacaoParcela>
          <Vencimento>2025-{(i % 12 + 1):D2}-15</Vencimento>
          <ValorParcela>{120.00m + i * 10}</ValorParcela>
          <Categoria>Despesas operacionais</Categoria>
          <FormaCobranca>Boleto</FormaCobranca>
          <DataPagamento>{(i % 5 == 0 ? "2025-03-10" : "")}</DataPagamento>
          <ValorPago>{(i % 5 == 0 ? 120.00m + i * 10 : 0)}</ValorPago>
        </wsParcelaPagar>
        """;

    private static string BuildMatriculaDemoXml(int i) => $"""

        <wsMatricula xmlns="{SponteXmlParser.SponteNamespace}">

          <ContratoID>{7000 + i}</ContratoID>

          <AlunoID>{1000 + i}</AlunoID>

          <NumeroContrato>CNT-{i}</NumeroContrato>

          <Situacao>Ativo</Situacao>

          <TurmaID>{200 + i}</TurmaID>

          <DataInicio>2024-01-15</DataInicio>

          <DataTermino>2025-12-15</DataTermino>

          <DataMatricula>2024-02-01</DataMatricula>

        </wsMatricula>

        """;

}


