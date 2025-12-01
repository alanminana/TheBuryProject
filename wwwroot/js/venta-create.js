(function ($) {
    const form = document.getElementById('formVenta');
    if (!form) {
        return;
    }

    const productosSeleccionados = [];
    const stockDisponible = {};
    let tarjetasConfig = [];

    const ivaInput = document.getElementById('ivaRate');
    const ivaRate = ivaInput ? parseFloat(ivaInput.value.replace(',', '.')) || 0 : 0;

    const getTarjetasUrl = form.dataset.getTarjetasUrl;
    const calcularCuotasUrl = form.dataset.calcularCuotasUrl;
    const getPrecioProductoUrl = form.dataset.getPrecioProductoUrl;

    const $productoSelect = $('#productoSelect');
    const $precioInput = $('#precioInput');
    const $cantidadInput = $('#cantidadInput');
    const $descuentoInput = $('#descuentoInput');
    const $tarjetaRow = $('#tarjetaRow');
    const $chequeRow = $('#chequeRow');
    const $tarjetaSelect = $('#tarjetaSelect');
    const $cuotasSelect = $('#cuotasSelect');
    const $cuotasDiv = $('#cuotasDiv');
    const $infoCuotas = $('#infoCuotas');

    function init() {
        cargarTarjetas();
        bindEvents();
    }

    function bindEvents() {
        $('#tipoPagoSelect').on('change', handleTipoPagoChange);

        $productoSelect.on('change', function () {
            const productoId = $(this).val();
            if (productoId) {
                cargarPrecioProducto(productoId);
            } else {
                resetProductoInputs();
            }
        });

        $('#btnAgregarProducto').on('click', agregarProducto);

        $tarjetaSelect.on('change', function () {
            configurarTarjeta($(this).val());
        });

        $cuotasSelect.on('change', calcularCuotasTarjeta);

        $('#descuentoGeneral').on('change', calcularTotales);

        $('#productosBody').on('click', '.btn-eliminar-producto', function () {
            const index = $(this).data('index');
            eliminarProducto(Number(index));
        });

        $(form).on('submit', function (e) {
            if (productosSeleccionados.length === 0) {
                e.preventDefault();
                alert('Debe agregar al menos un producto a la venta');
            }
        });
    }

    function handleTipoPagoChange() {
        const tipoPago = $(this).val();
        $tarjetaRow.add($chequeRow).addClass('d-none');

        if (tipoPago === 'TarjetaDebito' || tipoPago === 'TarjetaCredito') {
            $tarjetaRow.removeClass('d-none');
        } else if (tipoPago === 'Cheque') {
            $chequeRow.removeClass('d-none');
            $('#chequeFechaEmision').val(new Date().toISOString().split('T')[0]);
        }
    }

    function cargarTarjetas() {
        if (!getTarjetasUrl) {
            return;
        }

        $.get(getTarjetasUrl)
            .done(function (tarjetas) {
                tarjetasConfig = tarjetas || [];
                let options = '<option value="">Seleccione tarjeta...</option>';
                tarjetasConfig.forEach(function (tarjeta) {
                    const tipo = tarjeta.tipo === 0 ? 'Débito' : 'Crédito';
                    options += `<option value="${tarjeta.id}">${tarjeta.nombre} (${tipo})</option>`;
                });
                $tarjetaSelect.html(options);
            });
    }

    function configurarTarjeta(tarjetaId) {
        const tarjeta = tarjetasConfig.find(function (t) { return t.id == tarjetaId; });
        if (!tarjeta) {
            return;
        }

        if (tarjeta.permiteCuotas && tarjeta.cantidadMaximaCuotas > 1) {
            let options = '';
            for (let i = 1; i <= tarjeta.cantidadMaximaCuotas; i++) {
                options += `<option value="${i}">${i} cuota${i > 1 ? 's' : ''}</option>`;
            }
            $cuotasSelect.html(options);
            $cuotasDiv.show();
        } else {
            $cuotasDiv.hide();
            $infoCuotas.hide();
        }
    }

    function calcularCuotasTarjeta() {
        const tarjetaId = $tarjetaSelect.val();
        const cuotas = $cuotasSelect.val();
        const total = parseFloat($('#hiddenTotal').val()) || 0;

        if (!tarjetaId || !cuotas || cuotas == 1 || !calcularCuotasUrl) {
            $infoCuotas.hide();
            return;
        }

        $.get(calcularCuotasUrl, {
            tarjetaId: tarjetaId,
            monto: total,
            cuotas: cuotas
        })
            .done(function (resultado) {
                $('#lblMontoCuota').text(resultado.montoCuota.toFixed(2));
                $('#lblMontoTotal').text(resultado.montoTotal.toFixed(2));
                $('#lblInteres').text(resultado.interes.toFixed(2));
                $infoCuotas.removeClass('d-none').show();
            })
            .fail(function () {
                $infoCuotas.hide();
            });
    }

    function cargarPrecioProducto(productoId) {
        if (!getPrecioProductoUrl) {
            return;
        }

        const producto = $productoSelect.find('option:selected').text();
        const stock = producto.match(/Stock: (\d+)/);
        if (stock && stock[1]) {
            stockDisponible[productoId] = parseInt(stock[1], 10);
        }

        $.get(getPrecioProductoUrl, { id: productoId })
            .done(function (respuesta) {
                if (respuesta && typeof respuesta.precioVenta !== 'undefined') {
                    $precioInput.val(parseFloat(respuesta.precioVenta).toFixed(2));
                    stockDisponible[productoId] = respuesta.stockActual ?? stockDisponible[productoId];
                } else if (typeof respuesta === 'number') {
                    $precioInput.val(parseFloat(respuesta).toFixed(2));
                }
            })
            .fail(function () {
                alert('No se pudo obtener el precio del producto seleccionado.');
                $precioInput.val('');
            });
    }

    function agregarProducto() {
        const productoId = $productoSelect.val();
        if (!productoId) {
            alert('Seleccione un producto');
            return;
        }

        const productoTexto = $productoSelect.find('option:selected').text();
        const cantidad = parseInt($cantidadInput.val(), 10);
        const precio = parseFloat($precioInput.val());
        const descuentoPct = parseFloat($descuentoInput.val()) || 0;

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

        productosSeleccionados.push({
            productoId: productoId,
            codigo: codigo,
            nombre: nombre,
            cantidad: cantidad,
            precioUnitario: precio,
            descuento: descuentoMonto,
            subtotal: subtotal
        });

        actualizarTablaProductos();
        calcularTotales();
        resetProductoInputs();
    }

    function eliminarProducto(index) {
        if (Number.isNaN(index) || index < 0) {
            return;
        }
        productosSeleccionados.splice(index, 1);
        actualizarTablaProductos();
        calcularTotales();
    }

    function actualizarTablaProductos() {
        let html = '';
        productosSeleccionados.forEach(function (prod, index) {
            html += `
                <tr>
                    <td>${prod.codigo}</td>
                    <td>${prod.nombre}</td>
                    <td class="text-center">${prod.cantidad}</td>
                    <td class="text-end">$${prod.precioUnitario.toFixed(2)}</td>
                    <td class="text-end">$${prod.descuento.toFixed(2)}</td>
                    <td class="text-end">$${prod.subtotal.toFixed(2)}</td>
                    <td class="text-center">
                        <button type="button" class="btn btn-sm btn-danger btn-eliminar-producto" data-index="${index}">
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
        $('#productosBody').html(html);
    }

    function calcularTotales() {
        const descuentoGeneral = parseFloat($('#descuentoGeneral').val()) || 0;
        let subtotal = productosSeleccionados.reduce(function (acc, prod) { return acc + prod.subtotal; }, 0);
        const descuento = subtotal * (descuentoGeneral / 100);
        subtotal -= descuento;

        const iva = subtotal * ivaRate;
        const total = subtotal + iva;

        $('#hiddenSubtotal').val(subtotal.toFixed(2));
        $('#hiddenIVA').val(iva.toFixed(2));
        $('#hiddenTotal').val(total.toFixed(2));

        $('#lblSubtotal').text(`$${subtotal.toFixed(2)}`);
        $('#lblDescuento').text(`$${descuento.toFixed(2)}`);
        $('#lblIVA').text(`$${iva.toFixed(2)}`);
        $('#lblTotal').text(`$${total.toFixed(2)}`);
    }

    function resetProductoInputs() {
        $productoSelect.val('');
        $cantidadInput.val(1);
        $precioInput.val('');
        $descuentoInput.val(0);
    }

    init();
})(jQuery);
