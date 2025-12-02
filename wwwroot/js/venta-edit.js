(function () {
    const form = document.getElementById('formVenta');
    if (!form) {
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
    const detalleManager = VentaCommon.createDetalleManager({
        keyField: 'index',
        initialDetalles: detalles,
        keyFactory: function () { return detalleIndex++; },
        onChange: function (list) { detalles = list; }
    });
    let recalculoFinanciamientoEnCurso = false;

    const precioProductoUrl = form.dataset.getPrecioProductoUrl;
    const financiamientoUrl = form.dataset.calcularFinanciamientoUrl;
    const calcularTotalesUrl = form.dataset.calcularTotalesUrl;
    const descuentoEsPorcentaje = form.dataset.descuentoEsPorcentaje === 'true' || form.dataset.descuentoEsPorcentaje === true;
    const antiforgeryToken = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

    const toggleFinanciado = document.getElementById('toggleFinanciado');
    const financiamientoCampos = document.getElementById('financiamientoCampos');
    const tipoPagoSelect = document.getElementById('TipoPago');
    const productoSelect = document.getElementById('productoSelect');
    const precioInput = document.getElementById('precioInput');
    const cantidadInput = document.getElementById('cantidadInput');
    const descuentoInput = document.getElementById('descuentoInput');
    const detallesBody = document.getElementById('detallesBody');
    const descuentoGeneralInput = document.getElementById('Descuento');
    const totalHidden = document.getElementById('totalHidden');
    const subtotalHidden = document.getElementById('subtotalHidden');
    const ivaHidden = document.getElementById('ivaHidden');

    let ultimoTipoPagoNoFinanciado = tipoPagoSelect?.value && tipoPagoSelect.value !== 'CreditoPersonal'
        ? tipoPagoSelect.value
        : 'Efectivo';

    function init() {
        inicializarFilasExistentes();
        calcularTotales();
        bindEventos();
        sincronizarTipoPagoConFinanciado();
    }

    function bindEventos() {
        toggleFinanciado?.addEventListener('change', function () {
            const activo = this.checked;
            const esFinanciada = document.getElementById('EsFinanciada');
            if (esFinanciada) {
                esFinanciada.value = activo;
            }
            financiamientoCampos?.classList.toggle('d-none', !activo);

            if (activo) {
                sincronizarTipoPagoConFinanciado();
                recalcularFinanciamiento();
            } else {
                restaurarTipoPagoNoFinanciado();
                limpiarResumenFinanciamiento();
            }
        });

        const esFinanciadaHidden = document.getElementById('EsFinanciada');
        if (toggleFinanciado && esFinanciadaHidden) {
            esFinanciadaHidden.value = toggleFinanciado.checked;
            if (toggleFinanciado.checked) {
                sincronizarTipoPagoConFinanciado();
                recalcularFinanciamiento();
            }
        }

        tipoPagoSelect?.addEventListener('change', function () {
            if (this.value !== 'CreditoPersonal' && toggleFinanciado?.checked) {
                toggleFinanciado.checked = false;
                financiamientoCampos?.classList.add('d-none');
                restaurarTipoPagoNoFinanciado();
                limpiarResumenFinanciamiento();
            } else if (this.value === 'CreditoPersonal') {
                sincronizarTipoPagoConFinanciado();
            }
        });

        ['anticipoInput', 'tasaMensualInput', 'cuotasFinanciacionInput', 'ingresoNetoInput', 'otrasDeudasInput', 'antiguedadLaboralInput']
            .forEach(function (id) {
                const input = document.getElementById(id);
                input?.addEventListener('input', handleFinanciamientoInput);
                input?.addEventListener('change', handleFinanciamientoInput);
            });

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

            const creado = detalleManager.add(detalle);
            agregarFilaDetalle(creado);
            calcularTotales();

            if (productoSelect) productoSelect.value = '';
            if (cantidadInput) cantidadInput.value = 1;
            if (precioInput) precioInput.value = '';
            if (descuentoInput) descuentoInput.value = 0;
        });

        detallesBody?.addEventListener('click', function (event) {
            const btn = event.target.closest('.btn-eliminar-detalle');
            if (!btn) return;
            const index = parseInt(btn.dataset.index, 10);
            if (Number.isFinite(index)) {
                eliminarDetalle(index);
            }
        });

        form.addEventListener('submit', function () {
            document.querySelectorAll('input[name^="Detalles"]').forEach(function (input) { input.remove(); });
            detalleManager.getAll().forEach((detalle, index) => {
                form.insertAdjacentHTML('beforeend', `
                    <input type="hidden" name="Detalles[${index}].ProductoId" value="${detalle.ProductoId}" />
                    <input type="hidden" name="Detalles[${index}].Cantidad" value="${detalle.Cantidad}" />
                    <input type="hidden" name="Detalles[${index}].PrecioUnitario" value="${detalle.PrecioUnitario}" />
                    <input type="hidden" name="Detalles[${index}].Descuento" value="${detalle.Descuento}" />
                    <input type="hidden" name="Detalles[${index}].Subtotal" value="${detalle.Subtotal}" />
                `);
            });
        });

        descuentoGeneralInput?.addEventListener('input', calcularTotales);
    }

    function handleFinanciamientoInput() {
        if (toggleFinanciado?.checked) {
            recalcularFinanciamiento();
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
        detallesBody.insertAdjacentHTML('beforeend', `
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
        detalleManager.removeByKey(index);
        detallesBody?.querySelector(`tr[data-index="${index}"]`)?.remove();
        calcularTotales();
    }

    function limpiarResumenFinanciamiento() {
        setValue('MontoFinanciadoEstimado', '');
        setValue('CuotaEstimada', '');
        setText('lblMontoFinanciadoEstimado', '$0.00');
        setText('lblCuotaEstimada', '$0.00');
        actualizarSemaforo(null);
    }

    function restaurarTipoPagoNoFinanciado() {
        if (!tipoPagoSelect) return;
        if (tipoPagoSelect.value === 'CreditoPersonal') {
            tipoPagoSelect.value = ultimoTipoPagoNoFinanciado || 'Efectivo';
            tipoPagoSelect.dispatchEvent(new Event('change'));
        }
    }

    function sincronizarTipoPagoConFinanciado() {
        if (!toggleFinanciado || !tipoPagoSelect) return;
        if (toggleFinanciado.checked) {
            if (tipoPagoSelect.value !== 'CreditoPersonal') {
                ultimoTipoPagoNoFinanciado = tipoPagoSelect.value || ultimoTipoPagoNoFinanciado;
                tipoPagoSelect.value = 'CreditoPersonal';
                tipoPagoSelect.dispatchEvent(new Event('change'));
            }
        }
    }

    function setValue(id, value) {
        const el = typeof id === 'string' ? document.getElementById(id) : id;
        if (el) {
            el.value = value;
        }
    }

    function setText(id, value) {
        const el = typeof id === 'string' ? document.getElementById(id) : id;
        if (el) {
            el.textContent = value;
        }
    }

    function recalcularFinanciamiento() {
        if (recalculoFinanciamientoEnCurso || !financiamientoUrl) {
            return;
        }

        const total = parseFloat(totalHidden?.value) || 0;
        const anticipo = parseFloat(document.getElementById('anticipoInput')?.value || '') || 0;
        const tasa = (parseFloat(document.getElementById('tasaMensualInput')?.value || '') || 0) / 100;
        const cuotas = parseInt(document.getElementById('cuotasFinanciacionInput')?.value || '', 10) || 0;

        if (total <= 0 || cuotas < 1) {
            limpiarResumenFinanciamiento();
            return;
        }

        recalculoFinanciamientoEnCurso = true;
        const badge = document.getElementById('badgeSemaforo');
        badge?.classList.remove('bg-success', 'bg-warning', 'bg-danger');
        badge?.classList.add('bg-secondary');
        if (badge) badge.textContent = 'Calculando...';

        const headers = { 'Content-Type': 'application/json' };
        if (antiforgeryToken) {
            headers.RequestVerificationToken = antiforgeryToken;
        }

        const payload = {
            total: total,
            anticipo: anticipo,
            tasaMensual: tasa,
            cuotas: cuotas,
            ingresoNeto: parseFloat(document.getElementById('ingresoNetoInput')?.value || '') || null,
            otrasDeudas: parseFloat(document.getElementById('otrasDeudasInput')?.value || '') || null,
            antiguedadLaboralMeses: parseInt(document.getElementById('antiguedadLaboralInput')?.value || '', 10) || null
        };

        fetch(financiamientoUrl, {
            method: 'POST',
            headers: headers,
            body: JSON.stringify(payload)
        })
            .then(function (response) { return response.ok ? response.json() : null; })
            .then(function (data) {
                if (!data) {
                    actualizarSemaforo(null, 'No se pudo calcular el financiamiento');
                    return;
                }

                setValue('MontoFinanciadoEstimado', Number(data.financedAmount).toFixed(2));
                setValue('CuotaEstimada', Number(data.installment).toFixed(2));
                setText('lblMontoFinanciadoEstimado', `$${Number(data.financedAmount).toFixed(2)}`);
                setText('lblCuotaEstimada', `$${Number(data.installment).toFixed(2)}`);
                actualizarSemaforo(data.prequalification);
            })
            .catch(function (error) {
                const message = error?.message || 'No se pudo calcular el financiamiento';
                actualizarSemaforo(null, message);
            })
            .finally(function () {
                recalculoFinanciamientoEnCurso = false;
            });
    }

    function actualizarSemaforo(resultado, error) {
        const badge = document.getElementById('badgeSemaforo');
        const mensaje = document.getElementById('mensajeSemaforo');
        const flags = document.getElementById('flagsSemaforo');

        badge?.classList.remove('bg-success', 'bg-warning', 'bg-danger', 'bg-secondary');

        if (error) {
            badge?.classList.add('bg-danger');
            if (badge) badge.textContent = 'Error';
            if (mensaje) mensaje.textContent = error;
            if (flags) flags.textContent = '';
            return;
        }

        if (!resultado) {
            badge?.classList.add('bg-secondary');
            if (badge) badge.textContent = 'Sin datos';
            if (mensaje) mensaje.textContent = 'Completa los datos para precalificar.';
            if (flags) flags.textContent = '';
            return;
        }

        let label = 'Indeterminado';
        let badgeClass = 'bg-secondary';

        switch (resultado.status) {
            case 1:
                label = 'Verde';
                badgeClass = 'bg-success';
                if (mensaje) mensaje.textContent = 'Capacidad validada con política 30%.';
                break;
            case 2:
                label = 'Amarillo';
                badgeClass = 'bg-warning';
                if (mensaje) mensaje.textContent = resultado.recomendacion || 'Revisar datos adicionales.';
                break;
            case 3:
                label = 'Rojo';
                badgeClass = 'bg-danger';
                if (mensaje) mensaje.textContent = resultado.recomendacion || 'No cumple política.';
                break;
            default:
                if (mensaje) mensaje.textContent = 'Completa los datos para precalificar.';
                break;
        }

        badge?.classList.add(badgeClass);
        if (badge) badge.textContent = label;

        if (resultado.flags && resultado.flags.length > 0) {
            if (flags) flags.textContent = resultado.flags.join(' • ');
        } else if (flags) {
            flags.textContent = '';
        }
    }

    function calcularTotales() {
        const actuales = detalleManager.getAll();
        if (!calcularTotalesUrl || actuales.length === 0) {
            VentaCommon.resetTotalesUI({
                subtotalSelector: '#subtotalDisplay',
                descuentoSelector: '#descuentoDisplay',
                ivaSelector: '#ivaDisplay',
                totalSelector: '#totalDisplay',
                hiddenSubtotal: subtotalHidden,
                hiddenIVA: ivaHidden,
                hiddenTotal: totalHidden
            });

            if (toggleFinanciado?.checked) {
                recalcularFinanciamiento();
            }
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
                    subtotalSelector: '#subtotalDisplay',
                    descuentoSelector: '#descuentoDisplay',
                    ivaSelector: '#ivaDisplay',
                    totalSelector: '#totalDisplay',
                    hiddenSubtotal: subtotalHidden,
                    hiddenIVA: ivaHidden,
                    hiddenTotal: totalHidden
                });

                if (toggleFinanciado?.checked) {
                    recalcularFinanciamiento();
                }
            })
            .catch(() => {
                VentaCommon.resetTotalesUI({
                    subtotalSelector: '#subtotalDisplay',
                    descuentoSelector: '#descuentoDisplay',
                    ivaSelector: '#ivaDisplay',
                    totalSelector: '#totalDisplay',
                    hiddenSubtotal: subtotalHidden,
                    hiddenIVA: ivaHidden,
                    hiddenTotal: totalHidden
                });
            });
    }

    init();
})();
