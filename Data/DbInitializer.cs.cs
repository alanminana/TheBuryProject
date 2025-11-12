using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace TheBuryProject.Data
{
    /// <summary>
    /// Inicializador de base de datos para crear roles y usuarios por defecto
    /// </summary>
    public static class DbInitializer
    {
        /// <summary>
        /// Inicializa roles y usuario administrador
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
                await context.Database.MigrateAsync();
                logger.LogInformation("Migraciones aplicadas exitosamente");

                // Crear roles si no existen
                await CreateRolesAsync(roleManager, logger);

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
        /// Crea los roles del sistema
        /// </summary>
        private static async Task CreateRolesAsync(RoleManager<IdentityRole> roleManager, ILogger logger)
        {
            string[] roles = { "Admin", "Gerente", "Vendedor", "Contador" };

            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                    if (result.Succeeded)
                    {
                        logger.LogInformation("Rol creado: {RoleName}", roleName);
                    }
                    else
                    {
                        logger.LogError("Error al crear rol {RoleName}: {Errors}",
                            roleName,
                            string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
                else
                {
                    logger.LogInformation("Rol ya existe: {RoleName}", roleName);
                }
            }
        }

        /// <summary>
        /// Crea el usuario administrador por defecto
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

                    // Asignar rol Admin
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                    logger.LogInformation("Rol 'Admin' asignado al usuario {Email}", adminEmail);

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

                // Verificar que tenga el rol Admin
                if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                    logger.LogInformation("Rol 'Admin' asignado al usuario existente {Email}", adminEmail);
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
                new { Email = "gerente@thebury.com", Password = "Gerente123!", Role = "Gerente" },
                new { Email = "vendedor@thebury.com", Password = "Vendedor123!", Role = "Vendedor" },
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