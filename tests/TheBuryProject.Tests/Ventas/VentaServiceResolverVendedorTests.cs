using System;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TheBuryProject.Data;
using TheBuryProject.Models.Constants;
using TheBuryProject.Models.Entities;
using TheBuryProject.Services;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.Services.Validators;
using TheBuryProject.Tests.TestHelpers;
using TheBuryProject.ViewModels;
using Xunit;

namespace TheBuryProject.Tests.Ventas;

public class VentaServiceResolverVendedorTests
{
    [Fact]
    public async Task ResolverVendedorAsync_con_delegacion_y_usuario_en_rol_vendedor_retorna_vendedor()
    {
        using var db = new SqliteInMemoryDb(userName: "admin");
        var adminUser = db.Context.Users.Single(u => u.UserName == "admin");

        AsignarRol(db.Context, adminUser.Id, Roles.Administrador);

        var vendedor = new ApplicationUser
        {
            UserName = "vendedor",
            Email = "vendedor@test.local",
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
        db.Context.Users.Add(vendedor);
        await db.Context.SaveChangesAsync();

        AsignarRol(db.Context, vendedor.Id, Roles.Vendedor);

        var httpContextAccessor = WithRole(
            db.HttpContextAccessor,
            adminUser.UserName ?? "admin",
            adminUser.Id,
            Roles.Administrador);

        var ventaService = CreateVentaService(db.Context, httpContextAccessor);
        var viewModel = new VentaViewModel { VendedorUserId = vendedor.Id };

        var result = await ResolverAsync(
            ventaService,
            viewModel,
            httpContextAccessor.HttpContext!.User,
            adminUser.Id,
            adminUser.UserName ?? "admin");

        Assert.Equal(vendedor.Id, result.UserId);
        Assert.Equal(vendedor.UserName, result.Nombre);
    }

    [Fact]
    public async Task ResolverVendedorAsync_con_delegacion_y_usuario_sin_rol_vendedor_lanza_error()
    {
        using var db = new SqliteInMemoryDb(userName: "admin");
        var adminUser = db.Context.Users.Single(u => u.UserName == "admin");

        AsignarRol(db.Context, adminUser.Id, Roles.Administrador);

        var usuario = new ApplicationUser
        {
            UserName = "operador",
            Email = "operador@test.local",
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
        db.Context.Users.Add(usuario);
        await db.Context.SaveChangesAsync();

        var httpContextAccessor = WithRole(
            db.HttpContextAccessor,
            adminUser.UserName ?? "admin",
            adminUser.Id,
            Roles.Administrador);

        var ventaService = CreateVentaService(db.Context, httpContextAccessor);
        var viewModel = new VentaViewModel { VendedorUserId = usuario.Id };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => ResolverAsync(
            ventaService,
            viewModel,
            httpContextAccessor.HttpContext!.User,
            adminUser.Id,
            adminUser.UserName ?? "admin"));

        Assert.Equal("El usuario seleccionado no tiene el rol de vendedor.", exception.Message);
    }

    private static VentaService CreateVentaService(AppDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        return new VentaService(
            context,
            new Mock<IMapper>().Object,
            NullLogger<VentaService>.Instance,
            new Mock<IConfiguracionPagoService>().Object,
            new Mock<IAlertaStockService>().Object,
            new Mock<IMovimientoStockService>().Object,
            new Mock<IFinancialCalculationService>().Object,
            new Mock<IVentaValidator>().Object,
            new VentaNumberGenerator(context),
            new Mock<IPrecioService>().Object,
            httpContextAccessor,
            new Mock<IValidacionVentaService>().Object,
            new Mock<ICajaService>().Object);
    }

    private static async Task<(string? UserId, string Nombre)> ResolverAsync(
        VentaService ventaService,
        VentaViewModel viewModel,
        ClaimsPrincipal? currentUser,
        string? currentUserId,
        string currentUserName)
    {
        var method = typeof(VentaService).GetMethod(
            "ResolverVendedorAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);

        var task = (Task<(string? UserId, string Nombre)>)method!
            .Invoke(ventaService, new object?[] { viewModel, currentUser, currentUserId, currentUserName })!;

        return await task;
    }

    private static void AsignarRol(AppDbContext context, string userId, string roleName)
    {
        var role = context.Roles.SingleOrDefault(r => r.Name == roleName);
        if (role == null)
        {
            role = new IdentityRole
            {
                Name = roleName,
                NormalizedName = roleName.ToUpperInvariant()
            };
            context.Roles.Add(role);
            context.SaveChanges();
        }

        if (!context.UserRoles.Any(ur => ur.UserId == userId && ur.RoleId == role.Id))
        {
            context.UserRoles.Add(new IdentityUserRole<string> { UserId = userId, RoleId = role.Id });
            context.SaveChanges();
        }
    }

    private static IHttpContextAccessor WithRole(
        IHttpContextAccessor accessor,
        string userName,
        string userId,
        string roleName)
    {
        accessor.HttpContext!.User = new ClaimsPrincipal(
            new ClaimsIdentity(
                new[]
                {
                    new Claim(ClaimTypes.Name, userName),
                    new Claim(ClaimTypes.NameIdentifier, userId),
                    new Claim(ClaimTypes.Role, roleName)
                },
                authenticationType: "TestAuth"));

        return accessor;
    }
}
