# 🔧 Solución de Emergencia - Tablas Faltantes

## ❌ Problema
La migración `Update-Database` dice "Done" pero las tablas no existen en la base de datos. Esto causa el error:
```
Invalid object name 'DocumentosCliente'
```

## ✅ Solución Rápida

### **Opción 1: Ejecutar Scripts SQL Manualmente (RECOMENDADO)**

He creado scripts SQL que puedes ejecutar directamente en SQL Server Management Studio:

1. **Abrir SQL Server Management Studio (SSMS)**
2. **Conectarse a tu instancia de SQL Server**
3. **Seleccionar tu base de datos** (TheBuryProject o el nombre que uses)
4. **Ejecutar los scripts en este orden:**

#### Script 1: DocumentosCliente
```
Archivo: SQL_FIX_DocumentosCliente.sql
```
- Abre el archivo en SSMS
- Presiona F5 o clic en "Execute"
- Verifica que aparezca el mensaje: "Tabla DocumentosCliente creada exitosamente"

#### Script 2: EvaluacionesCredito
```
Archivo: SQL_FIX_EvaluacionCredito.sql
```
- Abre el archivo en SSMS
- Presiona F5 o clic en "Execute"
- Verifica que aparezca el mensaje: "Tabla EvaluacionesCredito creada exitosamente"

#### Script 3: Tablas de Mora
```
Archivo: SQL_FIX_Mora_Tables.sql
```
- Abre el archivo en SSMS
- Presiona F5 o clic en "Execute"
- Verifica que aparezcan los mensajes de creación de las 3 tablas:
  - ConfiguracionesMora
  - LogsMora
  - AlertasCobranza

---

### **Opción 2: Ejecutar desde Visual Studio**

Si prefieres no usar SSMS:

1. Abre **SQL Server Object Explorer** en Visual Studio
2. Conecta a tu base de datos
3. Clic derecho en la base de datos → **New Query**
4. Copia y pega el contenido de cada script
5. Ejecuta cada uno

---

### **Opción 3: Revertir y Volver a Aplicar Migración**

Si quieres intentar arreglar las migraciones (más riesgoso):

```powershell
# En Package Manager Console:

# 1. Revertir a la migración anterior
Update-Database -Migration AddVentaCreditoCuotasYMejorasCreditoPersonal

# 2. Eliminar las migraciones problemáticas
Remove-Migration
Remove-Migration
Remove-Migration

# 3. Crear nueva migración con todos los cambios
Add-Migration AddMissingTables

# 4. Aplicar la nueva migración
Update-Database
```

⚠️ **ADVERTENCIA**: Esta opción puede causar pérdida de datos si tienes información en las tablas existentes.

---

## 📋 Verificación

Después de ejecutar los scripts, verifica que las tablas existen:

```sql
-- Ejecuta esto en SSMS o SQL Server Object Explorer
SELECT TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME IN (
    'DocumentosCliente',
    'EvaluacionesCredito',
    'ConfiguracionesMora',
    'LogsMora',
    'AlertasCobranza'
)
ORDER BY TABLE_NAME;
```

**Resultado esperado:** Deberías ver las 5 tablas listadas.

---

## 🚀 Después de la Solución

1. **Reinicia la aplicación**
2. **Navega a los módulos afectados:**
   - DocumentoCliente
   - Evaluación de Crédito
   - Dashboard de Mora
3. **Verifica que no hay errores** "Invalid object name"

---

## 🔍 ¿Por qué pasó esto?

Las migraciones de Entity Framework pueden desincronizarse con la base de datos cuando:
- Se aplica una migración con código incorrecto
- Se eliminan tablas manualmente
- Se revierte código sin revertir migraciones
- Hay problemas de red durante `Update-Database`

---

## 📞 ¿Sigues teniendo problemas?

Si después de ejecutar los scripts siguen apareciendo errores:

1. **Verifica la cadena de conexión** en `appsettings.json`
2. **Verifica permisos** del usuario de SQL Server
3. **Revisa el log completo** de la aplicación para ver qué tabla específica falta
4. **Contacta al equipo de desarrollo** con el mensaje de error completo

---

**Última actualización:** 2025-11-10
**Archivos relacionados:**
- `SQL_FIX_DocumentosCliente.sql`
- `SQL_FIX_EvaluacionCredito.sql`
- `SQL_FIX_Mora_Tables.sql`
- `MIGRATION_INSTRUCTIONS.md`
