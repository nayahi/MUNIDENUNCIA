escribir # Guía de Demostración — A09: Fallas en el Registro y Monitoreo

**Semana 3 · OWASP Top 10 A09:2021 — Security Logging and Monitoring Failures**

---

## Objetivo pedagógico

Mostrar que el problema no es la ausencia total de logs, sino la ausencia de:

- Logs **suficientemente detallados** (qué pasó, quién, desde dónde, cuándo)
- **Reglas** que conviertan logs crudos en alertas accionables
- **Respuesta** cuando se detecta una anomalía

Conexión con **Ley 8968 Art. 10**: el deber de seguridad incluye detectar accesos no autorizados, no solo prevenirlos.

---

## Archivos clave

| Archivo | Propósito |
|---------|-----------|
| `Services/AnomalyDetectionService.cs` | Las 5 reglas de detección |
| `Controllers/AuditoriaController.cs` | Dashboard + logs + exportación CSV firmada |
| `Views/Auditoria/Index.cshtml` | Dashboard visual con alertas |
| `Views/Auditoria/Logs.cshtml` | Listado filtrable y paginado |

---

## Paso 0 — Pregunta abre-debate (2 min)

**"¿Qué pasa si alguien prueba 500 contraseñas por hora? ¿Cómo se entera la municipalidad?"**

La respuesta esperada es "mirando los logs". Contrapreguntar: **"¿Y si nadie los mira?"**

Ahí entra la diapositiva: *Puntos ciegos vs. Cobertura total*. A09 no es "tener logs" — es tener logs **+ reglas + alertas + respuesta**.

---

## Paso 1 — Generar eventos reales (3 min)

Antes de mostrar el dashboard, generar eventos para que las alertas se activen:

### Opción A — En vivo (recomendada)

1. Abrir ventana de incógnito
2. Navegar a `/Account/Login`
3. Intentar login con credenciales incorrectas **6 veces seguidas**
4. Los estudiantes ven el sistema detectarlo en tiempo real al abrir el dashboard

### Opción B — Datos pre-sembrados (si no hay tiempo)

Insertar directamente en SQL Server antes de clase:

```sql
INSERT INTO AuditLogs (EventType, UserId, IpAddress, Success, Timestamp, Description)
SELECT
  'LOGIN_FAILED',
  NULL,
  '192.168.1.100',
  0,
  DATEADD(MINUTE, -value, GETUTCDATE()),
  'Login fallido en demo'
FROM (VALUES (1),(2),(3),(4),(5),(6)) AS t(value);
```

---

## Paso 2 — Dashboard `/Auditoria` (5 min)

Loguear como `admin / admin` y navegar a `/Auditoria`.

### Qué señalar en pantalla

| Elemento visual | Concepto a explicar |
|----------------|---------------------|
| Badge rojo "X altas" | Severidad — no todas las alertas valen igual |
| Fila `LoginFallidoMultiple` en rojo | Regla 1: 5 intentos/IP/5 min → posible fuerza bruta |
| Métricas 24h | La **relación** entre exitosos y fallidos delata un ataque |
| Tabla "Últimos 20 eventos" | Lo que vería un operador en tiempo real |

### Punto pedagógico

Mostrar el código de la Regla 1 a lado del browser. Un `HAVING COUNT(*) >= 5` en SQL es todo lo que separa "tener un log" de "tener una alerta".

---

## Paso 3 — Las 5 reglas en código (5 min)

Abrir `AnomalyDetectionService.cs` y recorrer regla por regla:

```
Regla 1 → LoginFallidoMultiple   → 5 min   → alta   → agrupar por IP
Regla 2 → MfaFallidoMultiple     → 10 min  → alta   → agrupar por usuario
Regla 3 → AccesoMasivoDenuncias  → 1 hora  → media  → dato personal masivo (Ley 8968)
Regla 4 → DesactivacionMfa       → 24h     → media  → evento administrativo raro
Regla 5 → UsoCodigoRespaldo      → 24h     → baja   → puede indicar pérdida de dispositivo
```

### Pregunta al aula

**"¿Qué otras reglas agregarían para una municipalidad?"**

Ejemplos esperados:
- Funcionario que descarga denuncias de un barrio que no le corresponde
- Acceso a datos desde IP de otro país (geolocalización)
- Cambio de email o contraseña fuera de horario laboral

---

## Paso 4 — Filtrado de logs `/Auditoria/Logs` (3 min)

Demostrar los filtros en vivo:

1. Filtrar por `tipo = LOGIN_FAILED`
2. Filtrar por IP sospechosa
3. Filtrar por rango de fecha (señalar la etiqueta **UTC** y explicar por qué importa)

### Conexión Ley 8968

**Art. 17**: la institución debe responder en 5 días a solicitudes del titular de los datos. El filtro por usuario es exactamente el mecanismo para cumplirlo: "todos los eventos del ciudadano X en el período Y".

---

## Paso 5 — Exportar CSV firmado (3 min)

Hacer click en "Exportar CSV firmado" → descarga un ZIP con dos archivos:

- `auditoria.csv` — legible en Excel
- `auditoria.csv.sig` — la firma digital (RSA-2048, SHA-256, PSS)

### Verificar integridad en terminal

```bash
# Descargar la clave pública desde /Firma/ClavePublica
openssl dgst -sha256 -sigopt rsa_padding_mode:pss \
  -verify munidenuncia-public-key.pem \
  -signature auditoria.csv.sig \
  auditoria.csv
# → Verified OK
```

**Punto de conexión A08↔A09:** si alguien modifica el CSV para borrar su rastro, la firma falla. El registro de auditoría es **inviolable** porque está firmado.

---

## Paso 6 — El gap: alertas en tiempo real (2 min)

### Pregunta al aula

**"¿Qué problema tiene este sistema todavía?"**

Respuesta: las alertas solo aparecen cuando alguien abre el dashboard (*pull*). Un ataque de fuerza bruta a las 3 AM pasa desapercibido hasta las 8 AM.

### Diseño del INotificationService (no implementado)

```
DetectarAnomalias()            ──→  INotificationService.NotificarAlerta()
        ↑                                        ↓
BackgroundService (cada 60s)       LogCritical / Email / Slack / SignalR
```

Las implementaciones posibles se enchufan sin tocar `AnomalyDetectionService`:

| Implementación | Cuándo usar |
|---------------|-------------|
| `LogNotificationService` | Demo mínima — escribe en Serilog |
| `EmailNotificationService` | SmtpClient / SendGrid |
| `SignalRNotificationService` | Push al browser sin polling |
| `WebhookNotificationService` | POST a Slack / Teams / PagerDuty |

---

## Resumen de controles implementados

| Control | Archivo | Estado |
|---------|---------|--------|
| 5 reglas de detección de anomalías | `AnomalyDetectionService.cs` | ✅ |
| Dashboard con severidad y métricas 24h | `Views/Auditoria/Index.cshtml` | ✅ |
| Logs filtrables por tipo, usuario, IP, fecha (UTC) | `AuditoriaController.cs` | ✅ |
| Exportación CSV con firma digital (A08) | `AuditoriaController.cs` | ✅ |
| Acceso restringido a rol Administrador | `[Authorize(Policy="RequiereAdministrador")]` | ✅ |
| Alertas push / notificaciones en tiempo real | `INotificationService` (diseñado, no implementado) | ⏳ Semana 4 |

---

## Tiempo estimado de la demo

| Paso | Duración |
|------|----------|
| 0 — Pregunta debate | 2 min |
| 1 — Generar eventos | 3 min |
| 2 — Dashboard | 5 min |
| 3 — Código de las 5 reglas | 5 min |
| 4 — Filtrado de logs | 3 min |
| 5 — Exportar firmado | 3 min |
| 6 — Gap + diseño INotificationService | 2 min |
| **Total** | **~23 min** |
