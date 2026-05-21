# Execute no PowerShell COMO ADMINISTRADOR (uma vez)
# Prepara WSL 2 para Docker no Ubuntu sem Docker Desktop

Write-Host "=== Preparar WSL 2 para EduFlow ===" -ForegroundColor Cyan

Write-Host "`nAtualizando WSL..."
wsl --update

Write-Host "`nPadrao WSL 2..."
wsl --set-default-version 2

Write-Host "`nConvertendo Ubuntu para WSL 2 (pode demorar varios minutos)..."
wsl --set-version Ubuntu 2

Write-Host "`nReiniciando WSL..."
wsl --shutdown

Write-Host "`n=== Pronto ===" -ForegroundColor Green
Write-Host "Abra o app Ubuntu e rode:"
Write-Host '  cd "/mnt/c/Users/Aninha/OneDrive/Área de Trabalho/EduFlow"'
Write-Host "  chmod +x scripts/wsl/setup-ubuntu.sh"
Write-Host "  ./scripts/wsl/setup-ubuntu.sh"
Write-Host ""
Write-Host "Depois, no Windows, 3 terminais: Api, Workers, frontend (veja RODAR-WSL.md)"
