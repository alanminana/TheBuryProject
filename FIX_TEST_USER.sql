-- Script para verificar y corregir el usuario test@test.com
-- Ejecutar en SQL Server Management Studio o Azure Data Studio

USE TheBuryProjectDb;
GO

-- 1. Verificar el estado actual del usuario test@test.com
SELECT 
    Id,
    UserName,
    Email,
    EmailConfirmed,
    Activo,
    LockoutEnabled,
    LockoutEnd,
    AccessFailedCount,
    FechaCreacion,
    CASE 
        WHEN Activo = 0 THEN '❌ Usuario INACTIVO - no puede loguearse'
        WHEN EmailConfirmed = 0 THEN '❌ Email NO confirmado - no puede loguearse'
        WHEN LockoutEnd IS NOT NULL AND LockoutEnd > GETUTCDATE() THEN '🔒 Usuario BLOQUEADO'
        WHEN AccessFailedCount >= 5 THEN '⚠️ Demasiados intentos fallidos'
        ELSE '✅ Usuario OK'
    END as Estado
FROM AspNetUsers
WHERE Email = 'test@test.com' OR UserName = 'test@test.com';

-- 2. Verificar si tiene roles asignados
SELECT 
    u.UserName,
    u.Email,
    r.Name as RolAsignado
FROM AspNetUsers u
LEFT JOIN AspNetUserRoles ur ON u.Id = ur.UserId
LEFT JOIN AspNetRoles r ON ur.RoleId = r.Id
WHERE u.Email = 'test@test.com' OR u.UserName = 'test@test.com';

-- 3. CORREGIR el usuario test@test.com para que pueda loguearse
UPDATE AspNetUsers 
SET 
    Activo = 1,                    -- Activar el usuario
    EmailConfirmed = 1,            -- Confirmar el email
    LockoutEnd = NULL,             -- Quitar bloqueo si existe
    AccessFailedCount = 0          -- Resetear intentos fallidos
WHERE Email = 'test@test.com' OR UserName = 'test@test.com';

-- Mensaje de confirmación
SELECT 
    '✅ Usuario corregido' as Resultado,
    UserName,
    Email,
    EmailConfirmed,
    Activo
FROM AspNetUsers
WHERE Email = 'test@test.com' OR UserName = 'test@test.com';

-- 4. Si el usuario NO tiene rol asignado, asignar rol de Vendedor
-- Primero verificar si tiene rol
DECLARE @UserId NVARCHAR(450);
DECLARE @VendedorRoleId NVARCHAR(450);

SELECT @UserId = Id FROM AspNetUsers WHERE Email = 'test@test.com' OR UserName = 'test@test.com';
SELECT @VendedorRoleId = Id FROM AspNetRoles WHERE Name = 'Vendedor';

IF @UserId IS NOT NULL AND @VendedorRoleId IS NOT NULL
BEGIN
    -- Solo insertar si no existe la relación
    IF NOT EXISTS (SELECT 1 FROM AspNetUserRoles WHERE UserId = @UserId)
    BEGIN
        INSERT INTO AspNetUserRoles (UserId, RoleId)
        VALUES (@UserId, @VendedorRoleId);
        
        PRINT '✅ Rol Vendedor asignado al usuario test@test.com';
    END
    ELSE
    BEGIN
        PRINT 'ℹ️ El usuario ya tiene un rol asignado';
    END
END

-- Verificación final
SELECT 
    u.UserName,
    u.Email,
    u.EmailConfirmed,
    u.Activo,
    r.Name as Rol,
    '✅ Usuario listo para login' as Estado
FROM AspNetUsers u
LEFT JOIN AspNetUserRoles ur ON u.Id = ur.UserId
LEFT JOIN AspNetRoles r ON ur.RoleId = r.Id
WHERE u.Email = 'test@test.com' OR u.UserName = 'test@test.com';
