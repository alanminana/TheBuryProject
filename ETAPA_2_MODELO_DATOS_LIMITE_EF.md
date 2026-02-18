# Etapa 2 — Modelo de datos y relaciones (SQL/EF)

## Decisión de modelado
- Puntaje **discreto** (`NivelRiesgoCredito` 1..5), no continuo 0..100.
- Preset por puntaje en `PuntajeCreditoLimites` (mapping 1→monto, ..., 5→monto).
- Se mantiene constraint de rango `[Puntaje] BETWEEN 1 AND 5` y unicidad por puntaje.

## Diagrama rápido (texto)

```text
Cliente (1) ──────────────── (1) ClienteCreditoConfiguracion
  Id (PK)                        ClienteId (PK, FK -> Cliente.Id)
                                 CreditoPresetId (FK -> PuntajeCreditoLimites.Id, nullable)
                                 LimiteOverride (nullable)
                                 ExcepcionDelta, ExcepcionDesde/Hasta (nullable)
                                 MotivoExcepcion, AprobadoPor, AprobadoEnUtc
                                 MotivoOverride, OverrideAprobadoPor, OverrideAprobadoEnUtc
                                 RowVersion (concurrency)

Cliente (1) ──────────────── (N) ClientePuntajeHistorial
  Id (PK)                        Id (PK)
                                 ClienteId (FK)
                                 Puntaje, NivelRiesgo, Fecha, Origen, RegistradoPor

PuntajeCreditoLimites (1) ── (N) ClienteCreditoConfiguracion
PuntajeCreditoLimites (1) ── (N) Venta (PresetIdAlMomento, snapshot opcional)

Venta (snapshot)
  LimiteAplicado
  PuntajeAlMomento
  PresetIdAlMomento
  OverrideAlMomento
  ExcepcionAlMomento
```

## Auditoría mínima implementada
- Cambios de puntaje: `ClienteService.ActualizarPuntajeRiesgoAsync` registra en `ClientesPuntajeHistorial`.
- Override/excepción: quedan auditables en `ClienteCreditoConfiguracion` con quién/cuándo/motivo.
- Snapshot de venta: en creación de venta (`VentaService.CreateAsync`) se persiste el límite aplicado al momento.

## Fórmula operativa con configuración 1:1
`limiteEfectivo = limiteOverride ?? (limiteBase + excepcionDeltaVigente)`

- `limiteBase`: preset explícito de configuración o fallback al preset por puntaje.
- `excepcionDeltaVigente`: sólo aplica dentro de vigencia.

## Migración
- Se agrega migración EF con:
  - tablas nuevas: `ClientesCreditoConfiguraciones`, `ClientesPuntajeHistorial`
  - columnas snapshot en `Ventas`
  - constraints/checks e índices requeridos
  - FK opcionales para preset y relaciones 1:1 / 1:N.
