(function () {
    document.addEventListener('DOMContentLoaded', () => {
        const form = document.querySelector('form[data-total-venta]');
        if (!form) return;

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

        const totalVenta = parseFloat(form.dataset.totalVenta) || 0;

        function formatear(valor) {
            return valor.toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
        }

        function normalizarCamposIniciales() {
            if (anticipoInput.value === '0') anticipoInput.value = '';
            if (cuotasInput.value === '0') cuotasInput.value = '';
            if (tasaInput.value === '0') tasaInput.value = '';
            if (gastosInput.value === '0') gastosInput.value = '';
        }

        function limpiarResultados() {
            if (cuotaEstimadaLabel) cuotaEstimadaLabel.innerText = '$0,00';
            if (interesTotalLabel) interesTotalLabel.innerText = '$0,00';
            if (totalAPagarLabel) totalAPagarLabel.innerText = '$0,00';
            if (tasaAplicadaLabel) tasaAplicadaLabel.innerText = '0%';
            if (capitalFinanciadoLabel) capitalFinanciadoLabel.innerText = '$0,00';
            if (gastosAdministrativosLabel) gastosAdministrativosLabel.innerText = '$0,00';
            if (planTotalLabel) planTotalLabel.innerText = '$0,00';
            if (fechaPrimerPagoLabel) fechaPrimerPagoLabel.innerText = '--';
            if (mensajeSemaforo) mensajeSemaforo.innerText = 'Completa los datos para precalificar.';
            if (badgeSemaforo) {
                badgeSemaforo.className = 'badge state-yellow';
                badgeSemaforo.innerText = 'Sin datos';
            }
            if (msgIngreso) msgIngreso.classList.add('d-none');
            if (msgAntiguedad) msgAntiguedad.classList.add('d-none');
        }

        function actualizarSemaforo(estado, mensaje, mostrarIngreso, mostrarAntiguedad) {
            const clases = {
                verde: 'badge state-green',
                amarillo: 'badge state-yellow',
                rojo: 'badge state-red',
                sinDatos: 'badge state-yellow',
            };

            const etiquetas = {
                verde: 'Verde',
                amarillo: 'Amarillo',
                rojo: 'Rojo',
                sinDatos: 'Sin datos',
            };

            if (badgeSemaforo) {
                badgeSemaforo.className = clases[estado] || 'badge state-yellow';
                badgeSemaforo.innerText = etiquetas[estado] || 'Sin datos';
            }

            if (mensajeSemaforo) {
                mensajeSemaforo.innerText = mensaje || 'Completa los datos para precalificar.';
            }

            if (msgIngreso) {
                msgIngreso.classList.toggle('d-none', !mostrarIngreso);
            }

            if (msgAntiguedad) {
                msgAntiguedad.classList.toggle('d-none', !mostrarAntiguedad);
            }
        }

        async function actualizarCalculos() {
            const anticipo = parseFloat(anticipoInput.value) || 0;
            const cuotas = parseInt(cuotasInput.value, 10) || 0;
            const tasa = parseFloat(tasaInput.value) || 0;
            const gastos = parseFloat(gastosInput.value) || 0;
            const fecha = fechaInput.value;

            const datosCompletos = cuotas > 0 && tasa > 0 && totalVenta > 0;

            if (!datosCompletos) {
                limpiarResultados();
                if (alertaError) alertaError.classList.remove('d-none');
                return;
            }

            if (alertaError) alertaError.classList.add('d-none');

            try {
                const url = `/Credito/SimularPlanVenta?totalVenta=${encodeURIComponent(totalVenta)}&anticipo=${encodeURIComponent(anticipo)}&cuotas=${encodeURIComponent(cuotas)}&tasaMensual=${encodeURIComponent(tasa)}&gastosAdministrativos=${encodeURIComponent(gastos)}&fechaPrimeraCuota=${encodeURIComponent(fecha)}`;
                const response = await fetch(url, { headers: { 'Accept': 'application/json' } });

                if (!response.ok) {
                    throw new Error('No se pudo calcular el plan de crédito.');
                }

                const data = await response.json();

                if (montoFinanciadoInput) {
                    montoFinanciadoInput.value = data.montoFinanciado;
                }
                if (montoFinanciadoLabel) {
                    montoFinanciadoLabel.innerText = `$${formatear(data.montoFinanciado || 0)}`;
                }
                if (cuotaEstimadaLabel) {
                    cuotaEstimadaLabel.innerText = `$${formatear(data.cuotaEstimada || 0)}`;
                }
                if (tasaAplicadaLabel) {
                    const tasaAplicada = data.tasaAplicada || 0;
                    tasaAplicadaLabel.innerText = `${tasaAplicada.toFixed(2)}%`;
                }
                if (interesTotalLabel) {
                    interesTotalLabel.innerText = `$${formatear(data.interesTotal || 0)}`;
                }
                if (totalAPagarLabel) {
                    totalAPagarLabel.innerText = `$${formatear(data.totalAPagar || 0)}`;
                }
                if (capitalFinanciadoLabel) {
                    capitalFinanciadoLabel.innerText = `$${formatear(data.montoFinanciado || 0)}`;
                }
                if (gastosAdministrativosLabel) {
                    gastosAdministrativosLabel.innerText = `$${formatear(data.gastosAdministrativos || 0)}`;
                }
                if (planTotalLabel) {
                    planTotalLabel.innerText = `$${formatear(data.totalPlan || 0)}`;
                }
                if (fechaPrimerPagoLabel) {
                    fechaPrimerPagoLabel.innerText = data.fechaPrimerPago
                        ? new Date(data.fechaPrimerPago).toLocaleDateString('es-AR', { day: '2-digit', month: 'short', year: 'numeric' })
                        : '--';
                }

                actualizarSemaforo(
                    data.semaforoEstado || 'sinDatos',
                    data.semaforoMensaje,
                    data.mostrarMsgIngreso,
                    data.mostrarMsgAntiguedad
                );
            } catch (error) {
                limpiarResultados();
                if (alertaError) {
                    alertaError.classList.remove('d-none');
                    alertaError.textContent = error.message;
                }
            }
        }

        normalizarCamposIniciales();
        anticipoInput.addEventListener('input', actualizarCalculos);
        cuotasInput.addEventListener('input', actualizarCalculos);
        tasaInput.addEventListener('input', actualizarCalculos);
        gastosInput.addEventListener('input', actualizarCalculos);
        fechaInput.addEventListener('change', actualizarCalculos);

        actualizarCalculos();
    });
})();
