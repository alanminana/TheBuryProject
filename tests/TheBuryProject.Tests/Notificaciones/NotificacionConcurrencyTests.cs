using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TheBuryProject.Hubs;
using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services;
using TheBuryProject.Tests.TestHelpers;
using Xunit;

namespace TheBuryProject.Tests.Notificaciones;

public class NotificacionConcurrencyTests
{
    [Fact]
    public async Task MarcarComoLeidaAsync_con_RowVersion_viejo_lanza_conflicto_concurrencia_y_no_marca_leida()
    {
        using var db = new SqliteInMemoryDb(userName: "tester");

        var notificacion = new Notificacion
        {
            UsuarioDestino = "tester",
            Tipo = TipoNotificacion.SistemaMantenimiento,
            Prioridad = PrioridadNotificacion.Media,
            Titulo = "Test",
            Mensaje = "Msg",
            FechaNotificacion = DateTime.UtcNow,
            Leida = false,
            RowVersion = new byte[] { 1, 1, 1, 1, 1, 1, 1, 1 }
        };

        db.Context.Notificaciones.Add(notificacion);
        await db.Context.SaveChangesAsync();

        var rowVersionViejo = notificacion.RowVersion;
        Assert.NotNull(rowVersionViejo);
        Assert.NotEmpty(rowVersionViejo);

        await using (var ctx2 = db.CreateNewContext())
        {
            var notifOtraSesion = await ctx2.Notificaciones.SingleAsync(n => n.Id == notificacion.Id);
            notifOtraSesion.Mensaje = "Cambio por otro proceso";
            notifOtraSesion.RowVersion = new byte[] { 9, 9, 9, 9, 9, 9, 9, 9 };
            await ctx2.SaveChangesAsync();
        }

        var service = new NotificacionService(
            db.Context,
            TestIdentity.CreateUserManager(),
            NullLogger<NotificacionService>.Instance,
            new NoopHubContext());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await service.MarcarComoLeidaAsync(notificacion.Id, "tester", rowVersionViejo));

        Assert.Contains("modificada", ex.Message, StringComparison.OrdinalIgnoreCase);

        await using var ctx3 = db.CreateNewContext();
        var notifDb = await ctx3.Notificaciones.AsNoTracking().SingleAsync(n => n.Id == notificacion.Id);
        Assert.False(notifDb.Leida);
        Assert.Null(notifDb.FechaLeida);
     }

    [Fact]
    public async Task EliminarNotificacionAsync_con_RowVersion_viejo_lanza_conflicto_concurrencia_y_no_elimina()
    {
        using var db = new SqliteInMemoryDb(userName: "tester");

        var notificacion = new Notificacion
        {
            UsuarioDestino = "tester",
            Tipo = TipoNotificacion.SistemaMantenimiento,
            Prioridad = PrioridadNotificacion.Media,
            Titulo = "Test",
            Mensaje = "Msg",
            FechaNotificacion = DateTime.UtcNow,
            Leida = false,
            RowVersion = new byte[] { 2, 2, 2, 2, 2, 2, 2, 2 }
        };

        db.Context.Notificaciones.Add(notificacion);
        await db.Context.SaveChangesAsync();

        var rowVersionViejo = notificacion.RowVersion;
        Assert.NotNull(rowVersionViejo);
        Assert.NotEmpty(rowVersionViejo);

        await using (var ctx2 = db.CreateNewContext())
        {
            var notifOtraSesion = await ctx2.Notificaciones.SingleAsync(n => n.Id == notificacion.Id);
            notifOtraSesion.Titulo = "Cambio por otro proceso";
            notifOtraSesion.RowVersion = new byte[] { 8, 8, 8, 8, 8, 8, 8, 8 };
            await ctx2.SaveChangesAsync();
        }

        var service = new NotificacionService(
            db.Context,
            TestIdentity.CreateUserManager(),
            NullLogger<NotificacionService>.Instance,
            new NoopHubContext());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await service.EliminarNotificacionAsync(notificacion.Id, "tester", rowVersionViejo));

        Assert.Contains("modificada", ex.Message, StringComparison.OrdinalIgnoreCase);

        await using var ctx3 = db.CreateNewContext();
        var notifDb = await ctx3.Notificaciones.AsNoTracking().SingleAsync(n => n.Id == notificacion.Id);
        Assert.False(notifDb.IsDeleted);
    }

    private sealed class NoopHubContext : IHubContext<NotificacionesHub>
    {
        public IHubClients Clients { get; } = new NoopHubClients();
        public IGroupManager Groups { get; } = new NoopGroupManager();

        private sealed class NoopHubClients : IHubClients
        {
            private static readonly IClientProxy Proxy = new NoopClientProxy();
            public IClientProxy All => Proxy;
            public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => Proxy;
            public IClientProxy Client(string connectionId) => Proxy;
            public IClientProxy Clients(IReadOnlyList<string> connectionIds) => Proxy;
            public IClientProxy Group(string groupName) => Proxy;
            public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => Proxy;
            public IClientProxy Groups(IReadOnlyList<string> groupNames) => Proxy;
            public IClientProxy User(string userId) => Proxy;
            public IClientProxy Users(IReadOnlyList<string> userIds) => Proxy;

            private sealed class NoopClientProxy : IClientProxy
            {
                public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default) =>
                    Task.CompletedTask;
            }
        }

        private sealed class NoopGroupManager : IGroupManager
        {
            public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default) =>
                Task.CompletedTask;

            public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default) =>
                Task.CompletedTask;
        }
    }

    private static class TestIdentity
    {
        public static UserManager<ApplicationUser> CreateUserManager()
        {
            var store = new NoopUserStore();

            return new UserManager<ApplicationUser>(
                store,
                Options.Create(new IdentityOptions()),
                new PasswordHasher<ApplicationUser>(),
                Array.Empty<IUserValidator<ApplicationUser>>(),
                Array.Empty<IPasswordValidator<ApplicationUser>>(),
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                new ServiceCollection().BuildServiceProvider(),
                NullLogger<UserManager<ApplicationUser>>.Instance);
        }

        private sealed class NoopUserStore : IUserStore<ApplicationUser>
        {
            public void Dispose() { }
            public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(user.Id);
            public Task<string?> GetUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(user.UserName);
            public Task SetUserNameAsync(ApplicationUser user, string? userName, CancellationToken cancellationToken)
            {
                user.UserName = userName;
                return Task.CompletedTask;
            }
            public Task<string?> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(user.NormalizedUserName);
            public Task SetNormalizedUserNameAsync(ApplicationUser user, string? normalizedName, CancellationToken cancellationToken)
            {
                user.NormalizedUserName = normalizedName;
                return Task.CompletedTask;
            }
            public Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(IdentityResult.Success);
            public Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(IdentityResult.Success);
            public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(IdentityResult.Success);
            public Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken) => Task.FromResult<ApplicationUser?>(null);
            public Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) => Task.FromResult<ApplicationUser?>(null);
        }
    }
}
