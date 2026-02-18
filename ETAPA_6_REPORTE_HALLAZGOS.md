# Etapa 6 — Verificación de datos existentes

## Objetivo
Detectar inconsistencias de datos ya cargados en crédito/cupo/snapshot y dejar script de corrección segura.

## Alcance implementado
Se generaron dos scripts SQL para SQL Server:

- `ETAPA_6_CHECKS_CONSISTENCIA_CREDITO.sql`
- `ETAPA_6_FIX_CONSISTENCIA_CREDITO.sql` (con modo dry-run)

## Checks incluidos
1. Clientes con más de una config crediticia (`ClientesCreditoConfiguraciones`).
2. Presets duplicados por puntaje en `PuntajeCreditoLimites`.
3. Rangos solapados (solo si existen columnas `PuntajeDesde`/`PuntajeHasta`).
4. Ventas de crédito personal sin snapshot (`LimiteAplicado`, `PuntajeAlMomento`, `PresetIdAlMomento`).
5. Excepciones con vigencia faltante o invertida.

## Correcciones incluidas (si aplica)
El script de fix contempla:

- Duplicados de config: conserva la fila más reciente por cliente.
- Presets duplicados: inactiva duplicados y conserva el más reciente.
- Excepciones inválidas: desactiva excepción (delta + vigencia) de forma conservadora.
- Ventas sin snapshot: backfill con regla de negocio vigente:
  - `LimiteEfectivo = Override ?? (LimiteBase + ExcepcionDeltaVigente)`

## Ejecución recomendada
1. Ejecutar checks:
   - `ETAPA_6_CHECKS_CONSISTENCIA_CREDITO.sql`
2. Ejecutar fix en simulación:
   - `ETAPA_6_FIX_CONSISTENCIA_CREDITO.sql` con `@ApplyFix = 0`
3. Revisar conteos y muestras.
4. Aplicar corrección real:
   - cambiar `@ApplyFix = 1` y ejecutar nuevamente.
5. Re-ejecutar checks para confirmar estado final.

## Hallazgos
En este entorno no se ejecutó conexión directa contra la base productiva, por lo que no hay conteos reales en este reporte.

El resultado de hallazgos queda determinado por la ejecución del script de checks en tu BD objetivo.

## Notas
- Los fixes son idempotentes y transaccionales (rollback automático en dry-run).
- Se priorizó corrección conservadora para no romper historial de ventas.
