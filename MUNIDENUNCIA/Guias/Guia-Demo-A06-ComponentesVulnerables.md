# Guía de Demostración — A06: Componentes Vulnerables y Desactualizados

**Semana 3 · OWASP Top 10 A06:2021 — Vulnerable and Outdated Components**
**Tiempo total: ~12 minutos**

---

## La idea que debe quedar grabada

> Una dependencia desactualizada convierte código seguro en código inseguro, sin tocar una sola línea propia.

---

## Paso 1 — La pregunta que abre el tema (2 min)

**"¿Qué tan seguro es este sistema si todo el código propio está bien escrito, pero una de las librerías que usa tiene un CVE publicado?"**

Dejar que los estudiantes respondan. La respuesta correcta: **tan inseguro como la librería más débil**. El atacante no ataca tu código — ataca la librería que tú importaste.

Ejemplo real que conecta: en 2021, Log4Shell (CVE-2021-44228) afectó a miles de sistemas de gobierno porque usaban Apache Log4j. No importaba qué tan bien escrito estuviera el código propio.

---

## Paso 2 — Agregar un paquete intencionalmente vulnerable (2 min)

Abrir terminal en la raíz del proyecto y ejecutar:

```bash
# System.Text.RegularExpressions 4.3.0 tiene CVE-2019-0820
# Es una vulnerabilidad de tipo DoS por ReDoS (expresión regular catastrófica)
dotnet add package System.Text.RegularExpressions --version 4.3.0
```

**Mientras se instala, explicar:** este paquete ya viene incluido en .NET, pero al agregar una versión antigua como dependencia explícita, estamos "bajando" a una versión con CVE conocido — exactamente lo que pasa cuando un equipo no actualiza sus dependencias.

---

## Paso 3 — Ejecutar la auditoría (3 min)

```bash
dotnet list package --vulnerable --include-transitive
```

### Resultado esperado en pantalla

```
The following sources were used:
   https://api.nuget.org/v3/index.json

Project `MUNIDENUNCIA` has the following vulnerable packages

   [net8.0]:
   Top-level Package                  Requested  Resolved  Severity  Advisory URL
   > System.Text.RegularExpressions   4.3.0      4.3.0     High      https://github.com/advisories/GHSA-cmhx-cq75-c4mj
```

**Señalar:** la columna `Advisory URL` lleva directamente al CVE. Ahí se explica el vector de ataque, el CVSS score, y si hay parche disponible.

### También mostrar los paquetes desactualizados

```bash
dotnet list package --outdated
```

Diferencia clave para los estudiantes:
- `--vulnerable` → tiene CVE publicado → **urgente**
- `--outdated` → hay versión nueva → **revisar en el próximo sprint**

---

## Paso 4 — El pipeline de CI que lo automatiza (3 min)

Abrir `.github/workflows/security-audit.yml` y mostrar los puntos clave:

```yaml
# Se ejecuta en cada push Y diariamente a las 6:00 UTC
on:
  push:
    branches: [master, develop, 'feature/**']
  schedule:
    - cron: '0 6 * * *'   # ← CLAVE: detecta CVEs publicados DESPUÉS del merge
```

**Por qué el cron diario importa:** un paquete que hoy es seguro puede tener un CVE publicado mañana. Si solo se audita en el push, ese CVE existirá en producción hasta el próximo commit.

```yaml
# El build FALLA si hay vulnerabilidades — no llega a producción
- name: Fallar build si hay CVEs
  if: steps.audit.outputs.vuln_count != '0'
  run: exit 1
```

**Punto pedagógico:** no es suficiente con saber que hay un CVE — hay que **bloquearlo** en el pipeline. Un aviso que nadie lee no protege nada.

---

## Paso 5 — Limpiar después de la demo (1 min)

```bash
dotnet remove package System.Text.RegularExpressions
```

---

## Concepto que debe quedar grabado

| Sin A06 | Con A06 |
|---------|---------|
| "Seguro" hoy, vulnerable mañana sin saberlo | CVE nuevo → alerta automática esa noche |
| Revisión manual periódica (o nunca) | `dotnet list package --vulnerable` en cada push |
| El equipo decide cuándo actualizar | El pipeline **obliga** a actualizar antes de mergear |

---

## Conexión Ley 8968

**Art. 10** — deber de seguridad: usar un componente con CVE conocido y no actualizarlo es una negligencia técnica **documentada y pública** (el CVE está en el NVD). En una auditoría de PRODHAB, "no sabíamos" no es una defensa válida cuando la herramienta de detección es gratuita y tarda 10 segundos.

---

## Script reutilizable

Ver `Scripts/AuditoriaNuGet.ps1` — ejecuta la auditoría completa localmente y genera un reporte en `audit-report.json`.
