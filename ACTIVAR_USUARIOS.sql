-- Script para activar usuarios principales y verificar estado
USE TheBuryProjectDb;
GO

SET QUOTED_IDENTIFIER ON;
GO

-- Mostrar estado ANTES de la actualización
PRINT '========== ESTADO ANTES DE ACTUALIZAR =========='
SELECT 
    UserName, 
    Email, 
    EmailConfirmed, 
    Activo,
    CASE WHEN Activo = 1 THEN '✓ Activo' ELSE '✗ Inactivo' END as Estado
FROM AspNetUsers
ORDER BY FechaCreacion DESC;
GO

-- Activar usuarios principales
UPDATE AspNetUsers 
SET Activo = 1 
WHERE Email IN (
    'admin@thebury.com', 
    'gerente@thebury.com', 
    'vendedor@thebury.com', 
    'cajero@thebury.com',
    'repositor@thebury.com',
    'tecnico@thebury.com',
    'contador@thebury.com',
    'administrador@thebury.com'
);
GO

PRINT '========== ESTADO DESPUÉS DE ACTUALIZAR =========='
SELECT 
    UserName, 
    Email, 
    EmailConfirmed, 
    Activo,
    CASE WHEN Activo = 1 THEN '✓ Activo' ELSE '✗ Inactivo' END as Estado
FROM AspNetUsers
ORDER BY FechaCreacion DESC;
GO

-- Resumen final
PRINT '========== RESUMEN =========='
SELECT 
    COUNT(*) as TotalUsuarios,
    SUM(CASE WHEN Activo = 1 THEN 1 ELSE 0 END) as Activos,
    SUM(CASE WHEN Activo = 0 THEN 1 ELSE 0 END) as Inactivos
FROM AspNetUsers;
GO
