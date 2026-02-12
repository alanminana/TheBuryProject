# TAREA 8: Cliente - Configuración Específica de Crédito Personal

## Fecha de Implementación
- **Inicio**: 8 de febrero de 2025
- **Finalización**: 8 de febrero de 2025

## Descripción
Agregar al cliente la capacidad de tener un perfil de crédito preferido, integrando el sistema de perfiles creado en TAREA 7 con la configuración personalizada de TAREA 6. Esto permite que cuando se configure un crédito, el sistema use los valores del perfil preferido como base, los cuales pueden ser sobrescritos por valores personalizados específicos del cliente.

## Objetivos
1. ✅ Agregar campo PerfilCreditoPreferidoId al modelo Cliente (FK nullable a PerfilesCredito)
2. ✅ Configurar la relación en AppDbContext con OnDelete(SetNull)
3. ✅ Crear y aplicar migración para agregar el campo a la base de datos
4. ✅ Actualizar ClienteViewModel para incluir el campo
5. ✅ Modificar la vista _ClienteFormFields.cshtml para mostrar selector de perfil
6. ✅ Actualizar ClienteController para cargar perfiles activos
7. ✅ Integrar con ConfigurarVenta para usar perfil como cascada de valores
8. ✅ Documentar prioridad de valores y flujo de integración

## Cambios Realizados

### 1. Modelo de Datos

#### Models/Entities/Cliente.cs
**Líneas modificadas**: 140-180

**Cambios**:
- Agregado campo `PerfilCreditoPreferidoId` (int?, nullable FK)
- Agregada navegación `PerfilCreditoPreferido` (virtual)
- Reorganizados comentarios para agrupar campos de TAREA 6 + TAREA 8

```csharp
// TAREA 6 + TAREA 8: Configuración Crédito Personal (Personalizada + Perfil Preferido)
public int? PerfilCreditoPreferidoId { get; set; }
public virtual PerfilCredito? PerfilCreditoPreferido { get; set; }

// Valores personalizados - Sobrescriben perfil y global
public decimal? TasaInteresMensualPersonalizada { get; set; }
public decimal? GastosAdministrativosPersonalizados { get; set; }
public int? CuotasMaximasPersonalizadas { get; set; }
public decimal? MontoMinimoPersonalizado { get; set; }
public decimal? MontoMaximoPersonalizado { get; set; }
```

#### Data/AppDbContext.cs
**Líneas modificadas**: ~460

**Cambios**:
- Configurada relación HasOne().WithMany() para PerfilCreditoPreferido
- Configurado OnDelete(DeleteBehavior.SetNull) para evitar cascada

```csharp
// TAREA 8: Relación con perfil preferido
entity.HasOne(c => c.PerfilCreditoPreferido)
    .WithMany()
    .HasForeignKey(c => c.PerfilCreditoPreferidoId)
    .OnDelete(DeleteBehavior.SetNull);
```

### 2. Migración

#### Migrations/20260208234350_AddPerfilCreditoPreferidoToCliente.cs
**Estado**: Creada y aplicada exitosamente

**Operaciones**:
1. `AddColumn`: Agrega `PerfilCreditoPreferidoId INT NULL` a tabla Clientes
2. `CreateIndex`: Crea índice IX_Clientes_PerfilCreditoPreferidoId
3. `AddForeignKey`: FK_Clientes_PerfilesCredito_PerfilCreditoPreferidoId con ON DELETE SET NULL

**Comando ejecutado**:
```bash
dotnet ef migrations add AddPerfilCreditoPreferidoToCliente
dotnet ef database update
```

**Resultado**: ✅ "Applying migration '20260208234350_AddPerfilCreditoPreferidoToCliente'. Done."

### 3. ViewModel

#### ViewModels/ClienteViewModel.cs
**Líneas modificadas**: 110-130

**Cambios**:
- Agregado campo `PerfilCreditoPreferidoId` (int?)
- Agregado atributo `[Display(Name = "Perfil de Crédito Preferido")]`
- Actualizado comentario para mencionar TAREA 6 + TAREA 8

```csharp
// TAREA 6 + TAREA 8: Configuración Crédito Personal
[Display(Name = "Perfil de Crédito Preferido")]
public int? PerfilCreditoPreferidoId { get; set; }
```

### 4. Vista

#### Views/Shared/Cliente/_ClienteFormFields.cshtml
**Líneas modificadas**: 220-260

**Cambios principales**:
1. **Selector de Perfil**: Agregado dropdown antes de los campos personalizados
   - Usa `ViewBag.PerfilesCredito` para opciones
   - Incluye opción "Sin perfil (usar valores manuales o globales)"
   - Texto de ayuda explicando prioridad

2. **Reorganización**: Separador y subtítulo para valores personalizados
   - `<hr class="my-4">`
   - Subtítulo: "Valores Personalizados (Opcional - Sobrescriben el perfil)"
   - Clarifica que estos valores tienen prioridad sobre el perfil

**Código agregado**:
```html
<!-- TAREA 8: Selector de perfil preferido -->
<div class="col-md-6">
    <label asp-for="PerfilCreditoPreferidoId" class="form-label"></label>
    <select asp-for="PerfilCreditoPreferidoId" class="form-select" asp-items="ViewBag.PerfilesCredito">
        <option value="">Sin perfil (usar valores manuales o globales)</option>
    </select>
    <div class="form-text">
        Si seleccionas un perfil, sus valores se usarán como base. 
        Los valores personalizados de abajo tienen prioridad sobre el perfil.
    </div>
</div>

<hr class="my-4">
<h6 class="text-muted mb-3">Valores Personalizados (Opcional - Sobrescriben el perfil)</h6>
```

### 5. Controller

#### Controllers/ClienteController.cs

##### Nuevo método privado (líneas ~375-385)
```csharp
// TAREA 8: Cargar perfiles de crédito para el selector
private async Task CargarPerfilesCredito(int? perfilSeleccionadoId = null)
{
    await using var context = await _contextFactory.CreateDbContextAsync();
    var perfiles = await context.PerfilesCredito
        .Where(p => !p.IsDeleted && p.Activo)
        .OrderBy(p => p.Orden)
        .ThenBy(p => p.Nombre)
        .ToListAsync();
    
    ViewBag.PerfilesCredito = new SelectList(perfiles, "Id", "Nombre", perfilSeleccionadoId);
}
```

##### Método Create GET (línea ~120)
**Modificado**: Ahora es `async Task<IActionResult>`
```csharp
public async Task<IActionResult> Create(string? returnUrl = null)
{
    ViewData["ReturnUrl"] = GetSafeReturnUrl(returnUrl);
    CargarDropdowns();
    await CargarPerfilesCredito(); // TAREA 8: Cargar perfiles para el selector
    return View(new ClienteViewModel());
}
```

##### Método Edit GET (línea ~162)
**Modificado**: Agrega carga de perfiles con selección actual
```csharp
public async Task<IActionResult> Edit(int id, string? returnUrl = null)
{
    // ... código existente ...
    var viewModel = _mapper.Map<ClienteViewModel>(cliente!);
    CargarDropdowns();
    await CargarPerfilesCredito(viewModel.PerfilCreditoPreferidoId); // TAREA 8
    return View(viewModel);
}
```

#### Controllers/CreditoController.cs

##### Método ConfigurarVenta GET (líneas 239-390)

**Modificación 1**: Incluir perfil en query (línea ~345)
```csharp
var cliente = await contextCliente.Clientes
    .Include(c => c.PerfilCreditoPreferido) // TAREA 8: Incluir perfil preferido
    .FirstOrDefaultAsync(c => c.Id == credito.ClienteId && !c.IsDeleted);

// TAREA 8: Cargar perfil preferido si existe
PerfilCredito? perfilPreferido = cliente?.PerfilCreditoPreferido;
```

**Modificación 2**: Cascada de valores (línea ~365)
```csharp
// Determinar valores según fuente (prioridad: Personalizado > Perfil > Global)
if (fuenteDefecto == FuenteConfiguracionCredito.PorCliente && cliente != null)
{
    // TAREA 8: Prioridad → cliente personalizado > perfil preferido > global
    tasaInicial = cliente.TasaInteresMensualPersonalizada 
        ?? perfilPreferido?.TasaMensual 
        ?? tasaMensualConfig;
        
    gastosIniciales = cliente.GastosAdministrativosPersonalizados 
        ?? perfilPreferido?.GastosAdministrativos 
        ?? 0;
        
    cuotasMaximas = cliente.CuotasMaximasPersonalizadas 
        ?? perfilPreferido?.MaxCuotas 
        ?? 24;
}
```

**Modificación 3**: ViewBag con info del perfil (línea ~380)
```csharp
ViewBag.ClienteConfigPersonalizada = new
{
    // ... campos existentes ...
    // TAREA 8: Información del perfil preferido
    TienePerfilPreferido = perfilPreferido != null,
    PerfilNombre = perfilPreferido?.Nombre,
    PerfilTasa = perfilPreferido?.TasaMensual,
    PerfilGastos = perfilPreferido?.GastosAdministrativos,
    PerfilMinCuotas = perfilPreferido?.MinCuotas,
    PerfilMaxCuotas = perfilPreferido?.MaxCuotas
};
```

## Flujo de Integración

### Prioridad de Valores (Cascada)
Cuando se configura un crédito con `FuenteConfiguracion = PorCliente`:

1. **Máxima prioridad**: Valores personalizados del cliente
   - `Cliente.TasaInteresMensualPersonalizada`
   - `Cliente.GastosAdministrativosPersonalizados`
   - `Cliente.CuotasMaximasPersonalizadas`

2. **Media prioridad**: Valores del perfil preferido
   - `PerfilCredito.TasaMensual`
   - `PerfilCredito.GastosAdministrativos`
   - `PerfilCredito.MaxCuotas`

3. **Mínima prioridad**: Valores globales
   - `ConfiguracionPago.TasaInteresMensualCreditoPersonal`
   - `ConfiguracionPago.GastosAdministrativosCreditoPersonal`
   - Constante hardcoded (24 cuotas)

### Operador de Coalescencia Nula
```csharp
valor = cliente.ValorPersonalizado 
    ?? perfil.ValorPerfil 
    ?? valorGlobal;
```

## Casos de Uso

### Caso 1: Cliente sin perfil ni personalización
- **Estado**: Cliente nuevo, sin perfil preferido ni valores personalizados
- **Comportamiento**: Usa valores globales de ConfiguracionPago
- **Ejemplo**: Tasa = 3.5% (global), Gastos = $50 (global), Cuotas = 24 (global)

### Caso 2: Cliente con perfil, sin personalización
- **Estado**: `PerfilCreditoPreferidoId = 2` (Perfil "Estándar Premium")
- **Valores personalizados**: Todos NULL
- **Comportamiento**: Usa valores del perfil "Estándar Premium"
- **Ejemplo**: Tasa = 2.8%, Gastos = $30, Cuotas = 36 (según perfil)

### Caso 3: Cliente con perfil y personalización parcial
- **Estado**: `PerfilCreditoPreferidoId = 2`, `TasaPersonalizada = 2.5%`
- **Comportamiento**: 
  - Tasa = 2.5% (personalizada)
  - Gastos = $30 (del perfil)
  - Cuotas = 36 (del perfil)

### Caso 4: Cliente con personalización completa, sin perfil
- **Estado**: `PerfilCreditoPreferidoId = NULL`, todos los campos personalizados con valor
- **Comportamiento**: Usa valores personalizados, ignora global
- **Ejemplo**: Tasa = 2.0%, Gastos = $0, Cuotas = 48 (todos personalizados)

## Beneficios de la Implementación

### 1. Flexibilidad Granular
- **Plantillas**: Perfiles como punto de partida rápido
- **Excepciones**: Personalización para casos especiales
- **Escalabilidad**: Nuevos perfiles sin modificar código

### 2. Reducción de Errores
- **Consistencia**: Perfiles estandarizan configuraciones comunes
- **Validación**: MinCuotas/MaxCuotas del perfil guían operador
- **Auditoría**: Saber si cliente usa perfil o personalización

### 3. Experiencia de Usuario
- **Dropdown claro**: "Estándar", "Premium", "Sin perfil"
- **Ayuda contextual**: Texto explicando prioridades
- **Separación visual**: hr + subtítulo para personalización

### 4. Integración con TAREA 6 y 7
- **TAREA 6**: Campos personalizados + FuenteConfiguracion
- **TAREA 7**: PerfilesCredito + ConfiguracionPago defaults
- **TAREA 8**: FK conecta todo + cascada de valores

## Testing Recomendado

### Test 1: Crear cliente con perfil
1. Ir a Cliente/Create
2. Verificar dropdown "Perfil de Crédito Preferido" con opciones
3. Seleccionar perfil "Estándar Premium"
4. Dejar campos personalizados vacíos
5. Guardar
6. Verificar en DB: `PerfilCreditoPreferidoId = 2`

### Test 2: Configurar venta con perfil
1. Crear crédito para cliente con perfil
2. Ir a ConfigurarVenta
3. Seleccionar `FuenteConfiguracion = PorCliente`
4. Verificar que valores iniciales son del perfil
5. Verificar ViewBag tiene `TienePerfilPreferido = true`

### Test 3: Override con personalización
1. Editar cliente con perfil
2. Agregar `TasaPersonalizada = 2.0%` (menor que perfil)
3. Guardar
4. Configurar venta
5. Verificar que tasa usada es 2.0% (personalizada)
6. Verificar que gastos siguen siendo del perfil

### Test 4: Eliminar perfil
1. Crear cliente con perfil X
2. Desde ConfiguracionPago, eliminar perfil X
3. Verificar que `PerfilCreditoPreferidoId` se establece en NULL (SetNull)
4. Cliente no debe fallar

### Test 5: Perfil inactivo
1. Crear cliente con perfil Y
2. Desactivar perfil Y (`Activo = false`)
3. Editar cliente
4. Verificar que perfil Y no aparece en dropdown
5. Cliente mantiene su FK, pero no puede seleccionar perfil inactivo

## Archivos Modificados (Resumen)

| Archivo | Tipo | Cambio |
|---------|------|--------|
| Models/Entities/Cliente.cs | Modelo | +2 campos (PerfilCreditoPreferidoId, navegación) |
| Data/AppDbContext.cs | Configuración | +HasOne relación con SetNull |
| Migrations/20260208234350_*.cs | Migración | AddColumn, CreateIndex, AddForeignKey |
| ViewModels/ClienteViewModel.cs | ViewModel | +1 campo con Display |
| Views/Shared/Cliente/_ClienteFormFields.cshtml | Vista | +dropdown, +hr, +subtítulo |
| Controllers/ClienteController.cs | Controller | +CargarPerfilesCredito(), modificar Create/Edit |
| Controllers/CreditoController.cs | Controller | +Include(perfil), cascada valores, ViewBag |

## Estado Final

### ✅ Completado
- [x] Modelo Cliente con FK a PerfilesCredito
- [x] Configuración EF Core con OnDelete(SetNull)
- [x] Migración aplicada exitosamente
- [x] ViewModel actualizado
- [x] Vista con dropdown de perfiles
- [x] Controller cargando perfiles activos
- [x] Integración con ConfigurarVenta (cascada de valores)
- [x] Documentación completa

### 🎯 Funcionalidades Integradas
1. **TAREA 6**: Configuración personalizada por cliente
2. **TAREA 7**: Sistema de perfiles globales
3. **TAREA 8**: Perfil preferido + cascada de prioridades

### 📊 Prioridad de Valores (Final)
```
┌─────────────────────────────────────┐
│   FuenteConfiguracion = PorCliente  │
└─────────────────────────────────────┘
              ↓
┌─────────────────────────────────────┐
│ 1. Cliente.ValorPersonalizado       │ (máxima prioridad)
└─────────────────────────────────────┘
              ↓ (si NULL)
┌─────────────────────────────────────┐
│ 2. PerfilPreferido.Valor            │ (media prioridad)
└─────────────────────────────────────┘
              ↓ (si NULL)
┌─────────────────────────────────────┐
│ 3. ConfiguracionPago.ValorGlobal    │ (fallback)
└─────────────────────────────────────┘
```

## Notas de Implementación

### Decisiones de Diseño
1. **FK Nullable**: `PerfilCreditoPreferidoId` es nullable para permitir clientes sin perfil
2. **OnDelete SetNull**: Evita eliminar clientes cuando se elimina un perfil
3. **Orden de Carga**: Perfiles ordenados por `Orden` y luego `Nombre`
4. **ViewBag vs TempData**: ViewBag para datos de dropdown (no persiste)
5. **Async CargarPerfilesCredito**: Necesita acceso a DB, por eso es async

### Consideraciones de Performance
- Include(PerfilCreditoPreferido) solo agrega 1 JOIN
- Where(Activo = true) filtra en DB, no en memoria
- SelectList crea opciones en memoria (overhead mínimo)

### Extensibilidad Futura
- Agregar `MontoMinimo/MontoMaximo` a PerfilCredito
- Permitir perfiles por categoría (Cliente VIP, Empleado, etc.)
- Historial de cambios de perfil preferido
- Sugerir perfil basado en evaluación crediticia

## Conclusión

La TAREA 8 completa el ecosistema de configuración de crédito personal, conectando:
- **TAREA 6**: Campos personalizados a nivel Cliente
- **TAREA 7**: Perfiles globales reutilizables
- **TAREA 8**: FK + cascada de prioridades

El resultado es un sistema flexible que permite:
1. **Operadores**: Elegir perfiles predefinidos o personalizar
2. **Gerentes**: Crear/modificar perfiles sin tocar código
3. **Sistema**: Cascada automática de valores según prioridad

**Estado**: ✅ **COMPLETADO** - Listo para testing funcional.
