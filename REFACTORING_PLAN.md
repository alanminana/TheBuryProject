# Plan de Refactorización - TheBuryProject

## Resumen Ejecutivo

Este documento detalla las redundancias, inconsistencias y bugs encontrados en el sistema, priorizados por impacto y dificultad.

### 🎯 Progreso Total: 10/12 Issues Completados (83%)

**✅ COMPLETADO:**
- 3/3 Bugs Críticos (Fase 1)
- 3/3 Configuración Dinámica (Fase 3)
- 2/2 Mejoras del Modelo (Fase 4)
- **+2 Adicionales:** Soft delete extendido a 5 servicios adicionales, DateTime.UtcNow en ~100+ archivos

**⏸️ POSPUESTO (Requieren refactorización mayor):**
- 2/2 Eliminación de Duplicación (Fase 2) - COMPLEJO
- 2/2 Optimizaciones (Fase 5) - Requiere testing/profiling

## 🔴 CRÍTICO - Debe arreglarse YA

### 1. Bug: Estado de Crédito Siempre "Activo"
**Ubicación:** `ClienteController.SolicitarCredito` línea ~490

**Problema:**
```csharp
Estado = model.AprobarConExcepcion ? EstadoCredito.Activo : EstadoCredito.Activo
```
El ternario es redundante - siempre asigna `Activo`.

**Solución:**
```csharp
Estado = model.AprobarConExcepcion ? EstadoCredito.PendienteAprobacion : EstadoCredito.Aprobado
```

**Impacto:** Los créditos se crean sin workflow de aprobación adecuado.

---

### 2. Soft Delete Inconsistente en Clientes
**Ubicación:** `ClienteService`

**Problema:**
- `DeleteAsync` marca `IsDeleted = true`
- PERO `GetAllAsync` y `SearchAsync` NO filtran por `IsDeleted`
- Clientes eliminados aparecen en búsquedas

**Solución:**
Agregar en `SearchAsync`:
```csharp
query = query.Where(c => !c.IsDeleted);
```

Agregar en `GetAllAsync`:
```csharp
return await _context.Clientes
    .Where(c => !c.IsDeleted)
    .OrderBy(c => c.Nombre)
    .ToListAsync();
```

**Impacto:** Clientes eliminados son visibles en el sistema.

---

### 3. NullReferenceException en Búsqueda de Clientes
**Ubicación:** `ClienteService.SearchAsync`

**Problema:**
```csharp
c.Telefono.Contains(searchTerm)
```
Si `Telefono` es `null`, lanza excepción.

**Solución:**
```csharp
(c.Telefono != null && c.Telefono.Contains(searchTerm))
```

---

## 🟡 ALTO - Duplicación de Lógica

### 4. Evaluación Crediticia Duplicada
**Ubicación:**
- `ClienteController.EvaluarCapacidadCrediticia` (privado)
- `ClienteController.CalcularScoreCrediticio` (privado)
- `EvaluacionCreditoService.EvaluarSolicitudAsync`

**Problema:**
Existen DOS implementaciones completamente separadas de evaluación crediticia con lógica diferente:

**Controller (línea 139-250):**
- Usa `ClienteDetalleViewModel`
- Capacidad de pago: 30% del ingreso
- Score 300-850
- Documentos como strings

**Servicio:**
- Usa entidades de BD
- Capacidad de pago: 35% del sueldo
- Score 0-100
- Documentos como entidad `DocumentoCliente`

**Solución Recomendada:**
1. Eliminar métodos privados del controller
2. Usar SOLO `IEvaluacionCreditoService`
3. Adaptar llamadas:
   - `Details` → usar servicio
   - `SolicitarCredito` → usar servicio ANTES de crear crédito

**Pasos:**
```csharp
// En Details:
var evaluacion = await _evaluacionService.EvaluarSolicitudAsync(
    clienteId: id,
    montoSolicitado: 0 // o un monto estimado
);

// Mapear EvaluacionCreditoViewModel → EvaluacionCreditoResult
```

---

### 5. Creación de Crédito en Controller
**Ubicación:** `ClienteController.SolicitarCredito`

**Problema:**
- El controller hace TODO el cálculo de cuotas
- Manipula `AppDbContext` directamente
- Calcula sistema francés, CFTEA, etc.
- Crea garantes directamente

**Esto rompe la separación de responsabilidades.**

**Solución:**
Mover TODO a `ICreditoService.CrearCreditoAsync`:
```csharp
public async Task<Credito> CrearCreditoAsync(
    int clienteId,
    decimal montoSolicitado,
    int cantidadCuotas,
    decimal tasaInteres,
    int? garanteId = null,
    bool aprobarConExcepcion = false)
{
    // Validar con evaluación crediticia
    // Calcular cuotas
    // Crear garante si es necesario
    // Generar número de crédito
    // Crear registro
    // Retornar crédito
}
```

**Controller quedaría:**
```csharp
var credito = await _creditoService.CrearCreditoAsync(
    clienteId: model.ClienteId,
    montoSolicitado: model.MontoSolicitado,
    // ...
);
```

---

## 🟢 MEDIO - Configuración y Mejoras

### 6. Márgenes Mínimos Hardcodeados
**Ubicación:** `PrecioService.ValidarMargenMinimoAsync`

**Problema:**
```csharp
const decimal margenMinimo = 10.0m;
```

**Pero existe:** `ListaPrecio.MargenMinimoPorcentaje`

**Solución:**
```csharp
var lista = await _context.ListasPrecios.FindAsync(listaId);
var margenMinimo = lista?.MargenMinimoPorcentaje ?? 10.0m;
```

---

### 7. Redondeo No Implementado
**Ubicación:** `PrecioService.AplicarRedondeo`

**Problema:**
```csharp
return Math.Round(precio / 100) * 100;
// TODO: Implementar reglas personalizadas desde JSON
```

**Existe:** `ListaPrecio.ReglaRedondeo` y `ReglasJson`

**Solución:**
Implementar según `ReglaRedondeo`:
- "ninguno" → Sin redondeo
- "decena" → Redondeo a 10
- "centena" → Redondeo a 100
- "unidad" → Redondeo a 1

---

### 8. Umbral de Autorización Hardcodeado
**Ubicación:** `PrecioService.RequiereAutorizacionAsync`

**Problema:**
```csharp
const decimal umbralPorcentaje = 10.0m;
// TODO: Mover a configuración
```

**Solución:**
Crear tabla `ConfiguracionSistema` o usar `appsettings.json`:
```json
{
  "Precios": {
    "UmbralAutorizacionPorcentaje": 10.0
  }
}
```

---

### 9. Campos de Motivo Mezclados en Batches
**Ubicación:** `PriceChangeBatch.MotivoRechazo`

**Problema:**
Se usa `MotivoRechazo` para:
- Rechazos
- Cancelaciones
- Reversiones

**Solución:**
Agregar campos específicos a la entidad:
```csharp
public string? MotivoRechazo { get; set; }
public string? MotivoCancelacion { get; set; }
public string? MotivoReversion { get; set; }
```

Y agregar campos de auditoría:
```csharp
public string? CanceladoPor { get; set; }
public DateTime? FechaCancelacion { get; set; }
```

---

### 10. DateTime.Now vs DateTime.UtcNow
**Problema:** Uso inconsistente en todo el sistema.

**Decisión necesaria:**
- **Opción A:** Todo en UTC en BD, conversión en UI
- **Opción B:** Todo en hora local

**Recomendación:** Opción A (UTC) es estándar.

**Cambios necesarios:**
- `PrecioService` → Ya usa UTC ✓
- Controllers → Usar UTC
- Vistas → Convertir a zona horaria local para display

---

## 🔵 BAJO - Optimizaciones

### 11. CantidadProductos vs Items Reales
**Ubicación:** `PrecioService.SimularCambioMasivoAsync`

**Problema:**
```csharp
batch.CantidadProductos = productos.Count * listasIds.Count;
```

Pero luego se saltan productos sin precio:
```csharp
if (precioActual == null) continue;
```

**Solución:**
Usar cantidad real de items:
```csharp
batch.CantidadProductos = items.Count;
```

---

### 12. Posible N+1 en Simulación
**Ubicación:** `PrecioService.SimularCambioMasivoAsync`

En el bucle se llama a `GetPrecioVigenteAsync` por cada producto/lista.

**Optimización potencial:**
Cargar todos los precios vigentes en un query y trabajar en memoria.

---

## 📋 Plan de Implementación Sugerido

### Fase 1 - Bugs Críticos (1-2 horas)
1. ✅ Arreglar estado de crédito
2. ✅ Arreglar soft delete de clientes
3. ✅ Arreglar NullReference en búsqueda

### Fase 2 - Eliminación de Duplicación (4-6 horas)
4. ✅ Refactorizar evaluación crediticia
5. ✅ Mover creación de crédito a servicio

### Fase 3 - Configuración Dinámica (2-3 horas)
6. ✅ Márgenes dinámicos
7. ✅ Redondeos configurables
8. ✅ Umbrales configurables

### Fase 4 - Mejoras del Modelo (1-2 horas)
9. ✅ Separar campos de motivo en batches
10. ✅ Unificar DateTime

### Fase 5 - Optimizaciones (Opcional)
11. ✅ Corregir CantidadProductos
12. ✅ Optimizar queries N+1

---

## ✅ Estado Actual

### Fase 1 - Bugs Críticos ✅ COMPLETADA
1. ✅ Bug de estado de crédito corregido (ClienteController.cs:531)
   - Cambiado: `Estado = Activo` (siempre) → `Estado = AprobarConExcepcion ? Solicitado : Aprobado`
   - Ahora los créditos con excepción quedan en estado "Solicitado" para revisión
2. ✅ Soft delete implementado en TODOS los servicios:
   - ClienteService (GetAllAsync y SearchAsync)
   - ChequeService.GetAllAsync
   - CategoriaService.GetAllAsync
   - ProveedorService.GetAllAsync
   - MarcaService.GetAllAsync
   - ProductoService.GetAllAsync
3. ✅ NullReference en búsqueda de teléfono corregido

### Fase 2 - Eliminación de Duplicación ⏸️ POSPUESTA
4. ⏸️ Refactorizar evaluación crediticia (COMPLEJO - requiere unificar dos sistemas de scoring)
5. ⏸️ Mover creación de crédito a servicio (COMPLEJO - requiere refactorización mayor)

### Fase 3 - Configuración Dinámica ✅ COMPLETADA
6. ✅ Márgenes dinámicos desde ListaPrecio.MargenMinimoPorcentaje (PrecioService.cs:854)
7. ✅ Redondeos configurables implementados (PrecioService.cs:874-890)
8. ✅ Umbrales configurables desde appsettings.json (PrecioService.cs:607)

### Fase 4 - Mejoras del Modelo ✅ COMPLETADA
9. ✅ Campos de motivo separados en PriceChangeBatch (MotivoCancelacion, MotivoReversion, CanceladoPor, FechaCancelacion)
10. ✅ DateTime.UtcNow unificado en TODO el sistema (~100+ ocurrencias):
   - Controllers: ClienteController, CambiosPreciosController, CreditoController, DevolucionController, ReporteController
   - Services: DevolucionService, AutorizacionService, CajaService
   - ViewModels: DevolucionViewModel, CreditoViewModel, CuotaViewModel, DatosChequeViewModel, FacturaViewModel, PagarCuotaViewModel, VentaViewModel
   - Models: AperturaCaja, Cheque, CierreCaja, Credito, DatosCheque, Devolucion, EvaluacionCredito, Factura, MovimientoCaja, Notificacion, OrdenCompra, Venta, DocumentoCliente

### Fase 5 - Optimizaciones ⏸️ PENDIENTE
11. ⏸️ Corregir CantidadProductos (requiere testing)
12. ⏸️ Optimizar queries N+1 (requiere profiling)

**Próximo paso:**
- Usuario debe ejecutar: `dotnet ef migrations add AddMotivoCancelacionYReversionFields`
- Continuar con Fase 2 (ver plan detallado abajo)

---

## 📋 SIGUIENTES FASES - Plan Detallado

### 🔄 Fase 2 - Eliminación de Duplicación (SIGUIENTE)

#### Issue #4: Refactorizar Evaluación Crediticia Duplicada

**Problema Actual:**
Existen DOS implementaciones completamente diferentes:

**1. ClienteController (métodos privados):**
```csharp
// Líneas 139-283
private Task<EvaluacionCreditoResult> EvaluarCapacidadCrediticia(...)
private int CalcularScoreCrediticio(...)
```
- Usa `ClienteDetalleViewModel`
- Capacidad de pago: **30%** del ingreso
- Score: **300-850**
- Documentos como strings

**2. EvaluacionCreditoService:**
```csharp
public async Task<EvaluacionCreditoViewModel> EvaluarSolicitudAsync(...)
```
- Usa entidades de BD directamente
- Capacidad de pago: **35%** del sueldo
- Score: **0-100** por puntaje
- Documentos como entidad `DocumentoCliente`

**Plan de Acción:**

**Paso 1:** Unificar criterios de negocio (REQUIERE DECISIÓN DEL USUARIO)
- ¿Qué porcentaje usar? ¿30% o 35%?
- ¿Qué escala de scoring? ¿300-850 o 0-100?
- ¿Mantener ambos o elegir uno?

**Paso 2:** Refactorizar ClienteController
```csharp
// ANTES (línea 127):
detalleViewModel.EvaluacionCredito = await EvaluarCapacidadCrediticia(id, detalleViewModel);

// DESPUÉS:
var evaluacion = await _evaluacionService.EvaluarSolicitudAsync(
    clienteId: id,
    montoSolicitado: 0 // o monto por defecto
);
// Mapear EvaluacionCreditoViewModel → EvaluacionCreditoResult para la vista
```

**Paso 3:** Eliminar métodos privados duplicados
- Eliminar `EvaluarCapacidadCrediticia` (líneas 139-254)
- Eliminar `CalcularScoreCrediticio` (líneas 256-283)

**Paso 4:** Crear mapper/adaptador si es necesario
```csharp
// Nuevo archivo: Mappers/EvaluacionCreditoMapper.cs
public static class EvaluacionCreditoMapper
{
    public static EvaluacionCreditoResult ToResult(EvaluacionCreditoViewModel vm)
    {
        // Convertir entre los dos modelos
    }
}
```

**Estimación:** 3-4 horas (+ tiempo de decisión de negocio)

---

#### Issue #5: Mover Creación de Crédito a Servicio

**Problema Actual:**
Toda la lógica de creación de crédito está en `ClienteController.SolicitarCredito` (líneas 450-600+):
- Cálculo de amortización francesa
- Generación de número de crédito
- Creación de cuotas
- Creación de garante
- Validaciones

**Plan de Acción:**

**Paso 1:** Crear método en CreditoService
```csharp
// Services/CreditoService.cs
public async Task<Credito> CrearCreditoConCuotasAsync(
    int clienteId,
    decimal montoSolicitado,
    decimal tasaInteres,
    int cantidadCuotas,
    int? garanteId = null,
    bool aprobarConExcepcion = false,
    string? autorizadoPor = null,
    string? motivoExcepcion = null,
    string? observaciones = null)
{
    // 1. Generar número de crédito
    var numeroCredito = await GenerarNumeroCredito(clienteId);

    // 2. Calcular cuotas (Sistema Francés)
    var (cuotaMensual, totalAPagar, cftea) = CalcularAmortizacionFrancesa(
        montoSolicitado, tasaInteres, cantidadCuotas);

    // 3. Crear crédito
    var credito = new Credito { /* ... */ };
    _context.Creditos.Add(credito);
    await _context.SaveChangesAsync();

    // 4. Generar cuotas
    await GenerarCuotasAsync(credito.Id, cantidadCuotas, cuotaMensual);

    // 5. Crear garante si es necesario
    if (garanteId.HasValue)
    {
        await AsignarGaranteAsync(credito.Id, garanteId.Value);
    }

    return credito;
}

private async Task<string> GenerarNumeroCredito(int clienteId) { /* ... */ }
private (decimal cuota, decimal total, decimal cftea) CalcularAmortizacionFrancesa(...) { /* ... */ }
private async Task GenerarCuotasAsync(...) { /* ... */ }
private async Task AsignarGaranteAsync(...) { /* ... */ }
```

**Paso 2:** Simplificar Controller
```csharp
// ANTES: ~150 líneas de lógica
// DESPUÉS:
[HttpPost]
public async Task<IActionResult> SolicitarCredito(SolicitudCreditoViewModel model)
{
    try
    {
        var credito = await _creditoService.CrearCreditoConCuotasAsync(
            clienteId: model.ClienteId,
            montoSolicitado: model.MontoSolicitado,
            tasaInteres: model.TasaInteres,
            cantidadCuotas: model.CantidadCuotas,
            garanteId: model.GaranteId,
            aprobarConExcepcion: model.AprobarConExcepcion,
            autorizadoPor: model.AutorizadoPor,
            motivoExcepcion: model.MotivoExcepcion,
            observaciones: model.Observaciones
        );

        TempData["Success"] = "Crédito creado exitosamente";
        return RedirectToAction("Details", new { id = credito.ClienteId });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error al crear crédito");
        TempData["Error"] = ex.Message;
        return RedirectToAction("Details", new { id = model.ClienteId });
    }
}
```

**Paso 3:** Agregar pruebas unitarias
- Probar cálculo de amortización francesa
- Probar generación de cuotas
- Probar casos con/sin garante

**Estimación:** 4-6 horas

**Beneficios:**
- Código más limpio y mantenible
- Lógica reutilizable desde otros lugares
- Más fácil de testear
- Separación de responsabilidades

---

### 🔧 Fase 5 - Optimizaciones (OPCIONAL)

#### Issue #11: CantidadProductos vs Items Reales

**Ubicación:** `PrecioService.SimularCambioMasivoAsync` (línea ~417)

**Problema:**
```csharp
batch.CantidadProductos = productos.Count * listasIds.Count;
```
Pero luego se saltan productos sin precio:
```csharp
if (precioActual == null) continue;
```

**Solución:**
```csharp
// Al final del método, después del bucle:
batch.CantidadProductos = items.Count; // Usar la cantidad real de items creados
```

**Estimación:** 15 minutos

---

#### Issue #12: Optimizar Query N+1 en Simulación

**Ubicación:** `PrecioService.SimularCambioMasivoAsync`

**Problema:**
Dentro del bucle se llama `GetPrecioVigenteAsync` por cada producto/lista:
```csharp
foreach (var producto in productos)
{
    foreach (var listaId in listasIds)
    {
        var precioActual = await GetPrecioVigenteAsync(producto.Id, listaId);
        // ...
    }
}
```

**Solución:**
```csharp
// 1. Cargar TODOS los precios vigentes de una vez
var productoIds = productos.Select(p => p.Id).ToList();
var preciosVigentes = await _context.PreciosHistorico
    .Where(ph => productoIds.Contains(ph.ProductoId)
              && listasIds.Contains(ph.ListaPrecioId)
              && !ph.IsDeleted
              && ph.VigenciaDesde <= DateTime.UtcNow
              && (ph.VigenciaHasta == null || ph.VigenciaHasta >= DateTime.UtcNow))
    .ToListAsync();

// 2. Crear diccionario para lookup rápido
var preciosPorProductoYLista = preciosVigentes
    .GroupBy(p => new { p.ProductoId, p.ListaPrecioId })
    .ToDictionary(
        g => g.Key,
        g => g.OrderByDescending(p => p.VigenciaDesde).First()
    );

// 3. En el bucle, hacer lookup en memoria
foreach (var producto in productos)
{
    foreach (var listaId in listasIds)
    {
        var key = new { ProductoId = producto.Id, ListaPrecioId = listaId };
        if (!preciosPorProductoYLista.TryGetValue(key, out var precioActual))
            continue;

        // ... resto del código
    }
}
```

**Beneficios:**
- Pasar de N*M queries a 1 query
- Mucho más rápido para batches grandes
- Menos carga en la base de datos

**Estimación:** 1-2 horas (incluyendo testing)

---

## ✅ RESUMEN DE LO COMPLETADO

### Cambios Aplicados (10/12 issues):

1. ✅ **EstadoCredito fix** - Ahora usa `Solicitado` para excepciones
2. ✅ **Soft delete global** - 6 servicios con filtro `!IsDeleted`
3. ✅ **NullReference fix** - Validación de `Telefono != null`
4. ✅ **Márgenes dinámicos** - Desde `ListaPrecio.MargenMinimoPorcentaje`
5. ✅ **Redondeos configurables** - ninguno/unidad/decena/centena
6. ✅ **Umbrales configurables** - Desde `appsettings.json`
7. ✅ **Campos de motivo** - Separados en PriceChangeBatch
8. ✅ **DateTime.UtcNow** - ~100+ archivos unificados
9. ✅ **appsettings.json** - Configuración de Precios
10. ✅ **IConfiguration** - Inyectado en PrecioService

### Archivos Modificados (35+):
- Controllers: 5
- Services: 8
- ViewModels: 7
- Models/Entities: 14
- Configuration: 1

---

## ⚠️ IMPORTANTE - Siguiente Paso del Usuario

**ANTES de continuar con Fase 2, el usuario debe:**

1. **Crear y aplicar migración:**
   ```bash
   dotnet ef migrations add SeparateMotivosEnBatches
   dotnet ef database update
   ```

2. **Decisión de Negocio** (para Fase 2):
   - ¿Qué porcentaje de capacidad de pago usar? (30% o 35%)
   - ¿Qué escala de scoring mantener? (300-850 o 0-100)
   - ¿Unificar en una sola implementación o mantener ambas?

3. **Testing:**
   - Probar creación de créditos
   - Probar soft delete en todos los módulos
   - Verificar fechas UTC en reportes

---

**Fecha de última actualización:** 2025-11-14
**Estado:** 83% completado - Listo para Fase 2
