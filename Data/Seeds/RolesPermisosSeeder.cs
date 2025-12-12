using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TheBuryProject.Models.Constants;
using TheBuryProject.Models.Entities;

namespace TheBuryProject.Data.Seeds;

/// <summary>
/// Seeder para roles, módulos, acciones y permisos iniciales del sistema
/// </summary>
public static class RolesPermisosSeeder
{
    /// <summary>
    /// Ejecuta todos los seeds necesarios
    /// </summary>
    public static async Task SeedAsync(AppDbContext context, RoleManager<IdentityRole> roleManager)
    {
        // 1. Seed roles
        await SeedRolesAsync(roleManager);

        // 2. Seed módulos y acciones
        await SeedModulosYAccionesAsync(context);

        // 3. Seed permisos por rol
        await SeedPermisosAsync(context);
    }

    /// <summary>
    /// Crea los roles del sistema
    /// </summary>
    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        var roles = Models.Constants.Roles.GetAllRoles();

        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }
    }

    /// <summary>
    /// Crea módulos y acciones del sistema
    /// </summary>
    private static async Task SeedModulosYAccionesAsync(AppDbContext context)
    {
        // Definición de módulos con sus acciones
        var modulosData = new List<(string Nombre, string Clave, string Categoria, string Icono, int Orden, List<(string Nombre, string Clave, int Orden)> Acciones)>
        {
            // CATÁLOGO
            ("Productos", "productos", "Catálogo", "bi-box-seam", 1, new List<(string, string, int)>
            {
                ("Ver", "view", 1),
                ("Crear", "create", 2),
                ("Editar", "update", 3),
                ("Eliminar", "delete", 4),
                ("Exportar", "export", 5)
            }),
            ("Categorías", "categorias", "Catálogo", "bi-tags", 2, new List<(string, string, int)>
            {
                ("Ver", "view", 1),
                ("Crear", "create", 2),
                ("Editar", "update", 3),
                ("Eliminar", "delete", 4)
            }),
            ("Marcas", "marcas", "Catálogo", "bi-award", 3, new List<(string, string, int)>
            {
                ("Ver", "view", 1),
                ("Crear", "create", 2),
                ("Editar", "update", 3),
                ("Eliminar", "delete", 4)
            }),
            ("Precios", "precios", "Catálogo", "bi-currency-dollar", 4, new List<(string, string, int)>
            {
                ("Ver", "view", 1),
                ("Simular Cambio", "simulate", 2),
                ("Aprobar Cambio", "approve", 3),
                ("Aplicar Cambio", "apply", 4),
                ("Revertir Cambio", "revert", 5),
                ("Crear Lista", "create", 6),
                ("Editar Lista", "update", 7),
                ("Eliminar Lista", "delete", 8),
                ("Ver Historial", "history", 9)
            }),

            // CLIENTES
            ("Clientes", "clientes", "Clientes", "bi-people", 10, new List<(string, string, int)>
            {
                ("Ver", "view", 1),
                ("Crear", "create", 2),
                ("Editar", "update", 3),
                ("Eliminar", "delete", 4),
                ("Ver Documentos", "viewdocs", 5),
                ("Subir Documentos", "uploaddocs", 6),
                ("Exportar", "export", 7)
            }),
            ("Evaluación Crédito", "evaluacioncredito", "Clientes", "bi-clipboard-check", 11, new List<(string, string, int)>
            {
                ("Ver", "view", 1),
                ("Evaluar", "evaluate", 2),
                ("Aprobar", "approve", 3),
                ("Rechazar", "reject", 4)
            }),

            // VENTAS
            ("Ventas", "ventas", "Ventas", "bi-cart", 20, new List<(string, string, int)>
            {
                ("Ver", "view", 1),
                ("Crear", "create", 2),
                ("Editar", "update", 3),
                ("Eliminar", "delete", 4),
                ("Autorizar", "authorize", 5),
                ("Rechazar", "reject", 6),
                ("Facturar", "invoice", 7),
                ("Cancelar", "cancel", 8),
                ("Exportar", "export", 9)
            }),
            ("Cotizaciones", "cotizaciones", "Ventas", "bi-file-text", 21, new List<(string, string, int)>
            {
                ("Ver", "view", 1),
                ("Crear", "create", 2),
                ("Editar", "update", 3),
                ("Convertir a Venta", "convert", 4),
                ("Anular", "cancel", 5)
            }),
            ("Créditos", "creditos", "Ventas", "bi-credit-card-2-front", 22, new List<(string, string, int)>
            {
                ("Ver", "view", 1),
                ("Crear", "create", 2),
                ("Simular", "simulate", 3),
                ("Aprobar", "approve", 4),
                ("Ver Cuotas", "viewinstallments", 5),
                ("Reprogramar", "reschedule", 6)
            }),
            ("Cobranzas", "cobranzas", "Ventas", "bi-cash-stack", 23, new List<(string, string, int)>
            {
                ("Ver", "view", 1),
                ("Pagar Cuota", "payinstallment", 2),
                ("Ver Moras", "viewarrears", 3),
                ("Aplicar Punitorio", "applyfine", 4),
                ("Ver Alertas", "viewalerts", 5)
            }),

            // COMPRAS
            ("Proveedores", "proveedores", "Compras", "bi-building", 30, new List<(string, string, int)>
            {
                ("Ver", "view", 1),
                ("Crear", "create", 2),
                ("Editar", "update", 3),
                ("Eliminar", "delete", 4)
            }),
            ("Órdenes de Compra", "ordenescompra", "Compras", "bi-clipboard-data", 31, new List<(string, string, int)>
            {
                ("Ver", "view", 1),
                ("Crear", "create", 2),
                ("Editar", "update", 3),
                ("Recepcionar", "receive", 4),
                ("Cancelar", "cancel", 5)
            }),
            ("Cheques", "cheques", "Compras", "bi-file-earmark-text", 32, new List<(string, string, int)>
            {
                ("Ver", "view", 1),
                ("Crear", "create", 2),
                ("Editar", "update", 3),
                ("Depositar", "deposit", 4),
                ("Anular", "cancel", 5)
            }),

            // STOCK
            ("Stock", "stock", "Stock", "bi-boxes", 40, new List<(string, string, int)>
            {
                ("Ver", "view", 1),
                ("Ver Kardex", "viewkardex", 2),
                ("Ajustar", "adjust", 3),
                ("Transferir", "transfer", 4),
                ("Ver Alertas", "viewalerts", 5)
            }),
            ("Movimientos", "movimientos", "Stock", "bi-arrow-left-right", 41, new List<(string, string, int)>
            {
                ("Ver", "view", 1),
                ("Registrar", "create", 2)
            }),

            // DEVOLUCIONES
            ("Devoluciones", "devoluciones", "Devoluciones", "bi-arrow-return-left", 50, new List<(string, string, int)>
            {
                ("Ver", "view", 1),
                ("Crear", "create", 2),
                ("Aprobar", "approve", 3),
                ("Rechazar", "reject", 4),
                ("Completar", "complete", 5)
            }),
            ("Garantías", "garantias", "Devoluciones", "bi-shield-check", 51, new List<(string, string, int)>
            {
                ("Ver", "view", 1),
                ("Crear", "create", 2),
                ("Actualizar", "update", 3)
            }),
            ("RMAs", "rmas", "Devoluciones", "bi-truck", 52, new List<(string, string, int)>
            {
                ("Ver", "view", 1),
                ("Crear", "create", 2),
                ("Actualizar Estado", "updatestatus", 3)
            }),
            ("Notas de Crédito", "notascredito", "Devoluciones", "bi-file-earmark-check", 53, new List<(string, string, int)>
            {
                ("Ver", "view", 1),
                ("Aplicar", "apply", 2),
                ("Cancelar", "cancel", 3)
            }),

            // CAJA
            ("Caja", "caja", "Caja", "bi-cash-coin", 60, new List<(string, string, int)>
            {
                ("Ver", "view", 1),
                ("Abrir", "open", 2),
                ("Cerrar", "close", 3),
                ("Movimientos", "movements", 4),
                ("Ver Historial", "history", 5)
            }),

            // AUTORIZACIONES
            ("Autorizaciones", AutorizacionesConstants.Modulo, "Autorizaciones", "bi-check2-circle", 70, new List<(string, string, int)>
            {
                ("Ver", AutorizacionesConstants.Acciones.Ver, 1),
                ("Aprobar", AutorizacionesConstants.Acciones.Aprobar, 2),
                ("Rechazar", AutorizacionesConstants.Acciones.Rechazar, 3),
                ("Gestionar Umbrales", AutorizacionesConstants.Acciones.GestionarUmbrales, 4)
            }),

            // REPORTES
            ("Reportes", "reportes", "Reportes", "bi-graph-up", 80, new List<(string, string, int)>
            {
                ("Ver", "view", 1),
                ("Ventas", "sales", 2),
                ("Márgenes", "margins", 3),
                ("Morosidad", "arrears", 4),
                ("Stock", "stock", 5),
                ("Exportar", "export", 6)
            }),
            ("Dashboard", "dashboard", "Reportes", "bi-speedometer2", 81, new List<(string, string, int)>
            {
                ("Ver", "view", 1)
            }),

            // CONFIGURACIÓN
            ("Configuración", "configuracion", "Configuración", "bi-gear", 90, new List<(string, string, int)>
            {
                ("Ver", "view", 1),
                ("Editar", "update", 2)
            }),
            ("Usuarios", "usuarios", "Configuración", "bi-person", 91, new List<(string, string, int)>
            {
                ("Ver", "view", 1),
                ("Crear", "create", 2),
                ("Editar", "update", 3),
                ("Eliminar", "delete", 4),
                ("Asignar Roles", "assignroles", 5)
            }),
            ("Roles", "roles", "Configuración", "bi-shield", 92, new List<(string, string, int)>
            {
                ("Ver", "view", 1),
                ("Crear", "create", 2),
                ("Editar", "update", 3),
                ("Eliminar", "delete", 4),
                ("Asignar Permisos", "assignpermissions", 5)
            })
        };

        foreach (var (nombre, clave, categoria, icono, orden, acciones) in modulosData)
        {
            // Buscar o crear módulo
            var modulo = await context.ModulosSistema
                .FirstOrDefaultAsync(m => m.Clave == clave);

            if (modulo == null)
            {
                modulo = new ModuloSistema
                {
                    Nombre = nombre,
                    Clave = clave,
                    Categoria = categoria,
                    Icono = icono,
                    Orden = orden,
                    Activo = true
                };
                context.ModulosSistema.Add(modulo);
                await context.SaveChangesAsync();
            }

            // Crear acciones del módulo
            foreach (var (nombreAccion, claveAccion, ordenAccion) in acciones)
            {
                var accionExistente = await context.AccionesModulo
                    .FirstOrDefaultAsync(a => a.ModuloId == modulo.Id && a.Clave == claveAccion);

                if (accionExistente == null)
                {
                    var accion = new AccionModulo
                    {
                        ModuloId = modulo.Id,
                        Nombre = nombreAccion,
                        Clave = claveAccion,
                        Orden = ordenAccion,
                        Activa = true
                    };
                    context.AccionesModulo.Add(accion);
                }
            }
        }

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Asigna permisos a cada rol
    /// </summary>
    private static async Task SeedPermisosAsync(AppDbContext context)
    {
        var roles = await context.Roles.ToListAsync();
        var modulos = await context.ModulosSistema.Include(m => m.Acciones).ToListAsync();

        // ============================================
        // SUPERADMIN - TODOS LOS PERMISOS
        // ============================================
        var superAdminRole = roles.FirstOrDefault(r => r.Name == Models.Constants.Roles.SuperAdmin);
        if (superAdminRole != null)
        {
            await AsignarTodosLosPermisosAsync(context, superAdminRole.Id, modulos);
        }

        // ============================================
        // ADMINISTRADOR - CASI TODOS (sin delete críticos)
        // ============================================
        var adminRole = roles.FirstOrDefault(r => r.Name == Models.Constants.Roles.Administrador);
        if (adminRole != null)
        {
            await AsignarTodosLosPermisosAsync(context, adminRole.Id, modulos, exceptoAcciones: new[] { "usuarios.delete", "roles.delete", "configuracion.update" });
        }

        // ============================================
        // GERENTE - Ventas, Compras, Reportes, Autorizaciones
        // ============================================
        var gerenteRole = roles.FirstOrDefault(r => r.Name == Models.Constants.Roles.Gerente);
        if (gerenteRole != null)
        {
            var modulosGerente = new[] { "ventas", "cotizaciones", "creditos", "cobranzas", "clientes", "evaluacioncredito", "proveedores", "ordenescompra", "stock", "movimientos", "devoluciones", "garantias", "rmas", "notascredito", AutorizacionesConstants.Modulo, "reportes", "dashboard" };
            await AsignarPermisosModulosAsync(context, gerenteRole.Id, modulos, modulosGerente);
        }

        // ============================================
        // VENDEDOR - Ventas y Clientes
        // ============================================
        var vendedorRole = roles.FirstOrDefault(r => r.Name == Models.Constants.Roles.Vendedor);
        if (vendedorRole != null)
        {
            await AsignarPermisosEspecificosAsync(context, vendedorRole.Id, modulos, new Dictionary<string, string[]>
            {
                { "ventas", new[] { "view", "create" } },
                { "cotizaciones", new[] { "view", "create", "update", "convert" } },
                { "clientes", new[] { "view", "create", "update" } },
                { "productos", new[] { "view" } },
                { "categorias", new[] { "view" } },
                { "marcas", new[] { "view" } },
                { "stock", new[] { "view" } },
                { "dashboard", new[] { "view" } }
            });
        }

        // ============================================
        // CAJERO - Cobros y Caja
        // ============================================
        var cajeroRole = roles.FirstOrDefault(r => r.Name == Models.Constants.Roles.Cajero);
        if (cajeroRole != null)
        {
            await AsignarPermisosEspecificosAsync(context, cajeroRole.Id, modulos, new Dictionary<string, string[]>
            {
                { "ventas", new[] { "view" } },
                { "cobranzas", new[] { "view", "payinstallment", "viewarrears", "viewalerts" } },
                { "caja", new[] { "view", "open", "close", "movements", "history" } },
                { "clientes", new[] { "view" } },
                { "dashboard", new[] { "view" } }
            });
        }

        // ============================================
        // REPOSITOR - Stock
        // ============================================
        var repositorRole = roles.FirstOrDefault(r => r.Name == Models.Constants.Roles.Repositor);
        if (repositorRole != null)
        {
            await AsignarPermisosEspecificosAsync(context, repositorRole.Id, modulos, new Dictionary<string, string[]>
            {
                { "stock", new[] { "view", "viewkardex", "adjust", "transfer", "viewalerts" } },
                { "movimientos", new[] { "view", "create" } },
                { "productos", new[] { "view" } },
                { "devoluciones", new[] { "view" } },
                { "dashboard", new[] { "view" } }
            });
        }

        // ============================================
        // TECNICO - Devoluciones, Garantías, RMAs
        // ============================================
        var tecnicoRole = roles.FirstOrDefault(r => r.Name == Models.Constants.Roles.Tecnico);
        if (tecnicoRole != null)
        {
            await AsignarPermisosEspecificosAsync(context, tecnicoRole.Id, modulos, new Dictionary<string, string[]>
            {
                { "devoluciones", new[] { "view", "create", "approve", "reject", "complete" } },
                { "garantias", new[] { "view", "create", "update" } },
                { "rmas", new[] { "view", "create", "updatestatus" } },
                { "notascredito", new[] { "view" } },
                { "productos", new[] { "view" } },
                { "stock", new[] { "view", "viewkardex" } },
                { "dashboard", new[] { "view" } }
            });
        }

        // ============================================
        // CONTADOR - Solo lectura y reportes
        // ============================================
        var contadorRole = roles.FirstOrDefault(r => r.Name == Models.Constants.Roles.Contador);
        if (contadorRole != null)
        {
            await AsignarPermisosEspecificosAsync(context, contadorRole.Id, modulos, new Dictionary<string, string[]>
            {
                { "ventas", new[] { "view" } },
                { "creditos", new[] { "view", "viewinstallments" } },
                { "cobranzas", new[] { "view", "viewarrears", "viewalerts" } },
                { "clientes", new[] { "view" } },
                { "proveedores", new[] { "view" } },
                { "ordenescompra", new[] { "view" } },
                { "reportes", new[] { "view", "sales", "margins", "arrears", "stock", "export" } },
                { "dashboard", new[] { "view" } }
            });
        }

        await context.SaveChangesAsync();
    }

    private static async Task AsignarTodosLosPermisosAsync(AppDbContext context, string roleId, List<ModuloSistema> modulos, string[]? exceptoAcciones = null)
    {
        foreach (var modulo in modulos)
        {
            foreach (var accion in modulo.Acciones)
            {
                var claimValue = $"{modulo.Clave}.{accion.Clave}";

                if (exceptoAcciones != null && exceptoAcciones.Contains(claimValue))
                    continue;

                var existe = await context.RolPermisos
                    .AnyAsync(rp => rp.RoleId == roleId && rp.ModuloId == modulo.Id && rp.AccionId == accion.Id);

                if (!existe)
                {
                    context.RolPermisos.Add(new RolPermiso
                    {
                        RoleId = roleId,
                        ModuloId = modulo.Id,
                        AccionId = accion.Id,
                        ClaimValue = claimValue
                    });
                }
            }
        }
    }

    private static async Task AsignarPermisosModulosAsync(AppDbContext context, string roleId, List<ModuloSistema> modulos, string[] modulosClaves)
    {
        foreach (var moduloClave in modulosClaves)
        {
            var modulo = modulos.FirstOrDefault(m => m.Clave == moduloClave);
            if (modulo == null) continue;

            foreach (var accion in modulo.Acciones)
            {
                var claimValue = $"{modulo.Clave}.{accion.Clave}";

                var existe = await context.RolPermisos
                    .AnyAsync(rp => rp.RoleId == roleId && rp.ModuloId == modulo.Id && rp.AccionId == accion.Id);

                if (!existe)
                {
                    context.RolPermisos.Add(new RolPermiso
                    {
                        RoleId = roleId,
                        ModuloId = modulo.Id,
                        AccionId = accion.Id,
                        ClaimValue = claimValue
                    });
                }
            }
        }
    }

    private static async Task AsignarPermisosEspecificosAsync(AppDbContext context, string roleId, List<ModuloSistema> modulos, Dictionary<string, string[]> permisosEspecificos)
    {
        foreach (var (moduloClave, accionesClaves) in permisosEspecificos)
        {
            var modulo = modulos.FirstOrDefault(m => m.Clave == moduloClave);
            if (modulo == null) continue;

            foreach (var accionClave in accionesClaves)
            {
                var accion = modulo.Acciones.FirstOrDefault(a => a.Clave == accionClave);
                if (accion == null) continue;

                var claimValue = $"{modulo.Clave}.{accion.Clave}";

                var existe = await context.RolPermisos
                    .AnyAsync(rp => rp.RoleId == roleId && rp.ModuloId == modulo.Id && rp.AccionId == accion.Id);

                if (!existe)
                {
                    context.RolPermisos.Add(new RolPermiso
                    {
                        RoleId = roleId,
                        ModuloId = modulo.Id,
                        AccionId = accion.Id,
                        ClaimValue = claimValue
                    });
                }
            }
        }
    }
}