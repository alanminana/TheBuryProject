(function () {
    const form = document.getElementById('formVenta');
    if (!form) {
        return;
    }

    const dataContainer = document.getElementById('venta-edit-data');
    let detalles = [];

    function normalizeDetalle(detalle) {
        return {
            index: detalle.index ?? detalle.Id ?? detalle.id,
            ProductoId: detalle.ProductoId ?? detalle.productoId,
            ProductoCodigo: detalle.ProductoCodigo ?? detalle.productoCodigo,
            ProductoNombre: detalle.ProductoNombre ?? detalle.productoNombre,
            Cantidad: detalle.Cantidad ?? detalle.cantidad ?? 0,
            PrecioUnitario: detalle.PrecioUnitario ?? detalle.precioUnitario ?? 0,
            Descuento: detalle.Descuento ?? detalle.descuento ?? 0,
            Subtotal: detalle.Subtotal ?? detalle.subtotal ?? 0
        };
    }

    if (dataContainer) {
        try {
            detalles = JSON.parse(dataContainer.dataset.detalles || '[]');
            detalles = detalles.map(normalizeDetalle);
        } catch (error) {
            detalles = [];
        }
    }

    let detalleIndex = detalles.length ? Math.max(...detalles.map((d) => d.index || 0)) + 1 : 1;
    const detalleManager = VentaCommon.createDetalleManager({
        keyField: 'index',
        initialDetalles: detalles,
        keyFactory: function () { return detalleIndex++; },
        onChange: function (list) { detalles = list; }
    });
    const buscarProductosUrl = form.dataset.buscarProductosUrl;
    const calcularTotalesUrl = form.dataset.calcularTotalesUrl;
    const descuentoEsPorcentaje = form.dataset.descuentoEsPorcentaje === 'true' || form.dataset.descuentoEsPorcentaje === true;
    const antiforgeryToken = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

    const productoSearchInput = document.getElementById('productoSearchInput');
    const productoSearchResults = document.getElementById('productoSearchResults');
    const productoCategoriaFiltro = document.getElementById('productoCategoriaFiltro');
    const productoMarcaFiltro = document.getElementById('productoMarcaFiltro');
    const productoSoloStockFiltro = document.getElementById('productoSoloStockFiltro');
    const productoPrecioMinFiltro = document.getElementById('productoPrecioMinFiltro');
    const productoPrecioMaxFiltro = document.getElementById('productoPrecioMaxFiltro');
    const precioInput = document.getElementById('precioInput');
    const cantidadInput = document.getElementById('cantidadInput');
    const descuentoInput = document.getElementById('descuentoInput');
    const detallesBody = document.getElementById('productosBody');
    const descuentoGeneralInput = document.getElementById('descuentoGeneral') || document.getElementById('Descuento');
    const totalHidden = document.getElementById('hiddenTotal') || document.getElementById('totalHidden');
    const subtotalHidden = document.getElementById('hiddenSubtotal') || document.getElementById('subtotalHidden');
    const ivaHidden = document.getElementById('hiddenIVA') || document.getElementById('ivaHidden');

    let productosSugeridos = [];
    let indiceSugeridoActivo = -1;
    let productoSeleccionado = null;
    let debounceBusquedaId = null;

    function init() {
        inicializarFilasExistentes();
        calcularTotales();
        bindEventos();
        bindBusquedaProductosEvents();
    }

    function bindEventos() {
        document.getElementById('btnAgregarProducto')?.addEventListener('click', function () {
            const productoId = productoSeleccionado?.id;
            const cantidad = parseFloat(cantidadInput?.value || '');
            const precio = parseFloat(precioInput?.value || '');
            const descuento = parseFloat(descuentoInput?.value || '0');

            if (!productoId || !productoSeleccionado || cantidad <= 0 || precio <= 0) {
                alert('Complete todos los campos correctamente.');
                return;
            }

            const codigo = productoSeleccionado.codigo;
            const nombre = productoSeleccionado.nombre;

            const subtotal = (cantidad * precio) - descuento;

            const detalle = {
                index: detalleIndex++,
                ProductoId: productoId,
                ProductoCodigo: codigo,
                ProductoNombre: nombre,
                Cantidad: cantidad,
                PrecioUnitario: precio,
                Descuento: descuento,
                Subtotal: subtotal,
                Id: 0
            };

            const creado = detalleManager.add(detalle);
            agregarFilaDetalle(creado);
            calcularTotales();

            if (productoSearchInput) {
                productoSearchInput.value = '';
                productoSearchInput.focus();
            }
            productoSeleccionado = null;
            productosSugeridos = [];
            ocultarSugerenciasProducto();
            if (cantidadInput) cantidadInput.value = 1;
            if (precioInput) precioInput.value = '';
            if (descuentoInput) descuentoInput.value = 0;
        });

        detallesBody?.addEventListener('click', function (event) {
            const btn = event.target.closest('.btn-eliminar-producto');
            if (!btn) return;
            const index = parseInt(btn.dataset.index, 10);
            if (Number.isFinite(index)) {
                eliminarDetalle(index);
            }
        });

        form.addEventListener('submit', function () {
            document.querySelectorAll('input[name^="Detalles"]').forEach(function (input) { input.remove(); });
            detalleManager.getAll().forEach((detalle, index) => {
                const normalizado = normalizeDetalle(detalle);
                form.insertAdjacentHTML('beforeend', `
                    <input type="hidden" name="Detalles[${index}].ProductoId" value="${normalizado.ProductoId}" />
                    <input type="hidden" name="Detalles[${index}].Cantidad" value="${normalizado.Cantidad}" />
                    <input type="hidden" name="Detalles[${index}].PrecioUnitario" value="${normalizado.PrecioUnitario}" />
                    <input type="hidden" name="Detalles[${index}].Descuento" value="${normalizado.Descuento}" />
                    <input type="hidden" name="Detalles[${index}].Subtotal" value="${normalizado.Subtotal}" />
                `);
            });
        });

        descuentoGeneralInput?.addEventListener('input', calcularTotales);

        document.addEventListener('click', function (event) {
            if (!productoSearchInput || !productoSearchResults) return;
            if (event.target === productoSearchInput || productoSearchResults.contains(event.target)) return;
            ocultarSugerenciasProducto();
        });
    }

    function bindBusquedaProductosEvents() {
        if (!productoSearchInput) return;

        productoSearchInput.addEventListener('input', function () {
            const term = this.value.trim();
            productoSeleccionado = null;

            if (debounceBusquedaId) {
                clearTimeout(debounceBusquedaId);
            }

            if (term.length < 2) {
                ocultarSugerenciasProducto();
                return;
            }

            debounceBusquedaId = setTimeout(function () {
                buscarProductos(term);
            }, 250);
        });

        productoSearchInput.addEventListener('keydown', function (event) {
            if (!productoSearchResults || productoSearchResults.classList.contains('d-none')) {
                if (event.key === 'Enter') {
                    event.preventDefault();
                    intentarAgregarPorCodigoExacto(this.value.trim());
                }
                return;
            }

            if (event.key === 'ArrowDown') {
                event.preventDefault();
                moverSeleccionSugerencia(1);
                return;
            }

            if (event.key === 'ArrowUp') {
                event.preventDefault();
                moverSeleccionSugerencia(-1);
                return;
            }

            if (event.key === 'Enter') {
                event.preventDefault();
                if (indiceSugeridoActivo >= 0 && productosSugeridos[indiceSugeridoActivo]) {
                    seleccionarProducto(productosSugeridos[indiceSugeridoActivo]);
                    return;
                }
                intentarAgregarPorCodigoExacto(this.value.trim());
                return;
            }

            if (event.key === 'Escape') {
                ocultarSugerenciasProducto();
            }
        });

        [productoCategoriaFiltro, productoMarcaFiltro, productoSoloStockFiltro].forEach(function (filtro) {
            filtro?.addEventListener('change', reBuscarConFiltros);
        });

        [productoPrecioMinFiltro, productoPrecioMaxFiltro].forEach(function (filtroPrecio) {
            filtroPrecio?.addEventListener('input', function () {
                if (debounceBusquedaId) {
                    clearTimeout(debounceBusquedaId);
                }

                debounceBusquedaId = setTimeout(reBuscarConFiltros, 250);
            });
        });
    }

    function buscarProductos(term) {
        if (!buscarProductosUrl || !productoSearchResults) return;

        const params = getFiltroBusquedaProducto(term);
        fetch(`${buscarProductosUrl}?${params.toString()}`)
            .then(function (response) { return response.ok ? response.json() : []; })
            .then(function (data) {
                productosSugeridos = Array.isArray(data) ? data : [];
                indiceSugeridoActivo = -1;
                renderSugerenciasProducto();
            })
            .catch(function () {
                productosSugeridos = [];
                ocultarSugerenciasProducto();
            });
    }

    function getFiltroBusquedaProducto(term) {
        const params = new URLSearchParams({ term: term, take: '20' });

        if (productoCategoriaFiltro?.value) {
            params.set('categoriaId', productoCategoriaFiltro.value);
        }

        if (productoMarcaFiltro?.value) {
            params.set('marcaId', productoMarcaFiltro.value);
        }

        params.set('soloConStock', productoSoloStockFiltro?.checked === false ? 'false' : 'true');

        if (productoPrecioMinFiltro?.value) {
            params.set('precioMin', productoPrecioMinFiltro.value);
        }

        if (productoPrecioMaxFiltro?.value) {
            params.set('precioMax', productoPrecioMaxFiltro.value);
        }

        return params;
    }

    function reBuscarConFiltros() {
        const term = productoSearchInput?.value?.trim();
        if (!term || term.length < 2) {
            ocultarSugerenciasProducto();
            return;
        }

        buscarProductos(term);
    }

    function renderSugerenciasProducto() {
        if (!productoSearchResults) return;

        if (!productosSugeridos.length) {
            productoSearchResults.innerHTML = '<div class="list-group-item small text-muted">Sin resultados</div>';
            productoSearchResults.classList.remove('d-none');
            return;
        }

        productoSearchResults.innerHTML = productosSugeridos.map(function (producto, index) {
            const marcaCategoria = [producto.marca, producto.categoria].filter(Boolean).join(' / ');
            const caracteristicas = producto.caracteristicasResumen || '';
            const precio = Number(producto.precioVenta || 0).toFixed(2);
            return `
                <button type="button" class="list-group-item list-group-item-action producto-suggestion ${index === indiceSugeridoActivo ? 'active' : ''}" data-index="${index}">
                    <div class="d-flex justify-content-between">
                        <strong>${producto.codigo} - ${producto.nombre}</strong>
                        <span>$${precio}</span>
                    </div>
                    <small class="d-block text-muted">${marcaCategoria || 'Sin marca/categoría'} · Stock: ${producto.stockActual}</small>
                    ${caracteristicas ? `<small class="d-block text-info">${caracteristicas}</small>` : ''}
                </button>
            `;
        }).join('');

        productoSearchResults.classList.remove('d-none');

        productoSearchResults.querySelectorAll('.producto-suggestion').forEach(function (btn) {
            btn.addEventListener('click', function () {
                const idx = parseInt(this.dataset.index, 10);
                if (Number.isFinite(idx) && productosSugeridos[idx]) {
                    seleccionarProducto(productosSugeridos[idx]);
                }
            });
        });
    }

    function moverSeleccionSugerencia(delta) {
        if (!productosSugeridos.length) return;
        indiceSugeridoActivo += delta;

        if (indiceSugeridoActivo < 0) indiceSugeridoActivo = productosSugeridos.length - 1;
        if (indiceSugeridoActivo >= productosSugeridos.length) indiceSugeridoActivo = 0;

        renderSugerenciasProducto();
    }

    function seleccionarProducto(producto) {
        if (!productoSearchInput || !precioInput) return;

        productoSeleccionado = producto;
        precioInput.value = Number(producto.precioVenta || 0).toFixed(2);
        productoSearchInput.value = `${producto.codigo} - ${producto.nombre}`;
        ocultarSugerenciasProducto();
    }

    function ocultarSugerenciasProducto() {
        if (!productoSearchResults) return;
        productoSearchResults.classList.add('d-none');
        productoSearchResults.innerHTML = '';
        indiceSugeridoActivo = -1;
    }

    function intentarAgregarPorCodigoExacto(term) {
        if (!term || !productosSugeridos.length) return;

        const exacto = productosSugeridos.find(function (p) {
            return String(p.codigo || '').toLowerCase() === term.toLowerCase() || p.codigoExacto;
        });

        if (exacto) {
            seleccionarProducto(exacto);
        }
    }


    function inicializarFilasExistentes() {
        const existentes = detalleManager.getAll();
        if (!existentes.length) {
            return;
        }

        existentes.forEach((detalle) => {
            if (!detallesBody?.querySelector(`tr[data-index="${detalle.index}"]`)) {
                agregarFilaDetalle(detalle);
            }
        });
    }

    function agregarFilaDetalle(detalle) {
        if (!detallesBody) return;
        const normalizado = normalizeDetalle(detalle);
        detallesBody.insertAdjacentHTML('beforeend', `
            <tr data-index="${normalizado.index}">
                <td>${normalizado.ProductoCodigo || ''}</td>
                <td>${normalizado.ProductoNombre || ''}</td>
                <td class="text-center">${normalizado.Cantidad}</td>
                <td class="text-end">$${Number(normalizado.PrecioUnitario).toFixed(2)}</td>
                <td class="text-end">$${Number(normalizado.Descuento).toFixed(2)}</td>
                <td class="text-end">$${Number(normalizado.Subtotal).toFixed(2)}</td>
                <td class="text-center">
                    <button type="button" class="btn btn-sm btn-danger btn-eliminar-producto" data-index="${normalizado.index}">
                        <i class="bi bi-trash"></i>
                    </button>
                </td>
            </tr>
        `);
    }

    function eliminarDetalle(index) {
        detalleManager.removeByKey(index);
        detallesBody?.querySelector(`tr[data-index="${index}"]`)?.remove();
        calcularTotales();
    }

    function calcularTotales() {
        const actuales = detalleManager.getAll();
        if (!calcularTotalesUrl || actuales.length === 0) {
            VentaCommon.resetTotalesUI({
                subtotalSelector: '#lblSubtotal',
                descuentoSelector: '#lblDescuento',
                ivaSelector: '#lblIVA',
                totalSelector: '#lblTotal',
                hiddenSubtotal: subtotalHidden,
                hiddenIVA: ivaHidden,
                hiddenTotal: totalHidden
            });

            return;
        }

        VentaCommon.calcularTotales({
            detalles: actuales,
            url: calcularTotalesUrl,
            descuentoGeneral: parseFloat(descuentoGeneralInput?.value) || 0,
            descuentoEsPorcentaje: descuentoEsPorcentaje,
            antiforgeryToken: antiforgeryToken
        })
            .then((data) => {
                VentaCommon.aplicarTotalesUI(data, {
                    subtotalSelector: '#lblSubtotal',
                    descuentoSelector: '#lblDescuento',
                    ivaSelector: '#lblIVA',
                    totalSelector: '#lblTotal',
                    hiddenSubtotal: subtotalHidden,
                    hiddenIVA: ivaHidden,
                    hiddenTotal: totalHidden
                });

            })
            .catch(() => {
                VentaCommon.resetTotalesUI({
                    subtotalSelector: '#lblSubtotal',
                    descuentoSelector: '#lblDescuento',
                    ivaSelector: '#lblIVA',
                    totalSelector: '#lblTotal',
                    hiddenSubtotal: subtotalHidden,
                    hiddenIVA: ivaHidden,
                    hiddenTotal: totalHidden
                });
            });
    }

    init();
})();

