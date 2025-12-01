(function () {
    const form = $('#formVenta');
    if (!form.length) {
        return;
    }

    const dataContainer = document.getElementById('venta-edit-data');
    let detalles = [];

    if (dataContainer) {
        try {
            detalles = JSON.parse(dataContainer.dataset.detalles || '[]');
        } catch (error) {
            detalles = [];
        }
    }

    let detalleIndex = detalles.length ? Math.max(...detalles.map((d) => d.index || 0)) + 1 : 1;
    let recalculoFinanciamientoEnCurso = false;

    const ivaRate = parseFloat($('#ivaRate').val()?.replace(',', '.') || '0') || 0;
    const precioProductoUrl = form.data('get-precio-producto-url');
    const financiamientoUrl = form.data('calcular-financiamiento-url');

    $(document).ready(function () {
        inicializarFilasExistentes();
        calcularTotales();

        $('#toggleFinanciado').on('change', function () {
            const activo = $(this).is(':checked');
            $('#EsFinanciada').val(activo);
            $('#financiamientoCampos').toggleClass('d-none', !activo);

            if (activo) {
                recalcularFinanciamiento();
            } else {
                limpiarResumenFinanciamiento();
            }
        });

        $('#EsFinanciada').val($('#toggleFinanciado').is(':checked'));
        if ($('#toggleFinanciado').is(':checked')) {
            recalcularFinanciamiento();
        }

        $('#anticipoInput, #tasaMensualInput, #cuotasFinanciacionInput, #ingresoNetoInput, #otrasDeudasInput, #antiguedadLaboralInput')
            .on('input change', function () {
                if ($('#toggleFinanciado').is(':checked')) {
                    recalcularFinanciamiento();
                }
            });

        $('#productoSelect').on('change', function () {
            const productoId = $(this).val();
            if (productoId && precioProductoUrl) {
                $.get(precioProductoUrl, { id: productoId }, function (data) {
                    if (data?.precioVenta !== undefined) {
                        $('#precioInput').val(Number(data.precioVenta).toFixed(2));
                    }
                });
            }
        });

        $('#btnAgregarProducto').on('click', function () {
            const productoId = $('#productoSelect').val();
            const productoTexto = $('#productoSelect option:selected').text();
            const cantidad = parseFloat($('#cantidadInput').val());
            const precio = parseFloat($('#precioInput').val());
            const descuento = parseFloat($('#descuentoInput').val()) || 0;

            if (!productoId || cantidad <= 0 || precio <= 0) {
                alert('Complete todos los campos correctamente.');
                return;
            }

            const subtotal = (cantidad * precio) - descuento;

            const detalle = {
                index: detalleIndex++,
                ProductoId: productoId,
                ProductoNombre: productoTexto,
                Cantidad: cantidad,
                PrecioUnitario: precio,
                Descuento: descuento,
                Subtotal: subtotal,
                Id: 0
            };

            detalles.push(detalle);
            agregarFilaDetalle(detalle);
            calcularTotales();

            $('#productoSelect').val('');
            $('#cantidadInput').val(1);
            $('#precioInput').val('');
            $('#descuentoInput').val(0);
        });

        $('#detallesBody').on('click', '.btn-eliminar-detalle', function () {
            const index = parseInt($(this).data('index'), 10);
            if (Number.isFinite(index)) {
                eliminarDetalle(index);
            }
        });

        form.on('submit', function () {
            $('input[name^="Detalles"]').remove();
            detalles.forEach((detalle, index) => {
                $(this).append(`
                    <input type="hidden" name="Detalles[${index}].ProductoId" value="${detalle.ProductoId}" />
                    <input type="hidden" name="Detalles[${index}].Cantidad" value="${detalle.Cantidad}" />
                    <input type="hidden" name="Detalles[${index}].PrecioUnitario" value="${detalle.PrecioUnitario}" />
                    <input type="hidden" name="Detalles[${index}].Descuento" value="${detalle.Descuento}" />
                    <input type="hidden" name="Detalles[${index}].Subtotal" value="${detalle.Subtotal}" />
                `);
            });
        });

        $('#Descuento').on('input', calcularTotales);
    });

    function inicializarFilasExistentes() {
        if (!detalles.length) {
            return;
        }

        detalles.forEach((detalle) => {
            if (!$(`#detallesBody tr[data-index="${detalle.index}"]`).length) {
                agregarFilaDetalle(detalle);
            }
        });
    }

    function agregarFilaDetalle(detalle) {
        $('#detallesBody').append(`
            <tr data-index="${detalle.index}">
                <td>${detalle.ProductoNombre}</td>
                <td>${detalle.Cantidad}</td>
                <td>$${Number(detalle.PrecioUnitario).toFixed(2)}</td>
                <td>$${Number(detalle.Descuento).toFixed(2)}</td>
                <td>$${Number(detalle.Subtotal).toFixed(2)}</td>
                <td class="text-center">
                    <button type="button" class="btn btn-sm btn-danger btn-eliminar-detalle" data-index="${detalle.index}">
                        <i class="bi bi-trash"></i>
                    </button>
                </td>
            </tr>
        `);
    }

    function eliminarDetalle(index) {
        detalles = detalles.filter((d) => d.index !== index);
        $(`tr[data-index="${index}"]`).remove();
        calcularTotales();
    }

    function limpiarResumenFinanciamiento() {
        $('#MontoFinanciadoEstimado').val('');
        $('#CuotaEstimada').val('');
        $('#lblMontoFinanciadoEstimado').text('$0.00');
        $('#lblCuotaEstimada').text('$0.00');
        actualizarSemaforo(null);
    }

    function recalcularFinanciamiento() {
        if (recalculoFinanciamientoEnCurso || !financiamientoUrl) {
            return;
        }

        const total = parseFloat($('#totalHidden').val()) || 0;
        const anticipo = parseFloat($('#anticipoInput').val()) || 0;
        const tasa = (parseFloat($('#tasaMensualInput').val()) || 0) / 100;
        const cuotas = parseInt($('#cuotasFinanciacionInput').val(), 10) || 0;

        if (total <= 0 || cuotas < 1) {
            limpiarResumenFinanciamiento();
            return;
        }

        recalculoFinanciamientoEnCurso = true;
        $('#badgeSemaforo').removeClass('bg-success bg-warning bg-danger').addClass('bg-secondary').text('Calculando...');

        const token = $('input[name="__RequestVerificationToken"]').val();

        $.ajax({
            url: financiamientoUrl,
            type: 'POST',
            contentType: 'application/json',
            headers: { RequestVerificationToken: token },
            data: JSON.stringify({
                total: total,
                anticipo: anticipo,
                tasaMensual: tasa,
                cuotas: cuotas,
                ingresoNeto: parseFloat($('#ingresoNetoInput').val()) || null,
                otrasDeudas: parseFloat($('#otrasDeudasInput').val()) || null,
                antiguedadLaboralMeses: parseInt($('#antiguedadLaboralInput').val(), 10) || null
            })
        })
            .done(function (data) {
                if (!data) {
                    actualizarSemaforo(null, 'No se pudo calcular el financiamiento');
                    return;
                }

                $('#MontoFinanciadoEstimado').val(Number(data.financedAmount).toFixed(2));
                $('#CuotaEstimada').val(Number(data.installment).toFixed(2));
                $('#lblMontoFinanciadoEstimado').text(`$${Number(data.financedAmount).toFixed(2)}`);
                $('#lblCuotaEstimada').text(`$${Number(data.installment).toFixed(2)}`);
                actualizarSemaforo(data.prequalification);
            })
            .fail(function (xhr) {
                const error = xhr.responseJSON?.error || 'No se pudo calcular el financiamiento';
                actualizarSemaforo(null, error);
            })
            .always(function () {
                recalculoFinanciamientoEnCurso = false;
            });
    }

    function actualizarSemaforo(resultado, error) {
        const badge = $('#badgeSemaforo');
        const mensaje = $('#mensajeSemaforo');
        const flags = $('#flagsSemaforo');

        badge.removeClass('bg-success bg-warning bg-danger bg-secondary');

        if (error) {
            badge.addClass('bg-danger').text('Error');
            mensaje.text(error);
            flags.text('');
            return;
        }

        if (!resultado) {
            badge.addClass('bg-secondary').text('Sin datos');
            mensaje.text('Completa los datos para precalificar.');
            flags.text('');
            return;
        }

        let label = 'Indeterminado';
        let badgeClass = 'bg-secondary';

        switch (resultado.status) {
            case 1:
                label = 'Verde';
                badgeClass = 'bg-success';
                mensaje.text('Capacidad validada con política 30%.');
                break;
            case 2:
                label = 'Amarillo';
                badgeClass = 'bg-warning';
                mensaje.text(resultado.recomendacion || 'Revisar datos adicionales.');
                break;
            case 3:
                label = 'Rojo';
                badgeClass = 'bg-danger';
                mensaje.text(resultado.recomendacion || 'No cumple política.');
                break;
            default:
                mensaje.text('Completa los datos para precalificar.');
                break;
        }

        badge.addClass(badgeClass).text(label);

        if (resultado.flags && resultado.flags.length > 0) {
            flags.text(resultado.flags.join(' • '));
        } else {
            flags.text('');
        }
    }

    function calcularTotales() {
        const subtotal = detalles.reduce((sum, d) => sum + Number(d.Subtotal), 0);
        const descuentoGlobal = parseFloat($('#Descuento').val()) || 0;
        const subtotalConDescuento = subtotal - descuentoGlobal;
        const iva = subtotalConDescuento * ivaRate;
        const total = subtotalConDescuento + iva;

        $('#subtotalDisplay').text(`$${subtotal.toFixed(2)}`);
        $('#descuentoDisplay').text(`$${descuentoGlobal.toFixed(2)}`);
        $('#ivaDisplay').text(`$${iva.toFixed(2)}`);
        $('#totalDisplay').text(`$${total.toFixed(2)}`);

        $('#subtotalHidden').val(subtotal.toFixed(2));
        $('#ivaHidden').val(iva.toFixed(2));
        $('#totalHidden').val(total.toFixed(2));

        if ($('#toggleFinanciado').is(':checked')) {
            recalcularFinanciamiento();
        }
    }
})();
