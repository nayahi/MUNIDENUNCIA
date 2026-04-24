# Guía de Demostración — A10: SSRF (Falsificación de Solicitudes del Lado del Servidor)

**Semana 3 · OWASP Top 10 A10:2021 — Server-Side Request Forgery**
**Tiempo total: ~15 minutos**

---

## La idea que debe quedar grabada

> SSRF convierte el servidor de la municipalidad en un **proxy** que el atacante controla. El servidor hace las peticiones en nombre del atacante — desde adentro de la red interna, donde no hay firewall.

---

## Paso 1 — La pregunta que abre el tema (2 min)

**"Este sistema puede consultar el API de Hacienda para validar facturas. El usuario pega la URL del servicio. ¿Qué impide que en lugar de pegar la URL de Hacienda, pegue `http://localhost/admin` o `http://192.168.1.1/config`?"**

En el controlador vulnerable: absolutamente nada. El servidor hace la petición y devuelve la respuesta.

Contexto municipal: sistemas que integran TSE (cédulas), Hacienda (facturas), SINPE (pagos) son candidatos naturales a SSRF si aceptan URLs del usuario sin validar.

---

## Paso 2 — Demostrar el ataque SSRF (3 min)

Navegar a `/SsrfVulnerable`.

### Ataque 1: acceso al servidor mismo

```
URL a pegar: http://localhost:5013/Auditoria
```

El servidor hace la petición a **sí mismo** y devuelve el HTML del dashboard de auditoría — una página que normalmente requiere rol Administrador. El atacante la ve sin autenticarse.

### Ataque 2: escaneo de la red interna

```
URL a pegar: http://192.168.1.1/
```

El servidor intenta conectarse al router de la red local. Dependiendo de la respuesta (timeout vs. connection refused), el atacante puede inferir qué IPs están activas.

### Ataque 3: metadata de cloud (el más peligroso en producción)

```
URL a pegar: http://169.254.169.254/latest/meta-data/iam/security-credentials/
```

En AWS/Azure/GCP, esta IP especial devuelve las credenciales temporales del servidor. Un SSRF contra esta URL en producción equivale a robar las llaves de toda la infraestructura cloud.

**Señalar en código** `SsrfVulnerableController.cs`:

```csharp
// ⚠️ VULNERABLE: No valida nada sobre la URL
var client = _httpClientFactory.CreateClient();
var response = await client.GetAsync(url);   // cualquier URL pasa
```

---

## Paso 3 — Demostrar la defensa (4 min)

Navegar a `/SsrfSeguro` y repetir los mismos intentos.

### Whitelist de dominios

```
URL a pegar: http://localhost:5013/Auditoria
```

Resultado: **bloqueado** — `localhost` no está en la whitelist.

```
URL a pegar: http://169.254.169.254/latest/meta-data/
```

Resultado: **bloqueado** — dominio no permitido.

### Mostrar la whitelist en código `SsrfSeguroController.cs`:

```csharp
private static readonly Dictionary<string, string> ServiciosPermitidos = new()
{
    ["api.hacienda.go.cr"]  = "Ministerio de Hacienda",
    ["api.tse.go.cr"]       = "Tribunal Supremo de Elecciones",
    ["gee.bccr.fi.cr"]      = "Banco Central - Tipo de Cambio",
    ["www.pgrweb.go.cr"]    = "Procuraduría General - SINALEVI",
};
```

**Capas de defensa (señalar cada una):**

| Capa | Qué bloquea |
|------|-------------|
| Whitelist de dominio | URLs a servicios no aprobados |
| Validación de esquema HTTPS | `file://`, `ftp://`, `dict://` |
| Bloqueo de IPs privadas | `localhost`, `192.168.x.x`, `10.x.x.x`, `169.254.x.x` |
| Resolución DNS + revalidación | Bypasses con dominios que resuelven a IPs privadas |
| Timeout restrictivo | Escaneo de puertos por tiempo de respuesta |

---

## Paso 4 — TOCTOU: el bypass que la whitelist no alcanza (4 min)

**Este es el punto más sofisticado — dejarlo para el final si el tiempo apremia.**

Navegar a `/SsrfToctouDemo`.

### El ataque TOCTOU (Time Of Check - Time Of Use)

```
1. Atacante registra: malicioso.hacienda-demo.example
2. Primera consulta DNS (check):  → 8.8.8.8  (IP pública → pasa la whitelist)
3. Atacante cambia el DNS con TTL=0
4. Segunda consulta DNS (use):    → 127.0.0.1 (IP privada → el servidor ya conectó)
```

La validación y la conexión usan **distintas consultas DNS**. Entre una y otra, el atacante cambió el DNS. La whitelist vio un dominio "bueno" con IP "buena" — pero la conexión llegó a `localhost`.

### La mitigación: `IpFilteringHttpHandler`

```csharp
// Resolver DNS ANTES de conectar y validar la IP resultante
var ipAddresses = await Dns.GetHostAddressesAsync(host);
foreach (var ip in ipAddresses)
{
    if (IsPrivateOrReserved(ip))
        throw new HttpRequestException("IP privada bloqueada post-resolución DNS");
}
// Solo conectar si TODAS las IPs son públicas
```

**Punto clave:** el handler valida la IP resuelta, no solo el nombre de dominio. DNS rebinding ya no funciona porque la IP se valida justo antes de conectar.

---

## Resumen de capas de defensa

```
URL del usuario
      ↓
  ¿esquema HTTPS?          → no → RECHAZAR
      ↓
  ¿dominio en whitelist?   → no → RECHAZAR
      ↓
  Resolver DNS
      ↓
  ¿IP resultante pública?  → no → RECHAZAR  (bloquea TOCTOU/DNS rebinding)
      ↓
  Conectar con timeout
      ↓
  Respuesta limitada en tamaño
```

---

## Conexión Ley 8968

**Art. 10** — deber de seguridad: un SSRF exitoso puede exponer toda la base de datos de denuncias ciudadanas — nombres, cédulas, teléfonos, emails cifrados con las claves que también están en el servidor. No es una vulnerabilidad teórica: está en el Top 10 de OWASP precisamente porque ocurre constantemente en sistemas de integración gubernamental.

---

## Archivos relevantes

| Archivo | Qué muestra |
|---------|-------------|
| `Controllers/SsrfVulnerableController.cs` | El proxy abierto sin validación |
| `Controllers/SsrfSeguroController.cs` | Whitelist + validación de esquema + bloqueo de IPs |
| `Controllers/SsrfToctouDemoController.cs` | DNS rebinding y la mitigación post-resolución |
| `Services/IpFilteringHttpHandler.cs` | El handler que valida la IP resuelta antes de conectar |
