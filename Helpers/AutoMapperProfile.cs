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
        }
    }
}
