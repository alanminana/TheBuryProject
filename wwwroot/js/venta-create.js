(function () {
    const form = document.getElementById('formVenta');
    if (!form) {
        return;
    }

    const stockDisponible = {};
    const detalleManager = VentaCommon.createDetalleManager({
        keyFactory: function (index) { return index; },
        onChange: function () {
            actualizarTablaProductos();
            calcularTotales();
        }
    });

    const getTarjetasUrl = form.dataset.getTarjetasUrl;
    const calcularCuotasUrl = form.dataset.calcularCuotasUrl;
    const getPrecioProductoUrl = form.dataset.getPrecioProductoUrl;
    const calcularTotalesUrl = form.dataset.calcularTotalesUrl;
    const descuentoEsPorcentaje = form.dataset.descuentoEsPorcentaje === 'true';
    const antiforgeryToken = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

    const productoSelect = document.getElementById('productoSelect');
    const precioInput = document.getElementById('precioInput');
    const cantidadInput = document.getElementById('cantidadInput');
    const descuentoInput = document.getElementById('descuentoInput');
    const tarjetaRow = document.getElementById('tarjetaRow');
    const chequeRow = document.getElementById('chequeRow');
    const tarjetaSelect = document.getElementById('tarjetaSelect');
    const cuotasSelect = document.getElementById('cuotasSelect');
    const cuotasDiv = document.getElementById('cuotasDiv');
    const infoCuotas = document.getElementById('infoCuotas');
    const tipoPagoSelect = document.getElementById('tipoPagoSelect');
    const productosBody = document.getElementById('productosBody');
    const descuentoGeneralInput = document.getElementById('descuentoGeneral');

    const tarjetaHandlers = VentaCommon.initTarjetaHandlers({
        tipoPagoSelect: tipoPagoSelect,
        tarjetaRow: tarjetaRow,
        chequeRow: chequeRow,
        chequeFechaInput: '#chequeFechaEmision',
        tarjetaSelect: tarjetaSelect,
        cuotasSelect: cuotasSelect,
        cuotasDiv: cuotasDiv,
        infoCuotas: infoCuotas,
        getTarjetasUrl: getTarjetasUrl,
        calcularCuotasUrl: calcularCuotasUrl,
        totalHidden: '#hiddenTotal'
    });

    function init() {
        tarjetaHandlers.bindEvents();
        tarjetaHandlers.handleTipoPagoChange(tipoPagoSelect?.value);
        bindEvents();
    }

    function bindEvents() {
        productoSelect?.addEventListener('change', function () {
            const productoId = this.value;
            if (productoId) {
                cargarPrecioProducto(productoId);
            } else {
                resetProductoInputs();
            }
        });

        document.getElementById('btnAgregarProducto')?.addEventListener('click', agregarProducto);

        descuentoGeneralInput?.addEventListener('change', calcularTotales);

        productosBody?.addEventListener('click', function (event) {
            const btn = event.target.closest('.btn-eliminar-producto');
            if (!btn) return;
            const index = btn.dataset.index;
            if (index !== undefined) {
                eliminarProducto(index);
            }
        });

        form.addEventListener('submit', function (e) {
            if (detalleManager.getAll().length === 0) {
                e.preventDefault();
                alert('Debe agregar al menos un producto a la venta');
            }
        });
    }

    function cargarPrecioProducto(productoId) {
        if (!getPrecioProductoUrl || !productoSelect) {
            return;
        }

        const productoTexto = productoSelect.options[productoSelect.selectedIndex]?.text || '';
        const stockMatch = productoTexto.match(/Stock: (\d+)/);
        if (stockMatch && stockMatch[1]) {
            stockDisponible[productoId] = parseInt(stockMatch[1], 10);
        }

        const params = new URLSearchParams({ id: productoId });
        fetch(`${getPrecioProductoUrl}?${params.toString()}`)
            .then(function (response) { return response.ok ? response.json() : null; })
            .then(function (respuesta) {
                if (!respuesta) {
                    throw new Error();
                }

                if (typeof respuesta.precioVenta !== 'undefined') {
                    precioInput.value = parseFloat(respuesta.precioVenta).toFixed(2);
                    stockDisponible[productoId] = respuesta.stockActual ?? stockDisponible[productoId];
                } else if (typeof respuesta === 'number') {
                    precioInput.value = parseFloat(respuesta).toFixed(2);
                }
            })
            .catch(function () {
                alert('No se pudo obtener el precio del producto seleccionado.');
                precioInput.value = '';
            });
    }

    function agregarProducto() {
        if (!productoSelect || !precioInput || !cantidadInput || !descuentoInput) return;

        const productoId = productoSelect.value;
        if (!productoId) {
            alert('Seleccione un producto');
            return;
        }

        const productoTexto = productoSelect.options[productoSelect.selectedIndex]?.text || '';
        const cantidad = parseInt(cantidadInput.value, 10);
        const precio = parseFloat(precioInput.value);
        const descuentoPct = parseFloat(descuentoInput.value) || 0;

        if (!cantidad || cantidad < 1) {
            alert('La cantidad debe ser mayor a cero');
            return;
        }

        if (!precio || precio <= 0) {
            alert('Precio inválido');
            return;
        }

        const disponible = stockDisponible[productoId];
        if (Number.isFinite(disponible) && disponible < cantidad) {
            alert(`Stock insuficiente. Disponible: ${disponible}`);
            return;
        }

        const partes = productoTexto.split(' - ');
        const codigo = partes[0];
        const nombreCompleto = partes[1] || productoTexto;
        const nombre = nombreCompleto.split(' (Stock:')[0];

        const descuentoMonto = (precio * cantidad * descuentoPct) / 100;
        const subtotal = (precio * cantidad) - descuentoMonto;

        detalleManager.add({
            productoId: productoId,
            codigo: codigo,
            nombre: nombre,
            cantidad: cantidad,
            precioUnitario: precio,
            descuento: descuentoMonto,
            subtotal: subtotal
        });
        resetProductoInputs();
    }

    function eliminarProducto(index) {
        detalleManager.removeByKey(index);
    }

    function actualizarTablaProductos() {
        if (!productosBody) return;
        let html = '';
        detalleManager.getAll().forEach(function (prod, index) {
            html += `
                <tr>
                    <td>${prod.codigo}</td>
                    <td>${prod.nombre}</td>
                    <td class="text-center">${prod.cantidad}</td>
                    <td class="text-end">$${prod.precioUnitario.toFixed(2)}</td>
                    <td class="text-end">$${prod.descuento.toFixed(2)}</td>
                    <td class="text-end">$${prod.subtotal.toFixed(2)}</td>
                    <td class="text-center">
                        <button type="button" class="btn btn-sm btn-danger btn-eliminar-producto" data-index="${prod.key ?? index}">
                            <i class="bi bi-trash"></i>
                        </button>
                    </td>
                    <input type="hidden" name="Detalles[${index}].ProductoId" value="${prod.productoId}" />
                    <input type="hidden" name="Detalles[${index}].Cantidad" value="${prod.cantidad}" />
                    <input type="hidden" name="Detalles[${index}].PrecioUnitario" value="${prod.precioUnitario}" />
                    <input type="hidden" name="Detalles[${index}].Descuento" value="${prod.descuento}" />
                    <input type="hidden" name="Detalles[${index}].Subtotal" value="${prod.subtotal}" />
                </tr>
            `;
        });
        productosBody.innerHTML = html;
    }

    function calcularTotales() {
        const detalles = detalleManager.getAll();
        if (!calcularTotalesUrl || detalles.length === 0) {
            VentaCommon.resetTotalesUI({
                hiddenSubtotal: '#hiddenSubtotal',
                hiddenIVA: '#hiddenIVA',
                hiddenTotal: '#hiddenTotal',
                subtotalSelector: '#lblSubtotal',
                descuentoSelector: '#lblDescuento',
                ivaSelector: '#lblIVA',
                totalSelector: '#lblTotal'
            });
            return;
        }

        VentaCommon.calcularTotales({
            detalles: detalles,
            url: calcularTotalesUrl,
            descuentoGeneral: parseFloat(descuentoGeneralInput?.value) || 0,
            descuentoEsPorcentaje: descuentoEsPorcentaje,
            antiforgeryToken: antiforgeryToken
        })
            .then(function (data) {
                VentaCommon.aplicarTotalesUI(data, {
                    hiddenSubtotal: '#hiddenSubtotal',
                    hiddenIVA: '#hiddenIVA',
                    hiddenTotal: '#hiddenTotal',
                    subtotalSelector: '#lblSubtotal',
                    descuentoSelector: '#lblDescuento',
                    ivaSelector: '#lblIVA',
                    totalSelector: '#lblTotal'
                });
            })
            .catch(function () {
                VentaCommon.resetTotalesUI({
                    hiddenSubtotal: '#hiddenSubtotal',
                    hiddenIVA: '#hiddenIVA',
                    hiddenTotal: '#hiddenTotal',
                    subtotalSelector: '#lblSubtotal',
                    descuentoSelector: '#lblDescuento',
                    ivaSelector: '#lblIVA',
                    totalSelector: '#lblTotal'
                });
            });
    }

    function resetProductoInputs() {
        if (!productoSelect || !cantidadInput || !precioInput || !descuentoInput) return;
        productoSelect.value = '';
        cantidadInput.value = 1;
        precioInput.value = '';
        descuentoInput.value = 0;
    }

    init();
})();
