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
            CreateMap<Cliente, ClienteViewModel>()
                .ForMember(d => d.Edad, o => o.MapFrom(s =>
                    s.FechaNacimiento.HasValue
                        ? (int)((DateTime.Today - s.FechaNacimiento.Value).TotalDays / 365.25)
                        : (int?)null))
                .ForMember(d => d.CreditosActivos, o => o.MapFrom(s => s.Creditos.Count(c =>
                    c.Estado == EstadoCredito.Activo)))
                .ForMember(d => d.TotalAdeudado, o => o.MapFrom(s => s.Creditos
                    .Where(c => c.Estado == EstadoCredito.Activo)
                    .Sum(c => c.SaldoPendiente)));

            CreateMap<ClienteViewModel, Cliente>()
                .ForMember(d => d.Creditos, o => o.Ignore())
                .ForMember(d => d.ComoGarante, o => o.Ignore());

            // =======================
            // Credito
            // =======================
            CreateMap<Credito, CreditoViewModel>()
                .ForMember(d => d.ClienteNombre, o => o.MapFrom(s =>
                    s.Cliente != null ? $"{s.Cliente.Apellido}, {s.Cliente.Nombre}" : null))
                .ForMember(d => d.GaranteNombre, o => o.MapFrom(s =>
                    s.Garante != null ? $"{s.Garante.Apellido}, {s.Garante.Nombre}" : null))
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

            // Mappings para Ventas
            CreateMap<Venta, VentaViewModel>()
                .ForMember(dest => dest.ClienteNombre, opt => opt.MapFrom(src => src.Cliente.Nombre))
                .ForMember(dest => dest.ClienteApellido, opt => opt.MapFrom(src => src.Cliente.Apellido))
                .ForMember(dest => dest.ClienteDocumento, opt => opt.MapFrom(src => src.Cliente.Documento))
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

            CreateMap<Factura, FacturaViewModel>()
                .ForMember(dest => dest.VentaNumero, opt => opt.MapFrom(src => src.Venta.Numero));

            CreateMap<FacturaViewModel, Factura>()
                .ForMember(dest => dest.Venta, opt => opt.Ignore());
        }
    }
}