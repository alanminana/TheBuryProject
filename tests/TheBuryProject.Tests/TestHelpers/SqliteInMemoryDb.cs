using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TheBuryProject.Data;

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

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.Name, userName) },
                    authenticationType: "TestAuth"))
        };

        HttpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };

        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .EnableSensitiveDataLogging()
            .Options;

        Context = new AppDbContext(_options, HttpContextAccessor);
        Context.Database.EnsureCreated();
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
}
