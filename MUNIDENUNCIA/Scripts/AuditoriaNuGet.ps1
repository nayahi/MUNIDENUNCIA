#!/usr/bin/env pwsh
# =============================================================================
# AuditoriaNuGet.ps1
# Semana 2 - A06: Componentes Vulnerables y Desactualizados
# Curso: Blindaje de Aplicaciones Web Intermedio - CPIC Costa Rica
# =============================================================================
# USO: Desde la raíz del proyecto MuniDenuncia, ejecutar:
#   pwsh ./Scripts/AuditoriaNuGet.ps1
#   -- o con dotnet CLI directamente --
#   dotnet list package --vulnerable
#   dotnet list package --outdated
# =============================================================================

Write-Host "=============================================" -ForegroundColor Cyan
Write-Host " AUDITORIA DE SEGURIDAD - PAQUETES NuGet"     -ForegroundColor Cyan
Write-Host " MuniDenuncia - OWASP A06"                     -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""

# --- Paso 1: Restaurar paquetes ---
Write-Host "[1/4] Restaurando paquetes NuGet..." -ForegroundColor Yellow
dotnet restore --verbosity quiet
Write-Host ""

# --- Paso 2: Verificar paquetes con vulnerabilidades conocidas ---
Write-Host "[2/4] Buscando paquetes con CVEs conocidos..." -ForegroundColor Yellow
Write-Host "     (dotnet list package --vulnerable --include-transitive)" -ForegroundColor DarkGray
Write-Host ""
dotnet list package --vulnerable --include-transitive
Write-Host ""

# --- Paso 3: Verificar paquetes desactualizados ---
Write-Host "[3/4] Buscando paquetes desactualizados..." -ForegroundColor Yellow
Write-Host "     (dotnet list package --outdated)" -ForegroundColor DarkGray
Write-Host ""
dotnet list package --outdated
Write-Host ""

# --- Paso 4: Verificar paquetes deprecados ---
Write-Host "[4/4] Buscando paquetes deprecados..." -ForegroundColor Yellow
Write-Host "     (dotnet list package --deprecated)" -ForegroundColor DarkGray
Write-Host ""
dotnet list package --deprecated
Write-Host ""

Write-Host "=============================================" -ForegroundColor Cyan
Write-Host " AUDITORIA COMPLETADA"                        -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "SIGUIENTE PASO: Documente los hallazgos en su ficha de auditoría." -ForegroundColor Green
Write-Host "Para cada paquete vulnerable, registre:" -ForegroundColor Green
Write-Host "  - Nombre del paquete y versión actual" -ForegroundColor Green
Write-Host "  - CVE(s) asociado(s)" -ForegroundColor Green
Write-Host "  - Severidad (Crítica/Alta/Media/Baja)" -ForegroundColor Green
Write-Host "  - Versión recomendada para actualizar" -ForegroundColor Green
Write-Host "  - Impacto potencial en el contexto municipal" -ForegroundColor Green
