using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TheBuryProject.Models.Base;
using TheBuryProject.Models.Entities;

namespace TheBuryProject.Data
{
    /// <summary>
    /// Contexto principal de la base de datos del sistema.
    /// Hereda de IdentityDbContext para incluir tablas de autenticación.
    /// </summary>
    public class AppDbContext : IdentityDbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // DbSets - Cada uno representa una tabla en la base de datos
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Marca> Marcas { get; set; }
        public DbSet<Producto> Productos { get; set; }  // ✅ AGREGAR ESTA LÍNEA

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración de Categoria
            modelBuilder.Entity<Categoria>(entity =>
            {
                // Índice único en el código
                entity.HasIndex(e => e.Codigo)
                    .IsUnique()
                    .HasFilter("IsDeleted = 0"); // Solo para registros no eliminados

                // Configuración de la relación padre-hijo (auto-referencia)
                entity.HasOne(e => e.Parent)
                    .WithMany(e => e.Children)
                    .HasForeignKey(e => e.ParentId)
                    .OnDelete(DeleteBehavior.Restrict); // No borrar en cascada

                // RowVersion para control de concurrencia
                entity.Property(e => e.RowVersion)
                    .IsRowVersion();

                // Filtro global para soft delete
                entity.HasQueryFilter(e => !e.IsDeleted);
            });

            // Configuración de Marca
            modelBuilder.Entity<Marca>(entity =>
            {
                // Índice único en el código
                entity.HasIndex(e => e.Codigo)
                    .IsUnique()
                    .HasFilter("IsDeleted = 0"); // Solo para registros no eliminados

                // Configuración de la relación padre-hijo (auto-referencia)
                entity.HasOne(e => e.Parent)
                    .WithMany(e => e.Children)
                    .HasForeignKey(e => e.ParentId)
                    .OnDelete(DeleteBehavior.Restrict); // No borrar en cascada

                // RowVersion para control de concurrencia
                entity.Property(e => e.RowVersion)
                    .IsRowVersion();

                // Filtro global para soft delete
                entity.HasQueryFilter(e => !e.IsDeleted);
            });
            // ✅ AGREGAR TODA ESTA CONFIGURACIÓN DE PRODUCTO
            // Configuración de Producto
            modelBuilder.Entity<Producto>(entity =>
            {
                // Índice único en el código
                entity.HasIndex(e => e.Codigo)
                    .IsUnique()
                    .HasFilter("IsDeleted = 0"); // Solo para registros no eliminados

                // Relación con Categoria
                entity.HasOne(e => e.Categoria)
                    .WithMany()
                    .HasForeignKey(e => e.CategoriaId)
                    .OnDelete(DeleteBehavior.Restrict); // No borrar en cascada

                // Relación con Marca
                entity.HasOne(e => e.Marca)
                    .WithMany()
                    .HasForeignKey(e => e.MarcaId)
                    .OnDelete(DeleteBehavior.Restrict); // No borrar en cascada

                // Configuración de decimales para precios
                entity.Property(e => e.PrecioCompra)
                    .HasPrecision(18, 2);

                entity.Property(e => e.PrecioVenta)
                    .HasPrecision(18, 2);

                entity.Property(e => e.StockActual)
                    .HasPrecision(18, 2);

                entity.Property(e => e.StockMinimo)
                    .HasPrecision(18, 2);

                // RowVersion para control de concurrencia
                entity.Property(e => e.RowVersion)
                    .IsRowVersion();

                // Filtro global para soft delete
                entity.HasQueryFilter(e => !e.IsDeleted);
            });
            // Seed de datos inicial
            SeedData(modelBuilder);
        }

        /// <summary>
        /// Datos iniciales para la base de datos
        /// </summary>
        private void SeedData(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Categoria>().HasData(
                new Categoria
                {
                    Id = 1,
                    Codigo = "ELEC",
                    Nombre = "Electrónica",
                    Descripcion = "Productos electrónicos",
                    ControlSerieDefault = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new Categoria
                {
                    Id = 2,
                    Codigo = "FRIO",
                    Nombre = "Refrigeración",
                    Descripcion = "Heladeras, freezers y aire acondicionado",
                    ControlSerieDefault = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                }
            );

            modelBuilder.Entity<Marca>().HasData(
                new Marca
                {
                    Id = 1,
                    Codigo = "SAM",
                    Nombre = "Samsung",
                    Descripcion = "Electrónica y electrodomésticos",
                    PaisOrigen = "Corea del Sur",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new Marca
                {
                    Id = 2,
                    Codigo = "LG",
                    Nombre = "LG",
                    Descripcion = "Electrónica y electrodomésticos",
                    PaisOrigen = "Corea del Sur",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new Marca
                {
                    Id = 3,
                    Codigo = "WHI",
                    Nombre = "Whirlpool",
                    Descripcion = "Electrodomésticos",
                    PaisOrigen = "Estados Unidos",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                }
            );
        }

        /// <summary>
        /// Interceptor para auditoría automática antes de guardar cambios
        /// </summary>
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker.Entries<BaseEntity>();

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    // TODO: Obtener usuario del contexto HTTP
                    entry.Entity.CreatedBy = "System";
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    // TODO: Obtener usuario del contexto HTTP
                    entry.Entity.UpdatedBy = "System";

                    // IMPORTANTE: Proteger campos de auditoría de creación para que no se modifiquen
                    entry.Property(e => e.CreatedAt).IsModified = false;
                    entry.Property(e => e.CreatedBy).IsModified = false;
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}