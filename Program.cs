using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TheBuryProject.Data;
using TheBuryProject.Extensions;
using TheBuryProject.Helpers;
using TheBuryProject.Hubs;
using TheBuryProject.Services;
using TheBuryProject.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// 1. Configuración de infraestructura base
builder.Services.AddHttpContextAccessor();

// 2. Configuración de DbContext con SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDbContextFactory<AppDbContext>((serviceProvider, options) =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// 3. Configuración de Identity
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

// 4. Configuración de AutoMapper
builder.Services.AddSingleton<IMapper>(sp =>
{
    var loggerFactory = sp.GetService<ILoggerFactory>();
    var config = new MapperConfiguration(cfg =>
    {
        cfg.AddProfile<MappingProfile>();
    }, loggerFactory);

    return config.CreateMapper();
});

// 5. Registro de servicios (Dependency Injection)

// 5.1 Servicios auxiliares de ventas (registra: CurrentUserService, FinancialCalculationService, VentaValidator, VentaNumberGenerator)
builder.Services.AddVentaServices();

// 5.2 Servicios principales
builder.Services.AddScoped<ICategoriaService, CategoriaService>();
builder.Services.AddScoped<IMarcaService, MarcaService>();
builder.Services.AddScoped<IProductoService, ProductoService>();
builder.Services.AddScoped<ICatalogLookupService, CatalogLookupService>();
builder.Services.AddScoped<IPrecioHistoricoService, PrecioHistoricoService>();
builder.Services.AddScoped<IProveedorService, ProveedorService>();
builder.Services.AddScoped<IOrdenCompraService, OrdenCompraService>();
builder.Services.AddScoped<IMovimientoStockService, MovimientoStockService>();
builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<ICreditoService, CreditoService>();
builder.Services.AddScoped<IVentaService, VentaService>();
builder.Services.AddScoped<IConfiguracionPagoService, ConfiguracionPagoService>();
builder.Services.AddScoped<IRolService, RolService>();

// 5.3 Servicios de precios
builder.Services.AddScoped<IPrecioService, PrecioService>();

// 5.4 Otros servicios
builder.Services.AddScoped<IChequeService, ChequeService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IMoraService, MoraService>();
builder.Services.AddScoped<IEvaluacionCreditoService, EvaluacionCreditoService>();
builder.Services.AddScoped<IDocumentoClienteService, DocumentoClienteService>();
builder.Services.AddScoped<IAlertaStockService, AlertaStockService>();
builder.Services.AddScoped<IReporteService, ReporteService>();
builder.Services.AddScoped<IAutorizacionService, AutorizacionService>();
builder.Services.AddScoped<IDevolucionService, DevolucionService>();
builder.Services.AddScoped<ICajaService, CajaService>();
builder.Services.AddScoped<INotificacionService, NotificacionService>();
builder.Services.AddScoped<IDocumentacionService, DocumentacionService>();

// 5.5 SignalR
builder.Services.AddSignalR();

// 5.6 Servicios en background
builder.Services.AddHostedService<MoraBackgroundService>();
builder.Services.AddHostedService<AlertaStockBackgroundService>();
builder.Services.AddHostedService<DocumentoVencidoBackgroundService>();

// 6. Configuración de MVC
builder.Services.AddControllersWithViews();

// 7. Configuración de Razor Pages (para Identity UI)
builder.Services.AddRazorPages();

var app = builder.Build();

// 8. Configuración del pipeline HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 9. Autenticación y autorización
app.UseAuthentication();
app.UseAuthorization();

// 10. Mapeo de rutas
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// 11. Mapeo de Razor Pages (para Identity UI)
app.MapRazorPages();

// 12. Hubs de SignalR
app.MapHub<NotificacionesHub>("/hubs/notificaciones");

// 13. Inicializar base de datos (roles y usuario admin)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        // Inicializar roles y usuario administrador
        await DbInitializer.Initialize(services);

        // En desarrollo, crear usuarios de prueba
        if (app.Environment.IsDevelopment())
        {
            await DbInitializer.CreateTestUsersAsync(services);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error durante la inicialización de la base de datos");
    }
}

app.Run();