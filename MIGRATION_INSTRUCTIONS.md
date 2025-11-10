# 🔧 Instrucciones para Aplicar Migraciones Pendientes

## ❌ Error Actual
```
Invalid object name 'DocumentosCliente'.
```

Este error ocurre porque las migraciones del código no han sido aplicadas a la base de datos.

---

## ✅ Solución: Aplicar Migraciones

### Opción 1: Usar Package Manager Console (Visual Studio)

1. **Abrir Package Manager Console**:
   - En Visual Studio: `Tools` > `NuGet Package Manager` > `Package Manager Console`

2. **Verificar estado de migraciones**:
   ```powershell
   Get-Migration
   ```

   Esto mostrará todas las migraciones disponibles y cuáles están aplicadas.

3. **Aplicar todas las migraciones pendientes**:
   ```powershell
   Update-Database
   ```

4. **O aplicar una migración específica**:
   ```powershell
   Update-Database -Migration AddDocumentosClienteModule
   Update-Database -Migration CreateAlertasCobranza
   ```

---

### Opción 2: Usar .NET CLI (Terminal/Command Prompt)

1. **Navegar al directorio del proyecto**:
   ```bash
   cd C:\Users\xh4ac\source\repos\TheBuryProject
   ```

2. **Verificar estado de migraciones**:
   ```bash
   dotnet ef migrations list
   ```

3. **Aplicar todas las migraciones pendientes**:
   ```bash
   dotnet ef database update
   ```

4. **O aplicar migraciones específicas en orden**:
   ```bash
   dotnet ef database update AddDocumentosClienteModule
   dotnet ef database update CreateAlertasCobranza
   ```

---

### Opción 3: Script SQL Manual (Si no tienes dotnet ef)

Si las opciones anteriores no funcionan, puedes generar un script SQL:

1. **Generar script SQL desde Visual Studio**:
   - En Package Manager Console:
   ```powershell
   Script-Migration -From 0 -To CreateAlertasCobranza -Output migration_script.sql
   ```

2. **Ejecutar el script en SQL Server Management Studio**:
   - Abrir SSMS
   - Conectarse a tu instancia de SQL Server
   - Abrir el archivo `migration_script.sql`
   - Ejecutar el script

---

## 📋 Migraciones Pendientes (en orden)

Las siguientes migraciones deben aplicarse en este orden:

1. ✅ `InitialCreate` - (Ya aplicada)
2. ✅ `AddCreditosYCuotasModule` - (Ya aplicada)
3. ✅ `AddVentasModule` - (Ya aplicada)
4. ✅ `AddConfiguracionesTarjetaYDatosTarjeta` - (Ya aplicada)
5. ✅ `AddVentaCreditoCuotasYMejorasCreditoPersonal` - (Ya aplicada)
6. ⚠️ `AddEvaluacionCreditoModule` - **PENDIENTE**
7. ⚠️ `AddDocumentosClienteModule` - **PENDIENTE** (Esta es la que falta)
8. ⚠️ `CreateAlertasCobranza` - **PENDIENTE**

---

## 🔍 Verificar que las Migraciones se Aplicaron

Después de aplicar las migraciones, puedes verificar que las tablas existen:

### En SQL Server Management Studio:
```sql
-- Verificar que la tabla DocumentosCliente existe
SELECT * FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = 'DocumentosCliente';

-- Verificar que la tabla AlertasCobranza existe
SELECT * FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = 'AlertasCobranza';

-- Verificar que la tabla ConfiguracionesMora existe
SELECT * FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = 'ConfiguracionesMora';

-- Verificar todas las migraciones aplicadas
SELECT * FROM __EFMigrationsHistory ORDER BY MigrationId;
```

---

## ⚠️ Problemas Comunes

### Error: "A network-related or instance-specific error"
- Verifica que SQL Server esté corriendo
- Verifica la cadena de conexión en `appsettings.json`

### Error: "Cannot drop the table because it is being referenced"
- Asegúrate de aplicar las migraciones en orden
- Si necesitas revertir, usa: `Update-Database -Migration <PreviousMigration>`

### Error: "dotnet ef not found"
- Instala las herramientas de EF Core:
  ```bash
  dotnet tool install --global dotnet-ef
  ```

---

## 📝 Después de Aplicar las Migraciones

1. **Reiniciar la aplicación** para asegurarte de que los cambios surtan efecto
2. **Verificar que no hay errores** en la consola
3. **Probar la funcionalidad** de DocumentoCliente

---

## 🆘 Si Nada Funciona

Como última opción, puedes crear las tablas manualmente:

```sql
-- Crear tabla DocumentosCliente
CREATE TABLE [DocumentosCliente] (
    [Id] int NOT NULL IDENTITY,
    [ClienteId] int NOT NULL,
    [TipoDocumento] int NOT NULL,
    [NombreArchivo] nvarchar(200) NOT NULL,
    [RutaArchivo] nvarchar(500) NOT NULL,
    [TipoMIME] nvarchar(100) NULL,
    [TamanoBytes] bigint NOT NULL,
    [Estado] int NOT NULL,
    [FechaSubida] datetime2 NOT NULL,
    [FechaVencimiento] datetime2 NULL,
    [FechaVerificacion] datetime2 NULL,
    [VerificadoPor] nvarchar(100) NULL,
    [Observaciones] nvarchar(1000) NULL,
    [MotivoRechazo] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedBy] nvarchar(max) NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_DocumentosCliente] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DocumentosCliente_Clientes_ClienteId] FOREIGN KEY ([ClienteId]) REFERENCES [Clientes] ([Id]) ON DELETE NO ACTION
);

CREATE INDEX [IX_DocumentosCliente_ClienteId] ON [DocumentosCliente] ([ClienteId]);
CREATE INDEX [IX_DocumentosCliente_Estado] ON [DocumentosCliente] ([Estado]);
CREATE INDEX [IX_DocumentosCliente_FechaSubida] ON [DocumentosCliente] ([FechaSubida]);
CREATE INDEX [IX_DocumentosCliente_FechaVencimiento] ON [DocumentosCliente] ([FechaVencimiento]);
CREATE INDEX [IX_DocumentosCliente_TipoDocumento] ON [DocumentosCliente] ([TipoDocumento]);

-- Registrar la migración
INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
VALUES ('20251108174231_AddDocumentosClienteModule', '8.0.0');
```

---

## ✅ Resultado Esperado

Después de aplicar las migraciones, la aplicación debería:
- ✅ Iniciar sin errores de "Invalid object name"
- ✅ Poder acceder al módulo de DocumentoCliente
- ✅ Poder subir y gestionar documentos de clientes

---

**Última actualización**: 2025-11-10
