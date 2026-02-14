# Etapa 1 — Propuesta Técnica

## Objetivo
Modernizar:
1. Modelo funcional de `Producto` para soportar características variables por rubro (pulgadas, color, capacidad, voltaje, talle, etc.) sin romper el esquema actual.
2. UX de `Venta` (Create/Edit) reemplazando el `<select>` de productos por una búsqueda moderna con autocompletado para agregar ítems al carrito por código/nombre/marca/categoría/descripción/características.

> Esta etapa es de descubrimiento/diseño. No incluye cambios funcionales.

---

## Relevamiento del estado actual

### 1) Flujo actual de Venta

- Carga de productos para Create/Edit:
  - `VentaController.CargarViewBags(...)` usa `_productoService.SearchAsync(soloActivos: true, orderBy: "nombre")`.
  - Luego filtra en memoria: `StockActual > 0` o producto ya incluido en la venta (edición).
  - Construye `ViewBag.Productos` con texto: `"Código - Nombre (Stock: X) - $Precio"`.

- UI actual (Create/Edit):
  - Vistas usan `<select id="productoSelect">` + inputs `cantidad/precio/descuento` + botón agregar.
  - JS parsea el texto del option para extraer código/nombre y stock.

- Precio/stock al seleccionar:
  - `GET api/ventas/GetPrecioProducto?id=...` retorna `{ precioVenta, stockActual, codigo, nombre }`.

- Totales preview:
  - Frontend invoca `POST api/ventas/CalcularTotalesVenta`.
  - `venta-common.js` arma payload con detalles y actualiza labels/hidden fields de subtotal, descuento, IVA y total.

### 2) Módulo actual de Productos

- Entidad `Producto` contiene:
  - `Codigo`, `Nombre`, `Descripcion`, `CategoriaId`, `SubcategoriaId`, `MarcaId`, `SubmarcaId`, `PrecioCompra`, `PrecioVenta`, `PorcentajeIVA`, `StockMinimo`, `StockActual`, `UnidadMedida`, `Activo`, etc.
- UI Create/Edit de Producto hoy expone categoría/subcategoría, marca/submarca, descripción, precios, stock, activo.
- Búsqueda actual:
  - `ProductoService.SearchAsync(...)` filtra texto solo por `Codigo`, `Nombre`, `Descripcion`.
  - Además permite filtro por `CategoriaId`, `MarcaId`, `stockBajo`, `soloActivos`, y ordenamiento.
  - No busca por subcategoría, submarca ni características variables.

### 3) Restricciones técnicas actuales relevantes

- EF Core + SQL Server.
- Índice único por `Producto.Codigo` (con `IsDeleted = 0`).
- Sin endpoint dedicado de búsqueda de productos para autocompletado en Venta.
- Venta depende del texto visual del `<select>` para parte de la lógica de agregado.

---

## Alternativas para “Características de Producto”

## Opción A — Campo JSON en Producto (rápida)

### Diseño
- Agregar columna nullable `Producto.CaracteristicasJson` (NVARCHAR(MAX)) con pares clave/valor, por ejemplo:
  ```json
  {
    "pulgadas": "55",
    "color": "Negro",
    "resolucion": "4K"
  }
  ```
- UI de Producto: editor dinámico key/value (agregar/eliminar filas).
- Búsqueda en Venta/Catálogo: combinar búsqueda actual + `LIKE` sobre `CaracteristicasJson`.

### Pros
- Implementación más simple y rápida.
- Menor cantidad de tablas/migraciones.
- Baja fricción para adopción inicial.

### Contras
- Validación débil de tipos/unidades.
- Dificulta filtros exactos o facetas avanzadas.
- Rendimiento de búsqueda textual limitado en grandes volúmenes.

---

## Opción B — Modelo normalizado EAV liviano (recomendada)

### Diseño
Agregar 2 tablas:

1. `CaracteristicaDefinicion`
- `Id`
- `Nombre` (ej. pulgadas, color, capacidad)
- `TipoDato` (Texto, Numero, Booleano, Lista)
- `Unidad` (opcional: pulgadas, litros, volts)
- `CategoriaId` nullable (si aplica por rubro)
- `Activo`, `Orden`

2. `ProductoCaracteristicaValor`
- `Id`
- `ProductoId` (FK)
- `CaracteristicaDefinicionId` (FK)
- `ValorTexto` (guardar valor normalizado como string)
- `ValorNumero` nullable (para búsquedas numéricas futuras)
- `ValorBooleano` nullable
- `CreatedAt/UpdatedAt` (opcional según base)

Opcional para UX: vista/proyección de búsqueda (`vw_producto_busqueda`) concatenando campos base + características.

### Pros
- Estructura extensible y controlada por rubro/categoría.
- Buen equilibrio entre flexibilidad y capacidad de consulta.
- Facilita evolución futura (filtros por rango, facetas, validaciones por tipo).

### Contras
- Más cambios iniciales que JSON.
- Requiere diseño de UI para mantenimiento de definiciones/valores.

---

## Recomendación
Recomiendo **Opción B (EAV liviano)** por ser la más compatible con el objetivo de largo plazo (buscar por características en Venta y Catálogo) sin romper el esquema actual.

Justificación:
- Mantiene `Producto` y flujos existentes intactos.
- Permite rollout incremental: primero solo texto y búsqueda; luego validaciones/filtros avanzados.
- Evita acoplar la lógica de negocio a parsing de JSON libre en SQL.

---

## Propuesta UX para Venta (autocomplete moderno)

## Estado actual
- `<select id="productoSelect">` + parse de texto de option.

## Propuesta mínima viable
- Reemplazar select por un input de búsqueda con autocompletado (debounce 250–300 ms).
- Nuevo endpoint en `VentaApiController` (o endpoint dedicado) para sugerencias:
  - Query por texto sobre código, nombre, marca, categoría, descripción y características.
  - Respuesta compacta: `id, codigo, nombre, marca, categoria, stockActual, precioVenta, resumenCaracteristicas`.
- Al seleccionar sugerencia:
  - llenar precio unitario,
  - guardar `productoId` real,
  - mantener validación de stock y flujo de agregado actual.

## Compatibilidad
- Mantener `GetPrecioProducto` y `CalcularTotalesVenta` en esta etapa/etapas iniciales.
- Evitar romper Create/Edit: fallback temporal a select durante despliegue si se desea feature toggle.

---

## Impacto estimado por módulo

1. **Entidades/Modelo**
- Nuevas entidades para características (si Opción B).
- Navegaciones en `Producto` (colección de valores).

2. **Data/EF Core**
- `AppDbContext`: DbSet + configuraciones + índices.
- Migración SQL Server (tablas, FK, índices).

3. **Servicios**
- `IProductoService` / `ProductoService`:
  - incluir carga/guardado de características,
  - extender `SearchAsync` para incluir características.
- `CatalogoService` impactado indirectamente por uso de `SearchAsync`.

4. **API/Controladores**
- `VentaApiController`: endpoint de búsqueda/autocomplete.
- `ProductoController`: guardar/editar características (Create/Edit).

5. **Vistas/Frontend**
- `Views/Producto/Create/Edit`: editor de características.
- `Views/Venta/Create/Edit`: reemplazo de select por buscador.
- `wwwroot/js/venta-create.js`, `venta-edit.js`, `venta-common.js`: adaptar flujo de agregado sin parse de texto.

6. **AutoMapper**
- `Helpers/AutoMapperProfile.cs`: mapear características entre entidad y ViewModel.

---

## Plan incremental compatible con SQL Server

### Paso 1 (Infra mínima de datos)
- Crear tablas (Opción B) + índices básicos:
  - `IX_ProductoCaracteristicaValor_ProductoId`
  - `IX_ProductoCaracteristicaValor_DefinicionId`
  - opcional índice por `ValorTexto` (prefix/normalizado)
- Mantener intacta la tabla `Productos` y flujos actuales.

### Paso 2 (Backoffice Producto)
- Agregar UI de características en Create/Edit de Producto.
- Guardar y recuperar valores sin afectar campos actuales.

### Paso 3 (Búsqueda backend para Venta)
- Crear endpoint de sugerencias/autocomplete en `VentaApiController`.
- Búsqueda por campos actuales + join a características.

### Paso 4 (UX Venta)
- Reemplazar select por input con sugerencias.
- Mantener endpoints de precio/totales y comportamiento del carrito.

### Paso 5 (Catálogo)
- Extender búsqueda de catálogo para incluir características.

### Paso 6 (Hardening)
- Ajustes de performance (índices, normalización de texto).
- Pruebas funcionales de Create/Edit Venta y Producto.

---

## Criterios de aceptación de Etapa 1 (cumplidos por este documento)

- Se relevó flujo actual de Venta:
  - carga de `ViewBag.Productos`, regla de stock y formato actual,
  - JS de create/edit/common,
  - endpoint `GetPrecioProducto` y cálculo de totales.
- Se relevó módulo de Productos:
  - campos actuales,
  - búsqueda/listado actual (`SearchAsync`, filtros).
- Se presentan 2 alternativas para características con recomendación.
- Se incluye impacto estimado por módulo y plan incremental SQL Server.
- No se implementaron cambios funcionales en esta etapa.
