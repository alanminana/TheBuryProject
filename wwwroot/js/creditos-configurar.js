(function () {
    document.addEventListener('DOMContentLoaded', () => {
        const form = document.querySelector('form[data-total-venta]');
        if (!form) return;

        const totalVenta = parseFloat(form.dataset.totalVenta) || 0;
        const camposCompletados = {
            anticipo: false,
            cuotas: false,
            tasa: false,
            fecha: false,
        };

        const anticipoInput = document.getElementById('anticipoInput');
        const cuotasInput = document.getElementById('cuotasInput');
        const tasaInput = document.getElementById('tasaMensualInput');
        const gastosInput = document.getElementById('GastosAdministrativos');
        const fechaInput = document.getElementById('fechaPrimeraCuota');

        const alertaError = document.getElementById('errorCalculadora');
        const mensajeSemaforo = document.getElementById('mensajeSemaforo');
        const badgeSemaforo = document.getElementById('estadoSemaforo');
        const msgIngreso = document.getElementById('msgIngreso');
        const msgAntiguedad = document.getElementById('msgAntiguedad');
        const interesTotalLabel = document.getElementById('interesTotalLabel');
        const totalAPagarLabel = document.getElementById('totalAPagarLabel');
        const tasaAplicadaLabel = document.getElementById('tasaAplicadaLabel');
        const capitalFinanciadoLabel = document.getElementById('capitalFinanciadoLabel');
        const gastosAdministrativosLabel = document.getElementById('gastosAdministrativosLabel');
        const planTotalLabel = document.getElementById('planTotalLabel');
        const fechaPrimerPagoLabel = document.getElementById('fechaPrimerPagoLabel');
        const montoFinanciadoInput = document.getElementById('MontoFinanciado');
        const montoFinanciadoLabel = document.getElementById('montoFinanciadoLabel');
        const cuotaEstimadaLabel = document.getElementById('cuotaEstimadaLabel');

        if (!anticipoInput || !cuotasInput || !tasaInput || !gastosInput || !fechaInput) return;

        function formatear(valor) {
            return valor.toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
        }

        function normalizarCamposIniciales() {
            if (anticipoInput.value === '0') anticipoInput.value = '';
            if (cuotasInput.value === '0') cuotasInput.value = '';
            if (tasaInput.value === '0') tasaInput.value = '';
            if (gastosInput.value === '0') gastosInput.value = '';
        }

        function registrarCambio(campo, tieneValor) {
            camposCompletados[campo] = tieneValor;
            actualizarCalculos();
        }

        function actualizarCalculos() {
            const anticipo = parseFloat(anticipoInput.value) || 0;
            const cuotas = parseInt(cuotasInput.value) || 0;
            const tasa = parseFloat(tasaInput.value) || 0;
            const gastos = parseFloat(gastosInput.value) || 0;

            const montoFinanciado = Math.max(0, totalVenta - anticipo);
            if (montoFinanciadoInput) {
                montoFinanciadoInput.value = montoFinanciado;
            }
            if (montoFinanciadoLabel) {
                montoFinanciadoLabel.innerText = `$${formatear(montoFinanciado)}`;
            }

            const tieneAnticipo = true;
            const tieneCuotas = cuotas > 0;
            const tieneTasa = tasa > 0;
            const tieneFecha = !!fechaInput.value;

            const datosCompletos = tieneAnticipo && tieneCuotas && tieneTasa && montoFinanciado > 0;

            if (!datosCompletos) {
                if (cuotaEstimadaLabel) cuotaEstimadaLabel.innerText = '$0,00';
                if (interesTotalLabel) interesTotalLabel.innerText = '$0,00';
                if (totalAPagarLabel) totalAPagarLabel.innerText = '$0,00';
                if (tasaAplicadaLabel) tasaAplicadaLabel.innerText = '0%';
                if (alertaError) alertaError.classList.remove('d-none');
                if (badgeSemaforo) {
                    badgeSemaforo.className = 'badge state-yellow';
                    badgeSemaforo.innerText = 'Sin datos';
                }
                if (mensajeSemaforo) mensajeSemaforo.innerText = 'Completa los datos para precalificar.';
                if (msgIngreso) msgIngreso.classList.add('d-none');
                if (msgAntiguedad) msgAntiguedad.classList.add('d-none');
                if (capitalFinanciadoLabel) capitalFinanciadoLabel.innerText = '$0,00';
                if (gastosAdministrativosLabel) gastosAdministrativosLabel.innerText = '$0,00';
                if (planTotalLabel) planTotalLabel.innerText = '$0,00';
                if (fechaPrimerPagoLabel) fechaPrimerPagoLabel.innerText = '--';
                return;
            }

            camposCompletados.anticipo = tieneAnticipo;
            camposCompletados.cuotas = tieneCuotas;
            camposCompletados.tasa = tieneTasa;
            camposCompletados.fecha = tieneFecha;
            if (alertaError) alertaError.classList.add('d-none');

            const tasaDecimal = tasa / 100;
            const cuota = tasaDecimal === 0
                ? montoFinanciado / cuotas
                : (montoFinanciado * tasaDecimal) / (1 - Math.pow(1 + tasaDecimal, -cuotas));

            if (cuotaEstimadaLabel) cuotaEstimadaLabel.innerText = `$${formatear(cuota)}`;
            if (tasaAplicadaLabel) tasaAplicadaLabel.innerText = `${tasa.toFixed(2)}%`;
            const totalCuotas = cuota * cuotas;
            const interesTotal = Math.max(0, totalCuotas - montoFinanciado);
            const totalAPagar = totalCuotas + gastos;
            if (interesTotalLabel) interesTotalLabel.innerText = `$${formatear(interesTotal)}`;
            if (totalAPagarLabel) totalAPagarLabel.innerText = `$${formatear(totalAPagar)}`;
            if (capitalFinanciadoLabel) capitalFinanciadoLabel.innerText = `$${formatear(montoFinanciado)}`;
            if (gastosAdministrativosLabel) gastosAdministrativosLabel.innerText = `$${formatear(gastos)}`;
            if (planTotalLabel) planTotalLabel.innerText = `$${formatear(totalAPagar)}`;
            if (fechaPrimerPagoLabel) {
                if (fechaInput.value) {
                    const fecha = new Date(fechaInput.value);
                    fechaPrimerPagoLabel.innerText = fecha.toLocaleDateString('es-AR', { day: '2-digit', month: 'short', year: 'numeric' });
                } else {
                    fechaPrimerPagoLabel.innerText = '--';
                }
            }

            const ratio = cuota / (montoFinanciado || 1);
            if (ratio <= 0.08) {
                if (badgeSemaforo) {
                    badgeSemaforo.className = 'badge state-green';
                    badgeSemaforo.innerText = 'Verde';
                }
                if (mensajeSemaforo) mensajeSemaforo.innerText = 'Condiciones preliminares saludables.';
                if (msgIngreso) msgIngreso.classList.add('d-none');
                if (msgAntiguedad) msgAntiguedad.classList.add('d-none');
            } else if (ratio <= 0.15) {
                if (badgeSemaforo) {
                    badgeSemaforo.className = 'badge state-yellow';
                    badgeSemaforo.innerText = 'Amarillo';
                }
                if (mensajeSemaforo) mensajeSemaforo.innerText = 'Revisar ingresos declarados.';
                if (msgIngreso) msgIngreso.classList.remove('d-none');
                if (msgAntiguedad) msgAntiguedad.classList.add('d-none');
            } else {
                if (badgeSemaforo) {
                    badgeSemaforo.className = 'badge state-red';
                    badgeSemaforo.innerText = 'Rojo';
                }
                if (mensajeSemaforo) mensajeSemaforo.innerText = 'Las condiciones requieren ajustes.';
                if (msgIngreso) msgIngreso.classList.remove('d-none');
                if (msgAntiguedad) msgAntiguedad.classList.remove('d-none');
            }
        }

        normalizarCamposIniciales();
        anticipoInput.addEventListener('input', () => registrarCambio('anticipo', true));
        cuotasInput.addEventListener('input', () => registrarCambio('cuotas', true));
        tasaInput.addEventListener('input', () => registrarCambio('tasa', true));
        gastosInput.addEventListener('input', () => actualizarCalculos());
        fechaInput.addEventListener('change', () => registrarCambio('fecha', true));

        actualizarCalculos();
    });
})();
