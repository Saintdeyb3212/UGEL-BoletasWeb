using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;
using UGEL_BoletasWeb.Models.Entities;

namespace UGEL_BoletasWeb.Data.Context
{
    public class UgelDbContext : DbContext
    {
        // Constructor que recibe la cadena de conexión desde el Program.cs
        public UgelDbContext(DbContextOptions<UgelDbContext> options) : base(options)
        {
        }

        // 1. Declaración de las Tablas (DbSets)
        public DbSet<BoletaCabecera> BoletasCabecera { get; set; }
        public DbSet<BoletaDetalle> BoletasDetalle { get; set; }
        public DbSet<UsuarioSistema> UsuariosSistema { get; set; }

        // 2. Configuración Avanzada (Fluent API) para blindar la base de datos
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // -- A. REGLAS DE SEGURIDAD FINANCIERA --
            // Configurar la relación Uno a Muchos (Cabecera -> Detalles)
            modelBuilder.Entity<BoletaDetalle>()
                .HasOne(d => d.BoletaCabecera)
                .WithMany(c => c.Detalles)
                .HasForeignKey(d => d.BoletaCabeceraId)
                // CLAVE PARA EL ESTADO: Restringimos el borrado en cascada.
                // ¡Una boleta jamás debe borrarse por accidente si eliminamos otro registro!
                .OnDelete(DeleteBehavior.Restrict);

            // -- B. OPTIMIZACIÓN DE RENDIMIENTO (ÍNDICES) --
            // Cuando Pagaduría busque un DNI, la base de datos lo encontrará en milisegundos
            modelBuilder.Entity<BoletaCabecera>()
                .HasIndex(b => b.DNI)
                .HasDatabaseName("IX_BoletaCabecera_DNI");

            // Índice compuesto para buscar planillas enteras por Mes y Año rápidamente
            modelBuilder.Entity<BoletaCabecera>()
                .HasIndex(b => new { b.Mes, b.Anio })
                .HasDatabaseName("IX_BoletaCabecera_Periodo");

            // -- C. REGLAS DE USUARIO --
            // El Username de acceso debe ser único a nivel de base de datos
            modelBuilder.Entity<UsuarioSistema>()
                .HasIndex(u => u.Username)
                .IsUnique()
                .HasDatabaseName("IX_UsuarioSistema_Username_Unico");
        }
    }
}