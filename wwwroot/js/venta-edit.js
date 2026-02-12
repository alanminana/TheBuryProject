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
    const precioProductoUrl = form.dataset.getPrecioProductoUrl;
    const calcularTotalesUrl = form.dataset.calcularTotalesUrl;
    const descuentoEsPorcentaje = form.dataset.descuentoEsPorcentaje === 'true' || form.dataset.descuentoEsPorcentaje === true;
    const antiforgeryToken = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

    const productoSelect = document.getElementById('productoSelect');
    const precioInput = document.getElementById('precioInput');
    const cantidadInput = document.getElementById('cantidadInput');
    const descuentoInput = document.getElementById('descuentoInput');
    const detallesBody = document.getElementById('productosBody');
    const descuentoGeneralInput = document.getElementById('descuentoGeneral') || document.getElementById('Descuento');
    const totalHidden = document.getElementById('hiddenTotal') || document.getElementById('totalHidden');
    const subtotalHidden = document.getElementById('hiddenSubtotal') || document.getElementById('subtotalHidden');
    const ivaHidden = document.getElementById('hiddenIVA') || document.getElementById('ivaHidden');

    function init() {
        inicializarFilasExistentes();
        calcularTotales();
        bindEventos();
    }

    function bindEventos() {
        productoSelect?.addEventListener('change', function () {
            const productoId = this.value;
            if (productoId && precioProductoUrl) {
                const params = new URLSearchParams({ id: productoId });
                fetch(`${precioProductoUrl}?${params.toString()}`)
                    .then(function (response) { return response.ok ? response.json() : null; })
                    .then(function (data) {
                        if (data?.precioVenta !== undefined && precioInput) {
                            precioInput.value = Number(data.precioVenta).toFixed(2);
                        }
                    });
            }
        });

        document.getElementById('btnAgregarProducto')?.addEventListener('click', function () {
            const productoId = productoSelect?.value;
            const productoTexto = productoSelect?.options[productoSelect.selectedIndex]?.text;
            const cantidad = parseFloat(cantidadInput?.value || '');
            const precio = parseFloat(precioInput?.value || '');
            const descuento = parseFloat(descuentoInput?.value || '0');

            if (!productoId || !productoTexto || cantidad <= 0 || precio <= 0) {
                alert('Complete todos los campos correctamente.');
                return;
            }

            const partes = productoTexto.split(' - ');
            const codigo = partes[0];
            const nombreCompleto = partes[1] || productoTexto;
            const nombre = nombreCompleto.split(' (Stock:')[0];

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

            if (productoSelect) productoSelect.value = '';
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

