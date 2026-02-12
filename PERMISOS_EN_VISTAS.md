# Sistema de Validación de Permisos en Vistas (UI)

## 📋 Resumen

Se ha implementado un sistema completo para **ocultar elementos de la UI según los permisos del usuario**. Esto complementa la seguridad del backend (que ya valida permisos en cada endpoint con `[PermisoRequerido]`).

**⚠️ IMPORTANTE**: Este sistema **NO sustituye** la validación de permisos en el backend. Es solo para mejorar la experiencia del usuario ocultando botones/acciones a las que no tiene acceso.

---

## 🛠️ Implementación

### 1. Helper Creado

Se creó `Helpers/PermissionHelper.cs` con métodos de extensión para `ClaimsPrincipal`:

```csharp
// Verificar un permiso específico
User.TienePermiso("ventas", "create")

// Verificar cualquiera de varios permisos
User.TieneCualquierPermiso(
    ("ventas", "create"), 
    ("ventas", "update")
)

// Verificar todos los permisos
User.TieneTodosLosPermisos(
    ("ventas", "create"), 
    ("ventas", "authorize")
)

// Verificar si es SuperAdmin
User.EsSuperAdmin()

// Obtener todos los permisos
User.ObtenerPermisos()
```

### 2. Registro en ViewImports

El helper se agregó a `Views/_ViewImports.cshtml`:
```csharp
@using TheBuryProject.Helpers
```

Ahora está disponible en **todas las vistas** del proyecto.

---

## 📝 Cómo Usar en las Vistas

### Patrón Básico

```razor
@* Ocultar botón si no tiene permiso *@
@if (User.TienePermiso("modulo", "accion"))
{
    <a asp-action="Create" class="btn btn-primary">
        <i class="bi bi-plus-circle"></i> Crear
    </a>
}
```

### Ejemplo Real: Venta/Index.cshtml

```razor
<div class="btn-group">
    @* Bot\u00f3n Configurar Pagos - requiere configuraciones.view *@
    @if (User.TienePermiso("configuraciones", "view"))
    {
        <button type="button" class="btn btn-outline-warning" id="btnConfigPago">
            <i class="bi bi-gear"></i> Configurar recargos / descuentos
        </button>
    }

    @* Bot\u00f3n Nueva Venta - requiere ventas.create + caja abierta *@
    @if (User.TienePermiso("ventas", "create"))
    {
        @if ((ViewBag.PuedeCrearVenta as bool?) == true)
        {
            <a asp-action="Create" class="btn btn-primary">
                <i class="bi bi-plus-circle"></i> Nueva Venta
            </a>
        }
        else
        {
            <button type="button" class="btn btn-primary" disabled 
                    title="Debe abrir una caja para registrar una venta.">
                <i class="bi bi-plus-circle"></i> Nueva Venta
            </button>
        }
    }
</div>
```

### Combinando con Lógica de Negocio

**Importante**: Los permisos se combinan con la lógica de negocio. Ambos deben cumplirse:

```razor
@* El usuario necesita TANTO el permiso como que la venta esté en estado editable *@
@if (venta.PuedeEditar && User.TienePermiso("ventas", "update"))
{
    <a asp-action="Edit" asp-route-id="@venta.Id" class="btn btn-outline-warning">
        <i class="bi bi-pencil"></i> Editar
    </a>
}

@* Autorizar requiere permiso Y que la venta esté pendiente de autorización *@
@if (venta.PuedeAutorizar && User.TienePermiso("ventas", "authorize"))
{
    <a asp-action="Autorizar" asp-route-id="@venta.Id" class="btn btn-outline-success">
        <i class="bi bi-check-lg"></i> Autorizar
    </a>
}
```

---

## 🗺️ Mapa de Módulos y Acciones

| Módulo | Acciones Comunes | Ejemplo de Uso |
|--------|------------------|----------------|
| **ventas** | view, create, update, authorize, reject, invoice | `User.TienePermiso("ventas", "create")` |
| **clientes** | view, create, update, delete, viewdocs | `User.TienePermiso("clientes", "create")` |
| **creditos** | view, create, update, cancel | `User.TienePermiso("creditos", "view")` |
| **productos** | view, create, update, delete | `User.TienePermiso("productos", "create")` |
| **caja** | view, open, close | `User.TienePermiso("caja", "open")` |
| **configuraciones** | view, managemora | `User.TienePermiso("configuraciones", "view")` |
| **reportes** | view | `User.TienePermiso("reportes", "view")` |
| **usuarios** | view, create, update, delete | `User.TienePermiso("usuarios", "create")` |
| **roles** | view, create, update, delete | `User.TienePermiso("roles", "create")` |
| **proveedores** | view, create, update, delete | `User.TienePermiso("proveedores", "create")` |
| **ordenescompra** | view, create, update, receive, cancel | `User.TienePermiso("ordenescompra", "create")` |
| **movimientos** | view | `User.TienePermiso("movimientos", "view")` |
| **devoluciones** | view, create, update | `User.TienePermiso("devoluciones", "create")` |
| **cotizaciones** | view | `User.TienePermiso("cotizaciones", "view")` |

---

## ✅ Vistas Ya Actualizadas

Las siguientes vistas ya implementan validación de permisos:

1. **Venta/Index.cshtml**
   - Botón "Configurar recargos/descuentos" → `configuraciones.view`
   - Botón "Cotizar" → `ventas.create`
   - Botón "Nueva Venta" → `ventas.create`
   - Botones tabla: Ver detalles → `ventas.view`
   - Botones tabla: Autorizar → `ventas.authorize`
   - Botones tabla: Rechazar → `ventas.reject`
   - Botones tabla: Editar → `ventas.update`

2. **Dashboard/Index.cshtml**
   - Nueva Venta → `ventas.create`
   - Clientes → `clientes.view`
   - Créditos → `creditos.view`
   - Catálogo → `cotizaciones.view`
   - Proveedores → `proveedores.view`
   - Órdenes Compra → `ordenescompra.view`

3. **Cliente/Index.cshtml**
   - Nuevo Cliente → `clientes.create`

4. **Producto/Index.cshtml**
   - Movimientos de Stock → `movimientos.view`
   - Nuevo Producto → `productos.create`

5. **Proveedor/Index.cshtml**
   - Ver Órdenes de Compra → `ordenescompra.view`
   - Nuevo Proveedor → `proveedores.create`

---

## 🔄 Cómo Aplicar en Otras Vistas

### Paso 1: Identificar el botón/acción

Buscar en la vista elementos como:
- `<a asp-action="Create">`
- `<a asp-action="Edit">`
- `<button type="button">`
- Links de acción en tablas

### Paso 2: Determinar el permiso requerido

Revisar el controlador correspondiente para ver qué `[PermisoRequerido]` usa:

```csharp
// En el controlador:
[PermisoRequerido(Modulo = "productos", Accion = "create")]
public async Task<IActionResult> Create()

// En la vista, usar el mismo módulo y acción:
@if (User.TienePermiso("productos", "create"))
{
    <a asp-action="Create">Crear Producto</a>
}
```

### Paso 3: Envolver en `@if`

```razor
@* ANTES *@
<a asp-action="Create" class="btn btn-primary">
    <i class="bi bi-plus-circle"></i> Crear
</a>

@* DESPUÉS *@
@if (User.TienePermiso("modulo", "create"))
{
    <a asp-action="Create" class="btn btn-primary">
        <i class="bi bi-plus-circle"></i> Crear
    </a>
}
```

### Paso 4: Agregar comentario descriptivo

```razor
@* Crear Producto - requiere productos.create *@
@if (User.TienePermiso("productos", "create"))
{
    <a asp-action="Create" class="btn btn-primary">
        <i class="bi bi-plus-circle"></i> Crear Producto
    </a>
}
```

---

## 🎯 Casos Especiales

### Múltiples Permisos (OR)

```razor
@* Mostrar si tiene cualquiera de los permisos *@
@if (User.TieneCualquierPermiso(
    ("ventas", "authorize"), 
    ("ventas", "reject")))
{
    <div class="acciones-autorizacion">
        @if (User.TienePermiso("ventas", "authorize"))
        {
            <a asp-action="Autorizar">Autorizar</a>
        }
        @if (User.TienePermiso("ventas", "reject"))
        {
            <a asp-action="Rechazar">Rechazar</a>
        }
    </div>
}
```

### Múltiples Permisos (AND)

```razor
@* Mostrar solo si tiene TODOS los permisos *@
@if (User.TieneTodosLosPermisos(
    ("ventas", "create"), 
    ("creditos", "create")))
{
    <div class="venta-credito-especial">
        <button>Crear Venta con Crédito</button>
    </div>
}
```

### SuperAdmin Bypass

```razor
@* SuperAdmin ve todo, otros usuarios necesitan permiso *@
@if (User.EsSuperAdmin() || User.TienePermiso("configuraciones", "advanced"))
{
    <a asp-action="ConfiguracionAvanzada" class="btn btn-danger">
        <i class="bi bi-sliders"></i> Configuración Avanzada
    </a>
}
```

---

## 🔒 Arquitectura de Seguridad: Doble Capa

### Capa 1: Backend (OBLIGATORIO) ✅
```csharp
[Authorize]
[PermisoRequerido(Modulo = "ventas", Accion = "create")]
public async Task<IActionResult> Create()
{
    // Validación real de seguridad
}
```

### Capa 2: Frontend (UX) ✨
```razor
@if (User.TienePermiso("ventas", "create"))
{
    <a asp-action="Create">Nueva Venta</a>
}
```

**⚠️ La validación del backend SIEMPRE se ejecuta**, incluso si el botón está oculto. Un usuario malintencionado podría intentar acceder directamente a la URL, pero el `[PermisoRequerido]` en el controlador lo bloqueará.

**La validación en la vista solo mejora la UX** ocultando opciones inaccesibles.

---

## 🧪 Cómo Probar

### 1. Crear un rol con permisos limitados

```
1. Ir a /Roles/Create
2. Crear rol "Vendedor Junior"
3. Asignar solo: ventas.view, clientes.view
4. NO asignar: ventas.create, ventas.authorize
```

### 2. Asignar rol a usuario de prueba

```
1. Ir a /Usuarios
2. Asignar "Vendedor Junior" a un usuario
3. Hacer logout del usuario actual
4. Login con el usuario de prueba
```

### 3. Verificar UI

```
Al navegar a /Venta:
✅ El botón "Nueva Venta" NO debe aparecer (falta ventas.create)
✅ El botón "Cotizar" NO debe aparecer (falta ventas.create)
✅ En la tabla, los botones de "Autorizar" NO deben aparecer (falta ventas.authorize)
✅ Solo debe ver el botón "Ver Detalles" (tiene ventas.view)
```

### 4. Verificar Backend

```
Intentar acceder directamente a /Venta/Create:
❌ Debe redirigir o mostrar "Access Denied" (seguridad del backend)
```

---

## 📊 Beneficios

1. **Mejor UX**: Usuario solo ve opciones que puede usar
2. **Menos frustraciones**: No hay botones que den "Access Denied"
3. **UI más limpia**: Menos elementos innecesarios
4. **Consistencia**: Permisos del backend reflejados en la UI
5. **Seguridad real**: Backend siempre valida, UI solo oculta

---

## 🚀 Próximos Pasos Sugeridos

1. **Aplicar en vistas restantes**: Hay ~20 vistas más con botones de acción
2. **Menú lateral**: Ocultar items del menú según permisos
3. **Tabs/Pestañas**: Ocultar secciones completas sin permiso
4. **Reportes**: Mostrar solo reportes permitidos
5. **Configuraciones**: Ocultar configuraciones avanzadas

---

## 📖 Referencia Rápida

```razor
@* Patrón básico *@
@if (User.TienePermiso("modulo", "accion"))
{
    <boton-o-link />
}

@* Con lógica de negocio *@
@if (condicionNegocio && User.TienePermiso("modulo", "accion"))
{
    <boton-o-link />
}

@* Múltiples permisos (OR) *@
@if (User.TieneCualquierPermiso(("mod1", "acc1"), ("mod2", "acc2")))
{
    <boton-o-link />
}

@* Múltiples permisos (AND) *@
@if (User.TieneTodosLosPermisos(("mod1", "acc1"), ("mod2", "acc2")))
{
    <boton-o-link />
}

@* SuperAdmin bypass *@
@if (User.EsSuperAdmin() || User.TienePermiso("modulo", "accion"))
{
    <boton-o-link />
}
```

---

**✅ Sistema implementado y funcionando correctamente**
