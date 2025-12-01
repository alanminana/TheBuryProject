(() => {
    const form = document.getElementById('formSolicitarCredito');

    if (!form) return;

    const montoInput = document.getElementById('montoSolicitado');
    const cuotasInput = document.getElementById('cantidadCuotas');
    const tasaInput = document.getElementById('tasaInteres');
    const cuotaLabel = document.getElementById('cuotaMensual');
    const montoTotalLabel = document.getElementById('montoTotalConInteres');
    const capacidadPago = parseFloat(form.dataset.capacidadPago || '0') || 0;
    const calculoUrl = form.dataset.calculoCreditoUrl;
    const antiforgeryToken = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

    const formatCurrency = (value) => value.toLocaleString('es-AR', {
        style: 'currency',
        currency: 'ARS'
    });

    function mostrarErrorPreview() {
        cuotaLabel.textContent = '--';
        montoTotalLabel.textContent = '--';
        cuotaLabel.classList.remove('text-success');
        cuotaLabel.classList.add('text-danger');
    }

    function calcularCuota() {
        if (!montoInput || !cuotasInput || !tasaInput || !cuotaLabel || !montoTotalLabel || !calculoUrl) {
            return;
        }

        const monto = parseFloat(montoInput.value) || 0;
        const cuotas = parseInt(cuotasInput.value, 10) || 1;
        const tasaInteres = parseFloat(tasaInput.value) || 0;

        if (monto <= 0 || cuotas <= 0) {
            mostrarErrorPreview();
            return;
        }

        fetch(calculoUrl, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                ...(antiforgeryToken ? { RequestVerificationToken: antiforgeryToken } : {})
            },
            body: JSON.stringify({
                montoSolicitado: monto,
                tasaInteres: tasaInteres,
                cantidadCuotas: cuotas,
                capacidadPagoMensual: capacidadPago
            })
        })
            .then(response => {
                if (!response.ok) {
                    throw new Error('No se pudo calcular la cuota');
                }
                return response.json();
            })
            .then(data => {
                const cuotaMensual = data.cuotaMensual ?? 0;
                const montoTotal = data.montoTotal ?? 0;

                cuotaLabel.textContent = formatCurrency(cuotaMensual);
                montoTotalLabel.textContent = formatCurrency(montoTotal);

                if (data.superaCapacidadPago) {
                    cuotaLabel.classList.remove('text-success');
                    cuotaLabel.classList.add('text-danger');
                } else {
                    cuotaLabel.classList.remove('text-danger');
                    cuotaLabel.classList.add('text-success');
                }
            })
            .catch(() => {
                mostrarErrorPreview();
            });
    }

    montoInput?.addEventListener('input', calcularCuota);
    cuotasInput?.addEventListener('change', calcularCuota);
    tasaInput?.addEventListener('input', calcularCuota);

    document.addEventListener('DOMContentLoaded', calcularCuota);
})();
