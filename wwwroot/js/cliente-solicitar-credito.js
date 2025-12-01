(() => {
    const form = document.getElementById('formSolicitarCredito');

    if (!form) return;

    const montoInput = document.getElementById('montoSolicitado');
    const cuotasInput = document.getElementById('cantidadCuotas');
    const tasaInput = document.getElementById('tasaInteres');
    const cuotaLabel = document.getElementById('cuotaMensual');
    const montoTotalLabel = document.getElementById('montoTotalConInteres');
    const capacidadPago = parseFloat(form.dataset.capacidadPago || '0') || 0;

    const formatCurrency = (value) => value.toLocaleString('es-AR', {
        style: 'currency',
        currency: 'ARS'
    });

    function calcularCuota() {
        if (!montoInput || !cuotasInput || !tasaInput || !cuotaLabel || !montoTotalLabel) {
            return;
        }

        const monto = parseFloat(montoInput.value) || 0;
        const cuotas = parseInt(cuotasInput.value, 10) || 1;
        const tasaMensual = (parseFloat(tasaInput.value) || 0) / 100;

        if (monto <= 0 || cuotas <= 0) {
            return;
        }

        let cuotaMensual;
        if (tasaMensual > 0) {
            const factor = Math.pow(1 + tasaMensual, cuotas);
            cuotaMensual = monto * (tasaMensual * factor) / (factor - 1);
        } else {
            cuotaMensual = monto / cuotas;
        }

        const montoTotal = cuotaMensual * cuotas;

        cuotaLabel.textContent = formatCurrency(cuotaMensual);
        montoTotalLabel.textContent = formatCurrency(montoTotal);

        if (cuotaMensual > capacidadPago) {
            cuotaLabel.classList.remove('text-success');
            cuotaLabel.classList.add('text-danger');
        } else {
            cuotaLabel.classList.remove('text-danger');
            cuotaLabel.classList.add('text-success');
        }
    }

    montoInput?.addEventListener('input', calcularCuota);
    cuotasInput?.addEventListener('change', calcularCuota);
    tasaInput?.addEventListener('input', calcularCuota);

    document.addEventListener('DOMContentLoaded', calcularCuota);
})();
