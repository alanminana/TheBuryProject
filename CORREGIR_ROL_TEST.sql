-- Script para corregir el rol del usuario test@test.com
USE TheBuryProjectDb;
GO

SET QUOTED_IDENTIFIER ON;
GO

-- 1. Ver estado actual
SELECT 'ANTES:' as Estado, u.UserName, u.Email, r.Name as RolActual
FROM AspNetUsers u
INNER JOIN AspNetUserRoles ur ON u.Id = ur.UserId
INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
WHERE u.Email = 'test@test.com';
GO

-- 2. Eliminar el rol incorrecto "test vendedor"
DELETE FROM AspNetUserRoles
WHERE UserId = (SELECT Id FROM AspNetUsers WHERE Email = 'test@test.com')
  AND RoleId = (SELECT Id FROM AspNetRoles WHERE Name = 'test vendedor');
GO

-- 3. Asignar el rol correcto "Vendedor"
INSERT INTO AspNetUserRoles (UserId, RoleId)
VALUES (
    (SELECT Id FROM AspNetUsers WHERE Email = 'test@test.com'),
    (SELECT Id FROM AspNetRoles WHERE Name = 'Vendedor')
);
GO

-- 4. Eliminar el rol "test vendedor" del sistema (opcional - limpieza)
DELETE FROM AspNetRoles WHERE Name = 'test vendedor';
GO

-- 5. Verificar resultado final
SELECT 'DESPUÉS:' as Estado, u.UserName, u.Email, r.Name as RolCorrect
FROM AspNetUsers u
INNER JOIN AspNetUserRoles ur ON u.Id = ur.UserId
INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
WHERE u.Email = 'test@test.com';
GO

PRINT '✅ Usuario test@test.com corregido con rol Vendedor'
GO
