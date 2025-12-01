(function () {
    const form = document.querySelector('form[data-documentos-url]');
    const toggleReemplazo = document.getElementById('toggleReemplazo');
    const contenedorReemplazo = document.getElementById('reemplazoContainer');
    const selectReemplazo = document.getElementById('DocumentoAReemplazarId');
    const clienteInput = document.getElementById('ClienteId');

    if (!form || !toggleReemplazo || !contenedorReemplazo || !selectReemplazo || !clienteInput) {
        return;
    }

    const documentosUrl = form.dataset.documentosUrl;
    const documentoSeleccionado = form.dataset.documentoSeleccionado;
    const defaultOption = '<option value="">Seleccione un documento...</option>';

    async function cargarDocumentos(clienteId) {
        if (!clienteId || !documentosUrl) {
            selectReemplazo.innerHTML = defaultOption;
            return;
        }

        try {
            const response = await fetch(`${documentosUrl}?clienteId=${encodeURIComponent(clienteId)}`);
            if (!response.ok) {
                throw new Error('No se pudieron cargar los documentos');
            }

            const data = await response.json();
            selectReemplazo.innerHTML = defaultOption;

            data.forEach((doc) => {
                const option = document.createElement('option');
                const docId = doc.id ?? doc.Id;
                option.value = docId;

                const tipo = doc.tipoDocumentoNombre ?? doc.TipoDocumentoNombre;
                const archivo = doc.nombreArchivo ?? doc.NombreArchivo;
                const estado = doc.estadoNombre ?? doc.EstadoNombre;
                option.textContent = `${tipo} - ${archivo} (${estado})`;
                selectReemplazo.appendChild(option);
            });

            if (documentoSeleccionado) {
                selectReemplazo.value = documentoSeleccionado;
            }
        } catch (error) {
            selectReemplazo.innerHTML = '<option value="">No se pudieron cargar los documentos</option>';
        }
    }

    function syncReemplazo() {
        if (toggleReemplazo.checked) {
            contenedorReemplazo.classList.remove('d-none');
            cargarDocumentos(clienteInput.value);
        } else {
            contenedorReemplazo.classList.add('d-none');
            selectReemplazo.value = '';
        }
    }

    toggleReemplazo.addEventListener('change', syncReemplazo);

    if (clienteInput.tagName === 'SELECT') {
        clienteInput.addEventListener('change', () => {
            if (toggleReemplazo.checked) {
                cargarDocumentos(clienteInput.value);
            }
        });
    }

    if (toggleReemplazo.checked) {
        contenedorReemplazo.classList.remove('d-none');
        cargarDocumentos(clienteInput.value);
    }
})();
