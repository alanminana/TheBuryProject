-- =============================================
-- Script para crear tablas de módulo Mora
-- Ejecutar en SQL Server Management Studio
-- =============================================

-- ============================================
-- 1. Crear tabla ConfiguracionesMora
-- ============================================
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ConfiguracionesMora')
BEGIN
    PRINT 'Creando tabla ConfiguracionesMora...'

    CREATE TABLE [dbo].[ConfiguracionesMora] (
        [Id] INT NOT NULL IDENTITY(1,1),
        [DiasGracia] INT NOT NULL DEFAULT 3,
        [PorcentajeRecargo] DECIMAL(5,2) NOT NULL DEFAULT 5.0,
        [CalculoAutomatico] BIT NOT NULL DEFAULT 1,
        [NotificacionAutomatica] BIT NOT NULL DEFAULT 1,
        [JobActivo] BIT NOT NULL DEFAULT 1,
        [HoraEjecucion] TIME NOT NULL DEFAULT '08:00:00',
        [UltimaEjecucion] DATETIME2 NULL,
        [CreatedAt] DATETIME2 NOT NULL,
        [UpdatedAt] DATETIME2 NULL,
        [CreatedBy] NVARCHAR(MAX) NULL,
        [UpdatedBy] NVARCHAR(MAX) NULL,
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        [RowVersion] ROWVERSION NULL,

        CONSTRAINT [PK_ConfiguracionesMora] PRIMARY KEY ([Id])
    );

    PRINT 'Tabla ConfiguracionesMora creada exitosamente.'
END
ELSE
BEGIN
    PRINT 'La tabla ConfiguracionesMora ya existe.'
END
GO

-- ============================================
-- 2. Crear tabla LogsMora
-- ============================================
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'LogsMora')
BEGIN
    PRINT 'Creando tabla LogsMora...'

    CREATE TABLE [dbo].[LogsMora] (
        [Id] INT NOT NULL IDENTITY(1,1),
        [FechaEjecucion] DATETIME2 NOT NULL,
        [CuotasProcesadas] INT NOT NULL,
        [AlertasGeneradas] INT NOT NULL,
        [Exitoso] BIT NOT NULL,
        [Mensaje] NVARCHAR(MAX) NULL,
        [DetalleError] NVARCHAR(MAX) NULL,
        [CreatedAt] DATETIME2 NOT NULL,
        [UpdatedAt] DATETIME2 NULL,
        [CreatedBy] NVARCHAR(MAX) NULL,
        [UpdatedBy] NVARCHAR(MAX) NULL,
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        [RowVersion] ROWVERSION NULL,

        CONSTRAINT [PK_LogsMora] PRIMARY KEY ([Id])
    );

    -- Crear índices
    CREATE INDEX [IX_LogsMora_FechaEjecucion] ON [LogsMora] ([FechaEjecucion]);
    CREATE INDEX [IX_LogsMora_Exitoso] ON [LogsMora] ([Exitoso]);

    PRINT 'Tabla LogsMora creada exitosamente.'
END
ELSE
BEGIN
    PRINT 'La tabla LogsMora ya existe.'
END
GO

-- ============================================
-- 3. Crear tabla AlertasCobranza
-- ============================================
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AlertasCobranza')
BEGIN
    PRINT 'Creando tabla AlertasCobranza...'

    CREATE TABLE [dbo].[AlertasCobranza] (
        [Id] INT NOT NULL IDENTITY(1,1),
        [CreditoId] INT NOT NULL,
        [ClienteId] INT NOT NULL,
        [Tipo] INT NOT NULL,
        [Prioridad] INT NOT NULL,
        [Mensaje] NVARCHAR(MAX) NOT NULL,
        [MontoVencido] DECIMAL(18,2) NOT NULL,
        [CuotasVencidas] INT NOT NULL,
        [FechaAlerta] DATETIME2 NOT NULL,
        [Resuelta] BIT NOT NULL,
        [FechaResolucion] DATETIME2 NULL,
        [Observaciones] NVARCHAR(MAX) NULL,
        [CreatedAt] DATETIME2 NOT NULL,
        [UpdatedAt] DATETIME2 NULL,
        [CreatedBy] NVARCHAR(MAX) NULL,
        [UpdatedBy] NVARCHAR(MAX) NULL,
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        [RowVersion] ROWVERSION NULL,

        CONSTRAINT [PK_AlertasCobranza] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AlertasCobranza_Clientes_ClienteId]
            FOREIGN KEY ([ClienteId]) REFERENCES [Clientes] ([Id]),
        CONSTRAINT [FK_AlertasCobranza_Creditos_CreditoId]
            FOREIGN KEY ([CreditoId]) REFERENCES [Creditos] ([Id])
    );

    -- Crear índices
    CREATE INDEX [IX_AlertasCobranza_ClienteId] ON [AlertasCobranza] ([ClienteId]);
    CREATE INDEX [IX_AlertasCobranza_CreditoId] ON [AlertasCobranza] ([CreditoId]);
    CREATE INDEX [IX_AlertasCobranza_FechaAlerta] ON [AlertasCobranza] ([FechaAlerta]);
    CREATE INDEX [IX_AlertasCobranza_Tipo] ON [AlertasCobranza] ([Tipo]);
    CREATE INDEX [IX_AlertasCobranza_Prioridad] ON [AlertasCobranza] ([Prioridad]);
    CREATE INDEX [IX_AlertasCobranza_Resuelta] ON [AlertasCobranza] ([Resuelta]);

    PRINT 'Tabla AlertasCobranza creada exitosamente.'
END
ELSE
BEGIN
    PRINT 'La tabla AlertasCobranza ya existe.'
END
GO

-- ============================================
-- Verificar que todas las tablas se crearon
-- ============================================
PRINT ''
PRINT '===== VERIFICACIÓN DE TABLAS ====='

SELECT
    TABLE_NAME,
    TABLE_TYPE
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME IN ('ConfiguracionesMora', 'LogsMora', 'AlertasCobranza')
ORDER BY TABLE_NAME;
GO

-- Insertar configuración por defecto si no existe
IF NOT EXISTS (SELECT 1 FROM ConfiguracionesMora)
BEGIN
    PRINT ''
    PRINT 'Insertando configuración de mora por defecto...'

    INSERT INTO ConfiguracionesMora (
        DiasGracia,
        PorcentajeRecargo,
        CalculoAutomatico,
        NotificacionAutomatica,
        JobActivo,
        HoraEjecucion,
        CreatedAt,
        CreatedBy,
        IsDeleted
    )
    VALUES (
        3,           -- 3 días de gracia
        5.0,         -- 5% de recargo
        1,           -- Cálculo automático activado
        1,           -- Notificaciones activadas
        1,           -- Job activado
        '08:00:00',  -- Ejecutar a las 8 AM
        GETUTCDATE(),
        'System',
        0
    );

    PRINT 'Configuración por defecto insertada.'
END
GO
