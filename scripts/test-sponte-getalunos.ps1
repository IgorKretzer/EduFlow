param(
    [string]$ClientCode = "101411",
    [string]$Token = "",
    [string]$SearchParams = "Situacao=2|TOP=5"
)

if ([string]::IsNullOrWhiteSpace($Token)) {
    $Token = $env:SPONTE_TOKEN
}
if ([string]::IsNullOrWhiteSpace($Token)) {
    Write-Error "Informe -Token ou variável de ambiente SPONTE_TOKEN."
}

$body = @"
<?xml version="1.0" encoding="utf-8"?>
<soap:Envelope xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
               xmlns:xsd="http://www.w3.org/2001/XMLSchema"
               xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
  <soap:Body>
    <GetAlunos xmlns="http://api.sponteeducacional.net.br/">
      <nCodigoCliente>$ClientCode</nCodigoCliente>
      <sToken>$Token</sToken>
      <sParametrosBusca>$SearchParams</sParametrosBusca>
    </GetAlunos>
  </soap:Body>
</soap:Envelope>
"@

$response = Invoke-WebRequest `
    -Uri "https://api.sponteeducacional.net.br/WSAPIEdu.asmx" `
    -Method POST `
    -ContentType "text/xml; charset=utf-8" `
    -Headers @{ SOAPAction = "http://api.sponteeducacional.net.br/GetAlunos" } `
    -Body $body `
    -UseBasicParsing

Write-Host "HTTP" $response.StatusCode
$xml = $response.Content
$count = ([regex]::Matches($xml, "<wsAluno")).Count
Write-Host "wsAluno count:" $count
Write-Host $xml.Substring(0, [Math]::Min(2500, $xml.Length))
