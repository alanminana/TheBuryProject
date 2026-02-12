# TAREA 7: Modal "Configurar pagos" - Sección Crédito Personal

## Descripción General

Implementación completa de la sección "Crédito Personal" en el modal de configuración de pagos de Venta/Index, que incluye:
- **7.1.1 Defaults Globales**: Valores fallback del sistema cuando no hay perfil seleccionado
- **7.1.2 Perfiles/Planes de Crédito**: Múltiples escenarios predefinidos (Estándar, Conservador, Riesgoso, etc.)

## Problema que Resuelve

Anteriormente, la configuración de crédito personal estaba dispersa y limitada:
- Solo existía un campo de tasa mensual en ConfiguracionPago
- No había forma de definir múltiples escenarios de crédito
- Los operadores no podían elegir entre diferentes perfiles según el tipo de cliente
- No se gestionaban gastos administrativos, ni límites de cuotas a nivel de configuración

## Solución Implementada

### 7.1.1 Defaults Globales (Fallback)

Campos configurables cuando no hay un perfil específico seleccionado:
- **Tasa Mensual Default (%)**: Tasa de interés mensual por defecto
- **Gastos Administrativos Default ($)**: Monto fijo de gastos administrativos
- **Min Cuotas Default**: Mínimo de cuotas permitidas (1-120)
- **Max Cuotas Default**: Máximo de cuotas permitidas (1-120)

**Almacenamiento**: Se guardan en la entidad `ConfiguracionPago` con TipoPago = CreditoPersonal (5)

### 7.1.2 Perfiles/Planes de Crédito

Sistema completo de administración de perfiles predefinidos:
- **Nombre del Perfil**: Identificador único (ej: "Estándar", "Conservador", "Riesgoso")
- **Descripción**: Texto opcional explicativo
- **Tasa Mensual (%)**: Tasa específica del perfil (0-100%)
- **Gastos Administrativos ($)**: Monto fijo de gastos
- **Min Cuotas**: Mínimo de cuotas permitidas (1-120)
- **Max Cuotas**: Máximo de cuotas permitidas (1-120)
- **Activo**: Flag para activar/desactivar el perfil
- **Orden**: Campo para ordenar los perfiles en la interfaz

**Almacenamiento**: Nueva tabla `PerfilesCredito`

## Arquitectura de la Solución

### Base de Datos

**Migración**: `20260208233717_AddPerfilesCreditoYDefaultsGlobales`

#### Tabla ConfiguracionesPago (modificada)
```sql
-- Nuevos campos para defaults globales
GastosAdministrativosDefaultCreditoPersonal DECIMAL(18,2) NULL
MinCuotasDefaultCreditoPersonal INT NULL
MaxCuotasDefaultCreditoPersonal INT NULL

-- Campo existente con precisión actualizada
TasaInteresMensualCreditoPersonal DECIMAL(8,4) NULL  -- antes era (18,2)
```

#### Tabla PerfilesCredito (nueva)
```sql
CREATE TABLE PerfilesCredito (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Nombre NVARCHAR(100) NOT NULL,
    Descripcion NVARCHAR(500) NULL,
    TasaMensual DECIMAL(8,4) NOT NULL,
    GastosAdministrativos DECIMAL(18,2) NOT NULL,
    MinCuotas INT NOT NULL,
    MaxCuotas INT NOT NULL,
    Activo BIT NOT NULL,
    Orden INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NULL,
    CreatedBy NVARCHAR(MAX) NULL,
    UpdatedBy NVARCHAR(MAX) NULL,
    IsDeleted BIT NOT NULL,
    RowVersion ROWVERSION NOT NULL,
    
    CONSTRAINT IX_PerfilesCredito_Nombre UNIQUE (Nombre)
)

CREATE INDEX IX_PerfilesCredito_Orden ON PerfilesCredito(Orden)
```

### Modelo de Datos

#### Nueva Entidad: PerfilCredito
**Archivo**: `Models/Entities/PerfilCredito.cs`

Propiedades principales:
- `Nombre` (string, required, max 100): Identificador del perfil
- `Descripcion` (string?, max 500): Descripción opcional
- `TasaMensual` (decimal, 8,4): Tasa de interés mensual en porcentaje
- `GastosAdministrativos` (decimal, 18,2): Monto fijo de gastos
- `MinCuotas` (int, 1-120): Mínimo de cuotas permitidas
- `MaxCuotas` (int, 1-120): Máximo de cuotas permitidas
- `Activo` (bool): Indica si el perfil está disponible
- `Orden` (int): Orden de visualización

Hereda de `AuditableEntity` (CreatedAt, UpdatedAt, CreatedBy, UpdatedBy, IsDeleted, RowVersion)

#### Entidad Modificada: ConfiguracionPago
**Archivo**: `Models/Entities/ConfiguracionPago.cs`

Nuevos campos:
- `GastosAdministrativosDefaultCreditoPersonal` (decimal?, 18,2)
- `MinCuotasDefaultCreditoPersonal` (int?)
- `MaxCuotasDefaultCreditoPersonal` (int?)

Campo modificado:
- `TasaInteresMensualCreditoPersonal`: Cambió precisión de (18,2) a (8,4)

#### ViewModel: PerfilCreditoViewModel
**Archivo**: `ViewModels/PerfilCreditoViewModel.cs`

Incluye validaciones `[Range]` y `[Required]` para todos los campos numéricos.

#### ViewModel Modificado: ConfiguracionPagoViewModel
**Archivo**: `ViewModels/ConfiguracionPagoViewModel.cs`

Agregados 3 campos de defaults globales con validaciones.

### DbContext

**Archivo**: `Data/AppDbContext.cs`

Cambios:
1. Agregado `DbSet<PerfilCredito> PerfilesCredito`
2. Configuración de precisión decimal para campos de ConfiguracionPago
3. Nueva configuración completa de PerfilCredito en `OnModelCreating`:
   - Índice único en Nombre
   - Índice en Orden
   - Precisiones decimales configuradas

### AutoMapper

**Archivo**: `Helpers/AutoMapperProfile.cs`

Agregado mapeo bidireccional: `CreateMap<PerfilCredito, PerfilCreditoViewModel>().ReverseMap()`

### Controlador

**Archivo**: `Controllers/ConfiguracionPagoController.cs`

#### Dependencias Agregadas
- `IDbContextFactory<AppDbContext>`: Para acceso directo a PerfilesCredito
- `IMapper`: Para mapeo de entidades a ViewModels

#### Nuevos Endpoints

**1. GET GetPerfilesCredito**
```csharp
[HttpGet]
[AllowAnonymous]
public async Task<IActionResult> GetPerfilesCredito()
```
- Retorna todos los perfiles activos (no eliminados)
- Ordenados por Orden y Nombre
- Formato: `{ success: true, data: List<PerfilCreditoViewModel> }`

**2. POST GuardarCreditoPersonalModal**
```csharp
[HttpPost]
[IgnoreAntiforgeryToken]
public async Task<IActionResult> GuardarCreditoPersonalModal(
    [FromBody] CreditoPersonalConfigViewModel config)
```
- Recibe defaults globales y lista de perfiles
- Actualiza ConfiguracionPago (TipoPago = CreditoPersonal) con defaults globales
- Crea/actualiza perfiles de crédito
- Maneja transacción completa en una sola operación

#### ViewModels Auxiliares
```csharp
public class CreditoPersonalConfigViewModel
{
    public DefaultsGlobalesViewModel? DefaultsGlobales { get; set; }
    public List<PerfilCreditoViewModel>? Perfiles { get; set; }
}

public class DefaultsGlobalesViewModel
{
    public decimal TasaMensual { get; set; }
    public decimal GastosAdministrativos { get; set; }
    public int MinCuotas { get; set; }
    public int MaxCuotas { get; set; }
}
```

### JavaScript

**Archivo**: `wwwroot/js/venta-config-pago.js`

#### Variables Globales Agregadas
```javascript
let perfilesCredito = []; // Array de perfiles cargados
let defaultsGlobales = {}; // Objeto con defaults globales
```

#### Funciones Principales

**1. generarSeccionCreditoPersonal()**
- Genera HTML completo de la sección con card amarillo
- Incluye icono de wallet
- Contiene dos subsecciones: Defaults Globales y Perfiles

**2. generarDefaultsGlobales()**
- Card con borde azul (info)
- 4 inputs: Tasa, Gastos, Min Cuotas, Max Cuotas
- Badge indicando "Fallback"
- Textos de ayuda en cada campo

**3. generarPerfilesCredito()**
- Card con borde verde (success)
- Tabla responsiva con 8 columnas
- Botón "Nuevo Perfil" en el header
- Muestra badge con cantidad de perfiles
- Mensaje cuando no hay perfiles

**4. generarFilaPerfil(perfil, index)**
- Fila de tabla con datos del perfil
- Botones de editar y eliminar
- Badge de estado activo/inactivo
- Descripción como subtexto (si existe)

**5. agregarEventListenersCreditoPersonal()**
- Validación de rangos: Min Cuotas no puede ser > Max Cuotas
- Auto-ajuste cuando se violan límites

**6. agregarPerfilCredito()**
- Crea fila inline de edición en la tabla
- 7 campos editables: Nombre, Descripción, Tasa, Gastos, Min/Max Cuotas, Activo
- Botones de guardar y cancelar
- Validación para no tener múltiples filas de edición

**7. guardarNuevoPerfil()**
- Validaciones completas de todos los campos
- Verifica rangos numéricos (0-100% para tasa, 1-120 para cuotas)
- Verifica que Min <= Max
- Agrega perfil al array local
- Re-renderiza el formulario
- Notifica al usuario (recuerde guardar)

**8. cancelarNuevoPerfil()**
- Elimina fila de edición sin guardar

**9. eliminarPerfil(id)**
- Confirmación antes de eliminar
- Filtra array local
- Re-renderiza formulario
- Notifica al usuario

#### Modificaciones a Funciones Existentes

**cargarConfiguraciones()**
- Ahora carga también perfiles de crédito desde `/ConfiguracionPago/GetPerfilesCredito`
- Extrae defaults globales de ConfiguracionPago con TipoPago = 5
- Maneja errores si alguna carga falla

**renderizarFormulario()**
- Agrega llamada a `generarSeccionCreditoPersonal()` después de los tipos de pago
- Agrega llamada a `agregarEventListenersCreditoPersonal()`

**btnGuardarConfig (event listener)**
- Ahora guarda en dos etapas:
  1. Tipos de pago tradicionales (`/ConfiguracionPago/GuardarConfiguracionesModal`)
  2. Crédito Personal (`/ConfiguracionPago/GuardarCreditoPersonalModal`)
- Si cualquiera falla, muestra error y no cierra el modal
- Solo cierra modal si ambas operaciones son exitosas

### Vista

**Archivo**: `Views/Venta/Index.cshtml`

No se modificó. El modal existente (`#modalConfigPago`) se reutiliza. El contenido se carga dinámicamente via JavaScript en `#configPagoContent`.

## Flujo de Uso

### Escenario 1: Configurar Defaults Globales
1. Usuario hace clic en "Configurar recargos / descuentos" desde Venta/Index
2. Modal se abre y carga configuraciones existentes
3. Usuario scroll hasta la sección "Crédito Personal" (card amarillo)
4. Usuario edita campos en "Defaults Globales" (card azul)
   - Tasa Mensual Default: 7.50%
   - Gastos Admin Default: $500.00
   - Min Cuotas: 1
   - Max Cuotas: 24
5. Usuario hace clic en "Guardar Configuración"
6. Sistema guarda en ConfiguracionPago (TipoPago = CreditoPersonal)
7. Modal se cierra, notificación de éxito

### Escenario 2: Crear Perfil de Crédito
1. Usuario abre modal de configuración
2. Scroll a "Perfiles de Crédito" (card verde)
3. Usuario hace clic en "Nuevo Perfil"
4. Aparece fila de edición inline en la tabla
5. Usuario completa campos:
   - Nombre: "Estándar"
   - Descripción: "Para clientes con buen historial crediticio"
   - Tasa: 6.50%
   - Gastos: $300.00
   - Min Cuotas: 3
   - Max Cuotas: 36
   - Activo: ✓
6. Usuario hace clic en botón "✓" (guardar)
7. Validaciones JavaScript se ejecutan
8. Perfil se agrega al array local
9. Tabla se re-renderiza mostrando el nuevo perfil
10. Notificación: "Perfil 'Estándar' agregado (recuerde guardar la configuración)"
11. Usuario hace clic en "Guardar Configuración" (botón del modal)
12. Sistema persiste el perfil en tabla PerfilesCredito
13. Modal se cierra, notificación de éxito

### Escenario 3: Eliminar Perfil
1. Usuario abre modal de configuración
2. En tabla de perfiles, localiza el perfil a eliminar
3. Usuario hace clic en botón de eliminar (ícono basura)
4. Aparece confirmación: "¿Está seguro de eliminar este perfil de crédito?"
5. Usuario confirma
6. Perfil se elimina del array local
7. Tabla se re-renderiza sin el perfil
8. Notificación: "Perfil eliminado (recuerde guardar la configuración)"
9. Usuario guarda la configuración
10. Sistema marca perfil como IsDeleted en la base de datos

### Escenario 4: Validación de Rangos
1. Usuario intenta crear perfil con Min Cuotas = 10
2. Usuario intenta configurar Max Cuotas = 5
3. JavaScript detecta que Max < Min
4. Al hacer blur de Max Cuotas, automáticamente se ajusta Min Cuotas = 5
5. Notificación visual en el campo (o se puede agregar)

## Integración con TAREA 6

Esta TAREA 7 complementa la TAREA 6 (Configuración multi-fuente):

**TAREA 6**: Permite que un crédito use valores de:
- Global (sistema)
- Por Cliente (personalizado por cliente)
- Manual (operador ingresa en el momento)
- Por Plan (referencia a perfiles - **FUTURO**)

**TAREA 7**: Define qué son los valores "Global" y proporciona la infraestructura para "Por Plan":
- Los **Defaults Globales** de TAREA 7 son los valores que usa TAREA 6 cuando se elige fuente "Global"
- Los **Perfiles de Crédito** de TAREA 7 son la base para implementar fuente "Por Plan" en el futuro

### Próxima Integración (TAREA 8 - Futuro)
Para completar la integración:
1. Agregar campo `PerfilCreditoId` (FK nullable) a entidad `Cliente`
2. En ConfigurarVenta (GET), si existe `cliente.PerfilCreditoId`, cargar perfil y mostrar sus valores
3. Agregar selector de perfil en Cliente/Edit para asignar perfil predeterminado
4. Actualizar `FuenteConfiguracion.PorPlan` para usar perfil del cliente (o seleccionar perfil en el momento)

## Validaciones Implementadas

### Backend (C#)
- Rango de Tasa: 0% a 100%
- Rango de Gastos: $0 a $999,999.99
- Rango de Cuotas: 1 a 120
- Nombre de perfil requerido (max 100 caracteres)
- Descripción opcional (max 500 caracteres)
- Índice único en Nombre (no permite duplicados)

### Frontend (JavaScript)
- Campos numéricos con min/max
- Validación de rangos antes de guardar
- Verificación Min <= Max cuotas
- Auto-ajuste cuando se violan límites
- Validación de campos requeridos
- Mensajes de error descriptivos

## Observabilidad y Logs

### Logs en Controlador
```csharp
_logger.LogError(ex, "Error al obtener perfiles de crédito");
_logger.LogError(ex, "Error al guardar configuración de crédito personal");
```

### Notificaciones JavaScript
- **Success**: "Todas las configuraciones guardadas exitosamente"
- **Warning**: "Ya hay un perfil en edición. Guárdelo o cancele primero."
- **Danger**: "Error al guardar las configuraciones: [mensaje]"
- **Info**: "Perfil '[nombre]' agregado (recuerde guardar la configuración)"

## Mejoras Futuras Sugeridas

### TAREA 7.3: Edición Inline de Perfiles
- Implementar `editarPerfil(id)` similar a `agregarPerfilCredito()`
- Reemplazar fila con inputs editables
- Guardar cambios en array local

### TAREA 7.4: Reordenamiento con Drag & Drop
- Permitir arrastrar filas de perfiles para cambiar orden
- Actualizar campo `Orden` automáticamente
- Usar librería como SortableJS

### TAREA 7.5: Duplicar Perfil
- Botón para duplicar perfil existente
- Crear copia con nombre "Copia de [original]"
- Facilitar creación de variantes

### TAREA 7.6: Importar/Exportar Perfiles
- Exportar perfiles a JSON
- Importar perfiles desde archivo
- Útil para migrar entre ambientes

### TAREA 7.7: Preview de Cálculo
- Mostrar ejemplo de cuota calculada con valores del perfil
- Ayuda a entender impacto de cambios en tasas
- Monto de ejemplo configurable

### TAREA 7.8: Historial de Cambios
- Auditoría de cambios en perfiles
- Ver quién modificó qué y cuándo
- Tabla `HistorialPerfilesCredito`

## Comandos Importantes

### Aplicar Migración
```bash
dotnet ef migrations add AddPerfilesCreditoYDefaultsGlobales
dotnet ef database update
```

### Rollback (si necesario)
```bash
dotnet ef database update AddConfiguracionCreditoPersonalizadaCliente
dotnet ef migrations remove
```

### Compilar y Ejecutar
```bash
dotnet build
dotnet run
```

## Archivos Modificados/Creados

### Nuevos
- `Models/Entities/PerfilCredito.cs`
- `ViewModels/PerfilCreditoViewModel.cs`
- `Migrations/20260208233717_AddPerfilesCreditoYDefaultsGlobales.cs`

### Modificados
- `Models/Entities/ConfiguracionPago.cs` (+4 campos)
- `Data/AppDbContext.cs` (+DbSet, +configuración)
- `ViewModels/ConfiguracionPagoViewModel.cs` (+3 campos defaults)
- `Helpers/AutoMapperProfile.cs` (+mapeo PerfilCredito)
- `Controllers/ConfiguracionPagoController.cs`:
  - +2 dependencias (IDbContextFactory, IMapper)
  - +2 endpoints (GetPerfilesCredito, GuardarCreditoPersonalModal)
  - +2 ViewModels auxiliares
- `wwwroot/js/venta-config-pago.js`:
  - +2 variables globales
  - +13 funciones nuevas
  - Modificadas 3 funciones existentes

## Decisiones de Diseño

1. **Perfiles en tabla separada vs columnas en ConfiguracionPago**: 
   - Se optó por tabla separada para permitir N perfiles
   - Facilita agregar/modificar sin alterar estructura principal

2. **Defaults Globales en ConfiguracionPago**:
   - Reutiliza entidad existente (TipoPago = CreditoPersonal)
   - No requiere nueva entidad solo para 4 campos
   - Mantiene configuración de tipos de pago unificada

3. **Edición inline vs modal separado**:
   - Inline para perfiles (más rápido, menos clics)
   - Podría cambiar a modal para ediciones complejas

4. **Guardado en memoria vs guardado inmediato**:
   - Cambios se acumulan en arrays JavaScript
   - Un solo guardado al final (menos requests)
   - Usuario debe recordar guardar (se notifica)

5. **Orden de perfiles (campo vs posición)**:
   - Campo `Orden` persistido en DB
   - Permite reordenamiento sin cambiar IDs
   - Facilita futura implementación de drag & drop

6. **Validaciones duplicadas (backend + frontend)**:
   - Frontend para UX inmediata
   - Backend como seguridad y validación definitiva
   - Rangos consistentes en ambos lados

## Pruebas Sugeridas

### Pruebas Funcionales
1. ✅ Cargar modal sin configuración previa (debe mostrar valores por defecto)
2. ✅ Guardar defaults globales y verificar persistencia
3. ✅ Crear perfil nuevo y verificar en tabla
4. ✅ Editar perfil existente (cuando se implemente)
5. ✅ Eliminar perfil y confirmar que desaparece
6. ✅ Intentar crear perfil con nombre duplicado (debe fallar)
7. ✅ Validar rangos: tasa > 100%, cuotas > 120, etc.
8. ✅ Verificar que Min > Max se auto-corrige
9. ✅ Cerrar modal sin guardar y reabrir (cambios no persisten)
10. ✅ Guardar y recargar página (cambios persisten)

### Pruebas de Integración
1. Verificar que defaults globales se usan en TAREA 6 cuando fuente = Global
2. Crear perfil, asignarlo a cliente, verificar que se sugiere en ConfigurarVenta
3. Modificar tasa en perfil, verificar que créditos nuevos usan nueva tasa
4. Desactivar perfil, verificar que no aparece en selectores (futuro)

### Pruebas de Regresión
1. Configuraciones de tipos de pago siguen funcionando igual
2. Modal de tarjetas de crédito no se afectó
3. Guardado de tipos de pago no interfiere con guardado de crédito personal

## Resumen de Campos

| Campo | Tipo | Rango | Defaults | Perfiles | Requerido |
|-------|------|-------|----------|----------|-----------|
| Tasa Mensual (%) | decimal(8,4) | 0-100 | ✅ | ✅ | Perfiles: Sí |
| Gastos Admin ($) | decimal(18,2) | 0-999999.99 | ✅ | ✅ | Perfiles: Sí |
| Min Cuotas | int | 1-120 | ✅ | ✅ | Perfiles: Sí |
| Max Cuotas | int | 1-120 | ✅ | ✅ | Perfiles: Sí |
| Nombre | string(100) | - | ❌ | ✅ | Sí |
| Descripción | string(500) | - | ❌ | ✅ | No |
| Activo | bool | - | ❌ | ✅ | Sí |
| Orden | int | - | ❌ | ✅ | Sí |

---

**Autor**: TheBuryProject Development Team  
**Fecha**: Febrero 2026  
**Versión**: 1.0  
**Estado**: Implementación completa funcional
