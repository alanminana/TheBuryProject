(function () {
    const modalEl = document.getElementById('uploadDocumentoModal');
    if (!modalEl) {
        return;
    }

    const form = document.getElementById('uploadDocumentoForm');
    const clienteSelect = form?.querySelector('#ClienteId');
    const returnToVentaInput = form?.querySelector('input[name="ReturnToVentaId"]');
    const toggleReemplazo = form?.querySelector('#toggleReemplazo');
    const reemplazoContainer = form?.querySelector('#reemplazoContainer');
    const reemplazoSelect = form?.querySelector('#DocumentoAReemplazarId');
    const reemplazoAlert = modalEl.querySelector('[data-reemplazo-alert]');
    const reemplazoText = modalEl.querySelector('[data-reemplazo-text]');

    if (!form || !clienteSelect || !toggleReemplazo || !reemplazoContainer || !reemplazoSelect) {
        return;
    }

    function limpiarReemplazo() {
        toggleReemplazo.checked = false;
        reemplazoSelect.value = '';
        reemplazoContainer.classList.add('d-none');
        form.dataset.documentoSeleccionado = '';
        if (reemplazoAlert) {
            reemplazoAlert.classList.add('d-none');
            if (reemplazoText) {
                reemplazoText.textContent = '';
            }
        }
    }

    function prepararModal(trigger) {
        form.reset();
        const defaultClienteId = form.dataset.defaultClienteId || '';
        const defaultReturnToVentaId = form.dataset.defaultReturnToVentaId || '';

        const clienteId = trigger?.dataset.clienteId ?? defaultClienteId;
        const returnToVentaId = trigger?.dataset.returnToVentaId ?? defaultReturnToVentaId;
        const replaceId = trigger?.dataset.replaceId ?? '';
        const replaceName = trigger?.dataset.replaceName ?? '';

        if (returnToVentaInput) {
            returnToVentaInput.value = returnToVentaId || '';
        }

        if (clienteSelect) {
            clienteSelect.value = clienteId || '';
            clienteSelect.dispatchEvent(new Event('change'));
        }

        form.dataset.documentoSeleccionado = replaceId;

        if (replaceId) {
            toggleReemplazo.checked = true;
            toggleReemplazo.dispatchEvent(new Event('change'));
            if (reemplazoAlert && reemplazoText) {
                reemplazoAlert.classList.remove('d-none');
                reemplazoText.textContent = replaceName
                    ? `Se reemplazará el archivo "${replaceName}".`
                    : 'Se reemplazará el documento seleccionado.';
            }
        } else {
            limpiarReemplazo();
        }
    }

    modalEl.addEventListener('show.bs.modal', (event) => {
        prepararModal(event.relatedTarget);
    });

    modalEl.addEventListener('hidden.bs.modal', () => {
        limpiarReemplazo();
        form.reset();
    });
})();
