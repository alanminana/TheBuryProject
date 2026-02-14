(function () {
    const proveedorSelect = document.getElementById('ProveedorId');
    const productoSearchInput = document.getElementById('productoSearchInput');
    const productoSearchResults = document.getElementById('productoSearchResults');
    const cantidadInput = document.getElementById('cantidadInput');
    const precioInput = document.getElementById('precioInput');
    const agregarDetalleBtn = document.getElementById('agregarDetalleBtn');
    const descuentoInput = document.getElementById('Descuento');
    const detallesBody = document.getElementById('detallesBody');
    const subtotalDisplay = document.getElementById('subtotalDisplay');
    const descuentoDisplay = document.getElementById('descuentoDisplay');
    const ivaDisplay = document.getElementById('ivaDisplay');
    const totalDisplay = document.getElementById('totalDisplay');
    const subtotalHidden = document.getElementById('subtotalHidden');
    const ivaHidden = document.getElementById('ivaHidden');
    const totalHidden = document.getElementById('totalHidden');
    const form = document.getElementById('ordenCompraForm');
    const ivaRate = parseFloat(form?.dataset?.ivaRate || '0.21');

    const emptyRowTemplate = detallesBody ? detallesBody.innerHTML : '';

    let productosProveedor = [];
    let productoSeleccionado = null;
    let indiceSugeridoActivo = -1;
    let detalles = [];
    let detalleIndex = 0;

    function normalizarCantidadEntera(valor) {
        const parsed = Number.parseInt(String(valor ?? ''), 10);
        return Number.isFinite(parsed) ? parsed : NaN;
    }

    function normalizarNumero(valor) {
        const parsed = Number.parseFloat(String(valor ?? ''));
        return Number.isFinite(parsed) ? parsed : NaN;
    }

    async function cargarProductos(proveedorId) {
        if (!productoSearchInput) return;

        productoSearchInput.value = '';
        productoSearchInput.disabled = true;
        productoSeleccionado = null;
        productosProveedor = [];
        ocultarSugerencias();
        precioInput.value = 0;

        if (!proveedorId) {
            productoSearchInput.disabled = false;
            return;
        }

        try {
            const baseUrl = proveedorSelect?.dataset?.productosUrl;
            if (!baseUrl) {
                productoSearchInput.disabled = false;
                return;
            }
            const response = await fetch(`${baseUrl}/${proveedorId}`);
            if (!response.ok) {
                throw new Error('No se pudo obtener los productos');
            }

            const data = await response.json();
            if (!Array.isArray(data) || data.length === 0) {
                alert('Sin productos para este proveedor.');
                productoSearchInput.disabled = false;
                return;
            }

            productosProveedor = data;
        } catch (error) {
            alert('Error al cargar productos: ' + error.message);
        } finally {
            productoSearchInput.disabled = false;
        }
    }

    function filtrarProductos(term) {
        if (!Array.isArray(productosProveedor)) return [];
        const query = String(term || '').trim().toLowerCase();
        if (!query) return productosProveedor.slice(0, 20);
        return productosProveedor
            .filter(function (p) {
                return String(p.nombre || '').toLowerCase().includes(query);
            })
            .slice(0, 20);
    }

    function renderSugerencias(lista) {
        if (!productoSearchResults) return;

        if (!lista.length) {
            productoSearchResults.innerHTML = '<div class="list-group-item small text-muted">Sin resultados</div>';
            productoSearchResults.classList.remove('d-none');
            return;
        }

        productoSearchResults.innerHTML = lista.map(function (p, index) {
            const precio = Number.parseFloat(p.precio || 0);
            return `
                <button type="button" class="list-group-item list-group-item-action producto-suggestion ${index === indiceSugeridoActivo ? 'active' : ''}" data-index="${index}">
                    <div class="d-flex justify-content-between align-items-center">
                        <span>${p.nombre}</span>
                        <small>$${Number.isFinite(precio) ? precio.toFixed(2) : '0.00'}</small>
                    </div>
                </button>
            `;
        }).join('');

        productoSearchResults.classList.remove('d-none');

        productoSearchResults.querySelectorAll('.producto-suggestion').forEach(function (btn) {
            btn.addEventListener('click', function () {
                const idx = Number.parseInt(this.dataset.index, 10);
                if (!Number.isFinite(idx) || !lista[idx]) return;
                seleccionarProducto(lista[idx]);
            });
        });
    }

    function seleccionarProducto(producto) {
        productoSeleccionado = producto;
        if (productoSearchInput) {
            productoSearchInput.value = producto.nombre || '';
        }
        const precio = Number.parseFloat(producto.precio || 0);
        precioInput.value = Number.isFinite(precio) ? precio.toFixed(2) : '0.00';
        ocultarSugerencias();
    }

    function ocultarSugerencias() {
        if (!productoSearchResults) return;
        productoSearchResults.classList.add('d-none');
        productoSearchResults.innerHTML = '';
        indiceSugeridoActivo = -1;
    }

    function moverSeleccion(delta) {
        const lista = filtrarProductos(productoSearchInput?.value || '');
        if (!lista.length) return;
        indiceSugeridoActivo += delta;
        if (indiceSugeridoActivo < 0) indiceSugeridoActivo = lista.length - 1;
        if (indiceSugeridoActivo >= lista.length) indiceSugeridoActivo = 0;
        renderSugerencias(lista);
    }

    function bindBusquedaProductoEvents() {
        if (!productoSearchInput) return;

        productoSearchInput.addEventListener('input', function () {
            productoSeleccionado = null;
            indiceSugeridoActivo = -1;
            const lista = filtrarProductos(this.value);
            renderSugerencias(lista);
        });

        productoSearchInput.addEventListener('keydown', function (event) {
            if (event.key === 'ArrowDown') {
                event.preventDefault();
                moverSeleccion(1);
                return;
            }

            if (event.key === 'ArrowUp') {
                event.preventDefault();
                moverSeleccion(-1);
                return;
            }

            if (event.key === 'Escape') {
                ocultarSugerencias();
                return;
            }

            if (event.key === 'Enter') {
                event.preventDefault();
                const lista = filtrarProductos(this.value);
                if (indiceSugeridoActivo >= 0 && lista[indiceSugeridoActivo]) {
                    seleccionarProducto(lista[indiceSugeridoActivo]);
                    return;
                }

                if (lista.length === 1) {
                    seleccionarProducto(lista[0]);
                }
            }
        });

        document.addEventListener('click', function (event) {
            if (!productoSearchInput || !productoSearchResults) return;
            if (event.target === productoSearchInput || productoSearchResults.contains(event.target)) return;
            ocultarSugerencias();
        });
    }

    function agregarDetalle() {
        const productoId = productoSeleccionado?.id ? String(productoSeleccionado.id) : '';
        const productoNombre = productoSeleccionado?.nombre;
        const cantidad = normalizarCantidadEntera(cantidadInput.value);
        const precio = normalizarNumero(precioInput.value);

        if (!productoId || !productoNombre || isNaN(cantidad) || isNaN(precio) || cantidad <= 0 || precio <= 0) {
            alert('Complete los campos.');
            return;
        }

        if (detalles.some(d => d.productoId === productoId)) {
            alert('Producto duplicado.');
            return;
        }

        const detalle = {
            index: detalleIndex++,
            id: null,
            productoId,
            productoNombre,
            cantidad,
            precioUnitario: precio,
            subtotal: cantidad * precio
        };

        detalles.push(detalle);
        renderDetalles();
        calcularTotales();

        if (productoSearchInput) {
            productoSearchInput.value = '';
            productoSearchInput.focus();
        }
        productoSeleccionado = null;
        ocultarSugerencias();
        cantidadInput.value = 1;
        precioInput.value = 0;
    }

    function eliminarDetalle(detalleId) {
        detalles = detalles.filter(d => d.index !== detalleId);
        renderDetalles();
        calcularTotales();
    }

    function renderDetalles() {
        if (!detallesBody) return;

        if (detalles.length === 0) {
            detallesBody.innerHTML = emptyRowTemplate;
            return;
        }

        const rows = detalles.map((d, idx) => `
            <tr>
                <td>
                    ${d.productoNombre || ''}
                    <input type="hidden" name="Detalles[${idx}].Id" value="${d.id ?? 0}" />
                    <input type="hidden" name="Detalles[${idx}].ProductoId" value="${d.productoId}" />
                </td>
                <td class="text-end">${d.cantidad}<input type="hidden" name="Detalles[${idx}].Cantidad" value="${d.cantidad}" /></td>
                <td class="text-end">$${d.precioUnitario.toFixed(2)}<input type="hidden" name="Detalles[${idx}].PrecioUnitario" value="${d.precioUnitario}" /></td>
                <td class="text-end fw-bold">$${d.subtotal.toFixed(2)}<input type="hidden" name="Detalles[${idx}].Subtotal" value="${d.subtotal}" /></td>
                <td class="text-center"><button type="button" class="btn btn-sm btn-danger" data-detalle-index="${d.index}"><i class="bi bi-trash"></i></button></td>
            </tr>
        `);

        detallesBody.innerHTML = rows.join('');
    }

    function calcularTotales() {
        const subtotal = detalles.reduce((acc, d) => acc + d.subtotal, 0);
        const descuento = parseFloat(descuentoInput.value) || 0;
        const iva = (subtotal - descuento) * ivaRate;
        const total = subtotal - descuento + iva;

        subtotalDisplay.textContent = '$' + subtotal.toFixed(2);
        descuentoDisplay.textContent = '$' + descuento.toFixed(2);
        ivaDisplay.textContent = '$' + iva.toFixed(2);
        totalDisplay.textContent = '$' + total.toFixed(2);

        subtotalHidden.value = subtotal.toFixed(2);
        ivaHidden.value = iva.toFixed(2);
        totalHidden.value = total.toFixed(2);
    }

    function handleEliminarClick(event) {
        const target = event.target.closest('[data-detalle-index]');
        if (!target) return;

        const id = parseInt(target.dataset.detalleIndex, 10);
        if (!isNaN(id)) {
            eliminarDetalle(id);
        }
    }

    function getDetallesIniciales() {
        const jsonEl = document.getElementById('ordenCompraDetallesInicialesJson');
        const rawJson = (jsonEl && 'value' in jsonEl)
            ? String(jsonEl.value || '')
            : String(jsonEl?.textContent || '');

        if (rawJson && rawJson.trim()) {
            try {
                const parsed = JSON.parse(rawJson.trim());
                if (Array.isArray(parsed)) return parsed;
            } catch {
                // ignore
            }
        }

        return window.ordenCompraDetallesIniciales;
    }

    function inicializar() {
        const detallesIniciales = getDetallesIniciales();
        if (Array.isArray(detallesIniciales) && detallesIniciales.length > 0) {
            detalles = detallesIniciales
                .filter(d => d && d.productoId)
                .map((d, idx) => {
                    const cantidad = normalizarCantidadEntera(d.cantidad);
                    const precio = normalizarNumero(d.precioUnitario);
                    const subtotal = normalizarNumero(d.subtotal);

                    return {
                        index: idx,
                        id: d.id ?? 0,
                        productoId: String(d.productoId),
                        productoNombre: d.productoNombre ?? '',
                        cantidad: Number.isFinite(cantidad) ? cantidad : 0,
                        precioUnitario: Number.isFinite(precio) ? precio : 0,
                        subtotal: Number.isFinite(subtotal) ? subtotal : 0
                    };
                });

            detalleIndex = detalles.length;
            renderDetalles();
        }

        if (proveedorSelect) {
            proveedorSelect.addEventListener('change', () => cargarProductos(proveedorSelect.value));
            if (proveedorSelect.value) {
                cargarProductos(proveedorSelect.value);
            }
        }

        bindBusquedaProductoEvents();

        if (agregarDetalleBtn) {
            agregarDetalleBtn.addEventListener('click', agregarDetalle);
        }

        if (descuentoInput) {
            descuentoInput.addEventListener('input', calcularTotales);
        }

        if (detallesBody) {
            detallesBody.addEventListener('click', handleEliminarClick);
        }

        if (form) {
            form.addEventListener('submit', (event) => {
                if (detalles.length === 0) {
                    event.preventDefault();
                    alert('Agregue al menos un producto.');
                }
            });
        }

        calcularTotales();
    }

    inicializar();
})();
