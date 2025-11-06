using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TheBuryProject.Data;
using TheBuryProject.Helpers;
using TheBuryProject.Services;
using TheBuryProject.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// 1. Configuración de DbContext con SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Configuración de Identity
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
    options.User.RequireUniqueEmail = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<AppDbContext>();

// 3. Configuración de AutoMapper
builder.Services.AddSingleton<IMapper>(sp =>
{
    var loggerFactory = sp.GetService<ILoggerFactory>();
    var config = new MapperConfiguration(cfg =>
    {
        cfg.AddProfile<MappingProfile>();
    }, loggerFactory);

    return config.CreateMapper();
});

// 4. Registro de servicios (Dependency Injection)
builder.Services.AddScoped<ICategoriaService, CategoriaService>();
builder.Services.AddScoped<IMarcaService, MarcaService>();
builder.Services.AddScoped<IProductoService, ProductoService>();  // ← Esta línea
builder.Services.AddScoped<IProveedorService, ProveedorService>(); 
builder.Services.AddScoped<IOrdenCompraService, OrdenCompraService>();  // ← AGREGAR ESTA LÍNEA
builder.Services.AddScoped<IMovimientoStockService, MovimientoStockService>();
builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<ICreditoService, CreditoService>();

builder.Services.AddScoped<IChequeService, ChequeService>();

// 5. Configuración de MVC
builder.Services.AddControllersWithViews();

// 6. Configuración de Razor Pages (para Identity UI)
builder.Services.AddRazorPages();

var app = builder.Build();

// 7. Configuración del pipeline HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 8. Autenticación y autorización
app.UseAuthentication();
app.UseAuthorization();

// 9. Mapeo de rutas
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// 10. Mapeo de Razor Pages (para Identity UI)
app.MapRazorPages();

app.Run();