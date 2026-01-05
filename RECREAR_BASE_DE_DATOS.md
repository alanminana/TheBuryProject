# 🔄 RECREAR BASE DE DATOS DESDE CERO

## ✅ Solución Simple y Rápida

Esta es la manera más fácil de solucionar todos los problemas de migraciones.

---

## 📋 Opción 1: Desde Visual Studio (Package Manager Console)

### **Paso 1: Abrir Package Manager Console**
- En Visual Studio: `Tools` → `NuGet Package Manager` → `Package Manager Console`

### **Paso 2: Eliminar la base de datos**
```powershell
Drop-Database
```

**Resultado esperado:**
```
Build succeeded.
Dropping database 'TheBuryProject' on server '(localdb)\mssqllocaldb'.
Successfully dropped database 'TheBuryProject'.
Done.
```

### **Paso 3: Recrear la base de datos con TODAS las migraciones**
```powershell
Update-Database
```

**Resultado esperado:**
```
Build succeeded.
Applying migration '20251106042027_InitialCreate'.
Applying migration '20251107063024_AddCreditosYCuotasModule'.
Applying migration '20251107163553_AddVentasModule'.
Applying migration '20251107184835_AddConfiguracionesTarjetaYDatosTarjeta'.
Applying migration '20251107203320_AddVentaCreditoCuotasYMejorasCreditoPersonal'.
Applying migration '20251108164203_AddEvaluacionCreditoModule'.
Applying migration '20251108174231_AddDocumentosClienteModule'.
Applying migration '20251109043728_CreateAlertasCobranza'.
Done.
```

### **Paso 4: Reiniciar la aplicación**
- Presiona F5 en Visual Studio
- La aplicación debería funcionar sin errores

---

## 📋 Opción 2: Desde Terminal/Command Prompt

### **Paso 1: Navegar al directorio del proyecto**
```bash
cd C:\Users\xh4ac\source\repos\TheBuryProject
```

### **Paso 2: Eliminar la base de datos**
```bash
dotnet ef database drop --force
```

**Nota:** `--force` elimina sin preguntar confirmación

### **Paso 3: Recrear la base de datos**
```bash
dotnet ef database update
```

### **Paso 4: Reiniciar la aplicación**
```bash
dotnet run
```

---

## 📋 Opción 3: Comando TODO EN UNO (Más rápido)

En **Package Manager Console**:

```powershell
Drop-Database -Confirm:$false; Update-Database
```

O en **Terminal**:

```bash
dotnet ef database drop --force && dotnet ef database update
```

---

## ⚠️ ADVERTENCIAS

### **¿Perderé mis datos?**
**SÍ** - Esta operación elimina TODA la base de datos incluyendo:
- ✅ Todas las tablas
- ✅ Todos los datos
- ✅ Todos los usuarios
- ✅ Todo el historial de migraciones

### **¿Cuándo NO hacer esto?**
❌ NO hagas esto si tienes datos importantes en producción
❌ NO hagas esto si tienes datos de prueba que no quieres perder

### **¿Cuándo SÍ hacer esto?**
✅ Estás en desarrollo
✅ No tienes datos importantes
✅ Puedes recrear los datos de prueba fácilmente
✅ Quieres empezar limpio

---

## 🔍 Verificar que funcionó

Después de recrear la base de datos, puedes verificar que todas las tablas existen:

### En Package Manager Console:
```powershell
# Ver todas las migraciones aplicadas
Get-Migration
```

### En Terminal:
```bash
# Listar migraciones
dotnet ef migrations list
```

**Deberías ver todas las migraciones con "Applied" o "Pending: no"**

---

## 🎯 Resultado Esperado

Después de recrear la base de datos:

✅ Base de datos vacía y limpia
✅ TODAS las tablas creadas correctamente:
   - DocumentosCliente
   - EvaluacionesCredito
   - ConfiguracionesMora
   - LogsMora
   - AlertasCobranza
   - (y todas las demás)

✅ Sin errores de "Invalid object name"
✅ Aplicación funciona correctamente

---

## 🆘 Si algo sale mal

### Error: "dotnet command not found"
- Instala .NET SDK desde: https://dotnet.microsoft.com/download

### Error: "Cannot drop database because it is currently in use"
- Cierra Visual Studio
- Cierra todas las conexiones a la base de datos
- Intenta de nuevo

### Error: "A network-related error occurred"
- Verifica que SQL Server LocalDB esté corriendo
- Ejecuta: `sqllocaldb start mssqllocaldb`

### Error: "Cannot create file '...TheBuryProjectDb.mdf' because it already exists" (SQL Error 5170)
Esto pasa cuando quedó un archivo `.mdf` “huérfano” en disco y LocalDB intenta crear la base con el mismo nombre/ruta.

Pasos (desarrollo):
1. Detener LocalDB:
   - `sqllocaldb stop mssqllocaldb`
2. Borrar los archivos huérfanos (si existen):
   - `C:\Users\<tu_usuario>\TheBuryProjectDb.mdf`
   - `C:\Users\<tu_usuario>\TheBuryProjectDb_log.ldf` (si existe)
3. Volver a iniciar LocalDB:
   - `sqllocaldb start mssqllocaldb`
4. Recrear la base:
   - `dotnet ef database drop --force`
   - `dotnet ef database update`

Si no querés borrar archivos manualmente, alternativa: cambiar temporalmente el nombre de la DB en `appsettings.Development.json` (por ejemplo `TheBuryProjectDb_Dev`) para regenerar una base limpia.

---

## 💡 Consejo

Después de recrear la base de datos, necesitarás:
1. Crear usuario de prueba (si usas Identity)
2. Agregar datos de prueba (clientes, productos, etc.)
3. Configurar datos iniciales que necesites

---

## 📝 Notas Técnicas

**¿Qué hace `Drop-Database`?**
- Elimina completamente la base de datos del servidor
- Borra el archivo .mdf y .ldf (si es LocalDB)
- Limpia todas las referencias

**¿Qué hace `Update-Database`?**
- Crea la base de datos si no existe
- Aplica TODAS las migraciones en orden
- Inserta datos seed (categorías, marcas)
- Crea tablas, índices, foreign keys

---

**Última actualización:** 2025-11-10
**Tiempo estimado:** 1-2 minutos
