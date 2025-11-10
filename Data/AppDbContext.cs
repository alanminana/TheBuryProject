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
        public DbSet<Producto> Productos { get; set; }

        // Proveedores
        public DbSet<Proveedor> Proveedores { get; set; }
        public DbSet<ProveedorProducto> ProveedorProductos { get; set; }
        public DbSet<ProveedorMarca> ProveedorMarcas { get; set; }
        public DbSet<ProveedorCategoria> ProveedorCategorias { get; set; }

        // Órdenes de Compra
        public DbSet<OrdenCompra> OrdenesCompra { get; set; }
        public DbSet<OrdenCompraDetalle> OrdenCompraDetalles { get; set; }
        public DbSet<MovimientoStock> MovimientosStock { get; set; }

        // Cheques
        public DbSet<Cheque> Cheques { get; set; }

        // Clientes y Créditos
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Credito> Creditos { get; set; }
        public DbSet<Cuota> Cuotas { get; set; }
        public DbSet<Garante> Garantes { get; set; }
        public DbSet<DocumentoCliente> DocumentosCliente { get; set; }

        // Ventas
        public DbSet<Venta> Ventas { get; set; }
        public DbSet<VentaDetalle> VentaDetalles { get; set; }
        public DbSet<Factura> Facturas { get; set; }
        public DbSet<ConfiguracionPago> ConfiguracionesPago { get; set; }
        public DbSet<ConfiguracionTarjeta> ConfiguracionesTarjeta { get; set; }
        public DbSet<DatosTarjeta> DatosTarjeta { get; set; }
        public DbSet<DatosCheque> DatosCheque { get; set; }
        public DbSet<VentaCreditoCuota> VentaCreditoCuotas { get; set; }

        // Módulo de Mora y Alertas
        public DbSet<ConfiguracionMora> ConfiguracionesMora { get; set; }
        public DbSet<LogMora> LogsMora { get; set; }
        public DbSet<AlertaCobranza> AlertasCobranza { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración de Categoria
            modelBuilder.Entity<Categoria>(entity =>
            {
                entity.HasIndex(e => e.Codigo)
                    .IsUnique()
                    .HasFilter("IsDeleted = 0");

                entity.HasOne(e => e.Parent)
                    .WithMany(e => e.Children)
                    .HasForeignKey(e => e.ParentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.RowVersion)
                    .IsRowVersion();

                entity.HasQueryFilter(e => !e.IsDeleted);
            });
            // Configuración para VentaCreditoCuota
            modelBuilder.Entity<VentaCreditoCuota>(entity =>
            {
                entity.ToTable("VentaCreditoCuotas");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Monto)
                    .HasPrecision(18, 2);

                entity.Property(e => e.Saldo)
                    .HasPrecision(18, 2);

                entity.Property(e => e.MontoPagado)
                    .HasPrecision(18, 2);

                entity.HasIndex(e => new { e.VentaId, e.NumeroCuota });
                entity.HasIndex(e => e.FechaVencimiento);
                entity.HasIndex(e => e.Pagada);

                entity.HasOne(e => e.Venta)
                    .WithMany(v => v.VentaCreditoCuotas)
                    .HasForeignKey(e => e.VentaId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Credito)
                    .WithMany()
                    .HasForeignKey(e => e.CreditoId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            // Configuración de Marca
            modelBuilder.Entity<Marca>(entity =>
            {
                entity.HasIndex(e => e.Codigo)
                    .IsUnique()
                    .HasFilter("IsDeleted = 0");

                entity.HasOne(e => e.Parent)
                    .WithMany(e => e.Children)
                    .HasForeignKey(e => e.ParentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.RowVersion)
                    .IsRowVersion();

                entity.HasQueryFilter(e => !e.IsDeleted);
            });

            // Configuración de Producto
            modelBuilder.Entity<Producto>(entity =>
            {
                entity.HasIndex(e => e.Codigo)
                    .IsUnique()
                    .HasFilter("IsDeleted = 0");

                entity.HasOne(e => e.Categoria)
                    .WithMany()
                    .HasForeignKey(e => e.CategoriaId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Marca)
                    .WithMany()
                    .HasForeignKey(e => e.MarcaId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.PrecioCompra)
                    .HasPrecision(18, 2);

                entity.Property(e => e.PrecioVenta)
                    .HasPrecision(18, 2);

                entity.Property(e => e.StockActual)
                    .HasPrecision(18, 2);

                entity.Property(e => e.StockMinimo)
                    .HasPrecision(18, 2);

                entity.Property(e => e.RowVersion)
                    .IsRowVersion();

                entity.HasQueryFilter(e => !e.IsDeleted);
            });

            // Configuración de Proveedor
            modelBuilder.Entity<Proveedor>(entity =>
            {
                entity.HasIndex(e => e.Cuit)
                    .IsUnique()
                    .HasFilter("IsDeleted = 0");

                entity.Property(e => e.RowVersion)
                    .IsRowVersion();

                entity.HasQueryFilter(e => !e.IsDeleted);
            });

            // Configuración de ProveedorProducto (Relación N:N)
            modelBuilder.Entity<ProveedorProducto>(entity =>
            {
                entity.HasOne(e => e.Proveedor)
                    .WithMany(p => p.ProveedorProductos)
                    .HasForeignKey(e => e.ProveedorId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Producto)
                    .WithMany()
                    .HasForeignKey(e => e.ProductoId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.ProveedorId, e.ProductoId })
                    .IsUnique();

                entity.HasQueryFilter(e => !e.IsDeleted);
            });

            // Configuración de ProveedorMarca (Relación N:N)
            modelBuilder.Entity<ProveedorMarca>(entity =>
            {
                entity.HasOne(e => e.Proveedor)
                    .WithMany(p => p.ProveedorMarcas)
                    .HasForeignKey(e => e.ProveedorId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Marca)
                    .WithMany()
                    .HasForeignKey(e => e.MarcaId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.ProveedorId, e.MarcaId })
                    .IsUnique();

                entity.HasQueryFilter(e => !e.IsDeleted);
            });

            // Configuración de ProveedorCategoria (Relación N:N)
            modelBuilder.Entity<ProveedorCategoria>(entity =>
            {
                entity.HasOne(e => e.Proveedor)
                    .WithMany(p => p.ProveedorCategorias)
                    .HasForeignKey(e => e.ProveedorId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Categoria)
                    .WithMany()
                    .HasForeignKey(e => e.CategoriaId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.ProveedorId, e.CategoriaId })
                    .IsUnique();

                entity.HasQueryFilter(e => !e.IsDeleted);
            });

            // Configuración de OrdenCompra
            modelBuilder.Entity<OrdenCompra>(entity =>
            {
                entity.HasIndex(e => e.Numero)
                    .IsUnique()
                    .HasFilter("IsDeleted = 0");

                entity.HasOne(e => e.Proveedor)
                    .WithMany(p => p.OrdenesCompra)
                    .HasForeignKey(e => e.ProveedorId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.Subtotal)
                    .HasPrecision(18, 2);

                entity.Property(e => e.Descuento)
                    .HasPrecision(18, 2);

                entity.Property(e => e.Iva)
                    .HasPrecision(18, 2);

                entity.Property(e => e.Total)
                    .HasPrecision(18, 2);

                entity.Property(e => e.RowVersion)
                    .IsRowVersion();

                entity.HasQueryFilter(e => !e.IsDeleted);
            });

            // Configuración de OrdenCompraDetalle
            modelBuilder.Entity<OrdenCompraDetalle>(entity =>
            {
                entity.HasOne(e => e.OrdenCompra)
                    .WithMany(o => o.Detalles)
                    .HasForeignKey(e => e.OrdenCompraId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Producto)
                    .WithMany()
                    .HasForeignKey(e => e.ProductoId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.PrecioUnitario)
                    .HasPrecision(18, 2);

                entity.Property(e => e.Subtotal)
                    .HasPrecision(18, 2);

                entity.Property(e => e.RowVersion)
                    .IsRowVersion();

                entity.HasQueryFilter(e => !e.IsDeleted);
            });

            // Configuración de Cheque
            modelBuilder.Entity<Cheque>(entity =>
            {
                entity.HasIndex(e => e.Numero);

                entity.HasOne(e => e.Proveedor)
                    .WithMany(p => p.Cheques)
                    .HasForeignKey(e => e.ProveedorId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.OrdenCompra)
                    .WithMany()
                    .HasForeignKey(e => e.OrdenCompraId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.Property(e => e.Monto)
                    .HasPrecision(18, 2);

                entity.Property(e => e.RowVersion)
                    .IsRowVersion();

                entity.HasQueryFilter(e => !e.IsDeleted);
            });

            // Configuración de MovimientoStock
            modelBuilder.Entity<MovimientoStock>(entity =>
            {
                entity.HasOne(e => e.Producto)
                    .WithMany()
                    .HasForeignKey(e => e.ProductoId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.OrdenCompra)
                    .WithMany()
                    .HasForeignKey(e => e.OrdenCompraId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.Cantidad)
                    .HasPrecision(18, 2);

                entity.Property(e => e.StockAnterior)
                    .HasPrecision(18, 2);

                entity.Property(e => e.StockNuevo)
                    .HasPrecision(18, 2);

                entity.Property(e => e.RowVersion)
                    .IsRowVersion();

                entity.HasQueryFilter(e => !e.IsDeleted);
            });

            // Configuración de Cliente
            modelBuilder.Entity<Cliente>(entity =>
            {
                entity.HasIndex(e => new { e.TipoDocumento, e.NumeroDocumento })
                    .IsUnique()
                    .HasFilter("IsDeleted = 0");

                entity.Property(e => e.Sueldo)
                    .HasPrecision(18, 2);

                entity.Property(e => e.PuntajeRiesgo)
                    .HasPrecision(5, 2);

                entity.Property(e => e.RowVersion)
                    .IsRowVersion();

                entity.HasQueryFilter(e => !e.IsDeleted);
            });

            // Configuración de Credito
            modelBuilder.Entity<Credito>(entity =>
            {
                entity.HasIndex(e => e.Numero)
                    .IsUnique()
                    .HasFilter("IsDeleted = 0");

                entity.HasOne(e => e.Cliente)
                    .WithMany(c => c.Creditos)
                    .HasForeignKey(e => e.ClienteId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.MontoSolicitado)
                    .HasPrecision(18, 2);

                entity.Property(e => e.MontoAprobado)
                    .HasPrecision(18, 2);

                entity.Property(e => e.TasaInteres)
                    .HasPrecision(5, 2);

                entity.Property(e => e.MontoCuota)
                    .HasPrecision(18, 2);

                entity.Property(e => e.PuntajeRiesgoInicial)
                    .HasPrecision(5, 2);

                entity.Property(e => e.RowVersion)
                    .IsRowVersion();

                entity.HasQueryFilter(e => !e.IsDeleted);
                entity.Property(e => e.CFTEA)
    .HasPrecision(5, 2);

                entity.Property(e => e.TotalAPagar)
                    .HasPrecision(18, 2);

                entity.Property(e => e.SaldoPendiente)
                    .HasPrecision(18, 2);

                entity.HasOne(e => e.Garante)
                    .WithMany()
                    .HasForeignKey(e => e.GaranteId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configuración de Garante
            modelBuilder.Entity<Garante>(entity =>
            {
                entity.HasOne(e => e.Cliente)
                    .WithMany(c => c.ComoGarante)
                    .HasForeignKey(e => e.ClienteId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.GaranteCliente)
                    .WithMany()
                    .HasForeignKey(e => e.GaranteClienteId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.RowVersion)
                    .IsRowVersion();

                entity.HasQueryFilter(e => !e.IsDeleted);
            });

            // Configuración de DocumentoCliente
            modelBuilder.Entity<DocumentoCliente>(entity =>
            {
                entity.HasOne(e => e.Cliente)
                    .WithMany()
                    .HasForeignKey(e => e.ClienteId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.ClienteId);
                entity.HasIndex(e => e.Estado);
                entity.HasIndex(e => e.FechaSubida);
                entity.HasIndex(e => e.FechaVencimiento);
                entity.HasIndex(e => e.TipoDocumento);

                entity.Property(e => e.RowVersion)
                    .IsRowVersion();

                entity.HasQueryFilter(e => !e.IsDeleted);
            });
            // Configuración de Cuota
            modelBuilder.Entity<Cuota>(entity =>
            {
                entity.HasOne(e => e.Credito)
                    .WithMany(c => c.Cuotas)
                    .HasForeignKey(e => e.CreditoId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.CreditoId, e.NumeroCuota })
                    .IsUnique();

                entity.Property(e => e.MontoCapital)
                    .HasPrecision(18, 2);

                entity.Property(e => e.MontoInteres)
                    .HasPrecision(18, 2);

                entity.Property(e => e.MontoTotal)
                    .HasPrecision(18, 2);

                entity.Property(e => e.MontoPagado)
                    .HasPrecision(18, 2);

                entity.Property(e => e.MontoPunitorio)
                    .HasPrecision(18, 2);

                entity.Property(e => e.RowVersion)
                    .IsRowVersion();

                entity.HasQueryFilter(e => !e.IsDeleted);
            });

            // Configuración para Venta
            modelBuilder.Entity<Venta>(entity =>
            {
                entity.ToTable("Ventas");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Numero)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.Subtotal)
                    .HasPrecision(18, 2);

                entity.Property(e => e.Descuento)
                    .HasPrecision(18, 2);

                entity.Property(e => e.IVA)
                    .HasPrecision(18, 2);

                entity.Property(e => e.Total)
                    .HasPrecision(18, 2);

                entity.HasIndex(e => e.Numero).IsUnique();
                entity.HasIndex(e => e.FechaVenta);
                entity.HasIndex(e => e.Estado);

                entity.HasOne(e => e.Cliente)
                    .WithMany()
                    .HasForeignKey(e => e.ClienteId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Credito)
                    .WithMany()
                    .HasForeignKey(e => e.CreditoId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired(false);

                entity.HasMany(e => e.Detalles)
                    .WithOne(d => d.Venta)
                    .HasForeignKey(d => d.VentaId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.Facturas)
                    .WithOne(f => f.Venta)
                    .HasForeignKey(f => f.VentaId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuración para VentaDetalle
            modelBuilder.Entity<VentaDetalle>(entity =>
            {
                entity.ToTable("VentaDetalles");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.PrecioUnitario)
                    .HasPrecision(18, 2);

                entity.Property(e => e.Descuento)
                    .HasPrecision(18, 2);

                entity.Property(e => e.Subtotal)
                    .HasPrecision(18, 2);

                entity.HasOne(e => e.Venta)
                    .WithMany(v => v.Detalles)
                    .HasForeignKey(e => e.VentaId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Producto)
                    .WithMany()
                    .HasForeignKey(e => e.ProductoId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configuración para Factura
            modelBuilder.Entity<Factura>(entity =>
            {
                entity.ToTable("Facturas");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Numero)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.CAE)
                    .HasMaxLength(50);

                entity.HasIndex(e => e.Numero).IsUnique();
                entity.HasIndex(e => e.CAE);

                entity.HasOne(e => e.Venta)
                    .WithMany(v => v.Facturas)
                    .HasForeignKey(e => e.VentaId)
                    .OnDelete(DeleteBehavior.Cascade);
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
                    Activo = true,
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
                    Activo = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                }
            );
            // Configuración para ConfiguracionPago
            modelBuilder.Entity<ConfiguracionPago>(entity =>
            {
                entity.ToTable("ConfiguracionesPago");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Nombre)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.PorcentajeDescuentoMaximo)
                    .HasPrecision(5, 2);

                entity.Property(e => e.PorcentajeRecargo)
                    .HasPrecision(5, 2);

                entity.HasIndex(e => e.TipoPago).IsUnique();

                entity.HasMany(e => e.ConfiguracionesTarjeta)
                    .WithOne(t => t.ConfiguracionPago)
                    .HasForeignKey(t => t.ConfiguracionPagoId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuración para ConfiguracionTarjeta
            modelBuilder.Entity<ConfiguracionTarjeta>(entity =>
            {
                entity.ToTable("ConfiguracionesTarjeta");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.NombreTarjeta)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.TasaInteresesMensual)
                    .HasPrecision(5, 2);

                entity.Property(e => e.PorcentajeRecargoDebito)
                    .HasPrecision(5, 2);

                entity.HasIndex(e => new { e.ConfiguracionPagoId, e.NombreTarjeta });
            });

            // Configuración para DatosTarjeta
            modelBuilder.Entity<DatosTarjeta>(entity =>
            {
                entity.ToTable("DatosTarjeta");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.NombreTarjeta)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.TasaInteres)
                    .HasPrecision(5, 2);

                entity.Property(e => e.MontoCuota)
                    .HasPrecision(18, 2);

                entity.Property(e => e.MontoTotalConInteres)
                    .HasPrecision(18, 2);

                entity.Property(e => e.RecargoAplicado)
                    .HasPrecision(18, 2);

                entity.HasOne(e => e.Venta)
                    .WithOne(v => v.DatosTarjeta)
                    .HasForeignKey<DatosTarjeta>(e => e.VentaId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.ConfiguracionTarjeta)
                    .WithMany()
                    .HasForeignKey(e => e.ConfiguracionTarjetaId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Configuración para DatosCheque
            modelBuilder.Entity<DatosCheque>(entity =>
            {
                entity.ToTable("DatosCheque");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.NumeroCheque)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Banco)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Titular)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Monto)
                    .HasPrecision(18, 2);

                entity.HasIndex(e => e.NumeroCheque);

                entity.HasOne(e => e.Venta)
                    .WithOne(v => v.DatosCheque)
                    .HasForeignKey<DatosCheque>(e => e.VentaId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuración para AlertaCobranza
            modelBuilder.Entity<AlertaCobranza>(entity =>
            {
                entity.ToTable("AlertasCobranza");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.MontoVencido)
                    .HasPrecision(18, 2);

                entity.HasOne(e => e.Cliente)
                    .WithMany()
                    .HasForeignKey(e => e.ClienteId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Credito)
                    .WithMany()
                    .HasForeignKey(e => e.CreditoId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.FechaAlerta);
                entity.HasIndex(e => e.Tipo);
                entity.HasIndex(e => e.Prioridad);
                entity.HasIndex(e => e.Resuelta);

                entity.Property(e => e.RowVersion)
                    .IsRowVersion();

                entity.HasQueryFilter(e => !e.IsDeleted);
            });

            // Configuración para ConfiguracionMora
            modelBuilder.Entity<ConfiguracionMora>(entity =>
            {
                entity.ToTable("ConfiguracionesMora");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.PorcentajeRecargo)
                    .HasPrecision(5, 2);

                entity.Property(e => e.RowVersion)
                    .IsRowVersion();

                entity.HasQueryFilter(e => !e.IsDeleted);
            });

            // Configuración para LogMora
            modelBuilder.Entity<LogMora>(entity =>
            {
                entity.ToTable("LogsMora");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.TotalMora)
                    .HasPrecision(18, 2);

                entity.Property(e => e.TotalRecargosAplicados)
                    .HasPrecision(18, 2);

                entity.HasIndex(e => e.FechaEjecucion);
                entity.HasIndex(e => e.Exitoso);

                entity.Property(e => e.RowVersion)
                    .IsRowVersion();

                entity.HasQueryFilter(e => !e.IsDeleted);
            });

            modelBuilder.Entity<Marca>().HasData(
                new Marca
                {
                    Id = 1,
                    Codigo = "SAM",
                    Nombre = "Samsung",
                    Descripcion = "Electrónica y electrodomésticos",
                    PaisOrigen = "Corea del Sur",
                    Activo = true,
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
                    Activo = true,
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
                    Activo = true,
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
                    entry.Entity.CreatedBy = "System";
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedBy = "System";

                    entry.Property(e => e.CreatedAt).IsModified = false;
                    entry.Property(e => e.CreatedBy).IsModified = false;
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}