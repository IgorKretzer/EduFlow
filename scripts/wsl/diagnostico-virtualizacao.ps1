# Pode rodar sem admin (alguns itens pedem admin para alterar)
Write-Host "=== Diagnostico WSL / Virtualizacao (0x80370102) ===" -ForegroundColor Cyan

$proc = Get-CimInstance Win32_Processor -ErrorAction SilentlyContinue | Select-Object -First 1
if ($proc) {
    $virt = $proc.VirtualizationFirmwareEnabled
    Write-Host "Virtualizacao na BIOS (firmware):" -NoNewline
    if ($virt -eq $true) { Write-Host " ATIVADA" -ForegroundColor Green }
    elseif ($virt -eq $false) { Write-Host " DESATIVADA  <-- HABILITE NA BIOS" -ForegroundColor Red }
    else { Write-Host " (nao foi possivel ler)" -ForegroundColor Yellow }
    Write-Host "Processador:" $proc.Name
}

Write-Host "`nWSL:"
wsl --status 2>&1
wsl -l -v 2>&1

$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if ($isAdmin) {
    Write-Host "`nRecursos Windows (Admin):"
    @("VirtualMachinePlatform", "Microsoft-Windows-Subsystem-Linux", "HypervisorPlatform") | ForEach-Object {
        $f = Get-WindowsOptionalFeature -Online -FeatureName $_ -ErrorAction SilentlyContinue
        if ($f) { Write-Host "  $($f.FeatureName): $($f.State)" }
    }
} else {
    Write-Host "`nPara ver recursos Windows: rode este script como Administrador." -ForegroundColor Yellow
}

Write-Host "`nSe BIOS = DESATIVADA ou Ubuntu VERSION = 1 com erro:"
Write-Host "  1) Ative VT-x/SVM na BIOS"
Write-Host "  2) Rode: .\scripts\wsl\habilitar-wsl2-admin.ps1 (como Admin)"
Write-Host "  3) Reinicie o PC"
Write-Host "  Guia: FIX-0x80370102.md"
