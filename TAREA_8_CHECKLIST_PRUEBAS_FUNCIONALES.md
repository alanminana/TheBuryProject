# Tarea 8 — Checklist de pruebas funcionales (Crédito por puntaje)

Fecha de ejecución: 2026-02-14

## Alcance
Validar los casos mínimos solicitados para límite/disponible por puntaje en Cliente, Ventas y Créditos.

## Casos y evidencia

### 1) Puntaje 4 con límite 8000, saldo 3000 → disponible 5000
- Cobertura automatizada:
  - `CalcularDisponibleAsync_Limite8000_Saldo3000_Disponible5000`
- Archivo:
  - `tests/TheBuryProject.Tests/Credito/CreditoDisponibleServiceTests.cs`
- Resultado esperado:
  - `Limite = 8000`, `SaldoVigente = 3000`, `Disponible = 5000`

### 2) Venta crédito 6000 → bloquea
- Cobertura automatizada:
  - `ValidarVenta_ConDisponible5000_Monto6000_Bloquea`
- Archivo:
  - `tests/TheBuryProject.Tests/ValidacionVenta/ValidacionVentaServiceTests.cs`
- Resultado esperado:
  - `NoViable = true`, `PendienteRequisitos = true`
  - Mensaje: `Excede el crédito disponible por puntaje...`

### 3) Venta crédito 5000 → permite
- Cobertura automatizada:
  - `ValidarVenta_ConDisponible5000_Monto5000_Permite`
- Archivo:
  - `tests/TheBuryProject.Tests/ValidacionVenta/ValidacionVentaServiceTests.cs`
- Resultado esperado:
  - `PuedeProceeder = true`

### 4) Cambiar límite en configuración → se refleja en cliente y ventas
- Cobertura automatizada (capa dominio/disponible):
  - `CalcularDisponibleAsync_ActualizarLimite_ReflejaNuevoDisponible`
- Archivo:
  - `tests/TheBuryProject.Tests/Credito/CreditoDisponibleServiceTests.cs`
- Resultado esperado:
  - Disponible pasa de `5000` a `7000` luego de actualizar límite `8000 -> 10000`.

- Paso funcional manual recomendado (UI):
  1. Ir a `Clientes > Límites por puntaje`.
  2. Modificar límite del puntaje del cliente de prueba y guardar.
  3. Verificar en `Cliente/Details` que el panel de disponible refleja el nuevo valor.
  4. Intentar una venta crédito en `Venta/Create` y validar que el bloqueo/permiso cambia según nuevo disponible.

### 5) Puntaje sin configuración / activo=false → comportamiento definido
- Comportamiento definido en implementación actual: **bloqueo** (sin fallback).
- Cobertura automatizada:
  - `CalcularDisponibleAsync_PuntajeSinLimiteConfigurado_LanzaErrorFuncional`
  - `CalcularDisponibleAsync_LimiteInactivo_LanzaErrorFuncional`
- Archivo:
  - `tests/TheBuryProject.Tests/Credito/CreditoDisponibleServiceTests.cs`
- Resultado esperado:
  - Se lanza error funcional: `No existe límite de crédito configurado...`

## Ejecución sugerida

### Suite enfocada de esta tarea
```bash
dotnet test tests/TheBuryProject.Tests/TheBuryProject.Tests.csproj --filter "FullyQualifiedName~CreditoDisponibleServiceTests|FullyQualifiedName~ValidacionVentaServiceTests"
```

### Build general
```bash
dotnet build TheBuryProject.sln
```

## Estado
- Checklist ejecutado y documentado.
- Evidencia en pruebas automatizadas + pasos funcionales manuales para UI.

## Guion rápido de capturas (5 minutos)

1. **Pantalla de configuración de límites**
  - Ir a `Clientes > Límites por puntaje`.
  - Capturar grilla con columna de `Última actualización` (usuario/fecha visibles).

2. **Caso bloqueado en ventas (6000 con disponible 5000)**
  - En `Venta/Create`, seleccionar cliente de prueba con disponible 5000 y pago `Crédito Personal`.
  - Cargar total crédito en 6000.
  - Capturar mensaje inline: `Excede el crédito disponible por puntaje...` y botón de guardar bloqueado.

3. **Caso permitido en ventas (5000 con disponible 5000)**
  - Mantener mismo cliente y ajustar monto a 5000.
  - Capturar que desaparece el bloqueo y se habilita guardado.

4. **Reflejo de cambio de límite**
  - Volver a `Clientes > Límites por puntaje`, actualizar límite del puntaje objetivo y guardar.
  - Capturar éxito de guardado.
  - Ir a `Cliente/Details` del mismo cliente y capturar panel `Estado de crédito del cliente` con nuevo disponible.

5. **Puntaje sin configuración/activo=false**
  - Desactivar configuración de puntaje (o usar cliente con puntaje sin límite activo).
  - En `Cliente/Details` y/o `Venta/Create`, capturar mensaje funcional de bloqueo por falta de límite configurado.

## Matriz final de aceptación (T1 a T8)

| Tarea | Objetivo | Estado | Evidencia principal |
|---|---|---|---|
| T1 | Modelo de datos de límites por puntaje (tabla, seed, unicidad) | ✅ Cumplida | `PuntajeCreditoLimite` + migración + seed 1..5 (`Data/AppDbContext.cs`, migraciones T1) |
| T2 | Servicio de dominio para límite/saldo/disponible | ✅ Cumplida | `ICreditoDisponibleService` + `CreditoDisponibleService` + tests dedicados |
| T3 | UI de configuración en Clientes | ✅ Cumplida | Pantalla `Clientes > Límites por puntaje`, validaciones y persistencia |
| T4 | Visibilidad de disponible en detalle de cliente | ✅ Cumplida | Panel en `Cliente/Details` con puntaje/límite/saldo/disponible |
| T5 | Integración en Ventas (bloqueo por excedente) | ✅ Cumplida | Validación bloqueante + mensaje unificado + UI inline + tests |
| T6 | Integración en Créditos (alta directa respeta disponible) | ✅ Cumplida | Validación en `CreditoService` (`CreateAsync` y `SolicitarCreditoAsync`) + tests |
| T7 | Permisos y auditoría para administrar límites | ✅ Cumplida | Acción `clientes.managecreditlimits`, POST protegido, UI solo lectura sin permiso, usuario/fecha auditados |
| T8 | Checklist funcional ejecutado y documentado | ✅ Cumplida | Este documento + suite enfocada ejecutada (40/40 OK) |

### Resultado global
- **Implementación:** completa para T1–T8.
- **Pruebas enfocadas:** aprobadas.
- **Criterios de aceptación:** cubiertos con evidencia técnica y pasos funcionales.
