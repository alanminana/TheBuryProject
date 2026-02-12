-- Script para verificar todos los usuarios y su estado
-- Ejecutar en SQL Server Management Studio o Azure Data Studio

USE TheBuryProjectDb;
GO

-- 1. Ver TODOS los usuarios con su estado completo
SELECT 
    Id,
    UserName,
    Email,
    EmailConfirmed,
    Activo,
    LockoutEnabled,
    LockoutEnd,
    AccessFailedCount,
    PhoneNumber,
    PhoneNumberConfirmed,
    TwoFactorEnabled,
    ConcurrencyStamp
FROM AspNetUsers
ORDER BY FechaCreacion DESC;

-- 2. Ver usuarios con sus roles asignados
SELECT 
    u.UserName,
    u.Email,
    u.EmailConfirmed,
    u.Activo,
    r.Name as RoleName
FROM AspNetUsers u
LEFT JOIN AspNetUserRoles ur ON u.Id = ur.UserId
LEFT JOIN AspNetRoles r ON ur.RoleId = r.Id
ORDER BY u.UserName;

-- 3. Ver usuarios que NO pueden iniciar sesión (problemas potenciales)
SELECT 
    UserName,
    Email,
    EmailConfirmed,
    Activo,
    LockoutEnabled,
    LockoutEnd,
    AccessFailedCount,
    CASE 
        WHEN Activo = 0 THEN '❌ Usuario INACTIVO'
        WHEN EmailConfirmed = 0 THEN '⚠️ Email NO confirmado'
        WHEN LockoutEnd IS NOT NULL AND LockoutEnd > GETUTCDATE() THEN '🔒 Usuario BLOQUEADO'
        WHEN AccessFailedCount >= 5 THEN '⚠️ Demasiados intentos fallidos'
        ELSE '✅ OK'
    END as Estado
FROM AspNetUsers
WHERE Activo = 0 
   OR EmailConfirmed = 0 
   OR (LockoutEnd IS NOT NULL AND LockoutEnd > GETUTCDATE())
   OR AccessFailedCount >= 5;

-- 4. Si necesitas ACTIVAR un usuario específico (ejemplo: test)
/*
UPDATE AspNetUsers 
SET 
    Activo = 1, 
    EmailConfirmed = 1,
    LockoutEnd = NULL,
    AccessFailedCount = 0
WHERE UserName = 'test' OR Email = 'test@thebury.com';
*/

-- 5. Ver todos los roles disponibles
SELECT Id, Name, NormalizedName 
FROM AspNetRoles 
ORDER BY Name;
