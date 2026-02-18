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

        const templates = {
            evaluacion: document.getElementById('tpl-credito-evaluacion'),
            loading: document.getElementById('tpl-credito-loading'),
            error: document.getElementById('tpl-credito-error')
        };

        let evaluacionTimeout;

        const toggleGaranteSection = () => {
            if (!garanteSection || !requiereGarante) return;

            if (requiereGarante.checked) {
                garanteSection.classList.remove('d-none');
                requiereGarante.setAttribute('aria-expanded', 'true');
                if (garanteSelect) {
                    garanteSelect.setAttribute('required', 'required');
                }
                return;
            }

            garanteSection.classList.add('d-none');
            requiereGarante.setAttribute('aria-expanded', 'false');
            if (garanteSelect) {
                garanteSelect.value = '';
                garanteSelect.removeAttribute('required');
            }
        };

        const renderTemplate = (templateEl) => {
            if (!templateEl) return null;
            return templateEl.content.cloneNode(true);
        };

        const mostrarEvaluacion = (evalData) => {
            if (!resultadoEvaluacion) return;
            const fragment = renderTemplate(templates.evaluacion);
            if (!fragment) return;

            const colorClass = evalData.resultado === 2 ? 'success' : evalData.resultado === 1 ? 'warning' : 'danger';
            const icono = evalData.resultado === 2
                ? 'check-circle-fill'
                : evalData.resultado === 1
                    ? 'exclamation-triangle-fill'
                    : 'x-circle-fill';

            const alert = fragment.querySelector('[data-resultado-alert]');
            const icon = fragment.querySelector('[data-resultado-icon]');
            const titulo = fragment.querySelector('[data-resultado-titulo]');
            const motivo = fragment.querySelector('[data-resultado-motivo]');
            const puntaje = fragment.querySelector('[data-resultado-puntaje]');
            const progress = fragment.querySelector('[data-resultado-progress]');
            const ingresos = fragment.querySelector('[data-resultado-ingresos]');
            const relacion = fragment.querySelector('[data-resultado-relacion]');
            const documentacion = fragment.querySelector('[data-resultado-documentacion]');

            alert?.classList.add(`alert-${colorClass}`);
            if (icon) icon.classList.add(`bi-${icono}`);
            if (titulo) titulo.textContent = evalData.resultadoTexto;
            if (motivo) motivo.textContent = evalData.motivo;
            if (puntaje) puntaje.textContent = evalData.puntajeFinal.toFixed(0);
            if (progress) {
                const progressValue = Math.max(0, Math.min(100, Number(evalData.puntajeFinal) || 0));
                progress.style.width = `${progressValue}%`;
                progress.textContent = `${progressValue.toFixed(0)}%`;
                progress.classList.add(`bg-${colorClass}`);
            }

            if (ingresos) {
                ingresos.textContent = formatearMoneda(evalData.ingresosEstimados);
            }
            if (relacion) {
                relacion.textContent = evalData.relacionCuotaIngreso != null ? `${evalData.relacionCuotaIngreso.toFixed(2)}%` : 'N/D';
            }
            if (documentacion) {
                documentacion.textContent = evalData.documentacionCompleta ? 'Completa' : 'Incompleta';
            }

            resultadoEvaluacion.setAttribute('aria-live', 'polite');
            resultadoEvaluacion.setAttribute('aria-busy', 'false');
            resultadoEvaluacion.replaceChildren(fragment);
        };

        const setLoading = () => {
            if (!resultadoEvaluacion) return;
            const fragment = renderTemplate(templates.loading);
            if (!fragment) return;
            resultadoEvaluacion.setAttribute('aria-live', 'polite');
            resultadoEvaluacion.setAttribute('aria-busy', 'true');
            resultadoEvaluacion.replaceChildren(fragment);
        };

        const mostrarError = (message) => {
            if (!resultadoEvaluacion) return;
            const fragment = renderTemplate(templates.error);
            if (!fragment) return;
            const textEl = fragment.querySelector('[data-resultado-error]');
            if (textEl) {
                textEl.textContent = message;
            }
            resultadoEvaluacion.setAttribute('aria-live', 'assertive');
            resultadoEvaluacion.setAttribute('aria-busy', 'false');
            resultadoEvaluacion.replaceChildren(fragment);
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
                mostrarError(error?.message || 'Error desconocido');
            }
        };

        const scheduleEvaluacion = () => {
            clearTimeout(evaluacionTimeout);
            evaluacionTimeout = setTimeout(evaluarCredito, 500);
        };

        const formatearMoneda = (valor) => {
            if (valor == null || Number.isNaN(valor)) return 'N/D';
            try {
                return Number(valor).toLocaleString('es-AR', { style: 'currency', currency: 'ARS' });
            } catch (error) {
                return `$${Number(valor).toFixed(2)}`;
            }
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
