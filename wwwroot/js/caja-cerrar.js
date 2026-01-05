document.addEventListener('DOMContentLoaded', () => {
    const container = document.querySelector('[data-monto-esperado]');
    if (!container) return;

    const montoEsperado = parseFloat(container.getAttribute('data-monto-esperado')) || 0;
    const efectivoInput = document.getElementById('efectivoInput');
    const chequesInput = document.getElementById('chequesInput');
    const valesInput = document.getElementById('valesInput');
    const totalRealEl = document.getElementById('totalReal');
    const diferenciaEl = document.getElementById('diferencia');
    const justificacionDiv = document.getElementById('justificacionDiv');
    const justificacionInput = document.getElementById('justificacionInput');

    if (!efectivoInput || !chequesInput || !valesInput || !totalRealEl || !diferenciaEl || !justificacionDiv || !justificacionInput) {
        return;
    }

    const numberFormatter = new Intl.NumberFormat(undefined, {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    });

    const formatMoney = (value) => {
        if (Number.isNaN(value) || value === null || value === undefined) return '$0,00';
        return `$${numberFormatter.format(value)}`;
    };

    const setDifferenceClass = (difference) => {
        diferenciaEl.classList.remove('text-warning', 'text-success', 'text-danger', 'text-info');
        if (Math.abs(difference) > 0.01) {
            diferenciaEl.classList.add(difference > 0 ? 'text-success' : 'text-danger');
        } else {
            diferenciaEl.classList.add('text-success');
        }
    };

    const calcularDiferencia = () => {
        const efectivo = parseFloat(String(efectivoInput.value || '').replace(',', '.')) || 0;
        const cheques = parseFloat(String(chequesInput.value || '').replace(',', '.')) || 0;
        const vales = parseFloat(String(valesInput.value || '').replace(',', '.')) || 0;

        const totalReal = efectivo + cheques + vales;
        const diferencia = totalReal - montoEsperado;

        totalRealEl.textContent = formatMoney(totalReal);
        diferenciaEl.textContent = formatMoney(diferencia);
        setDifferenceClass(diferencia);

        if (Math.abs(diferencia) > 0.01) {
            justificacionDiv.style.display = 'block';
            justificacionInput.required = true;
        } else {
            justificacionDiv.style.display = 'none';
            justificacionInput.required = false;
        }
    };

    efectivoInput.addEventListener('input', calcularDiferencia);
    chequesInput.addEventListener('input', calcularDiferencia);
    valesInput.addEventListener('input', calcularDiferencia);

    calcularDiferencia();
});
