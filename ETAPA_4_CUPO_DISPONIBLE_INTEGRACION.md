# Etapa 4 — Integración con cupo disponible

## Fórmula implementada

Disponible = LimiteEfectivo - SaldoPendienteVigente

- `LimiteEfectivo`: se obtiene por regla vigente (override absoluto o `preset + excepción`), vía `CreditoDisponibleService`.
- `SaldoPendienteVigente`: suma de `SaldoPendiente` de créditos del cliente en estados vigentes (`Solicitado`, `Aprobado`, `Activo`, `PendienteConfiguracion`, `Configurado`, `Generado`).

## Aclaración de alcance

En esta implementación, el saldo vigente **ya representa la exposición actual** de créditos vigentes. Por eso:

- No se agregan “cuotas futuras” por separado.
- No se agrega “mora” por separado en esta validación.

Si en una etapa posterior se modela mora/capital/interés en un ledger específico de cupo, la fórmula puede extenderse.

## Integración en confirmación de venta a crédito

Se agregó validación de cupo en tiempo real al confirmar una venta de tipo `CreditoPersonal`:

1. Se asegura snapshot (`LimiteAplicado`) si no existía.
2. Se calcula disponible actual.
3. Si `monto operación > disponible`, se rechaza la confirmación.

Además, al crear el crédito definitivo desde el plan JSON, se revalida con el `MontoAFinanciar` para evitar confirmaciones por arriba del límite.

## Ledger de cupo

No existe entidad/tabla de ledger de cupo dedicada en el modelo actual.

- Se mantiene el comportamiento existente de descuento sobre `Credito.SaldoPendiente` al confirmar.
- Se deja trazabilidad por logs y snapshot en `Venta`.

## Test agregado

- `ConfirmarVenta_ExcedeDisponiblePorLimiteEfectivo_Falla`
  - Verifica que la venta se rechaza cuando excede el disponible calculado por límite efectivo.
  - Verifica que no se crean cuotas y no se descuenta saldo del crédito asociado.
