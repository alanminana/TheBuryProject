// Controllers/BaseEntityController.cs
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using TheBuryProject.Models.Base;
using TheBuryProject.Models.Entities;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.ViewModels;

public abstract class BaseEntityController<TEntity, TViewModel, TService> : Controller
    where TEntity : BaseEntity
    where TViewModel : class
    where TService : IRepository<TEntity>
{
    protected readonly TService _service;
    protected readonly IMapper _mapper;
    protected readonly ILogger _logger;

    // Métodos CRUD genéricos:
    // - Index()
    // - Details(id)
    // - Create() GET/POST
    // - Edit(id) GET/POST
    // - Delete(id) GET/POST
}

// Controllers/CategoriaController.cs (nuevo)
public class CategoriaController : BaseEntityController<Categoria, CategoriaViewModel, ICategoriaService>
{
    // Solo lógica específica si la hay
}