using Microsoft.AspNetCore.Http;
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
        private readonly IHttpContextAccessor? _httpContextAccessor;

        // FIX (Punto 6): Seed determinístico (evita DateTime.UtcNow en HasData)
        private static readonly DateTime SeedCreatedAtUtc =
            new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public AppDbContext(
            DbContextOptions<AppDbContext> options,
            IHttpContextAccessor? httpContextAccessor = null)
            : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        // DbSets - Cada uno representa una tabla en la base de datos
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Marca> Marcas { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<PrecioHistorico> PreciosHistoricos { get; set; }

        // Proveedores
        public DbSet<Proveedor> Proveedores { get; set; }
        public DbSet<ProveedorProducto> ProveedorProductos { get; set; }
        public DbSet<ProveedorMarca> ProveedorMarcas { get; set; }
        public DbSet<ProveedorCategoria> ProveedorCategorias { get; set; }

        // Órdenes de Compra
        public DbSet<OrdenCompra> OrdenesCompra { get; set; }
        public DbSet<OrdenCompraDetalle> OrdenCompraDetalles { get; set; }
        // Alias por compatibilidad. Mantener un único DbSet real por entidad.
        // Preferir OrdenCompraDetalles.
        [System.Obsolete("Use OrdenCompraDetalles")]
        public DbSet<OrdenCompraDetalle> OrdenesCompraDetalles => OrdenCompraDetalles;
        public DbSet<MovimientoStock> MovimientosStock { get; set; }

        // Cheques
        public DbSet<Cheque> Cheques { get; set; }

        // Clientes y Créditos
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Credito> Creditos { get; set; }
        public DbSet<Cuota> Cuotas { get; set; }
        public DbSet<Garante> Garantes { get; set; }
        public DbSet<DocumentoCliente> DocumentosCliente { get; set; }
        public DbSet<EvaluacionCredito> EvaluacionesCredito { get; set; } = null!;

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
        public DbSet<AlertaStock> AlertasStock { get; set; }

        // Módulo de Autorizaciones
        public DbSet<UmbralAutorizacion> UmbralesAutorizacion { get; set; }
        public DbSet<SolicitudAutorizacion> SolicitudesAutorizacion { get; set; }

        // Módulo de Devoluciones y Garantías
        public DbSet<Devolucion> Devoluciones { get; set; }
        public DbSet<DevolucionDetalle> DevolucionDetalles { get; set; }
        public DbSet<Garantia> Garantias { get; set; }
        public DbSet<RMA> RMAs { get; set; }
        public DbSet<NotaCredito> NotasCredito { get; set; }

        // Módulo de Cajas
        public DbSet<Caja> Cajas { get; set; }
        public DbSet<AperturaCaja> AperturasCaja { get; set; }
        public DbSet<MovimientoCaja> MovimientosCaja { get; set; }
        public DbSet<CierreCaja> CierresCaja { get; set; }

        // Módulo de Notificaciones
        public DbSet<Notificacion> Notificaciones { get; set; }

        // Módulo de Roles y Permisos
        public DbSet<RolPermiso> RolPermisos { get; set; }
        public DbSet<ModuloSistema> ModulosSistema { get; set; }
        public DbSet<AccionModulo> AccionesModulo { get; set; }

        // Módulo de Precios con Historial
        public DbSet<ListaPrecio> ListasPrecios { get; set; }
        public DbSet<ProductoPrecioLista> ProductosPrecios { get; set; }
        public DbSet<PriceChangeBatch> PriceChangeBatches { get; set; }
        public DbSet<PriceChangeItem> PriceChangeItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración de Categoria
            modelBuilder.Entity<Categoria>(entity =>
            {
                entity.HasIndex(e => e.Codigo)
                    .IsUnique()
                    .HasFilter("IsDeleted = 0");

                entity.Property(e => e.Codigo)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasDefaultValue(string.Empty);

                entity.Property(e => e.Nombre)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasDefaultValue(string.Empty);

                entity.Property(e => e.Activo)
                    .HasDefaultValue(true)
                    .ValueGeneratedNever();

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

            // Configuración de PrecioHistorico
            modelBuilder.Entity<PrecioHistorico>(entity =>
            {
                entity.HasOne(e => e.Producto)
                    .WithMany()
                    .HasForeignKey(e => e.ProductoId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.PrecioCompraAnterior)
                    .HasPrecision(18, 2);

                entity.Property(e => e.PrecioCompraNuevo)
                    .HasPrecision(18, 2);

                entity.Property(e => e.PrecioVentaAnterior)
                    .HasPrecision(18, 2);

                entity.Property(e => e.PrecioVentaNuevo)
                    .HasPrecision(18, 2);

                entity.HasIndex(e => e.ProductoId);
                entity.HasIndex(e => e.FechaCambio);
                entity.HasIndex(e => e.UsuarioModificacion);

                entity.Property(e => e.RowVersion)
                    .IsRowVersion();

                entity.HasQueryFilter(e => !e.IsDeleted);
            });

            modelBuilder.Entity<EvaluacionCredito>(entity =>
            {
                entity.ToTable("EvaluacionesCredito");

                entity.Property(e => e.MontoSolicitado)
                    .HasPrecision(18, 2);

                entity.Property(e => e.PuntajeRiesgoCliente)
                    .HasPrecision(18, 2);

                entity.Property(e => e.RelacionCuotaIngreso)
                    .HasPrecision(18, 4);

                entity.Property(e => e.PuntajeFinal)
                    .HasPrecision(18, 2);

                entity.Property(e => e.Motivo)
                    .HasMaxLength(1000);

                entity.Property(e => e.Observaciones)
                    .HasMaxLength(2000);

                entity.Property(e => e.RowVersion)
                    .IsRowVersion();

                entity.HasIndex(e => e.ClienteId);
                entity.HasIndex(e => e.CreditoId);
                entity.HasIndex(e => e.FechaEvaluacion);
                entity.HasIndex(e => e.Resultado);

                entity.HasOne(e => e.Cliente)
                    .WithMany()
                    .HasForeignKey(e => e.ClienteId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Credito)
                    .WithMany()
                    .HasForeignKey(e => e.CreditoId)
                    .OnDelete(DeleteBehavior.Restrict);

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

                entity.Property(e => e.TieneReciboSueldo)
                    .IsRequired()
                    .HasDefaultValue(false);

                entity.Property(e => e.PuntajeRiesgo)
                    .HasPrecision(5, 2);

                entity.Property(e => e.RowVersion)
                    .IsRowVersion();

                // Configurar relación con Garante
                entity.HasOne(e => e.Garante)
                    .WithMany()
                    .HasForeignKey(e => e.GaranteId)
                    .OnDelete(DeleteBehavior.NoAction);

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
                    .WithMany(c => c.Documentos)
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

            // =======================
            // Configuración para Caja
            // =======================
            modelBuilder.Entity<Caja>(entity =>
            {
                entity.HasIndex(e => e.Codigo)
                    .IsUnique()
                    .HasFilter("IsDeleted = 0");

                entity.Property(e => e.RowVersion)
                    .IsRowVersion();

                entity.HasQueryFilter(e => !e.IsDeleted);
            });

            // =======================
            // Configuración para AperturaCaja
            // =======================
            modelBuilder.Entity<AperturaCaja>(entity =>
            {
                entity.HasOne(e => e.Caja)
                    .WithMany(c => c.Aperturas)
                    .HasForeignKey(e => e.CajaId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.RowVersion)
                    .IsRowVersion();

                entity.HasQueryFilter(e => !e.IsDeleted);
            });

            // =======================
            // Configuración para MovimientoCaja
            // =======================
            modelBuilder.Entity<MovimientoCaja>(entity =>
            {
                entity.HasOne(e => e.AperturaCaja)
                    .WithMany(a => a.Movimientos)
                    .HasForeignKey(e => e.AperturaCajaId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.RowVersion)
                    .IsRowVersion();

                entity.HasQueryFilter(e => !e.IsDeleted);

                entity.HasIndex(e => e.FechaMovimiento);
                entity.HasIndex(e => e.Tipo);
                entity.HasIndex(e => e.Concepto);
            });

            // =======================
            // Configuración para CierreCaja
            // =======================
            modelBuilder.Entity<CierreCaja>(entity =>
            {
                entity.HasOne(e => e.AperturaCaja)
                    .WithOne(a => a.Cierre)
                    .HasForeignKey<CierreCaja>(e => e.AperturaCajaId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.RowVersion)
                    .IsRowVersion();

                entity.HasQueryFilter(e => !e.IsDeleted);

                entity.HasIndex(e => e.FechaCierre);
                entity.HasIndex(e => e.TieneDiferencia);
            });

            // =======================
            // Configuración para Notificacion
            // =======================
            modelBuilder.Entity<Notificacion>(entity =>
            {
                entity.Property(e => e.RowVersion)
                    .IsRowVersion();

                entity.HasQueryFilter(e => !e.IsDeleted);

                entity.HasIndex(e => e.UsuarioDestino);
                entity.HasIndex(e => e.Leida);
                entity.HasIndex(e => e.FechaNotificacion);
                entity.HasIndex(e => e.Tipo);
                entity.HasIndex(e => e.Prioridad);
            });

            // =======================
            // Configuración para ListaPrecio
            // =======================
            modelBuilder.Entity<ListaPrecio>(entity =>
            {
                entity.ToTable("ListasPrecios");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Nombre)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Codigo)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.Descripcion)
                    .HasMaxLength(500);

                entity.Property(e => e.MargenPorcentaje)
                    .HasPrecision(5, 2);

                entity.Property(e => e.RecargoPorcentaje)
                    .HasPrecision(5, 2);

                entity.Property(e => e.ReglasJson)
                    .HasColumnType("nvarchar(max)");

                entity.HasIndex(e => e.Codigo)
                    .IsUnique()
                    .HasFilter("IsDeleted = 0");

                entity.HasIndex(e => e.Activa);
                entity.HasIndex(e => e.EsPredeterminada);
                entity.HasIndex(e => e.Orden);

                entity.Property(e => e.RowVersion)
                    .IsRowVersion();

                entity.HasQueryFilter(e => !e.IsDeleted);
            });

            // =======================
            // Configuración para ProductoPrecioLista
            // =======================
            modelBuilder.Entity<ProductoPrecioLista>(entity =>
            {
                entity.ToTable("ProductosPrecios");
                entity.HasKey(e => e.Id);

                // Índice compuesto único para la clave de negocio
                entity.HasIndex(e => new { e.ProductoId, e.ListaId, e.VigenciaDesde })
                    .IsUnique()
                    .HasFilter("IsDeleted = 0");

                entity.Property(e => e.Costo)
                    .IsRequired()
                    .HasPrecision(18, 2);

                entity.Property(e => e.Precio)
                    .IsRequired()
                    .HasPrecision(18, 2);

                entity.Property(e => e.MargenPorcentaje)
                    .HasPrecision(5, 2);

                entity.Property(e => e.MargenValor)
                    .HasPrecision(18, 2);

                entity.Property(e => e.CreadoPor)
                    .HasMaxLength(50);

                entity.Property(e => e.Notas)
                    .HasMaxLength(500);

                // Índices para consultas comunes
                entity.HasIndex(e => new { e.ProductoId, e.ListaId, e.EsVigente });
                entity.HasIndex(e => e.VigenciaDesde);
                entity.HasIndex(e => e.VigenciaHasta);
                entity.HasIndex(e => e.BatchId);

                // Relaciones
                entity.HasOne(e => e.Producto)
                    .WithMany()
                    .HasForeignKey(e => e.ProductoId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Lista)
                    .WithMany(l => l.Precios)
                    .HasForeignKey(e => e.ListaId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Batch)
                    .WithMany()
                    .HasForeignKey(e => e.BatchId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.Property(e => e.RowVersion)
                    .IsRowVersion();

                entity.HasQueryFilter(e => !e.IsDeleted);
            });

            // =======================
            // Configuración para PriceChangeBatch
            // =======================
            modelBuilder.Entity<PriceChangeBatch>(entity =>
            {
                entity.ToTable("PriceChangeBatches");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Nombre)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.ValorCambio)
                    .HasPrecision(18, 2);

                entity.Property(e => e.AlcanceJson)
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                entity.Property(e => e.ListasAfectadasJson)
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                entity.Property(e => e.SimulacionJson)
                    .HasColumnType("nvarchar(max)");

                entity.Property(e => e.SolicitadoPor)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.AprobadoPor)
                    .HasMaxLength(50);

                entity.Property(e => e.AplicadoPor)
                    .HasMaxLength(50);

                entity.Property(e => e.RevertidoPor)
                    .HasMaxLength(50);

                entity.Property(e => e.MotivoRechazo)
                    .HasMaxLength(500);

                entity.Property(e => e.Notas)
                    .HasMaxLength(1000);

                entity.Property(e => e.PorcentajePromedioCambio)
                    .HasPrecision(5, 2);

                // Índices
                entity.HasIndex(e => e.Estado);
                entity.HasIndex(e => e.TipoCambio);
                entity.HasIndex(e => e.FechaSolicitud);
                entity.HasIndex(e => e.FechaAplicacion);
                entity.HasIndex(e => e.SolicitadoPor);

                entity.Property(e => e.RowVersion)
                    .IsRowVersion();

                entity.HasQueryFilter(e => !e.IsDeleted);
            });

            // =======================
            // Configuración para PriceChangeItem
            // =======================
            modelBuilder.Entity<PriceChangeItem>(entity =>
            {
                entity.ToTable("PriceChangeItems");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.ProductoCodigo)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.ProductoNombre)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.PrecioAnterior)
                    .HasPrecision(18, 2);

                entity.Property(e => e.PrecioNuevo)
                    .HasPrecision(18, 2);

                entity.Property(e => e.DiferenciaValor)
                    .HasPrecision(18, 2);

                entity.Property(e => e.DiferenciaPorcentaje)
                    .HasPrecision(5, 2);

                entity.Property(e => e.Costo)
                    .HasPrecision(18, 2);

                entity.Property(e => e.MargenAnterior)
                    .HasPrecision(5, 2);

                entity.Property(e => e.MargenNuevo)
                    .HasPrecision(5, 2);

                entity.Property(e => e.MensajeAdvertencia)
                    .HasMaxLength(500);

                // Índices
                entity.HasIndex(e => e.BatchId);
                entity.HasIndex(e => e.ProductoId);
                entity.HasIndex(e => e.ListaId);
                entity.HasIndex(e => e.TieneAdvertencia);

                // Relaciones
                entity.HasOne(e => e.Batch)
                    .WithMany(b => b.Items)
                    .HasForeignKey(e => e.BatchId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Producto)
                    .WithMany()
                    .HasForeignKey(e => e.ProductoId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Lista)
                    .WithMany()
                    .HasForeignKey(e => e.ListaId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.RowVersion)
                    .IsRowVersion();

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
            var seedUtc = SeedCreatedAtUtc;

            modelBuilder.Entity<Categoria>().HasData(
                new Categoria
                {
                    Id = 1,
                    Codigo = "ELEC",
                    Nombre = "Electrónica",
                    Descripcion = "Productos electrónicos",
                    ControlSerieDefault = true,
                    Activo = true,
                    CreatedAt = seedUtc,   // FIX
                    CreatedBy = "System",
                    IsDeleted = false
                },
                new Categoria
                {
                    Id = 2,
                    Codigo = "FRIO",
                    Nombre = "Refrigeración",
                    Descripcion = "Heladeras, freezers y aire acondicionado",
                    ControlSerieDefault = true,
                    Activo = true,
                    CreatedAt = seedUtc,   // FIX
                    CreatedBy = "System",
                    IsDeleted = false
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

            // Configuración para AlertaStock
            modelBuilder.Entity<AlertaStock>(entity =>
            {
                entity.ToTable("AlertasStock");
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.Producto)
                    .WithMany()
                    .HasForeignKey(e => e.ProductoId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.StockActual)
                    .HasPrecision(18, 2);

                entity.Property(e => e.StockMinimo)
                    .HasPrecision(18, 2);

                entity.Property(e => e.CantidadSugeridaReposicion)
                    .HasPrecision(18, 2);

                entity.HasIndex(e => e.ProductoId)
                    .HasDatabaseName("IX_AlertasStock_ProductoId");

                // Enforce: a lo sumo 1 alerta "activa" (no resuelta) por producto.
                // Definición de "activa": IsDeleted = 0 AND FechaResolucion IS NULL.
                entity.HasIndex(e => e.ProductoId)
                    .HasDatabaseName("UX_AlertasStock_Producto_Activa")
                    .IsUnique()
                    .HasFilter("[IsDeleted] = 0 AND [FechaResolucion] IS NULL");
                entity.HasIndex(e => e.FechaAlerta);
                entity.HasIndex(e => e.Tipo);
                entity.HasIndex(e => e.Prioridad);
                entity.HasIndex(e => e.Estado);
                entity.HasIndex(e => e.NotificacionUrgente);

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

            // Configuración para Devolucion
            modelBuilder.Entity<Devolucion>(entity =>
            {
                entity.ToTable("Devoluciones");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.NumeroDevolucion)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.TotalDevolucion)
                    .HasPrecision(18, 2);

                entity.HasIndex(e => e.NumeroDevolucion).IsUnique();
                entity.HasIndex(e => e.VentaId);
                entity.HasIndex(e => e.ClienteId);
                entity.HasIndex(e => e.FechaDevolucion);
                entity.HasIndex(e => e.Estado);

                entity.HasOne(e => e.Venta)
                    .WithMany()
                    .HasForeignKey(e => e.VentaId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Cliente)
                    .WithMany()
                    .HasForeignKey(e => e.ClienteId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(e => e.Detalles)
                    .WithOne(d => d.Devolucion)
                    .HasForeignKey(d => d.DevolucionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.RowVersion)
                    .IsRowVersion();

                entity.HasQueryFilter(e => !e.IsDeleted);
            });

            // Configuración para DevolucionDetalle
            modelBuilder.Entity<DevolucionDetalle>(entity =>
            {
                entity.ToTable("DevolucionDetalles");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.PrecioUnitario)
                    .HasPrecision(18, 2);

                entity.Property(e => e.Subtotal)
                    .HasPrecision(18, 2);

                entity.HasIndex(e => e.DevolucionId);
                entity.HasIndex(e => e.ProductoId);

                entity.HasOne(e => e.Devolucion)
                    .WithMany(d => d.Detalles)
                    .HasForeignKey(e => e.DevolucionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Producto)
                    .WithMany()
                    .HasForeignKey(e => e.ProductoId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.RowVersion)
                    .IsRowVersion();

                entity.HasQueryFilter(e => !e.IsDeleted);
            });

            // Configuración para Garantia
            modelBuilder.Entity<Garantia>(entity =>
            {
                entity.ToTable("Garantias");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.NumeroGarantia)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.HasIndex(e => e.NumeroGarantia).IsUnique();
                entity.HasIndex(e => e.VentaDetalleId);
                entity.HasIndex(e => e.ProductoId);
                entity.HasIndex(e => e.ClienteId);
                entity.HasIndex(e => e.FechaInicio);
                entity.HasIndex(e => e.FechaVencimiento);
                entity.HasIndex(e => e.Estado);

                entity.HasOne(e => e.VentaDetalle)
                    .WithMany()
                    .HasForeignKey(e => e.VentaDetalleId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Producto)
                    .WithMany()
                    .HasForeignKey(e => e.ProductoId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Cliente)
                    .WithMany()
                    .HasForeignKey(e => e.ClienteId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.RowVersion)
                    .IsRowVersion();

                entity.HasQueryFilter(e => !e.IsDeleted);
            });

            // Configuración para RMA
            modelBuilder.Entity<RMA>(entity =>
            {
                entity.ToTable("RMAs");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.NumeroRMA)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.MontoReembolso)
                    .HasPrecision(18, 2);

                entity.HasIndex(e => e.NumeroRMA).IsUnique();
                entity.HasIndex(e => e.DevolucionId);
                entity.HasIndex(e => e.ProveedorId);
                entity.HasIndex(e => e.FechaSolicitud);
                entity.HasIndex(e => e.Estado);

                entity.HasOne(e => e.Devolucion)
                    .WithOne(d => d.RMA)
                    .HasForeignKey<RMA>(e => e.DevolucionId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Proveedor)
                    .WithMany()
                    .HasForeignKey(e => e.ProveedorId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.RowVersion)
                    .IsRowVersion();

                entity.HasQueryFilter(e => !e.IsDeleted);
            });

            // Configuración para NotaCredito
            modelBuilder.Entity<NotaCredito>(entity =>
            {
                entity.ToTable("NotasCredito");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.NumeroNotaCredito)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.MontoTotal)
                    .HasPrecision(18, 2);

                entity.Property(e => e.MontoUtilizado)
                    .HasPrecision(18, 2);

                entity.HasIndex(e => e.NumeroNotaCredito).IsUnique();
                entity.HasIndex(e => e.DevolucionId);
                entity.HasIndex(e => e.ClienteId);
                entity.HasIndex(e => e.FechaEmision);
                entity.HasIndex(e => e.FechaVencimiento);
                entity.HasIndex(e => e.Estado);

                entity.HasOne(e => e.Devolucion)
                    .WithOne(d => d.NotaCredito)
                    .HasForeignKey<NotaCredito>(e => e.DevolucionId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Cliente)
                    .WithMany()
                    .HasForeignKey(e => e.ClienteId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.RowVersion)
                    .IsRowVersion();

                entity.HasQueryFilter(e => !e.IsDeleted);
            });

            // =======================
            // Configuración para ModuloSistema
            // =======================
            modelBuilder.Entity<ModuloSistema>(entity =>
            {
                entity.ToTable("ModulosSistema");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Nombre)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Clave)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Descripcion)
                    .HasMaxLength(500);

                entity.Property(e => e.Icono)
                    .HasMaxLength(50);

                entity.Property(e => e.Categoria)
                    .HasMaxLength(50);

                entity.HasIndex(e => e.Clave)
                    .IsUnique()
                    .HasFilter("IsDeleted = 0");

                entity.HasIndex(e => e.Orden);
                entity.HasIndex(e => e.Activo);
                entity.HasIndex(e => e.Categoria);

                entity.Property(e => e.RowVersion)
                    .IsRowVersion();

                entity.HasQueryFilter(e => !e.IsDeleted);
            });

            // =======================
            // Configuración para AccionModulo
            // =======================
            modelBuilder.Entity<AccionModulo>(entity =>
            {
                entity.ToTable("AccionesModulo");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Nombre)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Clave)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Descripcion)
                    .HasMaxLength(500);

                entity.Property(e => e.Icono)
                    .HasMaxLength(50);

                entity.HasIndex(e => e.ModuloId);
                entity.HasIndex(e => e.Orden);
                entity.HasIndex(e => e.Activa);

                entity.HasIndex(e => new { e.ModuloId, e.Clave })
                    .IsUnique()
                    .HasFilter("IsDeleted = 0");

                entity.HasOne(e => e.Modulo)
                    .WithMany(m => m.Acciones)
                    .HasForeignKey(e => e.ModuloId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.RowVersion)
                    .IsRowVersion();

                entity.HasQueryFilter(e => !e.IsDeleted);
            });

            // =======================
            // Configuración para RolPermiso
            // =======================
            modelBuilder.Entity<RolPermiso>(entity =>
            {
                entity.ToTable("RolPermisos");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.RoleId)
                    .IsRequired()
                    .HasMaxLength(450);

                entity.Property(e => e.ClaimValue)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Observaciones)
                    .HasMaxLength(500);

                entity.HasIndex(e => e.RoleId);
                entity.HasIndex(e => e.ModuloId);
                entity.HasIndex(e => e.AccionId);
                entity.HasIndex(e => e.ClaimValue);

                entity.HasIndex(e => new { e.RoleId, e.ModuloId, e.AccionId })
                    .IsUnique()
                    .HasFilter("IsDeleted = 0");

                entity.HasOne(e => e.Role)
                    .WithMany()
                    .HasForeignKey(e => e.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Modulo)
                    .WithMany(m => m.Permisos)
                    .HasForeignKey(e => e.ModuloId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.Accion)
                    .WithMany(a => a.Permisos)
                    .HasForeignKey(e => e.AccionId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.Property(e => e.RowVersion)
                    .IsRowVersion();

                entity.HasQueryFilter(e => !e.IsDeleted);
            });

            // =======================
            // Configuración para UmbralAutorizacion
            // =======================
            modelBuilder.Entity<UmbralAutorizacion>(entity =>
            {
                entity.ToTable("UmbralesAutorizacion");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Rol)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.ValorMaximo)
                    .HasPrecision(18, 2);

                entity.Property(e => e.Descripcion)
                    .HasMaxLength(500);

                entity.HasIndex(e => e.TipoUmbral);
                entity.HasIndex(e => e.Rol);
                entity.HasIndex(e => e.Activo);

                entity.Property(e => e.RowVersion)
                    .IsRowVersion();

                entity.HasQueryFilter(e => !e.IsDeleted);
            });

            // =======================
            // Configuración para SolicitudAutorizacion
            // =======================
            modelBuilder.Entity<SolicitudAutorizacion>(entity =>
            {
                entity.ToTable("SolicitudesAutorizacion");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.UsuarioSolicitante)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.RolSolicitante)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.ValorSolicitado)
                    .HasPrecision(18, 2);

                entity.Property(e => e.ValorPermitido)
                    .HasPrecision(18, 2);

                entity.Property(e => e.TipoOperacion)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Justificacion)
                    .IsRequired()
                    .HasMaxLength(1000);

                entity.Property(e => e.UsuarioAutorizador)
                    .HasMaxLength(50);

                entity.Property(e => e.ComentarioResolucion)
                    .HasMaxLength(500);

                entity.HasIndex(e => e.TipoUmbral);
                entity.HasIndex(e => e.Estado);
                entity.HasIndex(e => e.UsuarioSolicitante);
                entity.HasIndex(e => e.UsuarioAutorizador);
                entity.HasIndex(e => e.FechaResolucion);

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
                    CreatedAt = seedUtc,   // FIX
                    CreatedBy = "System",
                    IsDeleted = false
                },
                new Marca
                {
                    Id = 2,
                    Codigo = "LG",
                    Nombre = "LG",
                    Descripcion = "Electrónica y electrodomésticos",
                    PaisOrigen = "Corea del Sur",
                    Activo = true,
                    CreatedAt = seedUtc,   // FIX
                    CreatedBy = "System",
                    IsDeleted = false
                },
                new Marca
                {
                    Id = 3,
                    Codigo = "WHI",
                    Nombre = "Whirlpool",
                    Descripcion = "Electrodomésticos",
                    PaisOrigen = "Estados Unidos",
                    Activo = true,
                    CreatedAt = seedUtc,   // FIX
                    CreatedBy = "System",
                    IsDeleted = false
                }
            );
        }

        /// <summary>
        /// Interceptor para auditoría automática antes de guardar cambios
        /// </summary>
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker.Entries<BaseEntity>();
            var currentUser = _httpContextAccessor?.HttpContext?.User?.Identity?.Name ?? "System";

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.CreatedBy = currentUser;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedBy = currentUser;

                    entry.Property(e => e.CreatedAt).IsModified = false;
                    entry.Property(e => e.CreatedBy).IsModified = false;
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
