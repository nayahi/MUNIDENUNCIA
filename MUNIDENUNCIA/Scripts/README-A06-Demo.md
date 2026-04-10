# A06 - Demo de Componentes Vulnerables y Desactualizados

## Propósito
Demostrar cómo identificar paquetes NuGet con vulnerabilidades conocidas (CVEs)
usando las herramientas integradas del SDK de .NET.

### Paso 1: Agregar un paquete intencionalmente vulnerable
Para que `dotnet list package --vulnerable` produzca resultados en clase,
agregue temporalmente un paquete con CVE conocido al proyecto:

```bash
# System.Text.RegularExpressions 4.3.0 tiene CVE-2019-0820 (DoS por ReDoS)
dotnet add package System.Text.RegularExpressions --version 4.3.0

# Alternativa: System.Net.Http 4.3.0 tiene CVE-2018-8292
dotnet add package System.Net.Http --version 4.3.0
```

### Paso 2: Ejecutar la auditoría
```bash
dotnet list package --vulnerable --include-transitive
```

Resultado esperado:
```
The following sources were used:
   https://api.nuget.org/v3/index.json

Project `MuniDenuncia` has the following vulnerable packages

   [net8.0]:
   Top-level Package                    Requested   Resolved   Severity   Advisory URL
   > System.Text.RegularExpressions     4.3.0       4.3.0      High       https://github.com/advisories/GHSA-cmhx-cq75-c4mj
```

### Paso 3: Después de la demo, remover el paquete vulnerable
```bash
dotnet remove package System.Text.RegularExpressions
dotnet remove package System.Net.Http
```

## Contexto Municipal / Ley 8968
- Art. 10: Las instituciones deben implementar medidas técnicas para proteger
  datos personales. Usar componentes con CVEs conocidos es una negligencia
  técnica documentable.
- Ejemplo real CR: Si la municipalidad usa una librería de PDF con CVE que
  permite ejecución remota de código, un atacante podría acceder a las
  denuncias ciudadanas y datos personales protegidos por Ley 8968.

## Herramientas complementarias (gratuitas)
- `dotnet list package --outdated` — paquetes con nueva versión disponible
- `dotnet list package --deprecated` — paquetes marcados como obsoletos
- GitHub Dependabot — alertas automáticas en repositorio
- OWASP Dependency-Check — escaneo más profundo (Java/.NET/Python)
