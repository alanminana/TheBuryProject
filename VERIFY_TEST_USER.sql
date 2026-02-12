-- Verificar y corregir el usuario test

-- 1. Ver el estado actual del usuario test
SELECT 
    Id,
    UserName,
    Email,
    EmailConfirmed,
    Activo,
    LockoutEnabled,
    LockoutEnd,
    AccessFailedCount
FROM AspNetUsers
WHERE UserName = 'test';

-- 2. Si el usuario existe pero Activo = 0 o EmailConfirmed = 0, ejecutar:
-- UPDATE AspNetUsers 
-- SET Activo = 1, EmailConfirmed = 1
-- WHERE UserName = 'test';

-- 3. Verificar que tiene un rol asignado
SELECT 
    u.UserName,
    r.Name as RoleName
FROM AspNetUsers u
INNER JOIN AspNetUserRoles ur ON u.Id = ur.UserId
INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
WHERE u.UserName = 'test';

-- 4. Si necesitas ver todos los roles disponibles:
-- SELECT Id, Name FROM AspNetRoles;
