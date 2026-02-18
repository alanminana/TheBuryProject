# Etapa 1 — Inventario y regla de negocio de límite efectivo (bloqueante)

## Objetivo
Dejar **explícita y verificable** una única definición de límite efectivo para evitar la ambigüedad actual de “¿se suma o se reemplaza?”.

## Regla de negocio propuesta (recomendada)
Separar conceptos:

- `limiteBase`: preset por puntaje (tabla de presets).
- `limiteOverride`: monto personalizado absoluto que reemplaza todo (nullable).
- `excepcionDelta`: aumento temporal/condicionado que suma al base (nullable), con vigencia y motivo.

Fórmula objetivo:

`limiteEfectivo = limiteOverride ?? (limiteBase + (excepcionDelta ?? 0))`

## Inventario actual del código

### 1) Dónde se calcula hoy el límite
- `Services/CreditoDisponibleService.cs` (`CalcularDisponibleAsync`):
  - Obtiene `limitePorPuntaje`.
  - Si `Cliente.LimiteCredito` es mayor, lo usa.
  - Si `Cliente.MontoMaximoPersonalizado` es mayor, lo usa.
  - Regla efectiva actual: **elige el mayor valor** entre presets y límites manuales.

### 2) Dónde se usa ese límite en flujo de venta/crédito
- `Services/ClienteAptitudService.cs` (`EvaluarCupoInternoAsync`): usa `CalcularDisponibleAsync` para aptitud/cupo.
- `Services/ValidacionVentaService.cs` (`PoblarDatosBasicos`): propaga `aptitud.Cupo.LimiteCredito` y cupo disponible.
- `Services/CreditoService.cs` (`ValidarMontoDentroDelDisponibleAsync`): bloquea montos que exceden disponible.

### 3) Dónde se recalcula por puntaje
- `Services/ClienteService.cs` (`ActualizarPuntajeRiesgoAsync`) actualiza puntaje del cliente.
- El límite se vuelve a derivar indirectamente en cada `CalcularDisponibleAsync` según `NivelRiesgo`/preset.

### 4) Hallazgos de riesgo
- En `Models/Entities/Cliente.cs` existe un único campo semántico fuerte para límite manual (`LimiteCredito`) y otro (`MontoMaximoPersonalizado`) que también impacta cálculo por “máximo”.
- No existe hoy un modelo explícito para:
  - `limiteOverride` (con semántica de reemplazo),
  - `excepcionDelta` (con semántica aditiva),
  - vigencia/motivo de excepción de límite.

## Casos de aceptación (definidos para tests)
1. Cliente con preset, sin excepción, sin override → límite = preset.
2. Cliente con preset + excepciónDelta → límite = preset + delta.
3. Cliente con preset + override → límite = override (NO suma).
4. Cliente con override y luego cambio de preset por puntaje → límite sigue = override.
5. Cliente con excepción vencida → delta no aplica.
6. Venta ya generada: si luego cambia el límite del cliente → la venta existente no cambia (snapshot).

## Estado de Etapa 1
- Se crea suite de tests de aceptación en `tests/TheBuryProject.Tests/Credito/LimiteEfectivoAcceptanceTests.cs`.
- Dado el estado actual del modelo, se espera que varios tests fallen inicialmente para reflejar la brecha funcional de la regla objetivo.