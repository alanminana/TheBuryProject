// Helpers/MappingProfile.cs
using AutoMapper;
using TheBuryProject.Models.Entities;
using TheBuryProject.ViewModels;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Categoria, CategoriaViewModel>()
            .ForMember(d => d.ParentNombre, o => o.MapFrom(s => s.Parent.Nombre));

        CreateMap<Marca, MarcaViewModel>()
            .ForMember(d => d.ParentNombre, o => o.MapFrom(s => s.Parent.Nombre));
    }
}