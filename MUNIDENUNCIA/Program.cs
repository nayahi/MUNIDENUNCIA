using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using MUNIDENUNCIA.Data;
using MUNIDENUNCIA.Models;
using MUNIDENUNCIA.Services;
using Serilog;
using Serilog.Events;
using MuniDenuncia.Configuration;


// Configurar Serilog ANTES de crear el builder
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", (LogEventLevel)LogLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        "logs/security-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30
    )
    .CreateLogger();

//- .MinimumLevel.Information(): Registrar eventos Info, Warning y Error (no Debug)
//- .Enrich.WithMachineName(): Agregar nombre del servidor a cada log
//- .WriteTo.Console(): Mostrar en consola durante desarrollo
//- .WriteTo.File(): Guardar en archivos para producción
//- rollingInterval: RollingInterval.Day: Nuevo archivo cada día
//- retainedFileCountLimit: 30: Mantener últimos 30 días

var builder = WebApplication.CreateBuilder(args);

// Configurar Kestrel para NO enviar header Server
//Comentar para ejemplo
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.AddServerHeader = false;
});

//Ajustar bitacorizacion
builder.Host.UseSerilog();

//Ajustar db
//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//    options.UseSqlServer(
//        builder.Configuration.GetConnectionString("DefaultConnection")));

// Connection string desde variables de entorno o Secret Manager
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

//Ajustar parametros de identidad del usuario segun nist
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 12;
    options.Password.RequiredUniqueChars = 4;

    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();
//Maximo 5 intentos fallidos antes de bloquear cuenta por 15 minutos

// Configurar Data Protection API para cifrado de datos sensibles
builder.Services.AddDataProtection()
    .SetApplicationName("MUNIDENUNCIA")  // Nombre único de la aplicación
    .PersistKeysToFileSystem(new DirectoryInfo(@"./DataProtection-Keys")); // Almacenamiento de claves
                                                                           // NOTA: En producción, considerar usar Azure Key Vault o similar

// Registrar servicios personalizados con inyección de dependencias
builder.Services.AddScoped<MUNIDENUNCIA.Services.IDataProtectionService,
    MUNIDENUNCIA.Services.DataProtectionService>();
builder.Services.AddScoped<MUNIDENUNCIA.Services.IFileUploadService,
    MUNIDENUNCIA.Services.FileUploadService>();

// Configurar límite de tamaño de archivos subidos (importante para seguridad)
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10MB máximo
});

//Ajustar cookies de autenticacion
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true; // JavaScript NO puede leer la cookie
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Solo HTTPS
    options.Cookie.SameSite = SameSiteMode.Strict; // Previene CSRF
    options.Cookie.Name = "MUNIDENUNCIA.Auth";

    options.ExpireTimeSpan = TimeSpan.FromHours(2);
    options.SlidingExpiration = true;

    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});
//HttpOnly previene que scripts de JavaScript accedan a la cookie,
//protegiendo contra ataques XSS donde un atacante podría inyectar código malicioso para robar tokens de sesión.
//SecurePolicy en Always asegura que la cookie solo se transmita por HTTPS, previniendo intercepción en redes inseguras.
//SameSite en modo Strict protege contra ataques CSRF al prevenir que la cookie se envíe en solicitudes cross-site.

//--La expiración de dos horas con sliding expiration significa que la sesión se renueva automáticamente mientras
//el usuario permanezca activo. Si el usuario deja de interactuar con el sistema, la sesión expirará
//después de dos horas, reduciendo la ventana de oportunidad para que alguien use una sesión abandonada.

// Configurar políticas de autorización personalizadas (opcional, para escenarios avanzados)
//??
builder.Services.AddAuthorization(options =>
{
    // Política que requiere rol de Funcionario o Administrador
    options.AddPolicy("RequiereFuncionarioOSuperior", policy =>
        policy.RequireRole("Funcionario", "Administrador"));

    // Política que requiere solo Administrador
    options.AddPolicy("RequiereAdministrador", policy =>
        policy.RequireRole("Administrador"));

    // Política personalizada: Puede gestionar denuncias
    options.AddPolicy("PuedeGestionarDenuncias", policy =>
        policy.RequireAssertion(context =>
            context.User.IsInRole("Funcionario") ||
            context.User.IsInRole("Administrador")));
});

builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddControllersWithViews();

// ─────────────────────────────────────────────────────────────────────────────
// BLOQUE 1: HttpClientFactory (REQUERIDO para demos A10 - SSRF)
// Agregar DESPUÉS de builder.Services.AddControllersWithViews():
// ─────────────────────────────────────────────────────────────────────────────
// HttpClient genérico (usado por SsrfVulnerableController para la demo)
builder.Services.AddHttpClient();

// HttpClient seguro con restricciones (usado por SsrfSeguroController)
builder.Services.AddHttpClient("SsrfSeguro", client =>
{
    // ✅ Timeout restrictivo: 5 segundos máximo
    client.Timeout = TimeSpan.FromSeconds(5);

    // ✅ Limitar tamaño de respuesta: 1 MB máximo
    client.MaxResponseContentBufferSize = 1_048_576;

    // ✅ User-Agent identificable para auditoría
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "MuniDenuncia-HealthCheck/1.0 (+https://munidenuncia.go.cr)");

    // ✅ No seguir redirecciones automáticamente (previene SSRF por redirect)
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AllowAutoRedirect = false,             // ← Previene SSRF por cadena de redirects
    MaxAutomaticRedirections = 0
});

// ─────────────────────────────────────────────────────────────────────────────
// BLOQUE 2: Políticas de Autorización Centralizadas (MEJORA Semana 2)
// Agregar DESPUÉS del BLOQUE 1.
// Requiere: using MuniDenuncia.Configuration;
//
// NOTA PEDAGÓGICA:
// MuniDenuncia actualmente autoriza con [Authorize] + User.IsInRole("...")
// inline. Esta mejora centraliza las reglas en políticas nombradas, que es
// la práctica recomendada en ASP.NET Core 8. Los estudiantes verán AMBOS
// enfoques en el curso para entender la progresión.
// ─
// Registra RequiereAdmin, RequiereFuncionario, RequiereCiudadano, PersonalInterno
builder.Services.AddMuniDenunciaAuthorizationPolicies();

// ─────────────────────────────────────────────────────────────────────────────
// BLOQUE 3: Rate Limiting para demo A07 (OPCIONAL)
// Agregar DESPUÉS del BLOQUE 2.
// Requiere: using System.Threading.RateLimiting; (ya incluido en .NET 8)
// ───

builder.Services.AddRateLimiter(options =>
{
    // Limitar intentos de login: máximo 5 peticiones por minuto por IP
    options.AddFixedWindowLimiter("login", limiterOptions =>
    {
        limiterOptions.PermitLimit = 5;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0; // Sin cola — rechazar inmediatamente
    });

    // Limitar peticiones generales: 100 por minuto por IP
    options.AddFixedWindowLimiter("general", limiterOptions =>
    {
        limiterOptions.PermitLimit = 100;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
    });

    // Respuesta cuando se excede el límite
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            error = "Demasiadas solicitudes. Intente nuevamente en un minuto.",
            retryAfter = 60
        }, cancellationToken);
    };
});

/////////
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// Configurar HTTPS
builder.Services.AddHttpsRedirection(options =>
{
    options.RedirectStatusCode = StatusCodes.Status307TemporaryRedirect;
    options.HttpsPort = 5001;
});
//RedirectStatusCode = 307: Cuando alguien intenta acceder por HTTP (inseguro), el servidor responde con código 307 "Temporal Redirect" redirigiendo automáticamente a HTTPS
//HttpsPort = 5001: Define que el puerto HTTPS es 5001 (en producción sería 443)
//Resultado: Si usuario va a http://localhost:5000, automáticamente es redirigido a https://localhost:5001

// Configurar HSTS
builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365);
});
//**Esto configura: **
//-**MaxAge = 365 días * *: El navegador recordará por 1 año que DEBE usar HTTPS siempre
//- **IncludeSubDomains = true**: Aplica también a subdominios (ej: `api.ejemplo.gob.cr`)
//-**Preload = true * *: Permite incluir el sitio en la lista de precarga HSTS de navegadores
//- **Resultado**: Después de la primera visita, el navegador NUNCA intentará conexión HTTP, siempre usará HTTPS

///

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Middleware de headers de seguridad //Descomentar este codigo para revisar como falla el XSS en paginas vulnerables.
app.Use(async (context, next) =>
{
    //context.Response.Headers.Add("Content-Security-Policy",
    //    "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data:");

    context.Response.Headers.Add("Content-Security-Policy",
            "default-src 'self'; " +
            "script-src 'self'; " +  // NO 'unsafe-inline' - bloquea scripts inyectados
            "style-src 'self'; " +
            "object-src 'none'; " +   // Bloquea Flash, Java, etc.
            "base-uri 'self'; " +     // Previene ataques de base tag
            "frame-ancestors 'none'"); // Previene clickjacking


    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Add("Permissions-Policy", "geolocation=(), microphone=(), camera=()");

    // Remover headers que revelan información
    context.Response.Headers.Remove("Server");
    context.Response.Headers.Remove("X-Powered-By");

    /*
     * Header Estado Para qué sirve
Strict-Transport-Security FALTA — crítico Sin HSTS un MITM hace downgrade a HTTPX
X-XSS-Protection: 1; mode=block Falta (menor) Navegadores legacy; Chromium lo ignoró pero IE/Edge legacy no
Cross-Origin-Opener-Policy Falta Previene que ventanas externas accedan a window.opener
Cross-Origin-Resource-Policy Falta Controla quién puede cargar tus recursos
Cache-Control en respuestas sensibles Falta Sin no-store en páginas autenticadas, el botón "atrás" muestra datos
     * */
    // CRÍTICO - agregar
    context.Response.Headers.Add("Strict-Transport-Security",
        "max-age=31536000; includeSubDomains");

    // Recomendados
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Cross-Origin-Opener-Policy", "same-origin");
    context.Response.Headers.Add("Cross-Origin-Resource-Policy", "same-origin");

    // En páginas con datos sensibles (detrás de auth)
    context.Response.Headers.Add("Cache-Control", "no-store, no-cache, must-revalidate");
    context.Response.Headers.Add("Pragma", "no-cache");

    //o enviar un valor generico en lugar de Kestrel
    //context.Response.Headers.Remove("Server");
    //context.Response.Headers.Add("Server", "WebServer"); // Valor genérico

    await next();
});


app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter(); 
//UseAuthentication debe llamarse antes de UseAuthorization porque primero necesitamos identificar al usuario antes
//de determinar sus permisos.
//UseRouting debe preceder a UseAuthentication porque el sistema de autenticación necesita conocer qué endpoint
//se está solicitando.

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        // Llamar al inicializador de base de datos
        await MUNIDENUNCIA.Data.DbInitializer.InitializeAsync(services);

        Console.WriteLine("✅ Base de datos inicializada correctamente");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "❌ Error al inicializar la base de datos");
    }
}

//using (var scope = app.Services.CreateScope())
//{
//    var services = scope.ServiceProvider;
//    await SeedRolesAndAdminUser(services);
//}

Log.Information("MUNIDENUNCIA iniciado correctamente");

app.Run();

//static async Task SeedRolesAndAdminUser(IServiceProvider serviceProvider)
//{
//    var roleManager = serviceProvider
//        .GetRequiredService<RoleManager<IdentityRole>>();
//    var userManager = serviceProvider
//        .GetRequiredService<UserManager<ApplicationUser>>();
//    var logger = serviceProvider
//        .GetRequiredService<ILogger<Program>>();

//    string[] roleNames = {
//        RoleNames.Administrador,
//        RoleNames.Funcionario,
//        RoleNames.Auditor
//    };

//    foreach (var roleName in roleNames)
//    {
//        var roleExist = await roleManager.RoleExistsAsync(roleName);
//        if (!roleExist)
//        {
//            await roleManager.CreateAsync(new IdentityRole(roleName));
//            logger.LogInformation("Rol {RoleName} creado", roleName);
//        }
//    }

//    var adminEmail = "admin@sumunicipalidad.go.cr";
//    var adminUser = await userManager.FindByEmailAsync(adminEmail);

//    if (adminUser == null)
//    {
//        var newAdmin = new ApplicationUser
//        {
//            UserName = adminEmail,
//            Email = adminEmail,
//            EmailConfirmed = true,
//            NombreCompleto = "Administrador del Sistema",
//            Departamento = "Tecnologías de Información",
//            FechaRegistro = DateTime.UtcNow
//        };

//        var createAdmin = await userManager.CreateAsync(
//            newAdmin,
//            "Admin@MUNIDENUNCIA2025!");

//        if (createAdmin.Succeeded)
//        {
//            await userManager.AddToRoleAsync(newAdmin, RoleNames.Administrador);
//            logger.LogInformation("Usuario administrador creado");
//        }
//    }
//}


