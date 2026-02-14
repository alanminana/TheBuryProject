(function () {
    const form = document.getElementById('formMovimientoStock');
    const productoIdInput = document.getElementById('productoIdInput');
    const productoSearchInput = document.getElementById('productoSearchInput');
    const productoSearchResults = document.getElementById('productoSearchResults');
    const productoInfo = document.getElementById('productoInfo');
    const productoCodigo = document.getElementById('productoCodigo');
    const productoNombre = document.getElementById('productoNombre');
    const stockActualBadge = document.getElementById('stockActual');
    const tipoSelect = document.getElementById('tipoSelect');
    const cantidadInput = document.getElementById('cantidadInput');
    const stockResultado = document.getElementById('stockResultado');

    let stockActual = 0;
    let productosSugeridos = [];
    let indiceSugeridoActivo = -1;
    let debounceBusquedaId = null;

    async function cargarProductoInfo(id) {
        const baseUrl = productoSearchInput?.dataset?.productoInfoUrl;
        if (!baseUrl) {
            return;
        }

        try {
            const response = await fetch(`${baseUrl}/${id}`);
            if (!response.ok) {
                throw new Error('No se pudo obtener la información del producto');
            }

            const data = await response.json();
            productoCodigo.textContent = data.codigo || '-';
            productoNombre.textContent = data.nombre || '-';
            stockActualBadge.textContent = data.stockActual ?? 0;
            stockActual = parseFloat(data.stockActual) || 0;
            productoInfo?.classList.remove('d-none');
            calcularStock();
        } catch (error) {
            console.error(error);
            productoInfo?.classList.add('d-none');
            stockActual = 0;
            stockResultado.textContent = 'No se pudo cargar la información del producto.';
            stockResultado.className = 'form-text text-danger';
        }
    }

    function ocultarSugerencias() {
        if (!productoSearchResults) return;
        productoSearchResults.classList.add('d-none');
        productoSearchResults.innerHTML = '';
        indiceSugeridoActivo = -1;
    }

    function renderSugerencias() {
        if (!productoSearchResults) return;

        if (!productosSugeridos.length) {
            productoSearchResults.innerHTML = '<div class="list-group-item small text-muted">Sin resultados</div>';
            productoSearchResults.classList.remove('d-none');
            return;
        }

        productoSearchResults.innerHTML = productosSugeridos.map(function (p, index) {
            return `
                <button type="button" class="list-group-item list-group-item-action producto-suggestion ${index === indiceSugeridoActivo ? 'active' : ''}" data-index="${index}">
                    <div class="d-flex justify-content-between align-items-center">
                        <span><strong>${p.codigo || ''}</strong> - ${p.nombre || ''}</span>
                        <small class="text-muted">Stock: ${p.stockActual ?? 0}</small>
                    </div>
                </button>
            `;
        }).join('');

        productoSearchResults.classList.remove('d-none');

        productoSearchResults.querySelectorAll('.producto-suggestion').forEach(function (btn) {
            btn.addEventListener('click', function () {
                const idx = Number.parseInt(this.dataset.index, 10);
                if (!Number.isFinite(idx) || !productosSugeridos[idx]) return;
                seleccionarProducto(productosSugeridos[idx]);
            });
        });
    }

    function moverSeleccion(delta) {
        if (!productosSugeridos.length) return;
        indiceSugeridoActivo += delta;

        if (indiceSugeridoActivo < 0) indiceSugeridoActivo = productosSugeridos.length - 1;
        if (indiceSugeridoActivo >= productosSugeridos.length) indiceSugeridoActivo = 0;

        renderSugerencias();
    }

    function seleccionarProducto(producto) {
        if (productoSearchInput) {
            productoSearchInput.value = `${producto.codigo || ''} - ${producto.nombre || ''}`.trim();
        }

        if (productoIdInput) {
            productoIdInput.value = producto.id;
        }

        ocultarSugerencias();
        cargarProductoInfo(producto.id);
    }

    async function buscarProductos(term) {
        const baseUrl = form?.dataset?.buscarProductosUrl;
        if (!baseUrl) return;

        try {
            const params = new URLSearchParams({ term: term, take: '20' });
            const response = await fetch(`${baseUrl}?${params.toString()}`);
            if (!response.ok) {
                throw new Error('No se pudo buscar productos');
            }

            const data = await response.json();
            productosSugeridos = Array.isArray(data) ? data : [];
            indiceSugeridoActivo = -1;
            renderSugerencias();
        } catch {
            productosSugeridos = [];
            ocultarSugerencias();
        }
    }

    function bindBusquedaProductoEvents() {
        if (!productoSearchInput) return;

        productoSearchInput.addEventListener('input', function () {
            const term = this.value.trim();

            if (productoIdInput) {
                productoIdInput.value = '';
            }

            if (debounceBusquedaId) {
                clearTimeout(debounceBusquedaId);
            }

            if (term.length < 2) {
                ocultarSugerencias();
                productoInfo?.classList.add('d-none');
                stockActual = 0;
                stockResultado.textContent = '';
                return;
            }

            debounceBusquedaId = setTimeout(function () {
                buscarProductos(term);
            }, 250);
        });

        productoSearchInput.addEventListener('keydown', function (event) {
            if (!productoSearchResults || productoSearchResults.classList.contains('d-none')) return;

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

            if (event.key === 'Enter') {
                event.preventDefault();
                if (indiceSugeridoActivo >= 0 && productosSugeridos[indiceSugeridoActivo]) {
                    seleccionarProducto(productosSugeridos[indiceSugeridoActivo]);
                }
                return;
            }

            if (event.key === 'Escape') {
                ocultarSugerencias();
            }
        });

        document.addEventListener('click', function (event) {
            if (!productoSearchInput || !productoSearchResults) return;
            if (event.target === productoSearchInput || productoSearchResults.contains(event.target)) return;
            ocultarSugerencias();
        });
    }

    function calcularStock() {
        const tipo = tipoSelect?.value;
        const cantidad = parseFloat(cantidadInput?.value || '') || 0;
        if (!tipo || cantidad === 0) {
            stockResultado.textContent = '';
            return;
        }

        let nuevo = 0;
        let mensaje = '';
        let clase = '';

        switch (tipo) {
            case '0':
                nuevo = stockActual + cantidad;
                mensaje = `Stock resultante: ${nuevo.toFixed(2)} (actual: ${stockActual} + ${cantidad})`;
                clase = 'text-success';
                break;
            case '1':
                nuevo = stockActual - cantidad;
                if (nuevo < 0) {
                    mensaje = `⚠️ Stock insuficiente (actual: ${stockActual})`;
                    clase = 'text-danger';
                } else {
                    mensaje = `Stock resultante: ${nuevo.toFixed(2)} (actual: ${stockActual} - ${cantidad})`;
                    clase = 'text-warning';
                }
                break;
            case '2':
                nuevo = cantidad;
                const diferencia = cantidad - stockActual;
                mensaje = `Nuevo stock: ${nuevo.toFixed(2)} (dif: ${diferencia >= 0 ? '+' : ''}${diferencia.toFixed(2)})`;
                clase = 'text-info';
                break;
            default:
                mensaje = '';
        }

        stockResultado.textContent = mensaje;
        stockResultado.className = `form-text ${clase}`.trim();
    }

    tipoSelect?.addEventListener('change', calcularStock);
    cantidadInput?.addEventListener('change', calcularStock);
    cantidadInput?.addEventListener('keyup', calcularStock);
    bindBusquedaProductoEvents();

    document.addEventListener('DOMContentLoaded', function () {
        const idInicial = productoIdInput?.value;
        if (idInicial) {
            cargarProductoInfo(idInicial);
        }
    });
})();
