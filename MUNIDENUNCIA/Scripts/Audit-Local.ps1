# =============================================================================
# Audit-Local.ps1
# Semana 3 - A06: Auditoría local equivalente al workflow CI/CD
# Ubicación: Scripts/Audit-Local.ps1
# =============================================================================
# PROPÓSITO
# Este script es el EQUIVALENTE LOCAL del workflow security-audit.yml.
# Permite que un desarrollador o auditor:
#   - Ejecute la misma auditoría que corre GitHub Actions, ANTES de hacer push
#   - Genere el mismo reporte JSON
#   - Vea el mismo tipo de output pedagógico
#
# Útil en instituciones donde:
#   - No hay GitHub Actions (ej: GitLab self-hosted, Azure DevOps interno)
#   - La red no permite conexiones hacia api.nuget.org desde el servidor CI
#   - Un auditor externo necesita replicar la auditoría en su máquina
#
# USO
#   PS> .\Scripts\Audit-Local.ps1
#   PS> .\Scripts\Audit-Local.ps1 -FallarEnCVE   # Termina con exit 1 si hay CVEs
#   PS> .\Scripts\Audit-Local.ps1 -Proyecto "MuniDenuncia.csproj" -Salida "reporte.json"
#
# COMPATIBILIDAD: PowerShell 5.1+ (Windows 10/11) y PowerShell 7 (cross-platform).
# =============================================================================

[CmdletBinding()]
param(
    [string]$Proyecto = "MuniDenuncia.csproj",
    [string]$Salida   = "audit-report.json",
    [switch]$FallarEnCVE
)

$ErrorActionPreference = "Stop"

# -----------------------------------------------------------------------------
# Función auxiliar: imprimir encabezado con separadores
# -----------------------------------------------------------------------------
function Write-Seccion {
    param([string]$Titulo)
    Write-Host ""
    Write-Host ("=" * 75) -ForegroundColor Cyan
    Write-Host "  $Titulo" -ForegroundColor Cyan
    Write-Host ("=" * 75) -ForegroundColor Cyan
}

# -----------------------------------------------------------------------------
# PASO 0: Verificar que .NET 8 SDK esté disponible
# -----------------------------------------------------------------------------
Write-Seccion "Verificación del entorno"
try {
    $versionDotnet = & dotnet --version
    Write-Host ".NET SDK detectado: $versionDotnet" -ForegroundColor Green
}
catch {
    Write-Host "ERROR: .NET SDK no está instalado o no está en el PATH." -ForegroundColor Red
    exit 1
}

# Verificar que el archivo de proyecto exista
if (-not (Test-Path $Proyecto)) {
    Write-Host "ERROR: No se encontró el archivo $Proyecto en la ubicación actual." -ForegroundColor Red
    Write-Host "       Ejecutar el script desde la raíz del proyecto MuniDenuncia." -ForegroundColor Yellow
    exit 1
}

# -----------------------------------------------------------------------------
# PASO 1: Restaurar dependencias
# -----------------------------------------------------------------------------
Write-Seccion "Restaurando dependencias NuGet"
& dotnet restore $Proyecto
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Falló la restauración de paquetes." -ForegroundColor Red
    exit 1
}

# -----------------------------------------------------------------------------
# PASO 2: Auditoría de vulnerabilidades (directas + transitivas)
# -----------------------------------------------------------------------------
Write-Seccion "Auditoría de paquetes vulnerables (CVE conocidos)"
& dotnet list $Proyecto package --vulnerable --include-transitive

# Generar también la versión JSON para análisis programático
& dotnet list $Proyecto package --vulnerable --include-transitive `
    --format json --output-version 1 | Out-File -FilePath $Salida -Encoding utf8

Write-Host ""
Write-Host "Reporte JSON guardado en: $Salida" -ForegroundColor Green

# -----------------------------------------------------------------------------
# PASO 3: Contar vulnerabilidades en el JSON generado
# -----------------------------------------------------------------------------
$contadorCVE = 0
if (Test-Path $Salida) {
    try {
        $reporte = Get-Content $Salida -Raw | ConvertFrom-Json

        foreach ($proyecto in $reporte.projects) {
            foreach ($framework in $proyecto.frameworks) {
                foreach ($paquete in $framework.topLevelPackages) {
                    if ($paquete.vulnerabilities) {
                        $contadorCVE += $paquete.vulnerabilities.Count
                    }
                }
                foreach ($paquete in $framework.transitivePackages) {
                    if ($paquete.vulnerabilities) {
                        $contadorCVE += $paquete.vulnerabilities.Count
                    }
                }
            }
        }
    }
    catch {
        Write-Host "AVISO: No se pudo parsear el JSON. Formato inesperado." -ForegroundColor Yellow
    }
}

# -----------------------------------------------------------------------------
# PASO 4: Paquetes deprecated (informativo, no falla)
# -----------------------------------------------------------------------------
Write-Seccion "Paquetes marcados como deprecated"
& dotnet list $Proyecto package --deprecated

# -----------------------------------------------------------------------------
# PASO 5: Paquetes desactualizados (informativo)
# -----------------------------------------------------------------------------
Write-Seccion "Paquetes con versiones más recientes disponibles"
& dotnet list $Proyecto package --outdated

# -----------------------------------------------------------------------------
# PASO 6: Resumen final y código de salida
# -----------------------------------------------------------------------------
Write-Seccion "Resumen de auditoría"
Write-Host "Vulnerabilidades detectadas: $contadorCVE" -ForegroundColor $(
    if ($contadorCVE -eq 0) { "Green" } else { "Red" }
)

if ($contadorCVE -eq 0) {
    Write-Host ""
    Write-Host "✓ No se detectaron vulnerabilidades conocidas (CVE) en las dependencias." -ForegroundColor Green
    Write-Host "  Recordar ejecutar esta auditoría al menos una vez por semana" -ForegroundColor Green
    Write-Host "  (los CVE nuevos se publican constantemente)." -ForegroundColor Green
    exit 0
}
else {
    Write-Host ""
    Write-Host "✗ Se detectaron $contadorCVE vulnerabilidades." -ForegroundColor Red
    Write-Host "  Acciones recomendadas:" -ForegroundColor Yellow
    Write-Host "    1. Revisar $Salida para detalles de cada CVE" -ForegroundColor Yellow
    Write-Host "    2. Consultar https://github.com/advisories para severidad y patches" -ForegroundColor Yellow
    Write-Host "    3. Actualizar los paquetes afectados con: dotnet add package <nombre>" -ForegroundColor Yellow
    Write-Host "    4. Re-ejecutar este script para confirmar que las vulns fueron resueltas" -ForegroundColor Yellow

    if ($FallarEnCVE) {
        Write-Host ""
        Write-Host "Modo -FallarEnCVE activado. Exit 1." -ForegroundColor Red
        exit 1
    }
    exit 0
}
