using System.Reflection.Emit;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MUNIDENUNCIA.Models;

namespace MUNIDENUNCIA.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        //public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        // Nuevos DbSets para el módulo de Permisos de Construcción (Semana 3)
        public DbSet<SolicitudPermiso> SolicitudesPermisos { get; set; }
        public DbSet<Comentario> Comentarios { get; set; }

        public DbSet<Denuncia> Denuncias { get; set; }

        //Genera las tablas de usuarios, roles, claims y tokens.
        //protected override void OnModelCreating(ModelBuilder builder)
        //{
        //    base.OnModelCreating(builder);

        //    builder.Entity<AuditLog>()
        //        .HasIndex(a => a.UserId);

        //    builder.Entity<AuditLog>()
        //        .HasIndex(a => a.Timestamp);

        //    builder.Entity<AuditLog>()
        //        .HasIndex(a => a.EventType);
        //}

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<AuditLog>()
                .HasIndex(a => a.UserId);

            builder.Entity<AuditLog>()
                .HasIndex(a => a.Timestamp);

            builder.Entity<AuditLog>()
                .HasIndex(a => a.EventType);

            // Configuración de la entidad SolicitudPermiso
            builder.Entity<SolicitudPermiso>(entity =>
            {
                // Configuración de la tabla
                entity.ToTable("SolicitudesPermisos");
                entity.HasKey(e => e.Id);

                // Configuración de propiedades
                entity.Property(e => e.CedulaPropietario)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.NombreCompletoPropietario)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.EmailPropietario)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.TelefonoPropietario)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.Distrito)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.DireccionCompleta)
                    .IsRequired()
                    .HasMaxLength(300);

                entity.Property(e => e.PlanoCatastrado)
                    .IsRequired()
                    .HasMaxLength(30);

                entity.Property(e => e.DescripcionProyecto)
                    .IsRequired()
                    .HasMaxLength(2000);

                entity.Property(e => e.AreaConstruccionM2)
                    .HasPrecision(10, 2);

                entity.Property(e => e.PresupuestoEstimado)
                    .HasPrecision(18, 2);

                entity.Property(e => e.RevisadoPor)
                    .HasMaxLength(50);

                // Índices para mejorar rendimiento de búsquedas
                entity.HasIndex(e => e.CedulaPropietario)
                    .HasDatabaseName("IX_SolicitudesPermisos_CedulaPropietario");

                entity.HasIndex(e => e.PlanoCatastrado)
                    .HasDatabaseName("IX_SolicitudesPermisos_PlanoCatastrado");

                entity.HasIndex(e => e.Estado)
                    .HasDatabaseName("IX_SolicitudesPermisos_Estado");

                entity.HasIndex(e => e.FechaSolicitud)
                    .HasDatabaseName("IX_SolicitudesPermisos_FechaSolicitud");

                // Índice compuesto para búsquedas frecuentes
                entity.HasIndex(e => new { e.Estado, e.FechaSolicitud })
                    .HasDatabaseName("IX_SolicitudesPermisos_Estado_Fecha");

                // Conversión de enums a string para mejor legibilidad en base de datos
                entity.Property(e => e.TipoConstruccion)
                    .HasConversion<string>()
                    .HasMaxLength(50);

                entity.Property(e => e.Estado)
                    .HasConversion<string>()
                    .HasMaxLength(50);

                // Configuración de valores predeterminados
                entity.Property(e => e.FechaSolicitud)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.Estado)
                    .HasDefaultValue(EstadoSolicitud.Pendiente);
            });

            // Configuración de la entidad Comentario
            builder.Entity<Comentario>(entity =>
            {
                // Configuración de la tabla
                entity.ToTable("Comentarios");
                entity.HasKey(e => e.Id);

                // Configuración de propiedades
                entity.Property(e => e.NombreFuncionario)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.CargoFuncionario)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.TextoComentario)
                    .IsRequired()
                    .HasMaxLength(2000);

                entity.Property(e => e.FechaComentario)
                    .HasDefaultValueSql("GETUTCDATE()");

                // Relación con SolicitudPermiso
                entity.HasOne(e => e.SolicitudPermiso)
                    .WithMany(s => s.Comentarios)
                    .HasForeignKey(e => e.SolicitudPermisoId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Índices
                entity.HasIndex(e => e.SolicitudPermisoId)
                    .HasDatabaseName("IX_Comentarios_SolicitudPermisoId");

                entity.HasIndex(e => e.FechaComentario)
                    .HasDatabaseName("IX_Comentarios_FechaComentario");
            });

            // Configuración de la entidad Denuncia - SEMANA 4
            builder.Entity<Denuncia>(entity =>
            {
                // Configuración de la tabla
                entity.ToTable("Denuncias");
                entity.HasKey(e => e.Id);

                // ========================================================================
                // CONFIGURACIÓN DE CAMPOS CIFRADOS
                // Estos campos almacenan datos sensibles cifrados con Data Protection API
                // ========================================================================

                entity.Property(e => e.CedulaCifrada)
                    .IsRequired()
                    .HasMaxLength(500)  // Suficiente espacio para datos cifrados
                    .HasComment("Cédula del ciudadano cifrada con Data Protection API");

                entity.Property(e => e.TelefonoCifrado)
                    .IsRequired()
                    .HasMaxLength(500)
                    .HasComment("Teléfono del ciudadano cifrado con Data Protection API");

                entity.Property(e => e.EmailCifrado)
                    .IsRequired()
                    .HasMaxLength(500)
                    .HasComment("Email del ciudadano cifrado con Data Protection API");

                // ========================================================================
                // CONFIGURACIÓN DE CAMPOS NO SENSIBLES
                // ========================================================================

                entity.Property(e => e.NombreCompleto)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Ubicacion)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Descripcion)
                    .IsRequired()
                    .HasMaxLength(1000);

                entity.Property(e => e.Estado)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasDefaultValue("Recibida");

                entity.Property(e => e.Observaciones)
                    .HasMaxLength(500);

                // ========================================================================
                // CONFIGURACIÓN DE CAMPOS DE ARCHIVO - SEMANA 4
                // ========================================================================

                entity.Property(e => e.ArchivoNombreOriginal)
                    .HasMaxLength(255)
                    .HasComment("Nombre original del archivo PDF subido");

                entity.Property(e => e.ArchivoNombreServidor)
                    .HasMaxLength(255)
                    .HasComment("Nombre aleatorio del archivo en el servidor");

                entity.Property(e => e.ArchivoRuta)
                    .HasMaxLength(500)
                    .HasComment("Ruta completa donde se almacenó el archivo");

                entity.Property(e => e.ArchivoTipoMime)
                    .HasMaxLength(100)
                    .HasComment("Tipo MIME del archivo (application/pdf)");

                // ========================================================================
                // CONFIGURACIÓN DE FECHAS
                // ========================================================================

                entity.Property(e => e.FechaCreacion)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()")
                    .HasComment("Fecha de creación de la denuncia");

                entity.Property(e => e.FechaActualizacion)
                    .HasComment("Fecha de última actualización");

                entity.Property(e => e.FechaResolucion)
                    .HasComment("Fecha en que se resolvió la denuncia");

                entity.Property(e => e.ArchivoFechaSubida)
                    .HasComment("Fecha en que se subió el archivo PDF");

                // ========================================================================
                // RELACIÓN CON USUARIO (FUNCIONARIO ASIGNADO)
                // ========================================================================

                entity.Property(e => e.AsignadoAUserId)
                    .HasMaxLength(450); // Tamaño del Id de Identity

                // Relación opcional con AspNetUsers
                entity.HasOne<Microsoft.AspNetCore.Identity.IdentityUser>()
                    .WithMany()
                    .HasForeignKey(e => e.AsignadoAUserId)
                    .OnDelete(DeleteBehavior.SetNull); // Si se elimina el usuario, el campo se pone NULL

                // ========================================================================
                // ÍNDICES PARA OPTIMIZACIÓN DE CONSULTAS
                // ========================================================================

                // Índice para búsquedas por estado
                entity.HasIndex(e => e.Estado)
                    .HasDatabaseName("IX_Denuncias_Estado");

                // Índice para búsquedas por fecha de creación
                entity.HasIndex(e => e.FechaCreacion)
                    .HasDatabaseName("IX_Denuncias_FechaCreacion");

                // Índice para búsquedas por categoría
                entity.HasIndex(e => e.Categoria)
                    .HasDatabaseName("IX_Denuncias_Categoria");

                // Índice para búsquedas por funcionario asignado
                entity.HasIndex(e => e.AsignadoAUserId)
                    .HasDatabaseName("IX_Denuncias_AsignadoA");

                // Índice compuesto para consultas de denuncias por estado y fecha
                entity.HasIndex(e => new { e.Estado, e.FechaCreacion })
                    .HasDatabaseName("IX_Denuncias_Estado_Fecha");

                // ========================================================================
                // NOTA PEDAGÓGICA SOBRE PROPIEDADES [NotMapped]
                // ========================================================================
                // Las propiedades Cedula, Telefono y Email (sin el sufijo "Cifrada/o")
                // están marcadas con [NotMapped] en el modelo.
                // 
                // Esto significa que Entity Framework NO intentará mapearlas a columnas
                // de la base de datos. Son propiedades en memoria que se usan así:
                //
                // AL GUARDAR:
                // 1. Servicio recibe: denuncia.Cedula = "1-0234-0567"
                // 2. Servicio cifra y asigna: denuncia.CedulaCifrada = _protector.Protect(denuncia.Cedula)
                // 3. EF guarda solo CedulaCifrada a la BD
                //
                // AL LEER:
                // 1. EF carga: denuncia.CedulaCifrada = "CfDJ8..." (cifrado)
                // 2. Servicio descifra y asigna: denuncia.Cedula = _protector.Unprotect(denuncia.CedulaCifrada)
                // 3. Vista usa: @Model.Cedula (descifrado)
                //
                // Esta separación mantiene la lógica de cifrado/descifrado centralizada
                // y evita exponer datos sensibles en logs o errores.
                // ========================================================================
            });


            //// Configuración de la entidad Denuncia
            //builder.Entity<Denuncia>(entity =>
            //{
            //    entity.ToTable("Denuncias");
            //    entity.HasKey(e => e.Id);

            //    // Índices para optimizar búsquedas
            //    entity.HasIndex(e => e.Cedula)
            //        .HasDatabaseName("IX_Denuncias_Cedula");

            //    entity.HasIndex(e => e.Estado)
            //        .HasDatabaseName("IX_Denuncias_Estado");

            //    entity.HasIndex(e => e.FechaCreacion)
            //        .HasDatabaseName("IX_Denuncias_FechaCreacion");

            //    entity.HasIndex(e => e.Categoria)
            //        .HasDatabaseName("IX_Denuncias_Categoria");

            //    // Conversión del enum a string
            //    entity.Property(e => e.Categoria)
            //        .HasConversion<string>()
            //        .HasMaxLength(50);

            //    // Valores predeterminados
            //    entity.Property(e => e.FechaCreacion)
            //        .HasDefaultValueSql("GETUTCDATE()");

            //    entity.Property(e => e.Estado)
            //        .HasDefaultValue("Pendiente")
            //        .HasMaxLength(50);
            //});

            // Datos semilla para tipos de construcción (opcional)
            // Estos datos se pueden cargar también mediante scripts SQL
            SeedData(builder);
        }

        /// <summary>
        /// Método para cargar datos iniciales en la base de datos
        /// </summary>
        private void SeedData(ModelBuilder builder)
        {
            // Los datos semilla reales se cargarán mediante scripts SQL
            // para facilitar la demostración y permitir mayor flexibilidad

            // Este método puede usarse para cargar datos de configuración
            // que raramente cambian, como catálogos o configuraciones del sistema
        }
    }
}

// ============================================================================
// NOTAS IMPORTANTES PARA LA IMPLEMENTACIÓN
// ============================================================================
//
// 1. MIGRACIÓN DE DATOS EXISTENTES:
//    Si ya existen denuncias en la BD con campos Cedula, Telefono, Email,
//    necesitarás una migración de datos para cifrarlos:
//    
//    a) Crear nueva migración: Add-Migration UpdateDenunciaCifrado
//    b) ANTES de Update-Database, editar la migración para:
//       - Agregar columnas nuevas (CedulaCifrada, etc.)
//       - Copiar datos cifrados de las columnas antiguas
//       - Eliminar columnas antiguas
//    c) Ejecutar: Update-Database
//
// 2. PERFORMANCE:
//    El cifrado/descifrado tiene un costo computacional mínimo.
//    Para listas grandes, considerar cargar solo datos necesarios
//    y descifrar bajo demanda.
//
// 3. BÚSQUEDA DE DATOS CIFRADOS:
//    NO se pueden buscar datos cifrados directamente con LIKE o =
//    porque el cifrado cambia completamente el valor.
//    
//    Opciones:
//    a) Mantener hash del valor para búsquedas exactas
//    b) Descifrar en memoria y filtrar (para conjuntos pequeños)
//    c) Usar índices de texto completo en campos no cifrados
//
// 4. RESPALDO DE CLAVES:
//    Las claves de Data Protection se almacenan en:
//    %LOCALAPPDATA%\ASP.NET\DataProtection-Keys
//    
//    CRÍTICO: Respaldar estas claves en producción.
//    Sin ellas, los datos cifrados son IRRECUPERABLES.
//
// ============================================================================