(function () {
    'use strict';

    const tabla = document.getElementById('tablaProductosCatalogo');
    const checkMaster = document.getElementById('checkMaster');
    const countSeleccionados = document.getElementById('countSeleccionados');
    const selectAlcance = document.getElementById('selectAlcanceCambio');
    const selectListaCambio = document.getElementById('selectListaCambio');
    const inputPorcentaje = document.getElementById('inputPorcentajeCambio');
    const inputMotivo = document.getElementById('inputMotivoCambio');
    const btnPrevisualizar = document.getElementById('btnPrevisualizar');
    const resumenPrevio = document.getElementById('resumenPrevio');
    const listaPrevio = document.getElementById('listaPrevio');
    const btnAplicarRapido = document.getElementById('btnAplicarRapido');
    const btnCancelarPrevio = document.getElementById('btnCancelarPrevio');
    const errorGeneral = document.getElementById('cambioPrecioErrorGeneral');
    const errorAlcance = document.getElementById('errorAlcanceCambio');
    const errorPorcentaje = document.getElementById('errorPorcentajeCambio');

    if (!tabla || !selectAlcance || !inputPorcentaje || !btnPrevisualizar) {
        return;
    }

    let previewRows = [];

    function readInitConfig() {
        const el = document.getElementById('catalogo-init');
        if (!el) return null;
        try {
            return JSON.parse(el.textContent || '{}');
        } catch {
            return null;
        }
    }

    function setupListaSelector() {
        if (!selectListaCambio) return;

        const config = readInitConfig();
        const listas = Array.isArray(config?.listasPrecios) ? config.listasPrecios : [];
        const actual = config?.listaPrecioActualId;

        selectListaCambio.innerHTML = '';

        const optionBase = document.createElement('option');
        optionBase.value = '';
        optionBase.textContent = 'Precio base del producto';
        selectListaCambio.appendChild(optionBase);

        listas.forEach(lista => {
            const option = document.createElement('option');
            option.value = String(lista.id);
            option.textContent = `Lista: ${lista.nombre}`;
            if (actual && Number(actual) === Number(lista.id)) {
                option.selected = true;
            }
            selectListaCambio.appendChild(option);
        });
    }

    function getSelectedChecks() {
        return Array.from(tabla.querySelectorAll('.check-producto:checked'));
    }

    function getAllChecks() {
        return Array.from(tabla.querySelectorAll('.check-producto'));
    }

    function parsePrice(raw) {
        const number = Number(raw);
        return Number.isFinite(number) ? number : 0;
    }

    function setFieldError(control, errorElement, message) {
        if (control) {
            control.classList.add('is-invalid');
        }
        if (errorElement) {
            errorElement.textContent = message;
            errorElement.classList.remove('d-none');
        }
    }

    function clearFieldError(control, errorElement) {
        if (control) {
            control.classList.remove('is-invalid');
        }
        if (errorElement) {
            errorElement.textContent = '';
            errorElement.classList.add('d-none');
        }
    }

    function showGeneralError(message) {
        if (!errorGeneral) return;
        errorGeneral.textContent = message;
        errorGeneral.classList.remove('d-none');
    }

    function clearGeneralError() {
        if (!errorGeneral) return;
        errorGeneral.textContent = '';
        errorGeneral.classList.add('d-none');
    }

    function clearValidationErrors() {
        clearGeneralError();
        clearFieldError(inputPorcentaje, errorPorcentaje);
        clearFieldError(selectAlcance, errorAlcance);
    }

    function validateInputs() {
        clearValidationErrors();

        const porcentaje = Number(inputPorcentaje.value);
        if (!Number.isFinite(porcentaje) || porcentaje === 0) {
            setFieldError(inputPorcentaje, errorPorcentaje, 'Ingresá un porcentaje válido distinto de 0.');
            return false;
        }

        const scope = selectAlcance.value;
        const selectedIds = getSelectedChecks().map(c => c.value).join(',');
        if (scope === 'seleccionados' && !selectedIds) {
            setFieldError(selectAlcance, errorAlcance, 'Seleccioná al menos un producto o usá el alcance filtrados.');
            return false;
        }

        return true;
    }

    function formatMoney(value) {
        return value.toLocaleString('es-AR', { style: 'currency', currency: 'ARS' });
    }

    function getActiveRowsByScope() {
        const scope = selectAlcance.value;
        const selectedRows = getSelectedChecks().map(c => c.closest('tr')).filter(Boolean);

        if (scope === 'seleccionados') {
            return selectedRows;
        }

        return getAllChecks().map(c => c.closest('tr')).filter(Boolean);
    }

    function getFiltersJson() {
        const params = new URLSearchParams(window.location.search);

        const categoria = params.get('categoriaId');
        const marca = params.get('marcaId');
        const busqueda = params.get('searchTerm');
        const soloActivos = params.get('soloActivos');
        const stockBajo = params.get('stockBajo');

        return JSON.stringify({
            CategoriaId: categoria ? Number(categoria) : null,
            MarcaId: marca ? Number(marca) : null,
            Busqueda: busqueda || null,
            SoloActivos: soloActivos === 'true',
            StockBajo: stockBajo === 'true'
        });
    }

    function updateMasterState() {
        if (!checkMaster) return;

        const checks = getAllChecks();
        const checked = getSelectedChecks();

        checkMaster.checked = checks.length > 0 && checked.length === checks.length;
        checkMaster.indeterminate = checked.length > 0 && checked.length < checks.length;
    }

    function updateSelectionCount() {
        if (countSeleccionados) {
            countSeleccionados.textContent = String(getSelectedChecks().length);
        }

        const selectedCount = getSelectedChecks().length;
        if (selectAlcance && selectAlcance.value === 'seleccionados') {
            if (selectedCount === 1) {
                selectAlcance.options[0].text = 'Seleccionado (1 producto)';
            } else {
                selectAlcance.options[0].text = `Seleccionados (${selectedCount} productos)`;
            }
        }
    }

    function validateCanPreview() {
        const value = Number(inputPorcentaje.value);
        const hasValidValue = Number.isFinite(value) && value !== 0;

        if (!hasValidValue) {
            btnPrevisualizar.disabled = true;
            return;
        }

        if (selectAlcance.value === 'seleccionados') {
            btnPrevisualizar.disabled = getSelectedChecks().length === 0;
            return;
        }

        btnPrevisualizar.disabled = getAllChecks().length === 0;
    }

    function rebuildPreview() {
        const porcentaje = Number(inputPorcentaje.value);
        const rows = getActiveRowsByScope();

        previewRows = rows.map(row => {
            const productoId = Number(row.dataset.productoId);
            const nombre = row.dataset.productoNombre || `Producto ${productoId}`;
            const codigo = row.dataset.productoCodigo || '';
            const precioActual = parsePrice(row.dataset.productoPrecio);
            const precioNuevo = Math.round((precioActual * (1 + (porcentaje / 100))) * 100) / 100;

            return {
                productoId,
                nombre,
                codigo,
                precioActual,
                precioNuevo,
                diferencia: precioNuevo - precioActual
            };
        });
    }

    function renderPreview() {
        if (!listaPrevio) return;

        if (!validateInputs()) {
            btnAplicarRapido.disabled = true;
            return;
        }

        rebuildPreview();

        if (previewRows.length === 0) {
            listaPrevio.innerHTML = '<li class="text-warning">No hay productos para previsualizar.</li>';
            if (resumenPrevio) resumenPrevio.classList.remove('d-none');
            btnAplicarRapido.disabled = true;
            return;
        }

        const total = previewRows.length;
        const promedio = previewRows.reduce((acc, item) => acc + item.diferencia, 0) / total;
        const firstItems = previewRows.slice(0, 6);

        const lines = [];
        lines.push(`<li><strong>Alcance:</strong> ${selectAlcance.value === 'seleccionados' ? 'Seleccionados' : 'Filtrados'}</li>`);
        lines.push(`<li><strong>Productos:</strong> ${total}</li>`);
        lines.push(`<li><strong>Variación:</strong> ${inputPorcentaje.value}%</li>`);
        lines.push(`<li><strong>Diferencia promedio:</strong> ${formatMoney(promedio)}</li>`);
        lines.push('<li class="mt-2"><strong>Muestra:</strong></li>');

        firstItems.forEach(item => {
            const sign = item.diferencia >= 0 ? '+' : '';
            lines.push(`<li class="ms-2">${item.codigo} - ${item.nombre}: ${formatMoney(item.precioActual)} → ${formatMoney(item.precioNuevo)} (${sign}${formatMoney(item.diferencia)})</li>`);
        });

        if (total > firstItems.length) {
            lines.push(`<li class="ms-2 text-secondary">... y ${total - firstItems.length} más</li>`);
        }

        listaPrevio.innerHTML = lines.join('');
        if (resumenPrevio) resumenPrevio.classList.remove('d-none');
        btnAplicarRapido.disabled = false;
    }

    async function applyChange() {
        if (!validateInputs()) {
            btnAplicarRapido.disabled = true;
            return;
        }

        const porcentaje = Number(inputPorcentaje.value);

        const scope = selectAlcance.value;
        const selectedIds = getSelectedChecks().map(c => c.value).join(',');

        if (previewRows.length === 0) {
            showGeneralError('Primero previsualizá los cambios antes de confirmar.');
            return;
        }

        const payload = {
            alcance: scope,
            valorPorcentaje: porcentaje,
            productoIdsText: scope === 'seleccionados' ? selectedIds : null,
            filtrosJson: scope === 'filtrados' ? getFiltersJson() : null,
            listaPrecioId: selectListaCambio?.value ? Number(selectListaCambio.value) : null,
            motivo: inputMotivo?.value?.trim() || null
        };

        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';

        btnAplicarRapido.disabled = true;
        const originalText = btnAplicarRapido.innerHTML;
        btnAplicarRapido.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Aplicando...';
        clearGeneralError();

        try {
            const response = await fetch('/Catalogo/AplicarCambioPrecioDirecto', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify(payload)
            });

            const data = await response.json();
            if (!response.ok || !(data.exitoso ?? data.Exitoso)) {
                const errorMessage = data.error || data.mensaje || data.Mensaje || 'No se pudo aplicar el cambio.';
                throw new Error(errorMessage);
            }

            window.location.reload();
        } catch (error) {
            showGeneralError(error.message || 'Error al aplicar el cambio de precios.');
        } finally {
            btnAplicarRapido.disabled = false;
            btnAplicarRapido.innerHTML = originalText;
        }
    }

    function clearPreview() {
        previewRows = [];
        if (resumenPrevio) resumenPrevio.classList.add('d-none');
        if (listaPrevio) listaPrevio.innerHTML = '';
        if (btnAplicarRapido) btnAplicarRapido.disabled = true;
        clearValidationErrors();
    }

    tabla.addEventListener('change', function (event) {
        const target = event.target;
        if (!(target instanceof HTMLInputElement)) return;

        if (target.id === 'checkMaster') {
            const checks = getAllChecks();
            checks.forEach(c => {
                c.checked = target.checked;
            });
        }

        if (target.classList.contains('check-producto') || target.id === 'checkMaster') {
            updateMasterState();
            updateSelectionCount();
            validateCanPreview();
            clearPreview();
        }
    });

    checkMaster?.addEventListener('change', function () {
        const checks = getAllChecks();
        checks.forEach(c => {
            c.checked = checkMaster.checked;
        });
        updateMasterState();
        updateSelectionCount();
        validateCanPreview();
        clearPreview();
    });

    inputPorcentaje.addEventListener('input', function () {
        clearFieldError(inputPorcentaje, errorPorcentaje);
        clearGeneralError();
        validateCanPreview();
        clearPreview();
    });

    selectAlcance.addEventListener('change', function () {
        clearFieldError(selectAlcance, errorAlcance);
        clearGeneralError();
        validateCanPreview();
        clearPreview();
    });

    btnPrevisualizar.addEventListener('click', function () {
        renderPreview();
    });

    btnAplicarRapido?.addEventListener('click', function () {
        applyChange();
    });

    btnCancelarPrevio?.addEventListener('click', function () {
        clearPreview();
    });

    const modal = document.getElementById('modalActualizacionPrecios');
    modal?.addEventListener('show.bs.modal', function () {
        updateMasterState();
        updateSelectionCount();
        validateCanPreview();
        clearPreview();
    });

    setupListaSelector();
    updateMasterState();
    updateSelectionCount();
    validateCanPreview();
})();
