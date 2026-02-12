# TAREA 6: Crédito Personal - Configuración Multi-Fuente

## Descripción General

Implementación de un sistema flexible de configuración de créditos personales que permite obtener parámetros (tasa de interés, gastos administrativos, cuotas máximas, montos) desde múltiples fuentes, permitiendo al operador elegir el método en el momento de configurar el crédito.

## Problema que Resuelve

Anteriormente, los valores de configuración de crédito (tasa, gastos, cuotas) se obtenían únicamente de la configuración global del sistema. Esto no permitía:
- Aplicar condiciones especiales a clientes específicos
- Ajustar parámetros manualmente para casos excepcionales
- Adaptarse cuando el cliente es indeciso y el operador necesita flexibilidad

## Fuentes de Configuración Disponibles

### 1. 🌍 Global (Sistema)
- **Descripción**: Valores por defecto configurados en el sistema
- **Uso**: Clientes nuevos o sin configuración especial
- **Origen**: Servicio `IConfiguracionPagoService`
- **Campos**: Campos de solo lectura (no editables por el operador)

### 2. 👤 Por Cliente
- **Descripción**: Valores personalizados configurados en el perfil del cliente
- **Uso**: Clientes con condiciones crediticias especiales
- **Origen**: Entidad `Cliente` (campos nullable nuevos)
- **Campos**: 
  - `TasaInteresMensualPersonalizada` (decimal 8,4)
  - `GastosAdministrativosPersonalizados` (decimal 8,4)
  - `CuotasMaximasPersonalizadas` (int)
  - `MontoMinimoPersonalizado` (decimal 18,2)
  - `MontoMaximoPersonalizado` (decimal 18,2)
- **Comportamiento**: Si el cliente tiene valores configurados, se selecciona automáticamente como fuente por defecto

### 3. ✏️ Manual (Personalizado)
- **Descripción**: Valores ingresados manualmente por el operador en el momento
- **Uso**: Casos excepcionales, negociaciones, pruebas
- **Origen**: Entrada del usuario en el formulario
- **Campos**: Todos los campos son editables

### 4. 📊 Por Plan (No disponible)
- **Descripción**: Valores basados en perfiles de riesgo (futuro)
- **Estado**: Reservado para implementación futura
- **Uso planeado**: Diferentes planes según nivel de riesgo del cliente

## Arquitectura de la Solución

### Base de Datos

**Migración**: `20260208232407_AddConfiguracionCreditoPersonalizadaCliente`

**Tabla**: `Clientes`

**Nuevas columnas**:
```sql
TasaInteresMensualPersonalizada DECIMAL(8,4) NULL
GastosAdministrativosPersonalizados DECIMAL(8,4) NULL
CuotasMaximasPersonalizadas INT NULL
MontoMinimoPersonalizado DECIMAL(18,2) NULL
MontoMaximoPersonalizado DECIMAL(18,2) NULL
```

### Modelo de Datos

**Enum**: `Models/Enums/FuenteConfiguracionCredito.cs`
```csharp
public enum FuenteConfiguracionCredito
{
    Global = 0,      // Sistema (por defecto)
    PorCliente = 1,  // Valores del cliente
    Manual = 2,      // Ingreso manual
    PorPlan = 3      // Perfiles de riesgo (futuro)
}
```

**Entidad Cliente**: `Models/Entities/Cliente.cs`
- Se agregaron 5 campos nullable para configuración personalizada
- Todos con valor `null` por defecto (usa configuración global)

**ViewModel**: `ViewModels/ConfiguracionCreditoVentaViewModel.cs`
```csharp
public FuenteConfiguracionCredito FuenteConfiguracion { get; set; } = FuenteConfiguracionCredito.Global;
public int ClienteId { get; set; }
public decimal? TasaMensual { get; set; }
```

### Controlador

**Archivo**: `Controllers/CreditoController.cs`

#### GET ConfigurarVenta
1. Carga los datos del cliente desde la base de datos
2. Detecta si el cliente tiene configuración personalizada (cualquier campo no null)
3. Establece `fuenteDefecto`:
   - `PorCliente` si tiene valores personalizados
   - `Global` si no tiene valores personalizados
4. Carga valores iniciales según la fuente detectada
5. Prepara `ViewBag.ClienteConfigPersonalizada` con:
   - `TieneTasaPersonalizada` (bool)
   - `TasaPersonalizada` (decimal?)
   - `GastosPersonalizados` (decimal?)
   - `CuotasMaximas` (int)
   - `TasaGlobal` (decimal)

#### POST ConfigurarVenta
1. Lee `modelo.FuenteConfiguracion` del formulario
2. Switch según fuente:
   - **Global**: Carga tasa desde `IConfiguracionPagoService`
   - **PorCliente**: Carga cliente desde DB, usa valores personalizados con fallback a global
   - **Manual**: Usa valores del formulario tal como vienen
3. Aplica valores al crédito
4. Agrega información de fuente en `credito.Observaciones`
5. Guarda el crédito

#### GET SimularPlanVenta
- Actualizado para aceptar `decimal? tasaMensual` como parámetro
- Si no se proporciona, usa la tasa global
- Permite que JavaScript calcule en tiempo real con la tasa actual

### Vista

**Archivo**: `Views/Credito/ConfigurarVenta.cshtml`

**Selector de Fuente**:
```html
<select asp-for="FuenteConfiguracion" id="fuenteConfigSelect">
    <option value="0">🌍 Global (Sistema)</option>
    <option value="1" [disabled si no tiene config]>👤 Por Cliente</option>
    <option value="2">✏️ Manual (Personalizado)</option>
    <option value="3" disabled>📊 Por Plan (No disponible)</option>
</select>
```

**Campo de Tasa**:
- Ahora es editable (`asp-for="TasaMensual"`)
- Badge dinámico que muestra la fuente activa
- Readonly cuando fuente es Global o PorCliente
- Editable cuando fuente es Manual

**Data Attributes** (para JavaScript):
```html
<div id="configData"
     data-tasa-global="..."
     data-tasa-cliente="..."
     data-gastos-cliente="..."
     data-cuotas-cliente="..."
     data-tiene-config="...">
</div>
```

### JavaScript

**Archivo**: `wwwroot/js/creditos-configurar.js`

**Funcionalidad**:
1. **Carga inicial**: Lee datos de configuración desde data attributes
2. **Event listener**: Escucha cambios en el select de fuente
3. **Función `actualizarFuenteConfiguracion()`**:
   - Actualiza badge de tasa (color y texto)
   - Actualiza textos de ayuda
   - Cambia readonly del campo de tasa
   - Carga valores según fuente seleccionada
   - Ajusta límite de cuotas máximas
   - Recalcula plan de crédito
4. **Envío a API**: Incluye `tasaMensual` en llamada a `SimularPlanVenta`

### Configuración en Cliente

**Archivo**: `Views/Shared/Cliente/_ClienteFormFields.cshtml`

**Nueva sección**: "Configuración de Crédito Personalizada"
- Card con borde amarillo (badge "Opcional")
- Alert informativo explicando el propósito
- Campos:
  - Tasa de Interés Mensual (%)
  - Gastos Administrativos (%)
  - Cuotas Máximas Permitidas
  - Monto Mínimo de Crédito ($)
  - Monto Máximo de Crédito ($)
- Alert de advertencia sobre prioridad de valores
- Todos los campos son opcionales (nullable)

**ViewModel**: `ViewModels/ClienteViewModel.cs`
- Agregadas las 5 propiedades con validaciones `[Range]`
- AutoMapper las mapea automáticamente (nombres coincidentes)

## Flujo de Uso

### Escenario 1: Cliente sin configuración personalizada
1. Operador ingresa a "Configurar Crédito" desde una venta
2. Sistema detecta que cliente no tiene valores personalizados
3. Fuente por defecto: **Global**
4. Campos de tasa/gastos en readonly con valores del sistema
5. Operador puede cambiar a **Manual** si necesita ajustar valores

### Escenario 2: Cliente con configuración personalizada
1. Operador configura valores personalizados en Cliente > Editar
2. Operador ingresa a "Configurar Crédito" desde una venta
3. Sistema detecta valores personalizados en el cliente
4. Fuente por defecto: **Por Cliente**
5. Campos cargados con valores personalizados (readonly)
6. Operador puede cambiar a **Global** o **Manual** si lo desea

### Escenario 3: Caso excepcional/negociación
1. Operador necesita aplicar valores específicos para esta venta
2. Selecciona fuente: **Manual**
3. Todos los campos se vuelven editables
4. Operador ingresa los valores acordados
5. Sistema registra en Observaciones que fue configuración manual

## Cambios Técnicos Clave

### Migración Manual
- **Problema**: EF Core incluyó operaciones incorrectas para tabla `OrdenCompraDetalle`
- **Solución**: Edición manual de la migración para remover operaciones de OrdenCompra
- **Aprendizaje**: Migración generator puede incluir cambios no relacionados; revisar siempre antes de aplicar

### Precisión Decimal
- **Problema**: EF Core generaba decimal sin precisión (advertencias)
- **Solución**: Configuración explícita en `AppDbContext` con `HasPrecision(8, 4)` para tasas y `HasPrecision(18, 2)` para montos
- **Resultado**: Migración limpia sin advertencias

### Readonly Dinámico en JavaScript
- **Técnica**: Agregar/remover clases CSS y propiedad `readonly`
- **Efecto visual**: Campos cambian de `bg-body-secondary` (gris, readonly) a `bg-dark` (editable)
- **UX**: Usuario percibe claramente cuándo puede o no editar

### ViewBag vs TempData
- **Elección**: `ViewBag` para datos de configuración del cliente
- **Razón**: Datos necesarios solo en la vista actual, no persisten entre redirects
- **Estructura**: Objeto anónimo con propiedades tipadas en comentarios

## Testing

### Tests Actualizados
- `CreditoControllerConfigTasaTests.cs`: Agregado parámetro `tasaMensual` null a `SimularPlanVenta`
- **Estado**: Todos los tests pasan correctamente

### Tests Sugeridos (pendientes)
1. **Test**: GET ConfigurarVenta con cliente sin config → debe defaultear a Global
2. **Test**: GET ConfigurarVenta con cliente con config → debe defaultear a PorCliente
3. **Test**: POST ConfigurarVenta con fuente Manual → debe usar valores del formulario
4. **Test**: POST ConfigurarVenta con fuente PorCliente → debe cargar valores del cliente
5. **Test**: POST ConfigurarVenta con fuente Global → debe cargar valores del servicio

## Observabilidad

### Logs Agregados
```csharp
// En POST ConfigurarVenta
_logger.LogInformation(
    "Crédito {CreditoId}: Usando configuración personalizada del cliente {ClienteId} - Tasa: {Tasa}%, Gastos: ${Gastos}",
    modelo.CreditoId, modelo.ClienteId, tasaMensual, gastosAdministrativos);
```

### Trazabilidad
- Cada crédito registra en `Observaciones` la fuente de configuración utilizada
- Formato: `[Configuración del Cliente]`, `[Configuración Global]`, `[Configuración Manual]`

## Próximos Pasos (Futuro)

### TAREA 6.1: Implementar PorPlan
1. Crear entidad `PlanCredito` con campos de configuración y nivel de riesgo
2. Agregar FK `PlanCreditoId` a `Cliente` (nullable)
3. Actualizar lógica de ConfigurarVenta para manejar `FuenteConfiguracion.PorPlan`
4. Crear CRUD para gestionar planes de crédito

### TAREA 6.2: Validaciones Avanzadas
1. Validar que `MontoMinimoPersonalizado` ≤ `MontoMaximoPersonalizado`
2. Validar en POST ConfigurarVenta que monto solicitado esté dentro del rango del cliente
3. Mostrar alerta si se supera el límite de cuotas personalizadas

### TAREA 6.3: Historial de Configuraciones
1. Crear tabla `HistorialConfiguracionCredito` para auditoría
2. Registrar cada cambio de fuente con usuario y timestamp
3. Vista de historial en detalle del crédito

## Comandos Importantes

### Aplicar Migración
```bash
dotnet ef migrations add AddConfiguracionCreditoPersonalizadaCliente
dotnet ef database update
```

### Rollback (si necesario)
```bash
dotnet ef database update PreviousMigrationName
dotnet ef migrations remove
```

### Compilar
```bash
dotnet build
```

## Archivos Modificados

### Nuevos
- `Models/Enums/FuenteConfiguracionCredito.cs`
- `Migrations/20260208232407_AddConfiguracionCreditoPersonalizadaCliente.cs`

### Modificados
- `Models/Entities/Cliente.cs` (líneas 147-177)
- `Data/AppDbContext.cs` (líneas 449-454)
- `ViewModels/ConfiguracionCreditoVentaViewModel.cs` (agregadas 2 propiedades)
- `ViewModels/ClienteViewModel.cs` (agregadas 5 propiedades)
- `Controllers/CreditoController.cs`:
  - `ConfigurarVenta` GET (líneas ~332-390)
  - `ConfigurarVenta` POST (líneas ~400-470)
  - `SimularPlanVenta` (agregado parámetro `tasaMensual`)
- `Views/Credito/ConfigurarVenta.cshtml` (agregado selector y campos dinámicos)
- `Views/Shared/Cliente/_ClienteFormFields.cshtml` (agregada sección de config)
- `wwwroot/js/creditos-configurar.js` (agregada lógica de cambio de fuente)
- `tests/TheBuryProject.Tests/Creditos/CreditoControllerConfigTasaTests.cs`

## Resumen de Decisiones de Diseño

1. **Nullable fields en Cliente**: Permite distinguir entre "no configurado" (null) y "configurado en 0" (0.00)
2. **Enum para fuente**: Facilita agregar nuevas fuentes en el futuro sin breaking changes
3. **ViewBag para datos de configuración**: Datos transitorios que no justifican un ViewModel completo
4. **Badge visual dinámico**: Feedback visual inmediato de la fuente activa
5. **Readonly por fuente**: Previene errores del usuario, claridad sobre qué es editable
6. **Observaciones con fuente**: Auditoría simple sin complejidad de tabla adicional

---

**Autor**: TheBuryProject Development Team  
**Fecha**: Enero 2025  
**Versión**: 1.0
