using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TheBuryProject.Data.Seeds;

namespace TheBuryProject.Data
{
    /// <summary>
    /// Inicializador de base de datos para crear roles, permisos y usuarios por defecto
    /// </summary>
    public static class DbInitializer
    {
        /// <summary>
        /// Inicializa roles, módulos, permisos y usuario administrador
        /// </summary>
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var services = scope.ServiceProvider;

            try
            {
                var context = services.GetRequiredService<AppDbContext>();
                var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
                var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
                var logger = services.GetRequiredService<ILogger<Program>>();

                logger.LogInformation("Iniciando inicialización de base de datos...");

                // Aplicar migraciones pendientes
                // Evitar intentar aplicar migraciones si las tablas ya existen pero falta la tabla __EFMigrationsHistory
                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();
                try
                {
                    using var cmd = connection.CreateCommand();
                    cmd.CommandText = "SELECT OBJECT_ID(N'dbo.AspNetRoles', N'U')";
                    var objId = await cmd.ExecuteScalarAsync();

                    var hasAspNetRoles = objId != null && objId != DBNull.Value;

                    var shouldMigrate = true;

                    if (hasAspNetRoles)
                    {
                        // Si existen las tablas de Identity, verificar si existe el historial de migraciones
                        try
                        {
                            cmd.CommandText = "SELECT COUNT(*) FROM [__EFMigrationsHistory]";
                            var countObj = await cmd.ExecuteScalarAsync();
                            var count = 0;
                            if (countObj != null && countObj != DBNull.Value)
                                count = Convert.ToInt32(countObj);

                            if (count == 0)
                            {
                                shouldMigrate = false;
                                logger.LogWarning("La base de datos contiene tablas de Identity pero no tiene entradas en __EFMigrationsHistory. Se omitirá ApplyMigrations para evitar conflictos.");
                            }
                        }
                        catch
                        {
                            // Si la consulta falla (por ejemplo, la tabla __EFMigrationsHistory no existe), evitar migrar
                            shouldMigrate = false;
                            logger.LogWarning("No se pudo leer __EFMigrationsHistory. Se omitirá ApplyMigrations para evitar conflictos con tablas existentes.");
                        }
                    }

                    if (shouldMigrate)
                    {
                        await context.Database.MigrateAsync();
                        logger.LogInformation("Migraciones aplicadas exitosamente");
                    }
                    else
                    {
                        logger.LogInformation("Se omitió la aplicación de migraciones porque ya existen tablas en la base de datos sin historial de migraciones.");
                    }
                }
                finally
                {
                    await connection.CloseAsync();
                }

                // Ejecutar seeder de roles, módulos y permisos
                await RolesPermisosSeeder.SeedAsync(context, roleManager);
                logger.LogInformation("Roles, módulos y permisos inicializados exitosamente");

                // Crear usuario administrador si no existe
                await CreateAdminUserAsync(userManager, logger);

                logger.LogInformation("Inicialización de base de datos completada");
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "Error durante la inicialización de la base de datos");
                throw;
            }
        }

        /// <summary>
        /// Crea el usuario administrador por defecto con rol SuperAdmin
        /// </summary>
        private static async Task CreateAdminUserAsync(UserManager<IdentityUser> userManager, ILogger logger)
        {
            const string adminEmail = "admin@thebury.com";
            const string adminPassword = "Admin123!";

            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);

                if (result.Succeeded)
                {
                    logger.LogInformation("Usuario administrador creado: {Email}", adminEmail);

                    // Asignar rol SuperAdmin
                    await userManager.AddToRoleAsync(adminUser, "SuperAdmin");
                    logger.LogInformation("Rol 'SuperAdmin' asignado al usuario {Email}", adminEmail);

                    logger.LogWarning("Credenciales de administrador por defecto:");
                    logger.LogWarning("  Email: {Email}", adminEmail);
                    logger.LogWarning("  Password: {Password}", adminPassword);
                    logger.LogWarning("  ⚠️ CAMBIA ESTA CONTRASEÑA EN PRODUCCIÓN");
                }
                else
                {
                    logger.LogError("Error al crear usuario administrador: {Errors}",
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                logger.LogInformation("Usuario administrador ya existe: {Email}", adminEmail);

                // Verificar que tenga el rol SuperAdmin
                if (!await userManager.IsInRoleAsync(adminUser, "SuperAdmin"))
                {
                    await userManager.AddToRoleAsync(adminUser, "SuperAdmin");
                    logger.LogInformation("Rol 'SuperAdmin' asignado al usuario existente {Email}", adminEmail);
                }
            }
        }

        /// <summary>
        /// Crea usuarios de prueba para cada rol (solo en desarrollo)
        /// </summary>
        public static async Task CreateTestUsersAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var services = scope.ServiceProvider;
            var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
            var logger = services.GetRequiredService<ILogger<Program>>();

            var testUsers = new[]
            {
                new { Email = "administrador@thebury.com", Password = "Admin123!", Role = "Administrador" },
                new { Email = "gerente@thebury.com", Password = "Gerente123!", Role = "Gerente" },
                new { Email = "vendedor@thebury.com", Password = "Vendedor123!", Role = "Vendedor" },
                new { Email = "cajero@thebury.com", Password = "Cajero123!", Role = "Cajero" },
                new { Email = "repositor@thebury.com", Password = "Repositor123!", Role = "Repositor" },
                new { Email = "tecnico@thebury.com", Password = "Tecnico123!", Role = "Tecnico" },
                new { Email = "contador@thebury.com", Password = "Contador123!", Role = "Contador" }
            };

            foreach (var testUser in testUsers)
            {
                var user = await userManager.FindByEmailAsync(testUser.Email);
                if (user == null)
                {
                    user = new IdentityUser
                    {
                        UserName = testUser.Email,
                        Email = testUser.Email,
                        EmailConfirmed = true
                    };

                    var result = await userManager.CreateAsync(user, testUser.Password);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, testUser.Role);
                        logger.LogInformation("Usuario de prueba creado: {Email} con rol {Role}",
                            testUser.Email, testUser.Role);
                    }
                }
            }

            logger.LogWarning("Usuarios de prueba creados - ⚠️ SOLO PARA DESARROLLO");
        }
    }
}