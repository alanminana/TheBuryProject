-- ETAPA 6 - Corrección de inconsistencias de datos (Crédito / Cupo / Snapshot)
-- IMPORTANTE:
-- 1) Ejecutar primero ETAPA_6_CHECKS_CONSISTENCIA_CREDITO.sql
-- 2) Este script corre en modo DRY-RUN por defecto (@ApplyFix = 0)

USE TheBuryProjectDb;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @ApplyFix bit = 0; -- 0 = solo simulación / 1 = aplicar cambios
DECLARE @NowUtc datetime2(7) = SYSUTCDATETIME();

BEGIN TRAN;

BEGIN TRY
    PRINT '=== ETAPA 6 FIX - INICIO ===';
    PRINT CONCAT('Modo apply = ', @ApplyFix);

    ------------------------------------------------------------
    -- A) Duplicados de configuración por cliente (si existieran)
    -- Estrategia: conservar la fila más reciente por ClienteId
    ------------------------------------------------------------
    ;WITH Dup AS
    (
        SELECT
            ClienteId,
            ROW_NUMBER() OVER (
                PARTITION BY ClienteId
                ORDER BY ISNULL(UpdatedAt, CreatedAt) DESC, ClienteId DESC
            ) AS rn
        FROM dbo.ClientesCreditoConfiguraciones
    )
    SELECT COUNT(*) AS DuplicadosConfigAEliminar
    FROM Dup
    WHERE rn > 1;

    IF @ApplyFix = 1
    BEGIN
        ;WITH Dup AS
        (
            SELECT
                ClienteId,
                ROW_NUMBER() OVER (
                    PARTITION BY ClienteId
                    ORDER BY ISNULL(UpdatedAt, CreatedAt) DESC, ClienteId DESC
                ) AS rn
            FROM dbo.ClientesCreditoConfiguraciones
        )
        DELETE c
        FROM dbo.ClientesCreditoConfiguraciones c
        INNER JOIN Dup d
            ON c.ClienteId = d.ClienteId
        WHERE d.rn > 1;

        PRINT CONCAT('Configs duplicadas eliminadas: ', @@ROWCOUNT);
    END

    ------------------------------------------------------------
    -- B) Presets duplicados por puntaje
    -- Estrategia: desactivar duplicados, conservar el más reciente por Puntaje
    ------------------------------------------------------------
    ;WITH RankedPresets AS
    (
        SELECT
            Id,
            Puntaje,
            Activo,
            FechaActualizacion,
            ROW_NUMBER() OVER (
                PARTITION BY Puntaje
                ORDER BY Activo DESC, FechaActualizacion DESC, Id DESC
            ) AS rn
        FROM dbo.PuntajeCreditoLimites
    )
    SELECT COUNT(*) AS PresetsDuplicadosAInactivar
    FROM RankedPresets
    WHERE rn > 1
      AND Activo = 1;

    IF @ApplyFix = 1
    BEGIN
        ;WITH RankedPresets AS
        (
            SELECT
                Id,
                Puntaje,
                Activo,
                FechaActualizacion,
                ROW_NUMBER() OVER (
                    PARTITION BY Puntaje
                    ORDER BY Activo DESC, FechaActualizacion DESC, Id DESC
                ) AS rn
            FROM dbo.PuntajeCreditoLimites
        )
        UPDATE p
        SET
            Activo = 0,
            FechaActualizacion = @NowUtc,
            UsuarioActualizacion = COALESCE(UsuarioActualizacion, 'etapa6-fix')
        FROM dbo.PuntajeCreditoLimites p
        INNER JOIN RankedPresets r ON p.Id = r.Id
        WHERE r.rn > 1
          AND p.Activo = 1;

        PRINT CONCAT('Presets duplicados inactivados: ', @@ROWCOUNT);
    END

    ------------------------------------------------------------
    -- C) Excepciones sin vigencia o vigencia invertida
    -- Estrategia conservadora: desactivar excepción inválida
    ------------------------------------------------------------
    SELECT COUNT(*) AS ExcepcionesInvalidas
    FROM dbo.ClientesCreditoConfiguraciones c
    WHERE ISNULL(c.ExcepcionDelta, 0) > 0
      AND (
            c.ExcepcionDesde IS NULL
         OR c.ExcepcionHasta IS NULL
         OR c.ExcepcionDesde > c.ExcepcionHasta
      );

    IF @ApplyFix = 1
    BEGIN
        UPDATE c
        SET
            ExcepcionDelta = NULL,
            ExcepcionDesde = NULL,
            ExcepcionHasta = NULL,
            MotivoExcepcion = LEFT(
                CONCAT(
                    COALESCE(NULLIF(c.MotivoExcepcion, ''), 'AUTO-FIX ETAPA6'),
                    ' | Excepción invalidada por vigencia inconsistente el ',
                    CONVERT(varchar(33), @NowUtc, 126)
                ),
                1000
            ),
            UpdatedAt = @NowUtc
        FROM dbo.ClientesCreditoConfiguraciones c
        WHERE ISNULL(c.ExcepcionDelta, 0) > 0
          AND (
                c.ExcepcionDesde IS NULL
             OR c.ExcepcionHasta IS NULL
             OR c.ExcepcionDesde > c.ExcepcionHasta
          );

        PRINT CONCAT('Excepciones invalidadas: ', @@ROWCOUNT);
    END

    ------------------------------------------------------------
    -- D) Backfill de snapshot en ventas a crédito sin datos
    -- Regla aplicada:
    -- LimiteEfectivo = Override ?? (LimiteBase + ExcepcionDeltaVigente)
    ------------------------------------------------------------
    SELECT COUNT(*) AS VentasCreditoSinSnapshot
    FROM dbo.Ventas v
    WHERE v.IsDeleted = 0
      AND v.TipoPago = 5
      AND (
            v.LimiteAplicado IS NULL
         OR v.PuntajeAlMomento IS NULL
         OR v.PresetIdAlMomento IS NULL
      );

    IF @ApplyFix = 1
    BEGIN
        ;WITH Calc AS
        (
            SELECT
                v.Id AS VentaId,
                c.PuntajeRiesgo,
                COALESCE(pc.Id, pr.Id) AS PresetId,
                COALESCE(cfg.LimiteOverride, c.LimiteCredito) AS OverrideValor,
                CASE
                    WHEN ISNULL(cfg.ExcepcionDelta, 0) > 0
                     AND (cfg.ExcepcionDesde IS NULL OR cfg.ExcepcionDesde <= @NowUtc)
                     AND (cfg.ExcepcionHasta IS NULL OR cfg.ExcepcionHasta >= @NowUtc)
                    THEN cfg.ExcepcionDelta
                    ELSE 0
                END AS ExcepcionVigente,
                COALESCE(pc.LimiteMonto, pr.LimiteMonto, 0) AS LimiteBase
            FROM dbo.Ventas v
            INNER JOIN dbo.Clientes c ON c.Id = v.ClienteId
            LEFT JOIN dbo.ClientesCreditoConfiguraciones cfg ON cfg.ClienteId = c.Id
            LEFT JOIN dbo.PuntajeCreditoLimites pc
                ON pc.Id = cfg.CreditoPresetId
               AND pc.Activo = 1
            OUTER APPLY
            (
                SELECT TOP 1 p2.Id, p2.LimiteMonto
                FROM dbo.PuntajeCreditoLimites p2
                WHERE p2.Puntaje = c.NivelRiesgo
                  AND p2.Activo = 1
                ORDER BY p2.FechaActualizacion DESC, p2.Id DESC
            ) pr
            WHERE v.IsDeleted = 0
              AND v.TipoPago = 5
              AND (
                    v.LimiteAplicado IS NULL
                 OR v.PuntajeAlMomento IS NULL
                 OR v.PresetIdAlMomento IS NULL
              )
        ),
        Fill AS
        (
            SELECT
                VentaId,
                PuntajeRiesgo,
                PresetId,
                OverrideValor,
                CASE WHEN ExcepcionVigente > 0 THEN ExcepcionVigente ELSE NULL END AS ExcepcionSnapshot,
                COALESCE(OverrideValor, LimiteBase + ExcepcionVigente) AS LimiteEfectivo
            FROM Calc
        )
        UPDATE v
        SET
            v.PuntajeAlMomento = COALESCE(v.PuntajeAlMomento, f.PuntajeRiesgo),
            v.PresetIdAlMomento = COALESCE(v.PresetIdAlMomento, f.PresetId),
            v.OverrideAlMomento = COALESCE(v.OverrideAlMomento, f.OverrideValor),
            v.ExcepcionAlMomento = COALESCE(v.ExcepcionAlMomento, f.ExcepcionSnapshot),
            v.LimiteAplicado = COALESCE(
                v.LimiteAplicado,
                CASE WHEN f.LimiteEfectivo > 0 THEN f.LimiteEfectivo ELSE NULL END
            )
        FROM dbo.Ventas v
        INNER JOIN Fill f ON f.VentaId = v.Id;

        PRINT CONCAT('Ventas con snapshot backfill: ', @@ROWCOUNT);
    END

    IF @ApplyFix = 1
    BEGIN
        COMMIT TRAN;
        PRINT '=== ETAPA 6 FIX - COMMIT ===';
    END
    ELSE
    BEGIN
        ROLLBACK TRAN;
        PRINT '=== ETAPA 6 FIX - DRY RUN (ROLLBACK) ===';
    END
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRAN;

    DECLARE @Err nvarchar(4000) = ERROR_MESSAGE();
    DECLARE @ErrLine int = ERROR_LINE();
    DECLARE @ErrNum int = ERROR_NUMBER();

    PRINT CONCAT('ERROR ETAPA 6 FIX [', @ErrNum, '] LINEA ', @ErrLine, ': ', @Err);
    THROW;
END CATCH;
