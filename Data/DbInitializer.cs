using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TheBuryProject.Data.Seeds;
using TheBuryProject.Models.Constants;

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
                var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
                var logger = services.GetRequiredService<ILogger<Program>>();

                logger.LogInformation("Iniciando inicialización de base de datos...");

                // Aplicar migraciones pendientes
                await context.Database.MigrateAsync();
                logger.LogInformation("Migraciones aplicadas exitosamente");

                // Ejecutar seeder de roles, módulos y permisos
                await RolesPermisosSeeder.SeedAsync(context, roleManager);
                logger.LogInformation("Roles, módulos y permisos inicializados exitosamente");

                // Crear usuario administrador si no existe (lee credenciales desde configuración/secret)
                await CreateAdminUserAsync(services, logger);

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
            /// Crea el usuario administrador por defecto con rol SuperAdmin.
        /// Lee credenciales desde IConfiguration: "Admin:Email" y "Admin:Password".
        /// En desarrollo, si no están definidas, se puede usar la contraseña por defecto (sólo dev).
        /// Nunca registra la contraseña en logs.
        /// </summary>
        private static async Task CreateAdminUserAsync(IServiceProvider services, ILogger logger)
        {
            var configuration = services.GetRequiredService<IConfiguration>();
            var env = services.GetService<IWebHostEnvironment>();
            var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

            var adminEmail = configuration["Admin:Email"] ?? "admin@thebury.com";
            var adminPassword = configuration["Admin:Password"]; // debe venir de user-secrets / ENV

            // Fallback seguro: permitir el valor por defecto sólo en entorno Development
            if (string.IsNullOrWhiteSpace(adminPassword))
            {
                if (env != null && env.IsDevelopment())
                {
                    adminPassword = "Admin123!"; // único fallback para desarrollo
                    logger.LogWarning("No se encontró Admin:Password en configuración. Usando contraseña por defecto SOLO en Development.");
                }
                else
                {
                    logger.LogWarning("No se creó el usuario administrador automático: 'Admin:Password' no está configurado en la configuración/variables de entorno.");
                    logger.LogWarning("Configure 'Admin:Password' mediante user-secrets o variable de entorno antes de ejecutar en producción.");
                    return;
                }
            }

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
                    await userManager.AddToRoleAsync(adminUser, Roles.SuperAdmin);
                    logger.LogInformation("Rol 'SuperAdmin' asignado al usuario {Email}", adminEmail);

                    logger.LogWarning("⚠️ Credenciales provisionales creadas. Cambiar la contraseña inmediatamente si es necesario.");
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
                if (!await userManager.IsInRoleAsync(adminUser, Roles.SuperAdmin))
                {
                    await userManager.AddToRoleAsync(adminUser, Roles.SuperAdmin);
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
                new { Email = "administrador@thebury.com", Password = "Admin123!", Role = Roles.Administrador },
                new { Email = "gerente@thebury.com", Password = "Gerente123!", Role = Roles.Gerente },
                new { Email = "vendedor@thebury.com", Password = "Vendedor123!", Role = Roles.Vendedor },
                new { Email = "cajero@thebury.com", Password = "Cajero123!", Role = Roles.Cajero },
                new { Email = "repositor@thebury.com", Password = "Repositor123!", Role = Roles.Repositor },
                new { Email = "tecnico@thebury.com", Password = "Tecnico123!", Role = Roles.Tecnico },
                new { Email = "contador@thebury.com", Password = "Contador123!", Role = Roles.Contador }
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