(() => {
    document.addEventListener('DOMContentLoaded', () => {
        const form = document.querySelector('[data-credito-form]');
        if (!form) return;

        const clienteSelect = document.getElementById('ClienteId');
        const montoInput = document.getElementById('MontoSolicitado');
        const garanteSelect = document.getElementById('GaranteId');
        const requiereGarante = document.getElementById('chkRequiereGarante');
        const garanteSection = document.getElementById('garanteSection');
        const panelEvaluacion = document.getElementById('panelEvaluacion');
        const resultadoEvaluacion = document.getElementById('resultadoEvaluacion');
        const evaluarUrl = form.dataset.evaluarUrl;

        if (!evaluarUrl) return;

        let evaluacionTimeout;

        const toggleGaranteSection = () => {
            if (!garanteSection || !requiereGarante) return;

            if (requiereGarante.checked) {
                garanteSection.classList.remove('d-none');
                return;
            }

            garanteSection.classList.add('d-none');
            if (garanteSelect) {
                garanteSelect.value = '';
            }
        };

        const mostrarEvaluacion = (evalData) => {
            if (!resultadoEvaluacion) return;

            const colorClass = evalData.resultado === 2 ? 'success' : evalData.resultado === 1 ? 'warning' : 'danger';
            const icono = evalData.resultado === 2
                ? 'check-circle-fill'
                : evalData.resultado === 1
                    ? 'exclamation-triangle-fill'
                    : 'x-circle-fill';

            resultadoEvaluacion.innerHTML = `
                <div class="alert alert-${colorClass} border-0 mb-3">
                    <h5 class="alert-heading">
                        <i class="bi bi-${icono} me-2"></i>
                        ${evalData.resultadoTexto}
                    </h5>
                    <p class="mb-0">${evalData.motivo}</p>
                </div>

                <div class="row mb-3">
                    <div class="col-12">
                        <h6 class="text-muted mb-2">Puntaje: ${evalData.puntajeFinal.toFixed(0)} / 100</h6>
                        <div class="progress credit-progress-lg">
                            <div class="progress-bar bg-${colorClass}"
                                 role="progressbar"
                                 style="width: ${evalData.puntajeFinal}%">
                                ${evalData.puntajeFinal.toFixed(0)}%
                            </div>
                        </div>
                    </div>
                </div>

                <div class="row g-3">
                    <div class="col-md-4">
                        <div class="card bg-dark border-0 shadow-sm">
                            <div class="card-body">
                                <small class="text-secondary d-block">Ingresos estimados</small>
                                <h5 class="text-light mb-0">${(evalData.ingresosEstimados ?? null)?.toLocaleString('es-AR', { style: 'currency', currency: 'ARS' }) ?? 'N/D'}</h5>
                            </div>
                        </div>
                    </div>
                    <div class="col-md-4">
                        <div class="card bg-dark border-0 shadow-sm">
                            <div class="card-body">
                                <small class="text-secondary d-block">Relación cuota/ingreso</small>
                                <h5 class="text-light mb-0">${(evalData.relacionCuotaIngreso ?? null)?.toFixed(2) ?? 'N/D'}%</h5>
                            </div>
                        </div>
                    </div>
                    <div class="col-md-4">
                        <div class="card bg-dark border-0 shadow-sm">
                            <div class="card-body">
                                <small class="text-secondary d-block">Documentación</small>
                                <h5 class="text-light mb-0">${evalData.documentacionCompleta ? 'Completa' : 'Incompleta'}</h5>
                            </div>
                        </div>
                    </div>
                </div>
            `;
        };

        const setLoading = () => {
            if (!resultadoEvaluacion) return;

            resultadoEvaluacion.innerHTML = `
                <div class="text-center py-4">
                    <div class="spinner-border text-info" role="status">
                        <span class="visually-hidden">Evaluando...</span>
                    </div>
                    <p class="text-muted mt-2">Evaluando solicitud...</p>
                </div>
            `;
        };

        const evaluarCredito = async () => {
            const clienteId = clienteSelect?.value;
            const montoSolicitado = parseFloat(montoInput?.value || '');
            const garanteId = garanteSelect?.value;

            if (!clienteId || !montoSolicitado || montoSolicitado <= 0) {
                panelEvaluacion?.classList.add('d-none');
                return;
            }

            panelEvaluacion?.classList.remove('d-none');
            setLoading();

            try {
                const url = new URL(evaluarUrl, window.location.origin);
                url.searchParams.append('clienteId', clienteId);
                url.searchParams.append('montoSolicitado', montoSolicitado.toString());
                url.searchParams.append('garanteId', garanteId || '');

                const response = await fetch(url.toString());
                if (!response.ok) {
                    throw new Error('Error desconocido');
                }

                const evaluacion = await response.json();
                mostrarEvaluacion(evaluacion);
            } catch (error) {
                if (!resultadoEvaluacion) return;

                resultadoEvaluacion.innerHTML = `
                    <div class="alert alert-danger">
                        <i class="bi bi-exclamation-triangle me-2"></i>
                        Error al evaluar: ${error?.message || 'Error desconocido'}
                    </div>
                `;
            }
        };

        const scheduleEvaluacion = () => {
            clearTimeout(evaluacionTimeout);
            evaluacionTimeout = setTimeout(evaluarCredito, 500);
        };

        clienteSelect?.addEventListener('change', scheduleEvaluacion);
        montoInput?.addEventListener('input', scheduleEvaluacion);
        garanteSelect?.addEventListener('change', scheduleEvaluacion);

        requiereGarante?.addEventListener('change', () => {
            toggleGaranteSection();
            evaluarCredito();
        });

        toggleGaranteSection();
    });
})();
