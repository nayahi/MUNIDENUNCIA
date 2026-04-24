# =============================================================================
# Demo-A08-FirmaDigital.ps1
# Semana 3 — A08: Fallas en Software y en la Integridad de los Datos
# =============================================================================
# PROPÓSITO
#   Script de demostración interactiva para clase.
#   Ejecuta PASO A PASO los conceptos de firma digital RSA-PSS:
#
#     Paso 1 – Generar par de claves RSA-2048
#     Paso 2 – Firmar un reporte
#     Paso 3 – Verificar firma válida       → VALIDA
#     Paso 4 – Tamper: modificar el reporte (simula ataque)
#     Paso 5 – Verificar firma sobre doc alterado → INVALIDA
#     Paso 6 – Verificar con OpenSSL        → "Verified OK" / "Verification Failure"
#     Paso 7 – Rotación de claves: firmas antiguas ya no verifican
#
# REQUISITOS
#   - PowerShell 7+ (pwsh) — incluido en el entorno del repositorio
#   - .NET 8 SDK            — para ExportPkcs8PrivateKeyPem() / ImportFromPem()
#   - OpenSSL (opcional)    — solo para el Paso 6
#       Opción A: Git for Windows  → C:\Program Files\Git\usr\bin\openssl.exe
#       Opción B: winget install ShiningLight.OpenSSL.Light
#
# USO
#   pwsh .\Scripts\Demo-A08-FirmaDigital.ps1
#
#   Para demo sin pausas (grabación de pantalla):
#   pwsh .\Scripts\Demo-A08-FirmaDigital.ps1 -Auto
#
#   Para limpiar los archivos temporales generados:
#   Remove-Item .\demo-a08-tmp -Recurse -Force
# =============================================================================

param(
    [switch]$Auto   # Omite pausas entre pasos
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ─── Helpers de consola ──────────────────────────────────────────────────────

function Write-Title ([string]$msg) {
    Write-Host "`n$('='*68)`n  $msg`n$('='*68)" -ForegroundColor Cyan
}
function Write-Step ([string]$msg) {
    Write-Host "`n[PASO] $msg" -ForegroundColor Yellow
}
function Write-Ok   ([string]$msg) { Write-Host "  [OK]   $msg" -ForegroundColor Green }
function Write-Fail ([string]$msg) { Write-Host "  [!!!]  $msg" -ForegroundColor Red }
function Write-Info ([string]$msg) { Write-Host "         $msg" -ForegroundColor Gray }
function Write-Cmd  ([string]$msg) { Write-Host "  > $msg"      -ForegroundColor White }

function Pause-Demo {
    if (-not $Auto) {
        Write-Host "`n  [Presioná ENTER para continuar...]" -ForegroundColor DarkGray
        $null = Read-Host
    } else {
        Start-Sleep -Milliseconds 500
    }
}

# ─── Utilidad: buscar OpenSSL ────────────────────────────────────────────────

function Find-OpenSSL {
    $candidatos = @(
        "openssl",
        "C:\Program Files\Git\usr\bin\openssl.exe",
        "C:\Program Files\OpenSSL-Win64\bin\openssl.exe",
        "C:\Program Files (x86)\GnuWin32\bin\openssl.exe"
    )
    foreach ($c in $candidatos) {
        try {
            $v = & $c version 2>$null
            if ($LASTEXITCODE -eq 0) { return $c }
        } catch { }
    }
    return $null
}

# ─── Funciones de firma .NET (sin dependencias externas) ────────────────────

function New-RsaKeyPair ([string]$dirClaves) {
    $rsa = [System.Security.Cryptography.RSA]::Create(2048)
    try {
        [System.IO.File]::WriteAllText(
            [System.IO.Path]::Combine($dirClaves, "private-key.pem"),
            $rsa.ExportPkcs8PrivateKeyPem())
        [System.IO.File]::WriteAllText(
            [System.IO.Path]::Combine($dirClaves, "public-key.pem"),
            $rsa.ExportSubjectPublicKeyInfoPem())
        return $rsa.KeySize
    } finally {
        $rsa.Dispose()
    }
}

function Invoke-FirmarArchivo ([string]$privPemPath, [string]$archivoPath, [string]$firmaPath) {
    $rsa = [System.Security.Cryptography.RSA]::Create()
    try {
        $rsa.ImportFromPem([System.IO.File]::ReadAllText($privPemPath))
        $contenido = [System.IO.File]::ReadAllBytes($archivoPath)
        $firma = $rsa.SignData(
            $contenido,
            [System.Security.Cryptography.HashAlgorithmName]::SHA256,
            [System.Security.Cryptography.RSASignaturePadding]::Pss)
        [System.IO.File]::WriteAllBytes($firmaPath, $firma)
        return $firma.Length
    } finally {
        $rsa.Dispose()
    }
}

function Test-FirmaArchivo ([string]$pubPemPath, [string]$archivoPath, [string]$firmaPath) {
    $rsa = [System.Security.Cryptography.RSA]::Create()
    try {
        $rsa.ImportFromPem([System.IO.File]::ReadAllText($pubPemPath))
        $contenido = [System.IO.File]::ReadAllBytes($archivoPath)
        $firma     = [System.IO.File]::ReadAllBytes($firmaPath)
        return $rsa.VerifyData(
            $contenido, $firma,
            [System.Security.Cryptography.HashAlgorithmName]::SHA256,
            [System.Security.Cryptography.RSASignaturePadding]::Pss)
    } catch [System.Security.Cryptography.CryptographicException] {
        return $false   # igual que FirmaDigitalService.Verificar()
    } finally {
        $rsa.Dispose()
    }
}

# ─── Directorio de trabajo temporal ─────────────────────────────────────────

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$DemoDir  = Join-Path $repoRoot "demo-a08-tmp"
$null = New-Item -ItemType Directory -Force -Path $DemoDir

# ─── INICIO ──────────────────────────────────────────────────────────────────

Write-Title "DEMO A08 — Firma Digital de Reportes en MuniDenuncia"

Write-Info "OWASP A08: Software and Data Integrity Failures"
Write-Info ""
Write-Info "Sin firma digital, un reporte de denuncia descargado por el ciudadano"
Write-Info "puede ser modificado por cualquiera sin que nadie lo note."
Write-Info ""
Write-Info "MuniDenuncia firma cada reporte con RSA-2048 + SHA-256 + PSS padding."
Write-Info "Esta demo muestra cada paso del mecanismo y por qué importa."
Write-Info ""
Write-Info "Directorio de trabajo: $DemoDir"

Pause-Demo

# =============================================================================
# PASO 1 — Generar par de claves RSA-2048
# =============================================================================

Write-Step "1 — Generar par de claves RSA-2048"
Write-Info ""
Write-Info "  Clave PRIVADA → solo el servidor la conoce → firma documentos"
Write-Info "  Clave PÚBLICA → cualquiera puede descargarla → verifica firmas"
Write-Info ""
Write-Info "En MuniDenuncia: FirmaDigitalService genera este par en primer"
Write-Info "arranque (App_Data/firma-digital/ — excluido de wwwroot y de git)."
Write-Info ""
Write-Cmd  "RSA.Create(2048).ExportPkcs8PrivateKeyPem()  → private-key.pem"
Write-Cmd  "RSA.Create(2048).ExportSubjectPublicKeyInfoPem() → public-key.pem"

$privPath = Join-Path $DemoDir "private-key.pem"
$pubPath  = Join-Path $DemoDir "public-key.pem"

$bits = New-RsaKeyPair $DemoDir
Write-Ok "Par RSA-$bits generado."
Write-Ok "private-key.pem  ($([int](Get-Item $privPath).Length) bytes)"
Write-Ok "public-key.pem   ($([int](Get-Item $pubPath).Length) bytes)"

Pause-Demo

# =============================================================================
# PASO 2 — Crear y firmar un reporte
# =============================================================================

Write-Step "2 — Firmar un reporte oficial"
Write-Info ""
Write-Info "El servidor genera el reporte, calcula su hash SHA-256 y lo cifra"
Write-Info "con la clave privada. Eso es la firma (256 bytes para RSA-2048)."
Write-Info ""
Write-Info "La firma NO cifra el contenido — solo lo certifica."

$reportePath = Join-Path $DemoDir "reporte-denuncias.txt"
$contenido = @"
==============================================
  MUNIDENUNCIA - Reporte Oficial
  Municipalidad Demo Costa Rica
==============================================
Tipo:       denuncias
Generado:   $(Get-Date -Format 'dd/MM/yyyy HH:mm:ss') UTC
Expediente: MUN-2025-001234
Cedula:     1-0234-0567
Descripcion: Pozo pluvial obstruido en Av. Central 42
Estado:     En revision
==============================================
"@

[System.IO.File]::WriteAllText($reportePath, $contenido, [System.Text.Encoding]::UTF8)
Write-Info ""
Write-Info "Contenido del reporte:"
$contenido -split "`n" | ForEach-Object { Write-Info "  $_" }

$firmaPath = Join-Path $DemoDir "reporte-denuncias.sig"
$firmaBytes = Invoke-FirmarArchivo $privPath $reportePath $firmaPath

Write-Ok ""
Write-Ok "Archivo firmado:   reporte-denuncias.txt"
Write-Ok "Firma generada:    reporte-denuncias.sig ($firmaBytes bytes)"
Write-Info ""
Write-Info "Primeros bytes de la firma (hex):"
$firmaHex = [System.BitConverter]::ToString(
    [System.IO.File]::ReadAllBytes($firmaPath)[0..15]) -replace '-',':'
Write-Info "  $firmaHex ..."

Pause-Demo

# =============================================================================
# PASO 3 — Verificar firma válida → VALIDA
# =============================================================================

Write-Step "3 — Verificar la firma del reporte ORIGINAL → debe ser VALIDA"
Write-Info ""
Write-Info "Cualquiera con la clave pública puede verificar independientemente."
Write-Cmd  "RSA.VerifyData(contenido, firma, SHA256, PSS)"

$esValida = Test-FirmaArchivo $pubPath $reportePath $firmaPath
if ($esValida) {
    Write-Ok "FIRMA VALIDA — el documento no fue modificado."
} else {
    Write-Fail "FIRMA INVALIDA (inesperado — verificar el script)."
    exit 1
}

Pause-Demo

# =============================================================================
# PASO 4 — Tamper: modificar el reporte (simula ataque)
# =============================================================================

Write-Step "4 — ATAQUE: Modificar el reporte después de firmarlo"
Write-Info ""
Write-Info "Escenario: un funcionario malintencionado altera el expediente"
Write-Info "DESPUÉS de que el sistema lo firmó. O un MITM lo modifica en tránsito."
Write-Info ""
Write-Info "  ORIGINAL: Estado: En revision"
Write-Info "  TAMPERED: Estado: ARCHIVADO - sin accion municipal"

$tamperedPath = Join-Path $DemoDir "reporte-denuncias-TAMPERED.txt"
$contenidoTampered = $contenido -replace "En revision", "ARCHIVADO - sin accion municipal"
[System.IO.File]::WriteAllText($tamperedPath, $contenidoTampered, [System.Text.Encoding]::UTF8)

Write-Info ""
Write-Info "Archivo alterado guardado como: reporte-denuncias-TAMPERED.txt"
Write-Info "(La firma .sig NO fue modificada — el atacante la reutiliza)"

Pause-Demo

# =============================================================================
# PASO 5 — Verificar firma sobre documento alterado → INVALIDA
# =============================================================================

Write-Step "5 — Verificar la misma firma sobre el documento ALTERADO → INVALIDA"
Write-Info ""
Write-Info "La firma sigue siendo los mismos 256 bytes."
Write-Info "Pero el hash del contenido alterado es completamente diferente."
Write-Info "→ La verificación FALLA. La manipulación es detectada."

$esValida = Test-FirmaArchivo $pubPath $tamperedPath $firmaPath
if (-not $esValida) {
    Write-Fail "FIRMA INVALIDA — El documento alterado fue DETECTADO."
    Write-Ok   "La defensa A08 funciona correctamente."
} else {
    Write-Fail "FIRMA VALIDA (BUG GRAVE — la manipulación no fue detectada)."
    exit 1
}

Pause-Demo

# =============================================================================
# PASO 6 — Verificación con OpenSSL (perspectiva del ciudadano)
# =============================================================================

Write-Step "6 — Verificar con OpenSSL (como lo haría un ciudadano)"
Write-Info ""
Write-Info "El ciudadano NO depende del servidor para verificar."
Write-Info "Descarga la clave pública de  GET /Firma/ClavePublica  y ejecuta:"
Write-Info ""
Write-Cmd  "openssl dgst -sha256 -sigopt rsa_padding_mode:pss \"
Write-Cmd  "  -verify munidenuncia-public-key.pem \"
Write-Cmd  "  -signature reporte.sig reporte.txt"
Write-Info ""

$openssl = Find-OpenSSL
if ($openssl) {
    Write-Info "OpenSSL encontrado: $openssl"
    Write-Info ""

    Write-Info "Verificando reporte ORIGINAL:"
    $out = & $openssl dgst -sha256 -sigopt rsa_padding_mode:pss `
        -verify $pubPath -signature $firmaPath $reportePath 2>&1
    if ("$out" -match "Verified OK") {
        Write-Ok "OpenSSL dice: $out"
    } else {
        Write-Fail "OpenSSL dice: $out"
    }

    Write-Info ""
    Write-Info "Verificando reporte TAMPERED:"
    $out = & $openssl dgst -sha256 -sigopt rsa_padding_mode:pss `
        -verify $pubPath -signature $firmaPath $tamperedPath 2>&1
    if ("$out" -match "Verification Failure") {
        Write-Ok "OpenSSL dice: $out (correcto — manipulación detectada)"
    } else {
        Write-Fail "OpenSSL dice: $out"
    }
} else {
    Write-Info "(OpenSSL no encontrado. Instalarlo con una de estas opciones:)"
    Write-Info "  A) Git for Windows ya lo incluye:"
    Write-Info "       C:\Program Files\Git\usr\bin\openssl.exe"
    Write-Info "  B) winget install ShiningLight.OpenSSL.Light"
    Write-Info ""
    Write-Info "El Paso 6 ya quedó demostrado por PowerShell en los pasos 3 y 5."
    Write-Info "OpenSSL es solo la herramienta que usaría el ciudadano externamente."
}

Pause-Demo

# =============================================================================
# PASO 7 — Rotación de claves: firmas antiguas ya no verifican
# =============================================================================

Write-Step "7 — Rotación de claves: ¿qué pasa si el servidor regenera sus claves?"
Write-Info ""
Write-Info "ESCENARIO: el archivo de claves se borra (accidente, migración,"
Write-Info "ataque) y FirmaDigitalService genera un par de claves NUEVO."
Write-Info ""
Write-Info "→ Las firmas hechas con la clave vieja NO pueden verificarse"
Write-Info "  con la clave nueva. Todos los reportes anteriores se invalidan."
Write-Info ""

$dirClavesNuevas = Join-Path $DemoDir "claves-nuevas"
$null = New-Item -ItemType Directory -Force -Path $dirClavesNuevas

$bitsNuevos = New-RsaKeyPair $dirClavesNuevas
Write-Info "Par nuevo generado (simula reinicio del servidor con claves distintas)."
Write-Info ""

$pubNuevaPem = Join-Path $dirClavesNuevas "public-key.pem"
$esValida = Test-FirmaArchivo $pubNuevaPem $reportePath $firmaPath
if (-not $esValida) {
    Write-Fail "FIRMA INVALIDA con la clave nueva (esperado y correcto)."
    Write-Ok   "LECCIÓN: nunca perder las claves — invalida todas las firmas previas."
    Write-Info ""
    Write-Info "En producción:"
    Write-Info "  1. Generar claves fuera de banda (una sola vez)."
    Write-Info "  2. Respaldar en gestor de secretos (Azure Key Vault, Vault, etc.)."
    Write-Info "  3. Para rotación: conservar clave vieja en archivo histórico"
    Write-Info "     para seguir verificando reportes firmados anteriormente."
    Write-Info "     (Versionamiento de claves — próxima iteración del servicio)"
} else {
    Write-Fail "FIRMA VALIDA con clave nueva (BUG — dos claves distintas no deberían verificarse mutuamente)."
    exit 1
}

Pause-Demo

# =============================================================================
# RESUMEN FINAL
# =============================================================================

Write-Title "RESUMEN — A08: Software and Data Integrity Failures"

Write-Host @"

  CONTROLES IMPLEMENTADOS EN MUNIDENUNCIA
  ──────────────────────────────────────────────────────────────────

  CRIPTOGRAFÍA
    OK  RSA-2048 (NIST SP 800-57: seguro hasta ~2030)
    OK  SHA-256  (hash criptográfico — resistente a colisiones)
    OK  RSA-PSS  (padding moderno — más robusto que PKCS#1 v1.5)

  GESTIÓN DE CLAVES
    OK  Clave privada en App_Data/ — fuera de wwwroot (no expuesta por Static Files)
    OK  .gitignore excluye App_Data/firma-digital/
    OK  Permisos 600 aplicados programáticamente en Linux (File.SetUnixFileMode)
    OK  Singleton: claves cargadas UNA sola vez al arrancar

  CÓDIGO
    OK  Verificar() atrapa CryptographicException → devuelve false (no lanza)
    OK  Whitelist de tipoReporte → evita ZIP path traversal
    OK  Nombre de ZIP usa UTC → consistencia de auditoría

  TRANSPARENCIA
    OK  /Firma/ClavePublica [AllowAnonymous] → ciudadano verifica sin depender del servidor
    OK  LEEME.txt en el ZIP con instrucciones OpenSSL

  RIESGOS RESIDUALES (documentados, no implementados)
    !!  Sin versionamiento de claves: rotación invalida firmas históricas
    !!  ACL de clave privada en Windows requiere configuración manual al desplegar

  LEY 8968, ART. 10 (deber de seguridad):
    La firma digital es una medida técnica RAZONABLEMENTE EXIGIBLE
    para documentos oficiales emitidos por una entidad pública.

"@ -ForegroundColor White

Write-Info "Archivos de demo en: $DemoDir"
Write-Info "Para limpiar: Remove-Item '$DemoDir' -Recurse -Force"
