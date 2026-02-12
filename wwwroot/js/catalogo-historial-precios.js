// wwwroot/js/catalogo-historial-precios.js
const CatalogoHistorialPrecios = (function () {
    'use strict';

    const elementos = {};
    let modalRevertir = null;

    function leerInitConfig() {
        const el = document.getElementById('catalogo-init');
        if (!el) return null;
        try {
            return JSON.parse(el.textContent || '{}');
        } catch {
            return null;
        }
    }

    function obtenerCsrfToken() {
        // @Html.AntiForgeryToken() genera un input hidden con este name
        const input = document.querySelector('input[name="__RequestVerificationToken"]');
        return input ? input.value : '';
    }

    function init() {
        const cfg = leerInitConfig();
        if (!cfg || !cfg.puedeVerHistorial) return;

        elementos.offcanvas = document.getElementById('offcanvasHistorialPrecios');
        elementos.loading = document.getElementById('historialLoading');
        elementos.error = document.getElementById('historialError');
        elementos.errorMsg = document.getElementById('historialErrorMsg');
        elementos.contenido = document.getElementById('historialContenido');
        elementos.tbody = document.getElementById('historialTbody');
        elementos.empty = document.getElementById('historialEmpty');
        elementos.selectTake = document.getElementById('historialTake');
        elementos.btnRefresh = document.getElementById('btnRefreshHistorial');

        // Modal revertir
        elementos.modalRevertir = document.getElementById('modalRevertirPrecio');
        elementos.revertirBatchId = document.getElementById('revertirBatchId');
        elementos.revertirRowVersion = document.getElementById('revertirRowVersion');
        elementos.revertirNombre = document.getElementById('revertirNombre');
        elementos.revertirCambio = document.getElementById('revertirCambio');
        elementos.revertirCantidad = document.getElementById('revertirCantidad');
        elementos.revertirAplicadoPor = document.getElementById('revertirAplicadoPor');
        elementos.revertirFecha = document.getElementById('revertirFecha');
        elementos.revertirMotivo = document.getElementById('revertirMotivo');
        elementos.revertirError = document.getElementById('revertirError');
        elementos.btnConfirmarReversion = document.getElementById('btnConfirmarReversion');
        elementos.btnRevertirTexto = document.getElementById('btnRevertirTexto');
        elementos.btnRevertirLoading = document.getElementById('btnRevertirLoading');

        if (!elementos.offcanvas) return;

        if (elementos.modalRevertir) {
            modalRevertir = new bootstrap.Modal(elementos.modalRevertir);

            elementos.modalRevertir.addEventListener('hidden.bs.modal', limpiarModalRevertir);
            elementos.btnConfirmarReversion?.addEventListener('click', ejecutarReversion);
        }

        elementos.offcanvas.addEventListener('show.bs.offcanvas', cargarHistorial);
        elementos.selectTake?.addEventListener('change', cargarHistorial);
        elementos.btnRefresh?.addEventListener('click', cargarHistorial);
    }

    async function cargarHistorial() {
        mostrarLoading(true);

        const take = elementos.selectTake?.value || 20;

        try {
            const response = await fetch(`/CambiosPrecios/HistorialApi?take=${encodeURIComponent(take)}`, {
                headers: { 'Accept': 'application/json' }
            });
            const data = await response.json();

            if (!data.success) {
                throw new Error(data.error || 'Error desconocido');
            }

            renderizarHistorial(data.historial);
        } catch (error) {
            mostrarError(error.message || 'Error al cargar historial');
        }
    }

    function renderizarHistorial(historial) {
        elementos.loading?.classList.add('d-none');
        elementos.error?.classList.add('d-none');
        elementos.contenido?.classList.remove('d-none');

        if (!historial || historial.length === 0) {
            elementos.empty?.classList.remove('d-none');
            if (elementos.tbody) elementos.tbody.innerHTML = '';
            return;
        }

        elementos.empty?.classList.add('d-none');

        const html = historial.map(item => `
      <tr>
        <td class="small">
          <div>${item.fecha ?? ''}</div>
          <div class="text-muted small">${item.usuario ?? ''}</div>
        </td>
        <td>
          <span class="fw-semibold ${item.tipoAplicacion === 'Aumento' ? 'text-success' : 'text-danger'}">
            ${item.cambioDisplay ?? ''}
          </span>
        </td>
        <td class="text-center">
          <span class="badge bg-secondary">${item.cantidadProductos ?? 0}</span>
        </td>
        <td class="text-center">
          <span class="badge ${item.estadoBadgeClass ?? 'bg-secondary'}">${item.estado ?? ''}</span>
        </td>
        <td class="text-center">
          <div class="btn-group btn-group-sm">
            <a href="${item.previewUrl ?? '#'}" class="btn btn-outline-info btn-sm" title="Ver detalle">
              <i class="bi bi-eye"></i>
            </a>
            ${item.puedeRevertir ? `
              <button type="button" class="btn btn-outline-warning btn-sm"
                      data-batch-id="${item.id}"
                      data-action="revertir"
                      title="Revertir">
                <i class="bi bi-arrow-counterclockwise"></i>
              </button>
            ` : ''}
          </div>
        </td>
      </tr>
    `).join('');

        if (elementos.tbody) {
            elementos.tbody.innerHTML = html;

            // Delegación para botones revertir
            elementos.tbody.querySelectorAll('button[data-action="revertir"]').forEach(btn => {
                btn.addEventListener('click', () => {
                    const id = parseInt(btn.getAttribute('data-batch-id'), 10);
                    if (!Number.isNaN(id)) abrirModalRevertir(id);
                });
            });
        }
    }

    function mostrarLoading(show) {
        elementos.loading?.classList.toggle('d-none', !show);
        elementos.contenido?.classList.add('d-none');
        elementos.error?.classList.add('d-none');
    }

    function mostrarError(mensaje) {
        elementos.loading?.classList.add('d-none');
        elementos.contenido?.classList.add('d-none');
        elementos.error?.classList.remove('d-none');
        if (elementos.errorMsg) elementos.errorMsg.textContent = mensaje;
    }

    async function abrirModalRevertir(batchId) {
        if (!modalRevertir) return;

        // UI loading
        if (elementos.revertirNombre) elementos.revertirNombre.textContent = 'Cargando...';
        if (elementos.revertirCambio) elementos.revertirCambio.textContent = '-';
        if (elementos.revertirCantidad) elementos.revertirCantidad.textContent = '-';
        if (elementos.revertirAplicadoPor) elementos.revertirAplicadoPor.textContent = '-';
        if (elementos.revertirFecha) elementos.revertirFecha.textContent = '-';
        elementos.revertirError?.classList.add('d-none');

        modalRevertir.show();

        try {
            const response = await fetch(`/CambiosPrecios/GetBatchParaRevertirApi?id=${encodeURIComponent(batchId)}`, {
                headers: { 'Accept': 'application/json' }
            });
            const data = await response.json();

            if (!data.success) {
                throw new Error(data.error || 'Error al cargar datos');
            }

            const batch = data.batch || {};
            if (elementos.revertirBatchId) elementos.revertirBatchId.value = batch.id ?? '';
            if (elementos.revertirRowVersion) elementos.revertirRowVersion.value = batch.rowVersion ?? '';
            if (elementos.revertirNombre) elementos.revertirNombre.textContent = batch.nombre ?? '-';
            if (elementos.revertirCambio) elementos.revertirCambio.innerHTML = `<span class="fw-semibold">${batch.cambioDisplay ?? '-'}</span>`;
            if (elementos.revertirCantidad) elementos.revertirCantidad.textContent = batch.cantidadProductos ?? 0;
            if (elementos.revertirAplicadoPor) elementos.revertirAplicadoPor.textContent = batch.aplicadoPor ?? '-';
            if (elementos.revertirFecha) elementos.revertirFecha.textContent = batch.fechaAplicacion ?? '-';
        } catch (error) {
            if (elementos.revertirError) {
                elementos.revertirError.textContent = error.message || 'Error';
                elementos.revertirError.classList.remove('d-none');
            }
        }
    }

    function limpiarModalRevertir() {
        if (elementos.revertirBatchId) elementos.revertirBatchId.value = '';
        if (elementos.revertirRowVersion) elementos.revertirRowVersion.value = '';
        if (elementos.revertirMotivo) elementos.revertirMotivo.value = '';
        elementos.revertirError?.classList.add('d-none');
        setLoadingRevertir(false);
        limpiarArtefactosModal();
    }

    function limpiarArtefactosModal() {
        const modalesAbiertos = document.querySelectorAll('.modal.show').length;
        const backdrops = document.querySelectorAll('.modal-backdrop');

        if (modalesAbiertos === 0) {
            backdrops.forEach(backdrop => backdrop.remove());
            document.body.classList.remove('modal-open');
        } else if (backdrops.length > 1) {
            backdrops.forEach((backdrop, index) => {
                if (index > 0) backdrop.remove();
            });
        }
    }

    async function ejecutarReversion() {
        const batchId = parseInt(elementos.revertirBatchId?.value || '', 10);
        const rowVersion = elementos.revertirRowVersion?.value || '';
        const motivo = (elementos.revertirMotivo?.value || '').trim();

        if (!motivo) return mostrarErrorRevertir('Debe especificar un motivo para la reversión');
        if (motivo.length < 10) return mostrarErrorRevertir('El motivo debe tener al menos 10 caracteres');
        if (Number.isNaN(batchId) || batchId <= 0) return mostrarErrorRevertir('Batch inválido');

        setLoadingRevertir(true);
        elementos.revertirError?.classList.add('d-none');

        try {
            const token = obtenerCsrfToken();

            const response = await fetch('/CambiosPrecios/RevertirApi', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify({ batchId, rowVersion, motivo })
            });

            const data = await response.json();

            if (!response.ok || !data.success) {
                throw new Error(data.error || 'Error al revertir');
            }

            modalRevertir?.hide();

            if (window.showToast) window.showToast('success', data.mensaje || 'Cambio revertido exitosamente');
            else alert(data.mensaje || 'Cambio revertido exitosamente');

            cargarHistorial();
        } catch (error) {
            mostrarErrorRevertir(error.message || 'Error al revertir');
            setLoadingRevertir(false);
        }
    }

    function mostrarErrorRevertir(mensaje) {
        if (elementos.revertirError) {
            elementos.revertirError.textContent = mensaje;
            elementos.revertirError.classList.remove('d-none');
        }
        elementos.revertirMotivo?.focus();
    }

    function setLoadingRevertir(loading) {
        if (elementos.btnConfirmarReversion) elementos.btnConfirmarReversion.disabled = loading;
        elementos.btnRevertirTexto?.classList.toggle('d-none', loading);
        elementos.btnRevertirLoading?.classList.toggle('d-none', !loading);
    }

    return { init };
})();

document.addEventListener('DOMContentLoaded', function () {
    CatalogoHistorialPrecios.init();
});
