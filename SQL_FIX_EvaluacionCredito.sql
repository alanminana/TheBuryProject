-- =============================================
-- Script para crear tabla EvaluacionesCredito
-- Ejecutar en SQL Server Management Studio
-- =============================================

-- Verificar si la tabla existe
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'EvaluacionesCredito')
BEGIN
    PRINT 'Creando tabla EvaluacionesCredito...'

    CREATE TABLE [dbo].[EvaluacionesCredito] (
        [Id] INT NOT NULL IDENTITY(1,1),
        [ClienteId] INT NOT NULL,
        [CreditoId] INT NULL,
        [MontoSolicitado] DECIMAL(18,2) NOT NULL,
        [FechaEvaluacion] DATETIME2 NOT NULL,

        -- Resultado de la evaluación
        [ResultadoSemaforo] INT NOT NULL, -- 0=Rojo, 1=Amarillo, 2=Verde
        [PuntajeFinal] DECIMAL(5,2) NOT NULL,
        [Aprobado] BIT NOT NULL,
        [MotivoRechazo] NVARCHAR(MAX) NULL,

        -- Factores de evaluación
        [PuntajeRiesgoCliente] DECIMAL(5,2) NOT NULL,
        [TieneGarante] BIT NOT NULL,
        [TieneHistorialPositivo] BIT NOT NULL,
        [RelacionCuotaIngreso] DECIMAL(5,4) NULL,
        [TieneDocumentosCompletos] BIT NOT NULL,

        -- Observaciones
        [Observaciones] NVARCHAR(MAX) NULL,
        [EvaluadoPor] NVARCHAR(100) NULL,

        -- Auditoría
        [CreatedAt] DATETIME2 NOT NULL,
        [UpdatedAt] DATETIME2 NULL,
        [CreatedBy] NVARCHAR(MAX) NULL,
        [UpdatedBy] NVARCHAR(MAX) NULL,
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        [RowVersion] ROWVERSION NULL,

        CONSTRAINT [PK_EvaluacionesCredito] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EvaluacionesCredito_Clientes_ClienteId]
            FOREIGN KEY ([ClienteId]) REFERENCES [Clientes] ([Id]),
        CONSTRAINT [FK_EvaluacionesCredito_Creditos_CreditoId]
            FOREIGN KEY ([CreditoId]) REFERENCES [Creditos] ([Id])
    );

    -- Crear índices
    CREATE INDEX [IX_EvaluacionesCredito_ClienteId] ON [EvaluacionesCredito] ([ClienteId]);
    CREATE INDEX [IX_EvaluacionesCredito_CreditoId] ON [EvaluacionesCredito] ([CreditoId]);
    CREATE INDEX [IX_EvaluacionesCredito_FechaEvaluacion] ON [EvaluacionesCredito] ([FechaEvaluacion]);
    CREATE INDEX [IX_EvaluacionesCredito_ResultadoSemaforo] ON [EvaluacionesCredito] ([ResultadoSemaforo]);
    CREATE INDEX [IX_EvaluacionesCredito_Aprobado] ON [EvaluacionesCredito] ([Aprobado]);

    PRINT 'Tabla EvaluacionesCredito creada exitosamente.'
END
ELSE
BEGIN
    PRINT 'La tabla EvaluacionesCredito ya existe.'
END
GO

-- Verificar que se creó correctamente
SELECT
    TABLE_NAME,
    TABLE_TYPE
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = 'EvaluacionesCredito';
GO

-- Ver estructura de la tabla
SELECT
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'EvaluacionesCredito'
ORDER BY ORDINAL_POSITION;
GO
