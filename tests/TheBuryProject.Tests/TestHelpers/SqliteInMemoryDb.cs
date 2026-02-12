using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TheBuryProject.Data;
using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;

namespace TheBuryProject.Tests.TestHelpers;

internal sealed class SqliteInMemoryDb : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public AppDbContext Context { get; }
    public IHttpContextAccessor HttpContextAccessor { get; }

    public SqliteInMemoryDb(string userName)
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        var testUser = new ApplicationUser
        {
            UserName = userName,
            Email = $"{userName}@test.local",
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[]
                    {
                        new Claim(ClaimTypes.Name, userName),
                        new Claim(ClaimTypes.NameIdentifier, testUser.Id)
                    },
                    authenticationType: "TestAuth"))
        };

        HttpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };

        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .EnableSensitiveDataLogging()
            .Options;

        Context = new AppDbContext(_options, HttpContextAccessor);
        Context.Database.EnsureCreated();

        Context.Users.Add(testUser);
        Context.SaveChanges();
    }

    public AppDbContext CreateNewContext()
    {
        return new AppDbContext(_options, HttpContextAccessor);
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }

    public async Task<AperturaCaja> CrearAperturaCajaActivaAsync(
        decimal montoInicial = 0m,
        string? usuario = null)
    {
        var caja = new Caja
        {
            Codigo = $"CAJA-{Guid.NewGuid():N}",
            Nombre = "Caja Test",
            Activa = true,
            Estado = EstadoCaja.Abierta
        };
        Context.Cajas.Add(caja);
        await Context.SaveChangesAsync();

        var apertura = new AperturaCaja
        {
            CajaId = caja.Id,
            FechaApertura = DateTime.UtcNow,
            MontoInicial = montoInicial,
            UsuarioApertura = usuario ?? (HttpContextAccessor.HttpContext?.User?.Identity?.Name ?? "tester"),
            Cerrada = false
        };
        Context.AperturasCaja.Add(apertura);
        await Context.SaveChangesAsync();

        return apertura;
    }
}
