-- ETAPA 6 - Verificación de datos existentes (Crédito / Cupo / Snapshot)
-- Ejecutar en SQL Server (SSMS / Azure Data Studio)
-- Ajustar base si corresponde

USE TheBuryProjectDb;
GO

SET NOCOUNT ON;

DECLARE @NowUtc datetime2(7) = SYSUTCDATETIME();

IF OBJECT_ID('tempdb..#Hallazgos') IS NOT NULL
    DROP TABLE #Hallazgos;

CREATE TABLE #Hallazgos
(
    CheckId varchar(80) NOT NULL,
    Severidad varchar(20) NOT NULL,
    Conteo int NULL,
    Detalle nvarchar(500) NOT NULL
);

------------------------------------------------------------
-- 1) Clientes con más de 1 config (si no hay PK única por ClienteId)
------------------------------------------------------------
;WITH DupConfig AS
(
    SELECT ClienteId, COUNT(*) AS Cantidad
    FROM dbo.ClientesCreditoConfiguraciones
    GROUP BY ClienteId
    HAVING COUNT(*) > 1
)
INSERT INTO #Hallazgos(CheckId, Severidad, Conteo, Detalle)
SELECT
    'CFG_DUP_POR_CLIENTE',
    CASE WHEN COUNT(*) > 0 THEN 'ALTA' ELSE 'OK' END,
    COUNT(*),
    'Clientes con más de una configuración crediticia (debería ser 1:1).'
FROM DupConfig;

INSERT INTO #Hallazgos(CheckId, Severidad, Conteo, Detalle)
SELECT
    'CFG_PK_CLIENTE',
    CASE WHEN EXISTS (
        SELECT 1
        FROM sys.key_constraints kc
        INNER JOIN sys.tables t ON t.object_id = kc.parent_object_id
        INNER JOIN sys.index_columns ic ON ic.object_id = t.object_id AND ic.index_id = kc.unique_index_id
        INNER JOIN sys.columns c ON c.object_id = t.object_id AND c.column_id = ic.column_id
        WHERE t.name = 'ClientesCreditoConfiguraciones'
          AND kc.[type] = 'PK'
          AND c.name = 'ClienteId'
    ) THEN 'OK' ELSE 'MEDIA' END,
    NULL,
    'Validación de PK sobre ClienteId en ClientesCreditoConfiguraciones.';

------------------------------------------------------------
-- 2) Presets duplicados o rangos solapados
------------------------------------------------------------
;WITH DupPreset AS
(
    SELECT Puntaje, COUNT(*) AS Cantidad
    FROM dbo.PuntajeCreditoLimites
    GROUP BY Puntaje
    HAVING COUNT(*) > 1
)
INSERT INTO #Hallazgos(CheckId, Severidad, Conteo, Detalle)
SELECT
    'PRESET_DUP_PUNTAJE',
    CASE WHEN COUNT(*) > 0 THEN 'ALTA' ELSE 'OK' END,
    COUNT(*),
    'Puntajes con más de un preset en PuntajeCreditoLimites.'
FROM DupPreset;

IF COL_LENGTH('dbo.PuntajeCreditoLimites', 'PuntajeDesde') IS NOT NULL
   AND COL_LENGTH('dbo.PuntajeCreditoLimites', 'PuntajeHasta') IS NOT NULL
BEGIN
    ;WITH R AS
    (
        SELECT
            a.Id AS IdA,
            b.Id AS IdB
        FROM dbo.PuntajeCreditoLimites a
        INNER JOIN dbo.PuntajeCreditoLimites b
            ON a.Id < b.Id
           AND ISNULL(a.PuntajeDesde, -999999) <= ISNULL(b.PuntajeHasta, 999999)
           AND ISNULL(b.PuntajeDesde, -999999) <= ISNULL(a.PuntajeHasta, 999999)
    )
    INSERT INTO #Hallazgos(CheckId, Severidad, Conteo, Detalle)
    SELECT
        'PRESET_RANGO_SOLAPADO',
        CASE WHEN COUNT(*) > 0 THEN 'ALTA' ELSE 'OK' END,
        COUNT(*),
        'Se detectaron rangos solapados en presets (PuntajeDesde/PuntajeHasta).'
    FROM R;
END
ELSE
BEGIN
    INSERT INTO #Hallazgos(CheckId, Severidad, Conteo, Detalle)
    VALUES
    (
        'PRESET_RANGO_SOLAPADO',
        'N/A',
        NULL,
        'La tabla no tiene columnas de rango (PuntajeDesde/PuntajeHasta). Check no aplica.'
    );
END

------------------------------------------------------------
-- 3) Ventas a crédito sin snapshot de límite
------------------------------------------------------------
;WITH VentaCreditoSinSnapshot AS
(
    SELECT v.Id
    FROM dbo.Ventas v
    WHERE v.IsDeleted = 0
      AND v.TipoPago = 5 -- TipoPago.CreditoPersonal
      AND (
            v.LimiteAplicado IS NULL
         OR v.PuntajeAlMomento IS NULL
         OR v.PresetIdAlMomento IS NULL
      )
)
INSERT INTO #Hallazgos(CheckId, Severidad, Conteo, Detalle)
SELECT
    'VENTA_CREDITO_SIN_SNAPSHOT',
    CASE WHEN COUNT(*) > 0 THEN 'ALTA' ELSE 'OK' END,
    COUNT(*),
    'Ventas de crédito personal sin snapshot completo de límite.'
FROM VentaCreditoSinSnapshot;

------------------------------------------------------------
-- 4) Excepciones sin vigencia o vigencia invertida
------------------------------------------------------------
;WITH ExcepcionesInvalidas AS
(
    SELECT c.ClienteId
    FROM dbo.ClientesCreditoConfiguraciones c
    WHERE ISNULL(c.ExcepcionDelta, 0) > 0
      AND (
            c.ExcepcionDesde IS NULL
         OR c.ExcepcionHasta IS NULL
         OR c.ExcepcionDesde > c.ExcepcionHasta
      )
)
INSERT INTO #Hallazgos(CheckId, Severidad, Conteo, Detalle)
SELECT
    'EXCEPCION_VIGENCIA_INVALIDA',
    CASE WHEN COUNT(*) > 0 THEN 'ALTA' ELSE 'OK' END,
    COUNT(*),
    'Excepciones de límite con vigencia faltante o invertida.'
FROM ExcepcionesInvalidas;

------------------------------------------------------------
-- REPORTE RESUMIDO
------------------------------------------------------------
SELECT
    CheckId,
    Severidad,
    Conteo,
    Detalle
FROM #Hallazgos
ORDER BY
    CASE Severidad
        WHEN 'ALTA' THEN 1
        WHEN 'MEDIA' THEN 2
        WHEN 'OK' THEN 3
        ELSE 4
    END,
    CheckId;

------------------------------------------------------------
-- DETALLE DE REGISTROS (para análisis)
------------------------------------------------------------

-- Duplicados de config por cliente
SELECT ClienteId, COUNT(*) AS Cantidad
FROM dbo.ClientesCreditoConfiguraciones
GROUP BY ClienteId
HAVING COUNT(*) > 1
ORDER BY Cantidad DESC, ClienteId;

-- Presets duplicados por puntaje
SELECT Puntaje, COUNT(*) AS Cantidad
FROM dbo.PuntajeCreditoLimites
GROUP BY Puntaje
HAVING COUNT(*) > 1
ORDER BY Cantidad DESC, Puntaje;

-- Ventas crédito sin snapshot
SELECT
    v.Id,
    v.Numero,
    v.ClienteId,
    v.FechaVenta,
    v.Estado,
    v.CreditoId,
    v.LimiteAplicado,
    v.PuntajeAlMomento,
    v.PresetIdAlMomento,
    v.OverrideAlMomento,
    v.ExcepcionAlMomento
FROM dbo.Ventas v
WHERE v.IsDeleted = 0
  AND v.TipoPago = 5
  AND (
        v.LimiteAplicado IS NULL
     OR v.PuntajeAlMomento IS NULL
     OR v.PresetIdAlMomento IS NULL
  )
ORDER BY v.FechaVenta DESC, v.Id DESC;

-- Excepciones inválidas
SELECT
    c.ClienteId,
    c.CreditoPresetId,
    c.LimiteOverride,
    c.ExcepcionDelta,
    c.ExcepcionDesde,
    c.ExcepcionHasta,
    c.UpdatedAt,
    c.CreatedAt
FROM dbo.ClientesCreditoConfiguraciones c
WHERE ISNULL(c.ExcepcionDelta, 0) > 0
  AND (
        c.ExcepcionDesde IS NULL
     OR c.ExcepcionHasta IS NULL
     OR c.ExcepcionDesde > c.ExcepcionHasta
  )
ORDER BY c.ClienteId;

PRINT 'ETAPA 6 checks finalizados.';
