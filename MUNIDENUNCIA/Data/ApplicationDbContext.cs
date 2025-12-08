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

            // Configuración de la entidad Denuncia
            builder.Entity<Denuncia>(entity =>
            {
                entity.ToTable("Denuncias");
                entity.HasKey(e => e.Id);

                // Índices para optimizar búsquedas
                entity.HasIndex(e => e.Cedula)
                    .HasDatabaseName("IX_Denuncias_Cedula");

                entity.HasIndex(e => e.Estado)
                    .HasDatabaseName("IX_Denuncias_Estado");

                entity.HasIndex(e => e.FechaCreacion)
                    .HasDatabaseName("IX_Denuncias_FechaCreacion");

                entity.HasIndex(e => e.Categoria)
                    .HasDatabaseName("IX_Denuncias_Categoria");

                // Conversión del enum a string
                entity.Property(e => e.Categoria)
                    .HasConversion<string>()
                    .HasMaxLength(50);

                // Valores predeterminados
                entity.Property(e => e.FechaCreacion)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.Estado)
                    .HasDefaultValue("Pendiente")
                    .HasMaxLength(50);
            });

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
