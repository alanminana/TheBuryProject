using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TheBuryProject.Models;

namespace TheBuryProject.Data
{
    /// <summary>
    /// Contexto principal de la base de datos del sistema.
    /// Hereda de IdentityDbContext para incluir tablas de autenticaci�n.
    /// </summary>
    public class AppDbContext : IdentityDbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // DbSets - Cada uno representa una tabla en la base de datos
        public DbSet<Categoria> Categorias { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuraci�n de Categoria
            modelBuilder.Entity<Categoria>(entity =>
            {
                // �ndice �nico en el c�digo
                entity.HasIndex(e => e.Codigo)
                    .IsUnique()
                    .HasFilter("IsDeleted = 0"); // Solo para registros no eliminados

                // Configuraci�n de la relaci�n padre-hijo (auto-referencia)
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

            // Seed de datos inicial (opcional)
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
                    Nombre = "Electr�nica",
                    Descripcion = "Productos electr�nicos",
                    ControlSerieDefault = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new Categoria
                {
                    Id = 2,
                    Codigo = "FRIO",
                    Nombre = "Refrigeraci�n",
                    Descripcion = "Heladeras, freezers y aire acondicionado",
                    ControlSerieDefault = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                }
            );
        }

        /// <summary>
        /// Interceptor para auditor�a autom�tica antes de guardar cambios
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
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}