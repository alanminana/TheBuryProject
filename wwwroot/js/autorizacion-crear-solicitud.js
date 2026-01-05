(() => {
    const valorSolicitado = document.getElementById('ValorSolicitado');
    const valorPermitido = document.getElementById('ValorPermitido');
    const alertDiferencia = document.getElementById('alertDiferencia');
    const diferencia = document.getElementById('diferencia');
    const justificacion = document.getElementById('Justificacion');
    const charCount = document.getElementById('charCount');

    function calcularDiferencia() {
        if (!valorSolicitado || !valorPermitido || !alertDiferencia || !diferencia) return;

        const solicitado = parseFloat(valorSolicitado.value) || 0;
        const permitido = parseFloat(valorPermitido.value) || 0;

        if (solicitado > 0 && permitido > 0 && solicitado > permitido) {
            const diff = solicitado - permitido;
            diferencia.textContent = '$' + diff.toFixed(2);
            alertDiferencia.classList.remove('d-none');
        } else {
            alertDiferencia.classList.add('d-none');
        }
    }

    function actualizarContador() {
        if (!justificacion || !charCount) return;

        const length = justificacion.value.length;
        charCount.textContent = length + '/1000';
        if (length < 20) {
            charCount.classList.add('text-danger');
            charCount.classList.remove('text-success');
        } else {
            charCount.classList.remove('text-danger');
            charCount.classList.add('text-success');
        }
    }

    valorSolicitado?.addEventListener('input', calcularDiferencia);
    valorPermitido?.addEventListener('input', calcularDiferencia);

    justificacion?.addEventListener('input', actualizarContador);

    actualizarContador();
    calcularDiferencia();
})();
