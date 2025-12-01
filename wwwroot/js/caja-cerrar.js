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

    const calcularDiferencia = () => {
        const efectivo = parseFloat(efectivoInput.value.replace(',', '.')) || 0;
        const cheques = parseFloat(chequesInput.value.replace(',', '.')) || 0;
        const vales = parseFloat(valesInput.value.replace(',', '.')) || 0;

        const totalReal = efectivo + cheques + vales;
        const diferencia = totalReal - montoEsperado;

        totalRealEl.textContent = `$${totalReal.toFixed(2)}`;
        diferenciaEl.textContent = `$${diferencia.toFixed(2)}`;

        if (Math.abs(diferencia) > 0.01) {
            diferenciaEl.className = diferencia > 0 ? 'text-success' : 'text-danger';
            justificacionDiv.style.display = 'block';
            justificacionInput.required = true;
        } else {
            diferenciaEl.className = 'text-success';
            justificacionDiv.style.display = 'none';
            justificacionInput.required = false;
        }
    };

    efectivoInput.addEventListener('input', calcularDiferencia);
    chequesInput.addEventListener('input', calcularDiferencia);
    valesInput.addEventListener('input', calcularDiferencia);

    calcularDiferencia();
});
