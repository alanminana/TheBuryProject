// Helpers/MappingProfile.cs
using AutoMapper;
using TheBuryProject.Models.Entities;
using TheBuryProject.ViewModels;

namespace TheBuryProject.Helpers
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Categoria, CategoriaViewModel>()
                .ForMember(d => d.ParentNombre, o => o.MapFrom(s => s.Parent != null ? s.Parent.Nombre : null));

            CreateMap<Marca, MarcaViewModel>()
                .ForMember(d => d.ParentNombre, o => o.MapFrom(s => s.Parent != null ? s.Parent.Nombre : null));

            CreateMap<Producto, ProductoViewModel>()
            .ForMember(d => d.CategoriaNombre, o => o.MapFrom(s => s.Categoria != null ? s.Categoria.Nombre : null))
            .ForMember(d => d.MarcaNombre, o => o.MapFrom(s => s.Marca != null ? s.Marca.Nombre : null));

            CreateMap<ProductoViewModel, Producto>();
        }

    }
}