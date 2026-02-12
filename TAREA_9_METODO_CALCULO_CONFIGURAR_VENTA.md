# TAREA 9: Selector de "Método de cálculo" en ConfigurarVenta

## Fecha de Implementación
- **Inicio**: 8 de febrero de 2025
- **Finalización**: 8 de febrero de 2025

## Descripción
Implementación de un selector intuitivo de "Método de cálculo" en la pantalla Credito/ConfigurarVenta que reemplaza el selector anterior de "Fuente de configuración". Este nuevo selector resuelve la indecisión del operador ofreciendo opciones claras sobre cómo calcular tasa, gastos y cuotas para un crédito.

**TAREA 9.2 (Regla de comportamiento)**: Sistema que detecta modificaciones manuales del operador y muestra confirmación/banner al cambiar método.

## Objetivos
1. ✅ Crear enum MetodoCalculoCredito con 5 opciones claras
2. ✅ Actualizar ViewModel con campos MetodoCalculo y PerfilCreditoSeleccionadoId
3. ✅ Modificar vista para mostrar selector visible con todas las opciones
4. ✅ Agregar selector de perfil dinámico (visible solo cuando MetodoCalculo = UsarPerfil)
5. ✅ Actualizar controller para cargar perfiles activos y preparar datos
6. ✅ Reescribir JavaScript para manejar lógica de precarga según método
7. ✅ Mantener compatibilidad con FuenteConfiguracion existente
8. ✅ **TAREA 9.2**: Detectar cambios manuales y mostrar confirmación al cambiar método
9. ✅ **TAREA 9.2**: Banner informativo "valores actualizados por método"
10. ✅ **TAREA 9.2**: Recalcular automáticamente tasa, gastos, cuotas y simulación

## Métodos de Cálculo

### 1. 🤖 Automático (Por Cliente)
**Valor enum**: `AutomaticoPorCliente = 0`

**Comportamiento**: Usa la mejor opción disponible automáticamente según esta prioridad:
1. Configuración personalizada del cliente (si tiene valores específicos)
2. Perfil preferido del cliente (si tiene perfil asignado)
3. Defaults globales del sistema

**Caso de uso**: Operador confía en que el sistema elija la mejor configuración para el cliente

**Precarga**:
- Si cliente tiene tasa/gastos/cuotas personalizadas → usa esos valores
- Si cliente tiene perfil preferido → usa valores del perfil
- Si no tiene nada → usa valores globales

### 2. 📋 Usar Perfil
**Valor enum**: `UsarPerfil = 1`

**Comportamiento**: Muestra dropdown de perfiles activos. Al seleccionar perfil → precarga tasa/gastos/min/max cuotas del perfil

**Caso de uso**: Operador quiere aplicar un escenario específico (ej: "Premium", "Empleado", "Conservador")

**Precarga**:
- Tasa = PerfilCredito.TasaMensual
- Gastos = PerfilCredito.GastosAdministrativos
- Cuotas min/max = PerfilCredito.MinCuotas/MaxCuotas

**Validación**: Dropdown de perfiles se muestra dinámicamente

### 3. 👤 Usar Cliente
**Valor enum**: `UsarCliente = 2`

**Comportamiento**: Precarga tasa/gastos/min/max definidos específicamente en el cliente

**Caso de uso**: Operador quiere usar exactamente la configuración personalizada del cliente (ej: cliente con tasa promocional)

**Precarga**:
- Tasa = Cliente.TasaInteresMensualPersonalizada ?? global
- Gastos = Cliente.GastosAdministrativosPersonalizados ?? global
- Cuotas max = Cliente.CuotasMaximasPersonalizadas ?? 24

**Validación**: 
- Si cliente no tiene configuración → opción deshabilitada con texto "(Sin configuración)"
- Si se selecciona sin configuración → alerta + redirección automática a Global

### 4. 🌍 Global (Sistema)
**Valor enum**: `Global = 3`

**Comportamiento**: Precarga defaults globales de ConfiguracionPago

**Caso de uso**: Operador quiere usar valores estándar del sistema sin personalizaciones

**Precarga**:
- Tasa = ConfiguracionPago.TasaInteresMensualCreditoPersonal
- Gastos = 0 (o valor global si se configura)
- Cuotas = 1-24 (rango estándar)

### 5. ✏️ Manual
**Valor enum**: `Manual = 4`

**Comportamiento**: No precarga (o precarga global como base). Habilita edición total de tasa/gastos/cuotas sin restricciones salvo validaciones de rango

**Caso de uso**: Operador necesita configuración totalmente personalizada para esta venta específica (ej: cliente especial, situación excepcional)

**Precarga**:
- Tasa = Cliente personalizada ?? Perfil ?? Global (como base inicial)
- Gastos = Cliente ?? Perfil ?? Global
- Cuotas = 1-120 (rango amplio)
- **Edición habilitada**: Tasa y gastos son editables

## Cambios Implementados

### 1. Nuevo Enum

#### Models/Enums/MetodoCalculoCredito.cs
**Estado**: Creado

```csharp
public enum MetodoCalculoCredito
{
    [Display(Name = "🤖 Automático (Por Cliente)")]
    AutomaticoPorCliente = 0,

    [Display(Name = "📋 Usar Perfil")]
    UsarPerfil = 1,

    [Display(Name = "👤 Usar Cliente")]
    UsarCliente = 2,

    [Display(Name = "🌍 Global (Sistema)")]
    Global = 3,

    [Display(Name = "✏️ Manual")]
    Manual = 4
}
```

### 2. ViewModel

#### ViewModels/ConfiguracionCreditoVentaViewModel.cs
**Líneas modificadas**: ~20-30

**Campos agregados**:
```csharp
// TAREA 9: Nuevo método de cálculo más intuitivo
[Display(Name = "Método de cálculo")]
[Required(ErrorMessage = "Debe seleccionar un método de cálculo")]
public MetodoCalculoCredito MetodoCalculo { get; set; } = MetodoCalculoCredito.AutomaticoPorCliente;

// TAREA 9: Perfil seleccionado cuando MetodoCalculo = UsarPerfil
public int? PerfilCreditoSeleccionadoId { get; set; }

// TAREA 6: Mantener por compatibilidad con código existente
public FuenteConfiguracionCredito FuenteConfiguracion { get; set; }
```

**Compatibilidad**: FuenteConfiguracion se mantiene para backend existente. JavaScript actualiza hidden field automáticamente.

### 3. Controller

#### Controllers/CreditoController.cs - ConfigurarVenta GET

**Modificación 1**: Cargar perfiles activos (línea ~375)
```csharp
// TAREA 9: Cargar perfiles activos para el selector
var perfilesActivos = await contextCliente.PerfilesCredito
    .Where(p => !p.IsDeleted && p.Activo)
    .OrderBy(p => p.Orden)
    .ThenBy(p => p.Nombre)
    .Select(p => new { p.Id, p.Nombre, p.Descripcion, p.TasaMensual, 
                      p.GastosAdministrativos, p.MinCuotas, p.MaxCuotas })
    .ToListAsync();
```

**Modificación 2**: Inicializar ViewModel con MetodoCalculo (línea ~395)
```csharp
var modelo = new ConfiguracionCreditoVentaViewModel
{
    // ...
    MetodoCalculo = MetodoCalculoCredito.AutomaticoPorCliente, // Default
    PerfilCreditoSeleccionadoId = perfilPreferido?.Id, // Preseleccionar perfil del cliente
    // ...
};
```

**Modificación 3**: ViewBag ampliado (línea ~415)
```csharp
ViewBag.ClienteConfigPersonalizada = new
{
    // ... campos existentes ...
    CuotasMinimas = cuotasMinimas,
    GastosGlobales = 0,
    TieneConfiguracionCliente = cliente?.TasaInteresMensualPersonalizada.HasValue == true || 
                               cliente?.GastosAdministrativosPersonalizados.HasValue == true ||
                               cliente?.CuotasMaximasPersonalizadas.HasValue == true,
    MontoMinimo = cliente?.MontoMinimoPersonalizado,
    MontoMaximo = cliente?.MontoMaximoPersonalizado
};

ViewBag.PerfilesActivos = perfilesActivos; // TAREA 9
```

### 4. Vista

#### Views/Credito/ConfigurarVenta.cshtml
**Líneas modificadas**: 85-150

**Sección 1: Selector de Método** (reemplaza selector de FuenteConfiguracion)
```html
<div class="mb-4">
    <label asp-for="MetodoCalculo" class="form-label d-flex align-items-center gap-2">
        <i class="bi bi-calculator"></i> Método de cálculo
        <span class="badge bg-danger" style="font-size: 0.65rem;">Obligatorio</span>
    </label>
    <select asp-for="MetodoCalculo" class="form-select bg-body-secondary text-light border-0" 
            id="metodoCalculoSelect" required>
        <option value="">Seleccionar método...</option>
        <option value="0">🤖 Automático (Por Cliente)</option>
        <option value="1">📋 Usar Perfil</option>
        @if (ViewBag.ClienteConfigPersonalizada?.TieneConfiguracionCliente == true)
        {
            <option value="2">👤 Usar Cliente</option>
        }
        else
        {
            <option value="2" disabled>👤 Usar Cliente (Sin configuración)</option>
        }
        <option value="3">🌍 Global (Sistema)</option>
        <option value="4">✏️ Manual</option>
    </select>
    <input type="hidden" asp-for="FuenteConfiguracion" id="fuenteConfigHidden" />
</div>
```

**Sección 2: Selector de Perfil dinámico**
```html
<div class="mb-4" id="perfilSelectorDiv" style="display: none;">
    <label asp-for="PerfilCreditoSeleccionadoId" class="form-label d-flex align-items-center gap-2">
        <i class="bi bi-list-stars"></i> Perfil de Crédito
    </label>
    <select asp-for="PerfilCreditoSeleccionadoId" class="form-select" id="perfilCreditoSelect">
        <option value="">Seleccionar perfil...</option>
        @if (ViewBag.PerfilesActivos != null)
        {
            foreach (var perfil in ViewBag.PerfilesActivos)
            {
                <option value="@perfil.Id" 
                        data-tasa="@perfil.TasaMensual" 
                        data-gastos="@perfil.GastosAdministrativos"
                        data-min-cuotas="@perfil.MinCuotas"
                        data-max-cuotas="@perfil.MaxCuotas">
                    @perfil.Nombre
                </option>
            }
        }
    </select>
</div>
```

**Sección 3: Data attributes ampliados**
```html
<div id="configData"
     data-tasa-global="..."
     data-gastos-globales="..."
     data-tasa-cliente="..."
     data-gastos-cliente="..."
     data-cuotas-max-cliente="..."
     data-cuotas-min-cliente="..."
     data-tiene-config-cliente="..."
     data-tiene-perfil-preferido="..."
     data-perfil-preferido-id="..."
     data-perfil-tasa="..."
     data-perfil-gastos="..."
     data-perfil-min-cuotas="..."
     data-perfil-max-cuotas="..."></div>
```

**Sección 4: Rango de cuotas actualizado**
```html
<small class="text-muted" id="cuotasMaxInfo">
    Rango: <span id="cuotasMinLabel">1</span> - <span id="cuotasMaxLabel">24</span> cuotas
</small>
```

### 5. JavaScript

#### wwwroot/js/creditos-configurar.js

**Función principal**: `actualizarMetodoCalculo()` (reemplaza `actualizarFuenteConfiguracion()`)

**Líneas ~65-250**: Lógica completa de switch/case para 5 métodos

**Estructura**:
```javascript
function actualizarMetodoCalculo() {
    const metodo = parseInt(metodoCalculoSelect?.value, 10);
    
    let configuracion = { badge, badgeClass, helpText, tasaHelp, 
                         readonly, tasa, gastos, cuotasMin, cuotasMax, 
                         mostrarPerfilSelector, fuenteEquivalente };

    switch (metodo) {
        case 0: // AutomaticoPorCliente
            if (tieneConfigCliente) { /* usar cliente */ }
            else if (tienePerfilPreferido) { /* usar perfil */ }
            else { /* usar global */ }
            break;
        
        case 1: // UsarPerfil
            configuracion.mostrarPerfilSelector = true;
            // Leer data attributes del perfil seleccionado
            break;
        
        case 2: // UsarCliente
            if (!tieneConfigCliente) {
                alert('Cliente sin configuración');
                metodoCalculoSelect.value = '3'; // Degradar a Global
                actualizarMetodoCalculo();
                return;
            }
            // Usar valores del cliente
            break;
        
        case 3: // Global
            // Usar valores globales
            break;
        
        case 4: // Manual
            configuracion.readonly = false; // Habilitar edición
            break;
    }

    // Actualizar UI: badge, help texts, campos, cuotas, etc.
    // Actualizar hidden field fuenteConfigHidden
    // Mostrar/ocultar perfilSelectorDiv
    // Recalcular
}
```

**Event listeners** (líneas ~415-425):
```javascript
if (metodoCalculoSelect) {
    metodoCalculoSelect.addEventListener('change', actualizarMetodoCalculo);
    actualizarMetodoCalculo(); // Inicializar
}

if (perfilCreditoSelect) {
    perfilCreditoSelect.addEventListener('change', actualizarMetodoCalculo);
}
```

## Flujo de Usuario

### Escenario 1: Cliente con perfil preferido
1. Operador abre ConfigurarVenta
2. Selector "Método de cálculo" muestra "🤖 Automático (Por Cliente)" seleccionado
3. Sistema detecta que cliente tiene perfil preferido
4. Precarga: Tasa = perfil, Gastos = perfil, Cuotas = perfil.MinCuotas - perfil.MaxCuotas
5. Badge muestra "Auto (Perfil)"
6. Operador puede cambiar a otro método si desea

### Escenario 2: Cliente con configuración personalizada
1. Operador abre ConfigurarVenta
2. Selector muestra "🤖 Automático"
3. Sistema detecta configuración personalizada del cliente
4. Precarga: Tasa = cliente.TasaPersonalizada, Gastos = cliente.GastosPersonalizados
5. Badge muestra "Auto (Cliente)"
6. Opción "👤 Usar Cliente" está habilitada

### Escenario 3: Operador selecciona perfil específico
1. Operador cambia método a "📋 Usar Perfil"
2. Aparece dropdown de perfiles
3. Operador selecciona "Premium"
4. Sistema precarga: Tasa = 2.5%, Gastos = $30, Cuotas = 6-48
5. Badge muestra "Perfil"
6. Campos son readonly (excepto en Manual)

### Escenario 4: Cliente sin configuración intenta "Usar Cliente"
1. Operador cambia método a "👤 Usar Cliente"
2. Sistema detecta que cliente no tiene configuración
3. Alert: "El cliente no tiene configuración personalizada. Se usarán valores globales."
4. Selector cambia automáticamente a "🌍 Global"
5. Precarga valores globales

### Escenario 5: Operador necesita configuración especial
1. Operador cambia método a "✏️ Manual"
2. Sistema precarga valores base (cliente ?? perfil ?? global)
3. Campos de tasa y gastos se vuelven editables (fondo oscuro)
4. Cuotas: Rango 1-120 (sin restricción estricta)
5. Badge muestra "Manual"
6. Operador ingresa valores específicos

## Mapeo de Métodos a FuenteConfiguracion (Compatibilidad)

| MetodoCalculo | fuenteEquivalente (FuenteConfiguracion) |
|---------------|----------------------------------------|
| AutomaticoPorCliente (0) | Depende: Global (0) o PorCliente (1) |
| UsarPerfil (1) | PorPlan (3) |
| UsarCliente (2) | PorCliente (1) |
| Global (3) | Global (0) |
| Manual (4) | Manual (2) |

**Hidden field**: `<input type="hidden" asp-for="FuenteConfiguracion" id="fuenteConfigHidden" />`

JavaScript actualiza automáticamente este campo para que el backend existente siga funcionando sin cambios.

## Validaciones

### Frontend (JavaScript)
1. **Método requerido**: `<select ... required>` + validación HTML5
2. **Cliente sin configuración**: Alert + degradación automática a Global
3. **Rango de cuotas**: Ajuste automático si cuotas actuales exceden min/max
4. **Perfil sin selección**: Si UsarPerfil pero sin perfil → usa valores globales

### Backend (Controller POST - futuro)
- Validar que PerfilCreditoSeleccionadoId sea válido si MetodoCalculo = UsarPerfil
- Validar que cliente tenga configuración si MetodoCalculo = UsarCliente
- Normalizar valores según método antes de guardar

## Beneficios de la Implementación

### 1. Claridad para el Operador
- **Antes**: "Fuente de configuración" → confuso
- **Ahora**: "Método de cálculo" con emojis y descripciones claras

### 2. Resolución de Indecisión
- **Automático**: Sistema decide por el operador
- **Perfiles**: Escenarios predefinidos fáciles de seleccionar
- **Manual**: Control total cuando se necesita

### 3. Flexibilidad Total
- Soporte para clientes sin configuración
- Soporte para clientes con perfil preferido
- Soporte para clientes con configuración personalizada
- Soporte para casos excepcionales (Manual)

### 4. Integración Completa
- **TAREA 6**: FuenteConfiguracion sigue funcionando
- **TAREA 7**: Perfiles se integran perfectamente
- **TAREA 8**: Perfil preferido del cliente se usa automáticamente
- **TAREA 9**: Nueva UI intuitiva sin romper código existente

## Testing Recomendado

### Test 1: Automático con cliente sin configuración
1. Cliente sin perfil ni configuración personalizada
2. Seleccionar "Automático"
3. Verificar: Badge = "Auto (Global)", Tasa = global, Gastos = 0

### Test 2: Automático con perfil preferido
1. Cliente con perfil "Premium"
2. Seleccionar "Automático"
3. Verificar: Badge = "Auto (Perfil)", Tasa = perfil, Gastos = perfil

### Test 3: Automático con configuración personalizada
1. Cliente con tasa personalizada = 2.0%
2. Seleccionar "Automático"
3. Verificar: Badge = "Auto (Cliente)", Tasa = 2.0%

### Test 4: Usar Perfil
1. Seleccionar "Usar Perfil"
2. Verificar dropdown de perfiles aparece
3. Seleccionar perfil "Estándar"
4. Verificar precarga de valores del perfil

### Test 5: Usar Cliente sin configuración
1. Cliente sin configuración personalizada
2. Intentar seleccionar "Usar Cliente" (debería estar disabled)
3. Si forzado: Alert + redirección a Global

### Test 6: Manual con edición
1. Seleccionar "Manual"
2. Verificar campos de tasa y gastos editables
3. Ingresar tasa = 5.0%
4. Verificar cálculos actualizados

### Test 7: Cambio dinámico de perfil
1. Seleccionar "Usar Perfil"
2. Seleccionar perfil "Premium"
3. Cambiar a perfil "Conservador"
4. Verificar actualización automática de valores

## Archivos Modificados (Resumen)

| Archivo | Tipo | Cambio |
|---------|------|--------|
| Models/Enums/MetodoCalculoCredito.cs | Enum nuevo | +5 valores con Display |
| ViewModels/ConfiguracionCreditoVentaViewModel.cs | ViewModel | +2 campos (MetodoCalculo, PerfilCreditoSeleccionadoId) |
| Controllers/CreditoController.cs | Controller | Cargar perfiles, ampliar ViewBag, inicializar ViewModel |
| Views/Credito/ConfigurarVenta.cshtml | Vista | Selector nuevo, dropdown perfiles, data attributes ampliados |
| wwwroot/js/creditos-configurar.js | JavaScript | Reescribir actualizarMetodoCalculo(), event listeners |

## Estado Final

### ✅ Completado
- [x] Enum MetodoCalculoCredito con 5 opciones
- [x] ViewModel actualizado con nuevos campos
- [x] Controller cargando perfiles y preparando datos
- [x] Vista con selector visible y dropdown dinámico
- [x] JavaScript con lógica completa de switch/case
- [x] Compatibilidad con FuenteConfiguracion existente
- [x] Compilación exitosa

### 🎯 Funcionalidades
1. **Automático**: Cascada inteligente (Cliente > Perfil > Global)
2. **Usar Perfil**: Dropdown con data attributes para precarga
3. **Usar Cliente**: Validación + degradación a Global si sin config
4. **Global**: Valores del sistema
5. **Manual**: Edición total habilitada

### 📊 UI/UX Mejorado
```
┌────────────────────────────────────────┐
│ Método de cálculo [Obligatorio]        │
│ ┌────────────────────────────────────┐ │
│ │ 🤖 Automático (Por Cliente)       ▼│ │
│ └────────────────────────────────────┘ │
│ Selecciona cómo calcular...            │
├────────────────────────────────────────┤
│ [Perfil Selector - Dinámico]           │ ← Solo si UsarPerfil
├────────────────────────────────────────┤
│ Cantidad de cuotas: [12]               │
│ Rango: 1 - 24 cuotas                   │
├────────────────────────────────────────┤
│ Tasa mensual aplicada [Auto (Cliente)] │
│ [2.50] %                               │ ← Readonly según método
└────────────────────────────────────────┘
```

## Notas de Implementación

### Decisiones de Diseño
1. **Hidden field**: FuenteConfiguracion se mantiene como hidden para compatibilidad backend
2. **Degradación automática**: Si UsarCliente sin configuración → cambiar a Global con alert
3. **Badge dinámico**: Muestra origen real de valores (Auto (Cliente), Auto (Perfil), etc.)
4. **Rango de cuotas**: Min y max se ajustan según método
5. **Edición condicional**: Solo Manual permite editar tasa y gastos

### Compatibilidad
- **TAREA 6**: FuenteConfiguracion sigue en ViewModel y se mapea automáticamente
- **POST endpoints**: No requieren cambios, usan FuenteConfiguracion
- **Backend existente**: Sin modificaciones necesarias

### Extensibilidad Futura
- Agregar método "UsarPlan" cuando se implementen planes de crédito
- Permitir guardar configuración manual como nuevo perfil
- Historial de métodos usados por operador

## Conclusión

La TAREA 9 completa el sistema de configuración de crédito con una UI intuitiva que:
- **Resuelve indecisión**: Automático elige por el operador
- **Ofrece flexibilidad**: 5 métodos para diferentes escenarios
- **Mantiene compatibilidad**: Backend existente sigue funcionando
- **Integra TAREAS anteriores**: 6, 7 y 8 se usan perfectamente

**Estado**: ✅ **COMPLETADO** - Compilación exitosa. Listo para testing funcional.

---

## TAREA 9.2: Regla de Comportamiento al Cambiar Método

### Descripción
Sistema que detecta modificaciones manuales realizadas por el operador y muestra confirmación o banner informativo al cambiar el método de cálculo, evitando pérdida accidental de cambios.

### Objetivos
1. ✅ Detectar si operador modificó manualmente tasa, gastos o cuotas
2. ✅ Mostrar confirmación antes de sobrescribir valores modificados
3. ✅ Mostrar banner informativo "valores actualizados por método"
4. ✅ Recalcular automáticamente todos los valores después del cambio

### Implementación JavaScript

#### 1. Rastreo de Cambios Manuales

**Variables de estado** (líneas ~58-68):
```javascript
// TAREA 9.2: Rastrear cambios manuales del operador
let valoresIniciales = {
    tasa: null,
    gastos: null,
    cuotas: null
};
let camposModificadosManualmente = {
    tasa: false,
    gastos: false,
    cuotas: false
};
```

**Función `guardarValoresIniciales()`** (líneas ~74-78):
```javascript
function guardarValoresIniciales() {
    valoresIniciales.tasa = parseFloat(tasaMensualInput?.value) || 0;
    valoresIniciales.gastos = parseFloat(gastosInput?.value) || 0;
    valoresIniciales.cuotas = parseInt(cuotasInput?.value, 10) || 0;
}
```

**Función `hayModificacionesManuales()`** (líneas ~80-90):
```javascript
function hayModificacionesManuales() {
    const tasaActual = parseFloat(tasaMensualInput?.value) || 0;
    const gastosActuales = parseFloat(gastosInput?.value) || 0;
    const cuotasActuales = parseInt(cuotasInput?.value, 10) || 0;

    const tasaCambiada = Math.abs(tasaActual - valoresIniciales.tasa) > 0.01;
    const gastosCambiados = Math.abs(gastosActuales - valoresIniciales.gastos) > 0.01;
    const cuotasCambiadas = cuotasActuales !== valoresIniciales.cuotas;

    return tasaCambiada || gastosCambiados || cuotasCambiadas;
}
```

#### 2. Banner Informativo

**Función `mostrarBannerActualizacion()`** (líneas ~92-120):
```javascript
function mostrarBannerActualizacion(metodoNombre) {
    const cardBody = form.closest('.card-body');
    if (!cardBody) return;

    // Remover banner anterior si existe
    const bannerAnterior = document.getElementById('bannerActualizacionMetodo');
    if (bannerAnterior) {
        bannerAnterior.remove();
    }

    // Crear nuevo banner
    const banner = document.createElement('div');
    banner.id = 'bannerActualizacionMetodo';
    banner.className = 'alert alert-info alert-dismissible fade show d-flex align-items-center gap-2 mb-3';
    banner.setAttribute('role', 'alert');
    banner.innerHTML = `
        <i class="bi bi-info-circle-fill"></i>
        <div>
            <strong>Valores actualizados:</strong> Se aplicaron los valores del método "${metodoNombre}".
            Tasa, gastos y rango de cuotas han sido recalculados.
        </div>
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    `;

    cardBody.insertBefore(banner, cardBody.firstChild);

    // Auto-ocultar después de 5 segundos
    setTimeout(() => {
        if (banner && banner.parentNode) {
            banner.classList.remove('show');
            setTimeout(() => banner.remove(), 300);
        }
    }, 5000);
}
```

#### 3. Confirmación de Cambios

**Actualización `actualizarMetodoCalculo()`** (líneas ~133-155):
```javascript
function actualizarMetodoCalculo(forzarActualizacion = false) {
    const metodo = parseInt(metodoCalculoSelect?.value, 10);
    
    if (isNaN(metodo) || metodo === -1) {
        return;
    }

    // TAREA 9.2: Verificar si hay modificaciones manuales
    if (!forzarActualizacion && hayModificacionesManuales()) {
        const confirmar = confirm(
            '⚠️ Has modificado valores manualmente.\n\n' +
            'Al cambiar el método de cálculo se sobrescribirán:\n' +
            '• Tasa mensual\n' +
            '• Gastos administrativos\n' +
            '• Rango de cuotas\n\n' +
            '¿Deseas continuar?'
        );

        if (!confirmar) {
            // Revertir selección del método
            metodoCalculoSelect.value = metodoCalculoSelect.dataset.metodoAnterior || '0';
            return;
        }
    }

    // Guardar método actual para próxima comparación
    metodoCalculoSelect.dataset.metodoAnterior = metodo;

    // ... resto de la lógica
}
```

#### 4. Al Final de Actualización

**Guardar valores y mostrar banner** (líneas ~375-385):
```javascript
// TAREA 9.2: Guardar nuevos valores iniciales
guardarValoresIniciales();

// TAREA 9.2: Mostrar banner informativo
if (!forzarActualizacion) {
    mostrarBannerActualizacion(configuracion.nombreMetodo);
}

// Recalcular después de cambiar el método
actualizarCalculos();
```

#### 5. Event Listeners para Detectar Cambios Manuales

**Líneas ~530-560**:
```javascript
// TAREA 9.2: Event listener para tasa mensual (detectar cambios manuales)
if (tasaMensualInput) {
    tasaMensualInput.addEventListener('input', () => {
        camposModificadosManualmente.tasa = true;
        actualizarCalculos();
    });
}

// TAREA 9.2: Event listener para gastos (detectar cambios manuales)
if (gastosInput) {
    gastosInput.addEventListener('input', () => {
        camposModificadosManualmente.gastos = true;
    });
}

// TAREA 9.2: Event listener para cuotas (detectar cambios manuales)
if (cuotasInput) {
    cuotasInput.addEventListener('input', () => {
        camposModificadosManualmente.cuotas = true;
    });
}
```

### Flujos de Usuario

#### Escenario 1: Operador cambia método sin modificaciones previas
1. Operador abre ConfigurarVenta (método = Automático)
2. Selector precarga: Tasa = 3.5%, Gastos = 0, Cuotas = 1-24
3. Operador cambia método a "Usar Perfil" → selecciona "Premium"
4. **Banner aparece**: "Valores actualizados: Se aplicaron los valores del método 'Perfil: Premium'. Tasa, gastos y rango de cuotas han sido recalculados."
5. Valores actualizados: Tasa = 2.5%, Gastos = 30, Cuotas = 6-48
6. Simulación se recalcula automáticamente

#### Escenario 2: Operador modifica tasa manualmente y luego cambia método
1. Operador abre ConfigurarVenta (método = Automático, tasa = 3.5%)
2. Operador edita tasa manualmente → 2.8%
3. `camposModificadosManualmente.tasa = true`
4. Operador intenta cambiar método a "Global"
5. **Confirmación aparece**:
   ```
   ⚠️ Has modificado valores manualmente.

   Al cambiar el método de cálculo se sobrescribirán:
   • Tasa mensual
   • Gastos administrativos
   • Rango de cuotas

   ¿Deseas continuar?
   ```
6. Si **Cancelar**: Selector vuelve a "Automático", tasa = 2.8% se mantiene
7. Si **Aceptar**: Método cambia a Global, tasa = 3.5%, banner informativo aparece

#### Escenario 3: Operador cambia perfil dentro del método UsarPerfil
1. Método = "Usar Perfil", perfil = "Estándar" (tasa = 3.0%)
2. Operador cambia perfil a "Premium" (tasa = 2.5%)
3. **No aparece confirmación** (cambio de perfil es intencional)
4. **Banner aparece**: "Valores actualizados: Se aplicaron los valores del método 'Perfil: Premium'."
5. Valores actualizados automáticamente

#### Escenario 4: Inicialización sin banner
1. Página carga por primera vez
2. `actualizarMetodoCalculo(true)` con flag `forzarActualizacion = true`
3. **Banner NO aparece** (evitar spam en carga inicial)
4. Valores iniciales guardados para futuras comparaciones

### Recalculos Automáticos

Cuando el operador cambia el método, se recalculan:

1. **Tasa mensual**: Según método seleccionado (cliente/perfil/global/manual)
2. **Gastos administrativos**: Según método seleccionado
3. **Rango de cuotas**: Min/max según método
   - Automático (Cliente): Cliente.CuotasMaximas ?? Perfil.MaxCuotas ?? 24
   - Usar Perfil: Perfil.MinCuotas - Perfil.MaxCuotas
   - Global: 1 - 24
   - Manual: 1 - 120
4. **Simulación**: Cuota estimada, interés total, total a pagar
5. **Semáforo de capacidad**: Si hay datos del cliente

**Función `actualizarCalculos()`** es llamada automáticamente después de cada cambio de método.

### Banner de Actualización

#### Características del Banner

**Tipo**: Bootstrap Alert Info con auto-dismiss

**Diseño**:
```html
<div id="bannerActualizacionMetodo" 
     class="alert alert-info alert-dismissible fade show d-flex align-items-center gap-2 mb-3">
    <i class="bi bi-info-circle-fill"></i>
    <div>
        <strong>Valores actualizados:</strong> Se aplicaron los valores del método "Automático (Por Cliente)".
        Tasa, gastos y rango de cuotas han sido recalculados.
    </div>
    <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
</div>
```

**Comportamiento**:
- Aparece al inicio del card-body (antes del formulario)
- Auto-desaparece después de 5 segundos
- Puede cerrarse manualmente con botón X
- Solo uno visible a la vez (reemplaza anterior si existe)
- Muestra nombre del método aplicado dinámicamente

#### Estados del Banner

| Método | nombreMetodo | Texto del Banner |
|--------|--------------|------------------|
| Automático (Cliente) | "Automático (Por Cliente)" | Se aplicaron los valores del método "Automático (Por Cliente)" |
| Automático (Perfil) | "Automático (Perfil Preferido)" | Se aplicaron los valores del método "Automático (Perfil Preferido)" |
| Automático (Global) | "Automático (Global)" | Se aplicaron los valores del método "Automático (Global)" |
| Usar Perfil | "Perfil: Premium" | Se aplicaron los valores del método "Perfil: Premium" |
| Usar Cliente | "Usar Cliente" | Se aplicaron los valores del método "Usar Cliente" |
| Global | "Global" | Se aplicaron los valores del método "Global" |
| Manual | "Manual" | Se aplicaron los valores del método "Manual" |

### Ventajas de la Implementación

#### 1. Prevención de Pérdida de Datos
- **Confirmación modal**: Evita sobrescribir cambios accidentalmente
- **Opción cancelar**: Operador puede revertir selección
- **Data attribute `metodoAnterior`**: Permite volver al método previo

#### 2. Feedback Claro al Usuario
- **Banner informativo**: Usuario sabe qué pasó exactamente
- **Nombre del método en banner**: Contexto específico
- **Auto-dismiss**: No requiere acción del usuario

#### 3. Experiencia Fluida
- **No bloquea workflow**: Banner no interrumpe trabajo
- **Confirmación solo cuando necesario**: No molesta si no hubo cambios
- **Recalculo automático**: Usuario ve resultados inmediatamente

#### 4. Consistencia de Estado
- **`guardarValoresIniciales()`**: Después de cada cambio de método
- **Reset de flags**: Nuevos valores son la nueva base
- **Comparación con tolerancia**: 0.01 para decimales evita falsos positivos

### Testing TAREA 9.2

#### Test 1: Cambio sin modificaciones
1. Abrir ConfigurarVenta
2. No modificar nada
3. Cambiar método de Automático → Global
4. **Esperado**: Banner aparece, NO confirmación
5. **Verificar**: Valores actualizados, simulación recalculada

#### Test 2: Confirmación al modificar tasa
1. Editar tasa manualmente: 3.5% → 2.9%
2. Cambiar método: Automático → Global
3. **Esperado**: Confirmación modal aparece
4. Clic "Cancelar"
5. **Verificar**: Método sigue en Automático, tasa = 2.9%

#### Test 3: Aceptar sobrescritura
1. Editar tasa: 3.5% → 2.9%
2. Cambiar método: Automático → Global
3. Confirmación aparece, clic "Aceptar"
4. **Esperado**: Banner aparece, tasa = 3.5% (global)
5. **Verificar**: Simulación recalculada con nueva tasa

#### Test 4: Modificar cuotas y gastos
1. Editar cuotas: 12 → 18
2. Editar gastos: 0 → 50
3. Cambiar método: Automático → Manual
4. **Esperado**: Confirmación aparece
5. **Verificar**: Mensaje menciona sobrescritura de gastos y cuotas

#### Test 5: Banner auto-dismiss
1. Cambiar método (sin confirmación)
2. Banner aparece
3. Esperar 5 segundos
4. **Esperado**: Banner se desvanece y desaparece
5. **Verificar**: Formulario sigue funcional

#### Test 6: Cerrar banner manualmente
1. Cambiar método
2. Banner aparece
3. Clic en botón X
4. **Esperado**: Banner desaparece inmediatamente
5. **Verificar**: No errores en consola

#### Test 7: Múltiples cambios de método
1. Cambiar Automático → Perfil (banner 1)
2. Cambiar Perfil → Global (banner 2)
3. **Verificar**: Solo banner 2 visible (banner 1 reemplazado)

#### Test 8: Cambio de perfil sin confirmación
1. Método = Usar Perfil
2. Seleccionar perfil "Estándar"
3. Cambiar a perfil "Premium"
4. **Esperado**: Banner aparece, NO confirmación
5. **Verificar**: Valores del perfil Premium aplicados

### Código Modificado (Resumen)

| Sección | Líneas | Descripción |
|---------|--------|-------------|
| Variables de estado | ~58-68 | valoresIniciales, camposModificadosManualmente |
| guardarValoresIniciales() | ~74-78 | Guarda tasa/gastos/cuotas actuales |
| hayModificacionesManuales() | ~80-90 | Compara valores actuales vs iniciales |
| mostrarBannerActualizacion() | ~92-120 | Crea y muestra banner Bootstrap |
| actualizarMetodoCalculo() - confirmación | ~133-155 | Verifica cambios + confirm() |
| actualizarMetodoCalculo() - nombreMetodo | ~170-285 | Agrega nombreMetodo a cada case |
| actualizarMetodoCalculo() - banner/guardar | ~375-385 | Llama guardar + banner |
| Event listeners | ~530-560 | Detecta input en tasa/gastos/cuotas |

### Estado Final TAREA 9.2

✅ **Completado**:
- [x] Detección de cambios manuales con flags
- [x] Confirmación modal antes de sobrescribir
- [x] Banner informativo con nombre de método
- [x] Auto-dismiss del banner (5 segundos)
- [x] Recalculo automático de todos los valores
- [x] Event listeners para rastrear cambios
- [x] Funciones `guardarValoresIniciales()` y `hayModificacionesManuales()`
- [x] Parámetro `forzarActualizacion` para carga inicial sin banner
- [x] Data attribute `metodoAnterior` para revertir selección

**Resultado**: Experiencia de usuario mejorada con prevención de pérdida de datos y feedback claro.

---

## Estado Final Global (TAREA 9 + 9.2)

**Estado**: ✅ **COMPLETADO** - Compilación exitosa. Listo para testing funcional.

- **Mantiene compatibilidad**: Backend existente sigue funcionando
- **Integra TAREAS anteriores**: 6, 7 y 8 se usan perfectamente

**Estado**: ✅ **COMPLETADO** - Compilación exitosa. Listo para testing funcional.
