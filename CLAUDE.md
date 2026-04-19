# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Build
dotnet build MUNIDENUNCIA/MUNIDENUNCIA.csproj

# Run (default port http://localhost:5013)
dotnet run --project MUNIDENUNCIA/MUNIDENUNCIA.csproj

# Database migrations (from Package Manager Console in Visual Studio)
Add-Migration <MigrationName>
Update-Database

# CLI equivalent
dotnet ef migrations add <MigrationName> --project MUNIDENUNCIA
dotnet ef database update --project MUNIDENUNCIA
```

Connection string is read from `appsettings.json` → `ConnectionStrings:DefaultConnection`, or from .NET Secret Manager (UserSecretsId: `aspnet-MUNIDENUNCIA-a3ba2f6f-e302-4a70-b9cf-0b81487334bd`).

## Architecture

ASP.NET Core 8 MVC app on SQL Server via Entity Framework Core. The project is an academic security-training platform — it intentionally ships **paired controllers**: one secure implementation and one deliberately vulnerable, used in classroom demos.

### Controller pairs (secure vs. vulnerable)
| Secure | Vulnerable | Topic |
|---|---|---|
| `SsrfSeguroController` | `SsrfVulnerableController` | SSRF / TOCTOU |
| `IntegridadSeguroController` | `IntegridadVulnerableController` | Data integrity / digital signatures |
| `DenunciasCla4Controller` | `DenunciasCla4VulnerableController` | Citizen complaints (Clase 4) |
| `PermisosController` | `PermisosVulnerableController` | Construction permits |

Never "fix" a vulnerable controller — the vulnerabilities are intentional teaching material.

### Key services
- `DataProtectionService` — wraps ASP.NET Data Protection API to encrypt PII fields (`CedulaCifrada`, `TelefonoCifrado`, `EmailCifrado`) stored in `Denuncias`. Encrypted columns cannot be searched with LIKE; decrypt in memory.
- `TwoFactorAuthService` — TOTP-based MFA using `Otp.NET`. The TOTP secret is stored encrypted via a `IDataProtector` with purpose `"MuniDenuncia.Mfa.TotpSecret.v1"` — the same purpose string must be used in both `AccountController` and `ManageController`.
- `FirmaDigitalService` — singleton that loads RSA keys once on startup from `App_Data/firma-digital/`.
- `AnomalyDetectionService` — detects behavioral anomalies (rate, IP changes, etc.).
- `IpFilteringHttpHandler` — `DelegatingHandler` registered as transient, chained into the `"SsrfSeguro"` named `HttpClient` to block SSRF to private/loopback addresses.
- `FileUploadService` — validates and stores PDF attachments; 10 MB multipart limit enforced in `Program.cs`.

### Data model
- `ApplicationUser` extends `IdentityUser` with `Cedula`, `NombreCompleto`, `Departamento`, `FechaRegistro`, `MfaEnabledOn`, `RecoveryCodesHash`, `RequiereCambioContrasena`.
- `Denuncia` — sensitive fields stored encrypted; plaintext properties are `[NotMapped]` and populated by `DataProtectionService` after loading.
- `SolicitudPermiso` + `Comentario` — construction-permit workflow entities.
- `AuditLog` — all auth and sensitive events are written here and to Serilog (`logs/security-<date>.txt`).

### Roles and authorization
Three roles: `Ciudadano`, `Funcionario`, `Administrador`. Policies are defined inline in `Program.cs` and also available as named constants via `AuthorizationPoliciesExtensions` in `Configuration/AuthorizationPolicies.cs` (currently disabled — the extension method call is commented out in `Program.cs`).

### Security middleware (Program.cs order matters)
`UseRouting` → `UseAuthentication` → `UseAuthorization` → `UseRateLimiter`. Security response headers (CSP, HSTS, COOP, CORP, X-Frame-Options, etc.) are injected in a `Use` middleware block registered before `UseHttpsRedirection`. Rate limiting: 5 req/min on `/Account/Login`, 100 req/min general.

### Test credentials (from README)
- Ciudadano: `juan.perez` / `12345`
- Administrador: `admin` / `admin`
- `wwwroot/lib` is gitignored — run `dotnet restore` or restore via `libman` if front-end assets are missing.
