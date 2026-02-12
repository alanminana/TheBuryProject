-- Script para listar TODAS las bases de datos y sus tablas AspNetUsers
USE master;
GO

-- Listar todas las bases de datos que pueden ser TheBuryProject
SELECT 
    name as DatabaseName,
    database_id,
    create_date,
    state_desc
FROM sys.databases
WHERE name LIKE '%Bury%' OR name LIKE '%thebury%'
ORDER BY create_date DESC;
GO

-- Verificar cuántos usuarios hay en TheBuryProjectDb
USE TheBuryProjectDb;
GO

SELECT 
    'TheBuryProjectDb' as BaseDeDatos,
    COUNT(*) as TotalUsuarios,
    SUM(CASE WHEN Activo = 1 THEN 1 ELSE 0 END) as Activos
FROM AspNetUsers;
GO

SELECT 
    'TheBuryProjectDb' as BaseDeDatos,
    COUNT(*) as TotalRoles
FROM AspNetRoles;
GO
