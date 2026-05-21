# Clique direito -> Executar como administrador
# Habilita virtualizacao para WSL 2 + converte Ubuntu

$ErrorActionPreference = "Stop"

Write-Host "=== Habilitando WSL 2 (requer Admin + REINICIO) ===" -ForegroundColor Cyan

dism.exe /online /enable-feature /featurename:Microsoft-Windows-Subsystem-Linux /all /norestart
dism.exe /online /enable-feature /featurename:VirtualMachinePlatform /all /norestart

Write-Host "`nInstalando componentes WSL..."
wsl --install --no-distribution
wsl --update

Write-Host "`nConvertendo Ubuntu para WSL 2..."
wsl --set-version Ubuntu 2

wsl --shutdown
wsl -l -v

Write-Host "`n=== REINICIE O WINDOWS agora ===" -ForegroundColor Yellow
Write-Host "Depois do reboot, abra Ubuntu e rode: ./scripts/wsl/setup-ubuntu.sh"
