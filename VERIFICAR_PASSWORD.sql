-- Script para resetear password del usuario test@test.com
-- NOTA: No podemos establecer passwords directamente en SQL porque están hasheadas
-- Este script verifica el estado del usuario

USE TheBuryProjectDb;
GO

-- Ver estado completo del usuario
SELECT 
    Id,
    UserName,
    Email,
    EmailConfirmed,
    Activo,
    LockoutEnabled,
    LockoutEnd,
    AccessFailedCount,
    PasswordHash,
    CASE 
        WHEN PasswordHash IS NULL THEN '❌ SIN PASSWORD'
        WHEN LEN(PasswordHash) > 50 THEN '✅ Tiene password hasheada'
        ELSE '⚠️ Password inválida'
    END as EstadoPassword
FROM AspNetUsers
WHERE Email = 'test@test.com';
GO

-- Ver roles asignados
SELECT 
    u.Email,
    r.Name as Rol
FROM AspNetUsers u
LEFT JOIN AspNetUserRoles ur ON u.Id = ur.UserId
LEFT JOIN AspNetRoles r ON ur.RoleId = r.Id
WHERE u.Email = 'test@test.com';
GO

-- IMPORTANTE: Para cambiar la password, DEBES usar la herramienta web:
-- https://localhost:7189/Diagnostico/ResetPassword
PRINT '⚠️ Para resetear la password, usa: https://localhost:7189/Diagnostico/ResetPassword'
GO
