# theburyProject - Documento Maestro de Estado del Proyecto

**Fecha:** 18 de octubre de 2025  
**Desarrollador:** Alan  
**Asistente:** Zack (Claude)  
**Versión:** 1.0 - Fase de Planificación  

---

## 1. RESUMEN EJECUTIVO

### 1.1 Propósito del Sistema
ERP liviano para retail de electrodomésticos en Argentina con foco en:
- **Trazabilidad completa** de operaciones
- **Gestión de precios con historial reversible** (crítico en contexto inflacionario)
- **Parametrización fuerte** sin hardcoding
- **Autorizaciones con doble control** en operaciones sensibles
- **Auditoría automática** de todas las entidades críticas

### 1.2 Stack Tecnológico Confirmado

| Componente | Tecnología | Versión |
|------------|-----------|---------|
| Framework | .NET | 8.0 LTS |
| ORM | Entity Framework Core | 8.x |
| Tipo | ASP.NET Core MVC | 8.x |
| Base de Datos (Prod) | SQL Server | 2019+ |
| Base de Datos (Dev/Test) | SQL Server Express / InMemory | - |
| Autenticación | ASP.NET Core Identity | 8.x |
| Autorización | Claims-based RBAC | - |
| Testing | xUnit + Moq + FluentAssertions | Latest |
| Testing Integration | InMemory SQLite | Latest |
| Mapeo | AutoMapper | 13.x |
| UI Framework | Bootstrap | 5.x |
| UI (Futuro) | TailwindCSS | Migración futura |
| Grid/Datatables | jQuery DataTables | Latest |
| Jobs | Hangfire o Quartz.NET | A definir |

---

## 2. ARQUITECTURA DE SOLUCIÓN

### 2.1 Estructura de Proyectos

```
theburyProject.sln
├── theburyProject.Web (ASP.NET Core MVC)
│   ├── Controllers/
│   ├── ViewModels/
│   ├── Views/
│   ├── wwwroot/
│   ├── Filters/
│   ├── Middleware/
│   ├── Extensions/
│   └── Program.cs
│
├── theburyProject.Application (Capa de Aplicación)
│   ├── DTOs/
│   │   ├── Categoria/
│   │   ├── Producto/
│   │   └── ...
│   ├── Interfaces/ (IServices)
│   ├── Services/ (Implementaciones)
│   ├── Mappings/ (AutoMapper Profiles)
│   ├── Validators/ (FluentValidation)
│   └── Common/ (SearchRequest, PageResult, ServiceResult)
│
├── theburyProject.Domain (Dominio Puro)
│   ├── Entities/
│   ├── Enums/
│   ├── Exceptions/
│   ├── Interfaces/ (IRepository, IUnitOfWork)
│   └── ValueObjects/ (opcional DDD)
│
└── theburyProject.Infrastructure (Persistencia e Infra)
    ├── Data/
    │   ├── AppDbContext.cs
    │   ├── Configurations/ (EF Fluent API)
    │   ├── Migrations/
    │   ├── Interceptors/ (AuditInterceptor)
    │   └── Seeds/
    ├── Repositories/
    ├── Jobs/
    └── ExternalServices/
```

### 2.2 Convenciones de Código

#### Idioma
- **Código (clases, métodos, variables):** Inglés
- **Comentarios XML y logs:** Español
- **UI (labels, mensajes, validaciones):** Español

#### Naming
- **PascalCase:** Clases, métodos, properties públicas
- **camelCase:** Variables locales, parámetros
- **Prefijos:** 
  - `I` para interfaces
  - `_` para campos privados (`_context`, `_logger`)
- **Sufijos:**
  - `Async` en métodos asíncronos
  - `Dto` en Data Transfer Objects
  - `ViewModel` en modelos de vista

#### Async/Await
- Todos los métodos que tocan DB deben ser `async/await`
- Sufijo `Async` obligatorio
- Cancelation tokens en métodos públicos de servicios

#### Validación
- **DataAnnotations** en ViewModels para validaciones básicas
- **FluentValidation** (opcional) para reglas complejas
- **Validación de negocio** en Services (throw `DomainException`)

#### Dependency Injection
- Todo por **constructor injection**
- Registros en `Program.cs` mediante extension methods
- Ejemplo: `services.AddApplicationServices()`, `services.AddInfrastructureServices()`

---

## 3. MÓDULOS DEL SISTEMA (ALCANCE MVP)

### 3.1 Módulos Core (Fase 0)

#### Motor Común ABM y Búsqueda
- `BaseEntity` con propiedades comunes (Id, CreatedAt, UpdatedAt, IsDeleted, RowVersion)
- `IRepository<T>` genérico con métodos estándar
- `IUnitOfWork` para transacciones
- `SearchRequest` y `PageResult<T>` para paginación

#### Administración (RBAC + Auditoría)
- Gestión de usuarios, roles y permisos granulares
- Claims-based authorization (`"Modulo.Accion"`)
- Auditoría automática con interceptor EF
- Registro JSON de estado antes/después de cambios

#### Configuración
- Parámetros globales y por módulo
- Estructura: `Scope.Clave = ValorJson`
- Ejemplos: 
  - `Ventas.Autorizacion.UmbralImporte = 500000`
  - `Precios.UmbralAutorizacion = 10` (porcentaje)
  - `Stock.UmbralAlerta = 5`

### 3.2 Módulos de Negocio (Fases 1-8)

#### 1. Catálogo y Productos (Fase 1)
**Entidades:**
- `Categoria` (jerárquica con `ParentId`)
- `Marca`, `Submarca`
- `Producto` (con atributos JSON flexibles)
- `ProductAccessory` (accesorios relacionados)

#### 2. Precios (Fase 1) ⭐ CRÍTICO
**Características especiales:**
- **Historial append-only** con `VigenciaDesde`
- **Simulación** de aumentos/disminuciones
- **Autorización** por doble control si supera umbral
- **Aplicación** transaccional con nueva vigencia
- **Deshacer (Undo)** mediante batch inverso o rollback a vigencia anterior

**Entidades:**
- `ListaPrecio` (con reglas JSON)
- `ProductoPrecioLista` (PK: ProductoId + ListaId + VigenciaDesde)
- `PriceChangeBatch` (estados: Simulado → Aprobado → Aplicado → Revertido)
- `PriceChangeItem` (detalle antes/después por producto)

**Flujo:**
```
Simular → Revisar impacto → Autorizar (si Δ% > umbral) → Aplicar → [Deshacer si es necesario]
```

#### 3. Clientes (Fase 2)
- Scoring simplificado (Opción B): 2-3 criterios
- Criterios: Verificación externa + Ingresos + Garante (opcional)
- Políticas por tramo de score (MaxCuotas, LímiteCredito, Tasa)

#### 4. Ventas y Cotizaciones (Fase 3)
**Flujos:**
- Cotización → Venta → Autorización (si aplica) → Preparación → En viaje → Entregada
- **Recálculo automático** si cotización vencida (usa lista de precios vigente)
- **Reserva de stock** al autorizar (parametrizable)

**Disparadores de autorización:**
- Importe > umbral
- Descuento > umbral
- Score del cliente < mínimo
- Stock insuficiente
- Precio manual fuera de rango

#### 5. Stock (Fase 4)
- Kardex completo con trazabilidad
- Reservas automáticas al autorizar venta
- Alertas por umbral mínimo
- Ubicación "cuarentena" para devoluciones
- Control de series por familia (parametrizable)

#### 6. Crédito (Fase 5)
- Sistema francés por defecto
- Cálculo CFTEA
- Generación automática de cuotas
- Punitorios y moras (con autorización)
- Alertas de vencimientos

#### 7. Proveedores (Fase 6 - Simplificado MVP)
**Flujo simplificado:**
```
Pedido → Recepción → Pago
```
- Recepción impacta stock automáticamente
- Cheques básicos
- (Iteración 2: OC → Recepción → Factura → Pago completo)

#### 8. Devoluciones y Garantías (Fase 7)
- Checklist de accesorios con cargos parametrizables
- Stock en cuarentena
- Diagnóstico → Decisión (reemplazo/reparación/nota crédito/RMA)
- Enlace RMA con proveedor
- Nota de crédito con importe de venta original

#### 9. Empleados (Fase 8)
- Vinculación con usuarios Identity
- Datos básicos para auditoría y reporting

#### 10. Alertas y Reportes (Fase 8)
**Motor de alertas (DSL simple):**
- Stock bajo
- Cuotas vencidas
- Cheques por vencer
- Ventas pendientes de autorización
- Pedidos vencidos

**Reportes base:**
- Ventas por período/medio
- Margen por producto/categoría
- Ranking y rotación
- Efectividad de cotizaciones
- Morosidad y KPIs
- **Historial de precios** por producto/lista

---

## 4. REGLAS DE NEGOCIO CRÍTICAS

### 4.1 Precios
- **Margen mínimo** parametrizable
- **Reglas de tarjeta/cuotas** configurables
- **Deshacer/rehacer** por batch con trazabilidad completa
- Fórmula base: `PrecioContado = round(Costo * (1 + margen), regla)`
- Precio tarjeta/cuotas según `Precios.ReglaTarjeta`

### 4.2 Scoring de Clientes
- Puntaje 0-5 con suma ponderada
- Políticas por tramo (ejemplo):
  - Score 0-1: Sin financiación
  - Score 2-3: Hasta 6 cuotas, límite bajo, requiere garante
  - Score 4-5: Hasta 12 cuotas, límite alto, sin garante

### 4.3 Autorizaciones (Doble Control)
Requieren aprobación de superior:
- Ventas con importe > umbral
- Descuentos > umbral
- Aumentos/disminuciones masivos de precios (Δ% > umbral)
- Ajustes de stock
- Reprogramaciones de crédito

### 4.4 Reservas de Stock
- Configuración: `Ventas.Param.ReservaAlAutorizar = true/false`
- Si activo: al autorizar venta → genera movimientos tipo "Reserva"
- Al cancelar venta → libera automáticamente

### 4.5 Cotizaciones Vencidas
- Al convertir a venta: **recalcular precios** contra lista vigente
- Auditar el recálculo (precios antes/después)
- Notificar diferencias al usuario

---

## 5. MODELO DE DATOS CONCEPTUAL

### 5.1 Entidades Base

```csharp
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } // Concurrency
    public bool IsDeleted { get; set; } // Soft delete
}

public interface IAuditable
{
    string CreatedBy { get; set; }
    string UpdatedBy { get; set; }
    string IpAddress { get; set; }
}
```

### 5.2 Entidades de Precios (Ejemplo Clave)

```csharp
public class ProductoPrecioLista : BaseEntity, IAuditable
{
    public int ProductoId { get; set; }
    public int ListaId { get; set; }
    public DateTime VigenciaDesde { get; set; } // PK parte 3
    public decimal Costo { get; set; }
    public decimal PrecioContado { get; set; }
    public decimal PrecioTarjeta { get; set; }
    
    // Navigation
    public Producto Producto { get; set; }
    public ListaPrecio Lista { get; set; }
}

public class PriceChangeBatch : BaseEntity, IAuditable
{
    public TipoCambio Tipo { get; set; } // Aumento|Disminucion|Recalculo
    public string AlcanceJson { get; set; } // Filtros aplicados
    public decimal Delta { get; set; } // % o valor absoluto
    public string SimulacionJson { get; set; } // Preview
    public string AprobadoPor { get; set; }
    public EstadoBatch Estado { get; set; } // Simulado|Aprobado|Aplicado|Revertido
    public DateTime? AppliedAt { get; set; }
    
    // Navigation
    public ICollection<PriceChangeItem> Items { get; set; }
}

public class PriceChangeItem : BaseEntity
{
    public int BatchId { get; set; }
    public int ProductoId { get; set; }
    public int ListaId { get; set; }
    public decimal PrecioAntes { get; set; }
    public decimal PrecioDespues { get; set; }
    
    // Navigation
    public PriceChangeBatch Batch { get; set; }
    public Producto Producto { get; set; }
}
```

### 5.3 Índices Sugeridos

```sql
-- Precios
CREATE UNIQUE INDEX IX_Producto_Codigo ON Producto(Codigo);
CREATE INDEX IX_ProductoPrecioLista_Lookup 
    ON ProductoPrecioLista(ProductoId, ListaId, VigenciaDesde DESC);

-- Stock
CREATE INDEX IX_StockItem_Lookup ON StockItem(ProductoId, SucursalId);
CREATE INDEX IX_StockMovimiento_Trace ON StockMovimiento(ProductoId, Fecha DESC);

-- Ventas
CREATE INDEX IX_Venta_Cliente ON Venta(ClienteId, Fecha DESC, Estado);

-- Crédito
CREATE UNIQUE INDEX IX_Cuota_CreditoNro ON Cuota(CreditoId, Nro);
```

---

## 6. ROADMAP (SPRINTS DE 2 SEMANAS)

| Fase | Sprints | Entregables | Estado |
|------|---------|-------------|--------|
| **Fase 0: Fundaciones** | S1-S2 | Motor ABM/Búsqueda, RBAC, Auditoría, Config, Jobs, 2 entidades demo | 🔜 PRÓXIMO |
| **Fase 1: Catálogo & Precios** | S2-S3 | Categorías, Marcas, Productos, Listas, Simulación/Autorización/Aplicación/Deshacer de precios | ⏳ Pendiente |
| **Fase 2: Clientes & Scoring** | S3-S4 | Ficha cliente, motor scoring (Opción B), políticas por tramo | ⏳ Pendiente |
| **Fase 3: Ventas/Cotizaciones** | S4-S6 | E2E con recálculo, autorizaciones, logística | ⏳ Pendiente |
| **Fase 4: Stock** | S5-S6 | Kardex, reservas, alertas, cuarentena | ⏳ Pendiente |
| **Fase 5: Crédito** | S6-S7 | Planes (francés), cuotas, CFTEA, cobros, moras | ⏳ Pendiente |
| **Fase 6: Proveedores** | S7-S8 | Pedido→Recepción→Pago simplificado, cheques básicos | ⏳ Pendiente |
| **Fase 7: Devoluciones & Garantías** | S8-S9 | Checklist, nota crédito, RMA enlace | ⏳ Pendiente |
| **Fase 8: Alertas & Reportes** | S9-S10 | Motor reglas, dashboard, reportes base + historial precios | ⏳ Pendiente |

**Iteración 2 (Post-MVP):** Compras completo (OC), Cheques full, RMA avanzado, Series por familia, KPIs ampliados  
**Iteración 3:** Multi-sucursal, integraciones (AFIP, pagos), costeo contable avanzado

---

## 7. PRIMER ENTREGABLE PROPUESTO

### Objetivo
Scaffold mínimo funcional con motor ABM completo y 1 entidad demo (`Categoria`)

### Incluye
1. ✅ Solución con 4 proyectos y referencias
2. ✅ NuGet packages configurados
3. ✅ DbContext con interceptor de auditoría
4. ✅ BaseEntity + IAuditable
5. ✅ Repository<T> + UnitOfWork genéricos
6. ✅ SearchRequest/PageResult para paginación
7. ✅ Identity extendido con Claims
8. ✅ Entidad `Categoria` completa (Entity → Service → Controller → CRUD Views)
9. ✅ Migraciones iniciales + seeds básicos
10. ✅ 1 test unitario + 1 integration test
11. ✅ README con setup y comandos

### Resultado Esperado
- ABM de Categorías funcionando E2E
- Auditoría automática
- Búsqueda paginada
- Autorización por claims funcionando
- Todo compilable y testeado

---

## 8. PRÓXIMOS PASOS INMEDIATOS

### Paso 1: Crear estructura de solución
- Proyectos con referencias correctas
- .csproj con packages necesarios

### Paso 2: Configurar Program.cs
- Identity + EF Core
- DI de servicios
- AutoMapper
- Middleware de auditoría y errores

### Paso 3: Implementar fundaciones
- BaseEntity, repositorios genéricos
- Interceptor de auditoría
- SearchRequest/PageResult

### Paso 4: Entidad demo (Categoria)
- Domain entity
- EF configuration
- Service + interface
- Controller + ViewModels
- CRUD views (Razor)

### Paso 5: Tests básicos
- Unit test del servicio
- Integration test del controller

### Paso 6: Documentación
- README con setup
- Comentarios XML en código crítico

---

## 9. PENDIENTES Y DECISIONES FUTURAS

### A Definir Próximamente
- [ ] Motor de jobs: ¿Hangfire o Quartz.NET?
- [ ] FluentValidation: ¿lo usamos o solo DataAnnotations?
- [ ] Admin template Bootstrap específico (AdminLTE, CoreUI, etc.)
- [ ] Estrategia de logging: ¿Serilog? ¿Seq para dev?
- [ ] Integración AFIP (Iteración 3)
- [ ] Integración mercado pago / QR (Iteración 3)

### Riesgos Identificados
- Complejidad del sistema de precios con historial (mitigar con tests exhaustivos)
- Performance en búsquedas con grandes volúmenes (índices críticos desde el inicio)
- Concurrencia en actualizaciones de stock (RowVersion + transacciones)

---

## 10. COMANDOS Y ATAJOS DE COMUNICACIÓN

Cuando retomes la conversación, podés usar:

- **CONTEXTO**: Pedime el mapa del proyecto actual
- **OBJETIVO**: Decime qué querés lograr
- **GENERA**: Pedime generar un componente o feature
- **REFACT**: Indicame qué archivo/sección mejorar
- **DIFF**: Pedime cambios específicos con contexto (±3 líneas)
- **TEST**: Indicame qué probar
- **DOCS**: Pedime resumen técnico
- **MEJORA**: Indicame área a optimizar

---

## 11. INFORMACIÓN DE CONTACTO Y ROLES

**Desarrollador:** Alan  
**Asistente:** Zack (Claude Sonnet 4.5)  
**Inicio de proyecto:** 18 de octubre de 2025  
**Última actualización:** 18 de octubre de 2025

---

## 12. NOTAS FINALES

Este documento es el **checkpoint maestro**. Si iniciás una conversación nueva, cargá este documento y decí:

> "Hola Zack, soy Alan. Estoy trabajando en theburyProject. Te cargo el documento maestro de estado para que sepas dónde estamos. [pegar este documento]"

Y yo podré continuar exactamente donde quedamos.

**Estado actual:** Fase de planificación completa. Listo para empezar código.

---

**FIN DEL DOCUMENTO MAESTRO**