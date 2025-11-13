# Plan de Refactorización - TheBuryProject

## Resumen Ejecutivo

Este documento detalla las redundancias, inconsistencias y bugs encontrados en el sistema, priorizados por impacto y dificultad.

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
2. ✅ Soft delete implementado en ClienteService (GetAllAsync y SearchAsync)
3. ✅ NullReference en búsqueda de teléfono corregido

### Fase 2 - Eliminación de Duplicación ⏸️ POSPUESTA
4. ⏸️ Refactorizar evaluación crediticia (COMPLEJO - requiere unificar dos sistemas de scoring)
5. ⏸️ Mover creación de crédito a servicio (COMPLEJO - requiere refactorización mayor)

### Fase 3 - Configuración Dinámica ✅ COMPLETADA
6. ✅ Márgenes dinámicos desde ListaPrecio.MargenMinimoPorcentaje (PrecioService.cs:854)
7. ✅ Redondeos configurables implementados (PrecioService.cs:874-890)
8. ✅ Umbrales configurables desde appsettings.json (PrecioService.cs:607)

### Fase 4 - Mejoras del Modelo ✅ COMPLETADA
9. ✅ Campos de motivo separados en PriceChangeBatch (MotivoCancelacion, MotivoReversion)
10. ✅ DateTime.UtcNow unificado en ClienteController y CambiosPreciosController

### Fase 5 - Optimizaciones ⏸️ PENDIENTE
11. ⏸️ Corregir CantidadProductos (requiere testing)
12. ⏸️ Optimizar queries N+1 (requiere profiling)

**Próximo paso:**
- Usuario debe ejecutar: `dotnet ef migrations add AddMotivoCancelacionYReversionFields`
- Fase 2 requiere análisis adicional para unificar lógica de negocio.
