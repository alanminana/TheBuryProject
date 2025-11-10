-- =============================================
-- Script para crear tabla DocumentosCliente
-- Ejecutar en SQL Server Management Studio
-- =============================================

-- Verificar si la tabla existe
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'DocumentosCliente')
BEGIN
    PRINT 'Creando tabla DocumentosCliente...'

    CREATE TABLE [dbo].[DocumentosCliente] (
        [Id] INT NOT NULL IDENTITY(1,1),
        [ClienteId] INT NOT NULL,
        [TipoDocumento] INT NOT NULL,
        [NombreArchivo] NVARCHAR(200) NOT NULL,
        [RutaArchivo] NVARCHAR(500) NOT NULL,
        [TipoMIME] NVARCHAR(100) NULL,
        [TamanoBytes] BIGINT NOT NULL,
        [Estado] INT NOT NULL,
        [FechaSubida] DATETIME2 NOT NULL,
        [FechaVencimiento] DATETIME2 NULL,
        [FechaVerificacion] DATETIME2 NULL,
        [VerificadoPor] NVARCHAR(100) NULL,
        [Observaciones] NVARCHAR(1000) NULL,
        [MotivoRechazo] NVARCHAR(500) NULL,
        [CreatedAt] DATETIME2 NOT NULL,
        [UpdatedAt] DATETIME2 NULL,
        [CreatedBy] NVARCHAR(MAX) NULL,
        [UpdatedBy] NVARCHAR(MAX) NULL,
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        [RowVersion] ROWVERSION NULL,

        CONSTRAINT [PK_DocumentosCliente] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DocumentosCliente_Clientes_ClienteId]
            FOREIGN KEY ([ClienteId]) REFERENCES [Clientes] ([Id])
    );

    -- Crear índices
    CREATE INDEX [IX_DocumentosCliente_ClienteId] ON [DocumentosCliente] ([ClienteId]);
    CREATE INDEX [IX_DocumentosCliente_Estado] ON [DocumentosCliente] ([Estado]);
    CREATE INDEX [IX_DocumentosCliente_FechaSubida] ON [DocumentosCliente] ([FechaSubida]);
    CREATE INDEX [IX_DocumentosCliente_FechaVencimiento] ON [DocumentosCliente] ([FechaVencimiento]);
    CREATE INDEX [IX_DocumentosCliente_TipoDocumento] ON [DocumentosCliente] ([TipoDocumento]);

    PRINT 'Tabla DocumentosCliente creada exitosamente.'
END
ELSE
BEGIN
    PRINT 'La tabla DocumentosCliente ya existe.'
END
GO

-- Verificar que se creó correctamente
SELECT
    TABLE_NAME,
    TABLE_TYPE
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = 'DocumentosCliente';
GO

-- Ver estructura de la tabla
SELECT
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'DocumentosCliente'
ORDER BY ORDINAL_POSITION;
GO
