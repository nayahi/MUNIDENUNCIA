# Guía de Demostración — A07: Fallas de Identificación y Autenticación

**Semana 3 · OWASP Top 10 A07:2021 — Identification and Authentication Failures**
**Tiempo total: ~15 minutos**

---

## La idea que debe quedar grabada

> Una contraseña sola no es suficiente. Si se filtra, el atacante entra. El segundo factor asegura que **saber la contraseña no es suficiente para autenticarse**.

---

## Paso 1 — La pregunta que abre el tema (2 min)

**"Si alguien consigue la contraseña de un funcionario municipal — por phishing, por una base de datos filtrada, o porque la anotó en un Post-it — ¿qué impide que entre al sistema?"**

Sin MFA: nada. Con MFA: necesita también el teléfono del funcionario.

Mostrar en pantalla: `haveibeenpwned.com` y buscar un email genérico. Explicar que hay miles de millones de credenciales en bases de datos filtradas — el atacante no necesita adivinar, solo buscar.

---

## Paso 2 — Activar MFA en vivo (4 min)

Loguear como `juan.perez / 12345` y navegar a `/Manage/ConfigurarMfa`.

### Lo que el estudiante ve

1. Un **código QR** — escanearlo con Google Authenticator / Microsoft Authenticator
2. El app genera un código de 6 dígitos que cambia cada 30 segundos
3. Ingresar el código para confirmar que el QR se escaneó correctamente
4. El sistema muestra los **8 códigos de respaldo** (única vez que se muestran)

### Lo que mostrar en el código

Abrir `Services/TwoFactorAuthService.cs`:

```csharp
// Secreto de 160 bits — estándar RFC 4226
private const int TamanioSecreto = 20;

// NUNCA usar System.Random — usa generador criptográfico
var bytesAleatorios = RandomNumberGenerator.GetBytes(TamanioSecreto);
```

**Punto clave:** el QR no es magia — es una URI `otpauth://totp/...` con el secreto en Base32. El teléfono y el servidor comparten ese secreto y calculan el mismo código en el mismo instante (TOTP = Time-based One-Time Password).

---

## Paso 3 — Demostrar el segundo factor en el login (3 min)

Hacer logout y volver a loguear como `juan.perez / 12345`.

- El sistema **no da acceso todavía** — pide el código TOTP
- Mostrar la pantalla `/Account/LoginMfa`
- Ingresar el código del authenticator → acceso concedido

**Pregunta al aula:** "¿Qué pasa si el atacante tiene la contraseña pero no el teléfono?"  
Respuesta: la pantalla de TOTP aparece pero el atacante no puede generar el código correcto — el secreto nunca salió del servidor ni del teléfono.

### Mostrar la protección anti-replay en código

Abrir `Controllers/AccountController.cs`, buscar `LoginMfa`:

```csharp
// El código TOTP solo es válido UNA VEZ dentro de su ventana de 30s
// Se guarda en caché para evitar que el mismo código se use dos veces
if (_replayCache.TryGetValue(codigoLimpio + userId, out _))
{
    // Código ya usado — posible replay attack
    return View(modelo);
}
```

**Por qué importa:** sin esta protección, si alguien captura el código en tránsito (MITM) podría usarlo en los próximos 30 segundos.

---

## Paso 4 — Códigos de respaldo (2 min)

**Escenario:** el funcionario pierde su teléfono el día de una emergencia. ¿Qué hace?

Los códigos de respaldo permiten entrar **una sola vez** sin TOTP. Después se invalidan.

Mostrar en código `TwoFactorAuthService.cs`:

```csharp
// Formato: XXXX-XXXX — legible, difícil de confundir dígitos
var numero = BitConverter.ToUInt32(bytes, 0) % 100_000_000U;
codigos.Add($"{numero / 10_000:D4}-{numero % 10_000:D4}");
```

**Punto pedagógico:** los códigos de respaldo son como la llave de repuesto de tu casa. Si los guardás en el mismo lugar que la llave principal, no sirven de nada.

---

## Paso 5 — El rate limiter que frena la fuerza bruta (2 min)

Sin límite de intentos, un atacante puede probar todos los códigos TOTP posibles (000000–999999 = 1 millón). Con la ventana de 30 segundos y 1 millón de códigos, en teoría podría probarse en horas.

Mostrar `Program.cs`:

```csharp
// 5 intentos por minuto en el endpoint de MFA
options.AddFixedWindowLimiter("mfa", opt => {
    opt.PermitLimit   = 5;
    opt.Window        = TimeSpan.FromMinutes(1);
});
```

5 intentos/minuto → probar 1 millón de códigos tomaría **138 días**. Para entonces el código ya cambió millones de veces.

---

## Resumen visual para los estudiantes

```
SIN MFA:                         CON MFA:
Contraseña → DENTRO              Contraseña + TOTP → DENTRO
Contraseña robada → DENTRO       Contraseña robada → PANTALLA DE TOTP
                                 Contraseña robada + TOTP robado → replay bloqueado
                                 500 intentos de fuerza bruta → rate limiter
```

---

## Conexión Ley 8968

**Art. 10** — medidas de seguridad razonablemente exigibles. Para cuentas de funcionarios con acceso a denuncias ciudadanas (datos sensibles), el MFA es considerado hoy una medida mínima. Su ausencia podría interpretarse como negligencia en una auditoría de PRODHAB.

---

## Archivos relevantes

| Archivo | Qué muestra |
|---------|-------------|
| `Services/TwoFactorAuthService.cs` | Generación de secreto, QR, verificación TOTP, códigos de respaldo |
| `Controllers/AccountController.cs` | Login en dos pasos, anti-replay, rate limiter |
| `Controllers/ManageController.cs` | Activar/desactivar MFA, ver códigos de respaldo |
