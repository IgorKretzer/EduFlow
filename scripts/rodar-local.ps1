$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent

Write-Host "=== EduFlow — checagem local ===" -ForegroundColor Cyan

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Warning "Docker não encontrado no PATH."
} else {
    docker compose -f "$root\docker-compose.yml" ps
}

if (Get-Command dotnet -ErrorAction SilentlyContinue) {
    Write-Host "dotnet:" (dotnet --version)
} else {
    Write-Warning "dotnet SDK não encontrado."
}

if (Get-Command node -ErrorAction SilentlyContinue) {
    Write-Host "node:" (node --version)
} else {
    Write-Warning "node não encontrado."
}

Write-Host ""
Write-Host "Próximos passos (veja RODAR-SPONTE.md):" -ForegroundColor Green
Write-Host "  1. docker compose up -d sqlserver rabbitmq"
Write-Host "  2. Executar scripts sql/01,02,04,05,06,07"
Write-Host "  3. dotnet run --project src\EduFlow.Api"
Write-Host "  4. dotnet run --project src\EduFlow.Workers"
Write-Host "  5. cd src\frontend && npm run dev"
Write-Host "  6. /settings → código 101411 + token + Situacao=2|TOP=5"
Write-Host "  7. Dashboard → Sync alunos"
