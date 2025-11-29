using AutoMapper;
using System.Linq;
using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;
using TheBuryProject.ViewModels;

namespace TheBuryProject.Helpers
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // =======================
            // Categoria
            // =======================
            CreateMap<Categoria, CategoriaViewModel>()
                .ForMember(d => d.ParentNombre, o => o.MapFrom(s => s.Parent != null ? s.Parent.Nombre : null));

            // =======================
            // Marca
            // =======================
            CreateMap<Marca, MarcaViewModel>()
                .ForMember(d => d.ParentNombre, o => o.MapFrom(s => s.Parent != null ? s.Parent.Nombre : null));

            // =======================
            // Producto
            // =======================
            CreateMap<Producto, ProductoViewModel>()
                .ForMember(d => d.CategoriaNombre, o => o.MapFrom(s => s.Categoria != null ? s.Categoria.Nombre : null))
                .ForMember(d => d.MarcaNombre, o => o.MapFrom(s => s.Marca != null ? s.Marca.Nombre : null));
            CreateMap<Producto, ProductoViewModel>()
    .ForMember(dest => dest.CategoriaNombre, opt => opt.MapFrom(src => src.Categoria.Nombre))
    .ForMember(dest => dest.MarcaNombre, opt => opt.MapFrom(src => src.Marca.Nombre))
    .ReverseMap();
            CreateMap<ProductoViewModel, Producto>();

            // =======================
            // Proveedor
            // =======================
            CreateMap<Proveedor, ProveedorViewModel>()
                .ForMember(d => d.TotalOrdenesCompra, o => o.MapFrom(s => s.OrdenesCompra.Count))
                .ForMember(d => d.ChequesVigentes, o => o.MapFrom(s => s.Cheques.Count(c =>
                    c.Estado != EstadoCheque.Cobrado &&
                    c.Estado != EstadoCheque.Rechazado &&
                    c.Estado != EstadoCheque.Anulado)))
                .ForMember(d => d.TotalDeuda, o => o.MapFrom(s => s.OrdenesCompra
                    .Where(oc => oc.Estado != EstadoOrdenCompra.Cancelada)
                    .Sum(oc => oc.Total)))
                .ForMember(d => d.ProductosSeleccionados, o => o.MapFrom(s => s.ProveedorProductos.Select(pp => pp.ProductoId)))
                .ForMember(d => d.MarcasSeleccionadas, o => o.MapFrom(s => s.ProveedorMarcas.Select(pm => pm.MarcaId)))
                .ForMember(d => d.CategoriasSeleccionadas, o => o.MapFrom(s => s.ProveedorCategorias.Select(pc => pc.CategoriaId)))
                .ForMember(d => d.ProductosAsociados, o => o.MapFrom(s => s.ProveedorProductos
                    .Where(pp => pp.Producto != null)
                    .Select(pp => pp.Producto!.Nombre)))
                .ForMember(d => d.MarcasAsociadas, o => o.MapFrom(s => s.ProveedorMarcas
                    .Where(pm => pm.Marca != null)
                    .Select(pm => pm.Marca!.Nombre)))
                .ForMember(d => d.CategoriasAsociadas, o => o.MapFrom(s => s.ProveedorCategorias
                    .Where(pc => pc.Categoria != null)
                    .Select(pc => pc.Categoria!.Nombre)));

            CreateMap<ProveedorViewModel, Proveedor>()
                .ForMember(d => d.ProveedorProductos, o => o.MapFrom(s => s.ProductosSeleccionados
                    .Select(id => new ProveedorProducto { ProductoId = id })))
                .ForMember(d => d.ProveedorMarcas, o => o.MapFrom(s => s.MarcasSeleccionadas
                    .Select(id => new ProveedorMarca { MarcaId = id })))
                .ForMember(d => d.ProveedorCategorias, o => o.MapFrom(s => s.CategoriasSeleccionadas
                    .Select(id => new ProveedorCategoria { CategoriaId = id })))
                .ForMember(d => d.OrdenesCompra, o => o.Ignore())
                .ForMember(d => d.Cheques, o => o.Ignore());

            // =======================
            // OrdenCompra
            // =======================
            CreateMap<OrdenCompra, OrdenCompraViewModel>()
                .ForMember(d => d.ProveedorNombre, o => o.MapFrom(s => s.Proveedor != null ? s.Proveedor.RazonSocial : null))
                .ForMember(d => d.EstadoNombre, o => o.MapFrom(s => s.Estado.ToString()))
                .ForMember(d => d.TotalItems, o => o.MapFrom(s => s.Detalles.Sum(d => d.Cantidad)))
                .ForMember(d => d.TotalRecibido, o => o.MapFrom(s => s.Detalles.Sum(d => d.CantidadRecibida)))
                .ForMember(d => d.Detalles, o => o.MapFrom(s => s.Detalles));

            CreateMap<OrdenCompraViewModel, OrdenCompra>()
                .ForMember(d => d.Proveedor, o => o.Ignore())
                .ForMember(d => d.Detalles, o => o.MapFrom(s => s.Detalles));

            // =======================
            // OrdenCompraDetalle
            // =======================
            CreateMap<OrdenCompraDetalle, OrdenCompraDetalleViewModel>()
                .ForMember(d => d.ProductoNombre, o => o.MapFrom(s => s.Producto != null ? s.Producto.Nombre : null))
                .ForMember(d => d.ProductoCodigo, o => o.MapFrom(s => s.Producto != null ? s.Producto.Codigo : null));

            CreateMap<OrdenCompraDetalleViewModel, OrdenCompraDetalle>()
                .ForMember(d => d.Producto, o => o.Ignore())
                .ForMember(d => d.OrdenCompra, o => o.Ignore());

            // =======================
            // Cheques
            // =======================
            CreateMap<Cheque, ChequeViewModel>()
                .ForMember(d => d.ProveedorNombre, o => o.MapFrom(s => s.Proveedor != null ? s.Proveedor.RazonSocial : null))
                .ForMember(d => d.OrdenCompraNumero, o => o.MapFrom(s => s.OrdenCompra != null ? s.OrdenCompra.Numero : null))
                .ForMember(d => d.EstadoNombre, o => o.MapFrom(s => s.Estado.ToString()))
                .ForMember(d => d.DiasPorVencer, o => o.MapFrom(s =>
                    s.FechaVencimiento.HasValue
                        ? (int)(s.FechaVencimiento.Value - DateTime.Today).TotalDays
                        : 0));

            CreateMap<ChequeViewModel, Cheque>()
                .ForMember(d => d.Proveedor, o => o.Ignore())
                .ForMember(d => d.OrdenCompra, o => o.Ignore());

            // Mappings para MovimientoStock
            CreateMap<MovimientoStock, MovimientoStockViewModel>()
                .ForMember(d => d.ProductoNombre, o => o.MapFrom(s => s.Producto != null ? s.Producto.Nombre : null))
                .ForMember(d => d.ProductoCodigo, o => o.MapFrom(s => s.Producto != null ? s.Producto.Codigo : null))
                .ForMember(d => d.TipoNombre, o => o.MapFrom(s => s.Tipo.ToString()))
                .ForMember(d => d.OrdenCompraNumero, o => o.MapFrom(s => s.OrdenCompra != null ? s.OrdenCompra.Numero : null))
                .ForMember(d => d.Fecha, o => o.MapFrom(s => s.CreatedAt));

            // =======================
            // Cliente
            // =======================
            CreateMap<Cliente, ClienteResumenViewModel>()
                .ForMember(d => d.NombreCompleto, o => o.MapFrom(s =>
                    !string.IsNullOrWhiteSpace(s.NombreCompleto)
                        ? s.NombreCompleto
                        : $"{s.Apellido}, {s.Nombre}"));

            CreateMap<Garante, ClienteResumenViewModel>()
                .ForMember(d => d.Id, o => o.MapFrom(s => s.GaranteClienteId ?? 0))
                .ForMember(d => d.NombreCompleto, o => o.MapFrom(s =>
                    s.GaranteCliente != null
                        ? (!string.IsNullOrWhiteSpace(s.GaranteCliente.NombreCompleto)
                            ? s.GaranteCliente.NombreCompleto
                            : $"{s.GaranteCliente.Apellido}, {s.GaranteCliente.Nombre}")
                        : $"{s.Apellido}, {s.Nombre}"))
                .ForMember(d => d.NumeroDocumento, o => o.MapFrom(s =>
                    s.GaranteCliente != null ? s.GaranteCliente.NumeroDocumento : s.NumeroDocumento ?? string.Empty))
                .ForMember(d => d.Telefono, o => o.MapFrom(s =>
                    s.GaranteCliente != null ? s.GaranteCliente.Telefono : s.Telefono ?? string.Empty))
                .ForMember(d => d.Email, o => o.MapFrom(s => s.GaranteCliente != null ? s.GaranteCliente.Email : null))
                .ForMember(d => d.Domicilio, o => o.MapFrom(s => s.GaranteCliente != null ? s.GaranteCliente.Domicilio : s.Domicilio))
                .ForMember(d => d.PuntajeRiesgo, o => o.MapFrom(s => s.GaranteCliente != null ? s.GaranteCliente.PuntajeRiesgo :
 0))
                .ForMember(d => d.Sueldo, o => o.MapFrom(s => s.GaranteCliente != null ? s.GaranteCliente.Sueldo : (decimal?)null));

            CreateMap<Cliente, ClienteViewModel>()
                .ForMember(d => d.Edad, o => o.MapFrom(s =>
                    s.FechaNacimiento.HasValue
                        ? (int)((DateTime.Today - s.FechaNacimiento.Value).TotalDays / 365.25)
                        : (int?)null))
                .ForMember(d => d.CreditosActivos, o => o.MapFrom(s => s.Creditos.Count(c =>
                    c.Estado == EstadoCredito.Activo)))
                .ForMember(d => d.MontoAdeudado, o => o.MapFrom(s => s.Creditos
                    .Where(c => c.Estado == EstadoCredito.Activo)
                    .Sum(c => c.SaldoPendiente)));

            CreateMap<ClienteViewModel, Cliente>()
                .ForMember(d => d.Creditos, o => o.Ignore())
                .ForMember(d => d.ComoGarante, o => o.Ignore());

            // =======================
            // Credito
            // =======================
            CreateMap<Credito, CreditoViewModel>()
                .ForMember(d => d.Cliente, o => o.MapFrom(s => s.Cliente))
                .ForMember(d => d.Garante, o => o.MapFrom(s => s.Garante))
                .ForMember(d => d.Cuotas, o => o.MapFrom(s => s.Cuotas));

            CreateMap<CreditoViewModel, Credito>()
                .ForMember(d => d.Cliente, o => o.Ignore())
                .ForMember(d => d.Garante, o => o.Ignore())
                .ForMember(d => d.Cuotas, o => o.Ignore());

            // =======================
            // Cuota
            // =======================
            CreateMap<Cuota, CuotaViewModel>();

            CreateMap<CuotaViewModel, Cuota>()
                .ForMember(d => d.Credito, o => o.Ignore());

            // =======================
            // Mappings para Ventas
            // =======================
            CreateMap<Venta, VentaViewModel>()
                .ForMember(dest => dest.ClienteNombre, opt => opt.MapFrom(src => $"{src.Cliente.Apellido}, {src.Cliente.Nombre}"))
                .ForMember(dest => dest.ClienteDocumento, opt => opt.MapFrom(src => src.Cliente.NumeroDocumento))
                .ForMember(dest => dest.CreditoNumero, opt => opt.MapFrom(src => src.Credito != null ? src.Credito.Numero : null))
                .ForMember(dest => dest.Detalles, opt => opt.MapFrom(src => src.Detalles))
                .ForMember(dest => dest.Facturas, opt => opt.MapFrom(src => src.Facturas));

            CreateMap<VentaViewModel, Venta>()
                .ForMember(dest => dest.Cliente, opt => opt.Ignore())
                .ForMember(dest => dest.Credito, opt => opt.Ignore())
                .ForMember(dest => dest.Detalles, opt => opt.Ignore())
                .ForMember(dest => dest.Facturas, opt => opt.Ignore());

            CreateMap<VentaDetalle, VentaDetalleViewModel>()
                .ForMember(dest => dest.ProductoNombre, opt => opt.MapFrom(src => src.Producto.Nombre))
                .ForMember(dest => dest.ProductoCodigo, opt => opt.MapFrom(src => src.Producto.Codigo))
                .ForMember(dest => dest.StockDisponible, opt => opt.MapFrom(src => src.Producto.StockActual));

            CreateMap<VentaDetalleViewModel, VentaDetalle>()
                .ForMember(dest => dest.Producto, opt => opt.Ignore())
                .ForMember(dest => dest.Venta, opt => opt.Ignore());

            CreateMap<Factura, FacturaViewModel>();

            CreateMap<FacturaViewModel, Factura>()
                .ForMember(dest => dest.Venta, opt => opt.Ignore());

            // =======================
            // ConfiguracionPago
            // =======================
            CreateMap<ConfiguracionPago, ConfiguracionPagoViewModel>()
                .ForMember(d => d.ConfiguracionesTarjeta, o => o.MapFrom(s => s.ConfiguracionesTarjeta));

            CreateMap<ConfiguracionPagoViewModel, ConfiguracionPago>()
                .ForMember(d => d.ConfiguracionesTarjeta, o => o.Ignore());

            // =======================
            // ConfiguracionTarjeta
            // =======================
            CreateMap<ConfiguracionTarjeta, ConfiguracionTarjetaViewModel>();

            CreateMap<ConfiguracionTarjetaViewModel, ConfiguracionTarjeta>()
                .ForMember(d => d.ConfiguracionPago, o => o.Ignore());

            // =======================
            // DatosTarjeta
            // =======================
            CreateMap<DatosTarjeta, DatosTarjetaViewModel>();

            CreateMap<DatosTarjetaViewModel, DatosTarjeta>()
                .ForMember(d => d.Venta, o => o.Ignore())
                .ForMember(d => d.ConfiguracionTarjeta, o => o.Ignore());

            // =======================
            // DatosCheque
            // =======================
            CreateMap<DatosCheque, DatosChequeViewModel>();

            CreateMap<DatosChequeViewModel, DatosCheque>()
                .ForMember(d => d.Venta, o => o.Ignore());

            // =======================
            // VentaCreditoCuota
            // =======================
            CreateMap<VentaCreditoCuota, VentaCreditoCuotaViewModel>();

            CreateMap<VentaCreditoCuotaViewModel, VentaCreditoCuota>()
                .ForMember(d => d.Venta, o => o.Ignore())
                .ForMember(d => d.Credito, o => o.Ignore());

            // =======================
            // DatosCreditoPersonal
            // =======================
            CreateMap<DatosCreditoPersonalViewModel, VentaCreditoCuota>()
                .ForMember(d => d.Venta, o => o.Ignore())
                .ForMember(d => d.Credito, o => o.Ignore());

            // Evaluación Crédito
            CreateMap<EvaluacionCredito, EvaluacionCreditoViewModel>()
                .ForMember(dest => dest.ClienteNombre, opt => opt.MapFrom(src =>
                    src.Cliente != null ? $"{src.Cliente.Apellido}, {src.Cliente.Nombre}" : null))
                .ReverseMap();
            // =======================
            // DocumentoCliente
            // =======================
            CreateMap<DocumentoCliente, DocumentoClienteViewModel>()
                .ForMember(dest => dest.Cliente, opt => opt.MapFrom(src => src.Cliente))
                .ForMember(dest => dest.Archivo, opt => opt.Ignore());

            CreateMap<DocumentoClienteViewModel, DocumentoCliente>()
                .ForMember(dest => dest.Cliente, opt => opt.Ignore())
                .ForMember(dest => dest.NombreArchivo, opt => opt.Ignore())
                .ForMember(dest => dest.RutaArchivo, opt => opt.Ignore())
                .ForMember(dest => dest.TipoMIME, opt => opt.Ignore())
                .ForMember(dest => dest.TamanoBytes, opt => opt.Ignore());
            // Mora
            // Mora
            CreateMap<ConfiguracionMora, ConfiguracionMoraViewModel>().ReverseMap();
            CreateMap<AlertaCobranza, AlertaCobranzaViewModel>()
                .ForMember(dest => dest.ClienteNombre, opt => opt.Ignore());
            CreateMap<AlertaCobranzaViewModel, AlertaCobranza>();

            // =======================
            // AlertaStock
            // =======================
            CreateMap<AlertaStock, AlertaStockViewModel>()
                .ForMember(dest => dest.ProductoCodigo, opt => opt.MapFrom(src => src.Producto != null ? src.Producto.Codigo : string.Empty))
                .ForMember(dest => dest.ProductoNombre, opt => opt.MapFrom(src => src.Producto != null ? src.Producto.Nombre : string.Empty))
                .ForMember(dest => dest.CategoriaNombre, opt => opt.MapFrom(src => src.Producto != null && src.Producto.Categoria != null ? src.Producto.Categoria.Nombre : string.Empty))
                .ForMember(dest => dest.MarcaNombre, opt => opt.MapFrom(src => src.Producto != null && src.Producto.Marca != null ? src.Producto.Marca.Nombre : string.Empty))
                .ForMember(dest => dest.PorcentajeStockMinimo, opt => opt.MapFrom(src =>
                    src.StockMinimo == 0 ? 0 : (src.StockActual / src.StockMinimo) * 100))
                .ForMember(dest => dest.DiasDesdeAlerta, opt => opt.MapFrom(src =>
                    (int)(DateTime.Now - src.FechaAlerta).TotalDays))
                .ForMember(dest => dest.EstaVencida, opt => opt.MapFrom(src =>
                    src.FechaResolucion == null && (DateTime.Now - src.FechaAlerta).TotalDays > 30));

            CreateMap<AlertaStockViewModel, AlertaStock>()
                .ForMember(dest => dest.Producto, opt => opt.Ignore());

            // =======================
            // PrecioHistorico
            // =======================
            CreateMap<PrecioHistorico, PrecioHistoricoViewModel>()
                .ForMember(dest => dest.ProductoCodigo, opt => opt.MapFrom(src => src.Producto != null ? src.Producto.Codigo : string.Empty))
                .ForMember(dest => dest.ProductoNombre, opt => opt.MapFrom(src => src.Producto != null ? src.Producto.Nombre : string.Empty))
                .ForMember(dest => dest.PorcentajeCambioCompra, opt => opt.MapFrom(src =>
                    src.PrecioCompraAnterior == 0 ? 0 : ((src.PrecioCompraNuevo - src.PrecioCompraAnterior) / src.PrecioCompraAnterior) * 100))
                .ForMember(dest => dest.PorcentajeCambioVenta, opt => opt.MapFrom(src =>
                    src.PrecioVentaAnterior == 0 ? 0 : ((src.PrecioVentaNuevo - src.PrecioVentaAnterior) / src.PrecioVentaAnterior) * 100))
                .ForMember(dest => dest.MargenAnterior, opt => opt.MapFrom(src =>
                    src.PrecioCompraAnterior == 0 ? 0 : ((src.PrecioVentaAnterior - src.PrecioCompraAnterior) / src.PrecioCompraAnterior) * 100))
                .ForMember(dest => dest.MargenNuevo, opt => opt.MapFrom(src =>
                    src.PrecioCompraNuevo == 0 ? 0 : ((src.PrecioVentaNuevo - src.PrecioCompraNuevo) / src.PrecioCompraNuevo) * 100));

            CreateMap<PrecioHistoricoViewModel, PrecioHistorico>()
                .ForMember(dest => dest.Producto, opt => opt.Ignore());

            // =======================
            // Autorizaciones
            // =======================
            CreateMap<UmbralAutorizacion, UmbralAutorizacionViewModel>().ReverseMap();

            CreateMap<SolicitudAutorizacion, GestionarSolicitudViewModel>();

            CreateMap<GestionarSolicitudViewModel, SolicitudAutorizacion>()
                .ForMember(dest => dest.UsuarioAutorizador, opt => opt.Ignore())
                .ForMember(dest => dest.FechaResolucion, opt => opt.Ignore());

            // =======================
            // Devoluciones y Garantías
            // =======================
            CreateMap<Devolucion, CrearDevolucionViewModel>()
                .ForMember(dest => dest.Productos, opt => opt.Ignore());

            CreateMap<CrearDevolucionViewModel, Devolucion>()
                .ForMember(dest => dest.Venta, opt => opt.Ignore())
                .ForMember(dest => dest.Cliente, opt => opt.Ignore())
                .ForMember(dest => dest.Detalles, opt => opt.Ignore())
                .ForMember(dest => dest.NotaCredito, opt => opt.Ignore())
                .ForMember(dest => dest.RMA, opt => opt.Ignore());

            CreateMap<DevolucionDetalle, ProductoDevolucionViewModel>()
                .ForMember(dest => dest.ProductoNombre, opt => opt.MapFrom(src => src.Producto != null ? src.Producto.Nombre : string.Empty))
                .ForMember(dest => dest.CantidadComprada, opt => opt.Ignore())
                .ForMember(dest => dest.CantidadDevolver, opt => opt.MapFrom(src => src.Cantidad));

            CreateMap<ProductoDevolucionViewModel, DevolucionDetalle>()
                .ForMember(dest => dest.Cantidad, opt => opt.MapFrom(src => src.CantidadDevolver))
                .ForMember(dest => dest.Producto, opt => opt.Ignore())
                .ForMember(dest => dest.Devolucion, opt => opt.Ignore());

            CreateMap<Garantia, CrearGarantiaViewModel>();

            CreateMap<CrearGarantiaViewModel, Garantia>()
                .ForMember(dest => dest.VentaDetalle, opt => opt.Ignore())
                .ForMember(dest => dest.Producto, opt => opt.Ignore())
                .ForMember(dest => dest.Cliente, opt => opt.Ignore())
                .ForMember(dest => dest.NumeroGarantia, opt => opt.Ignore())
                .ForMember(dest => dest.FechaVencimiento, opt => opt.Ignore())
                .ForMember(dest => dest.Estado, opt => opt.Ignore());

            CreateMap<RMA, CrearRMAViewModel>();

            CreateMap<CrearRMAViewModel, RMA>()
                .ForMember(dest => dest.Devolucion, opt => opt.Ignore())
                .ForMember(dest => dest.Proveedor, opt => opt.Ignore())
                .ForMember(dest => dest.NumeroRMA, opt => opt.Ignore())
                .ForMember(dest => dest.FechaSolicitud, opt => opt.Ignore())
                .ForMember(dest => dest.Estado, opt => opt.Ignore());

            CreateMap<RMA, GestionarRMAViewModel>()
                .ForMember(dest => dest.RMA, opt => opt.MapFrom(src => src));

            CreateMap<GestionarRMAViewModel, RMA>()
                .ForMember(dest => dest.Devolucion, opt => opt.Ignore())
                .ForMember(dest => dest.Proveedor, opt => opt.Ignore());

        }
    }
}