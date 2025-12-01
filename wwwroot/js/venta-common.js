(function () {
    function resolveElement(ref) {
        if (!ref) return null;
        if (typeof ref === 'string') return document.querySelector(ref);
        return ref;
    }

    function setText(ref, value) {
        const el = resolveElement(ref);
        if (el) {
            el.textContent = value;
        }
    }

    function setValue(ref, value) {
        const el = resolveElement(ref);
        if (el) {
            el.value = value;
        }
    }

    function toggleDisplay(ref, visible) {
        const el = resolveElement(ref);
        if (!el) return;
        el.classList.toggle('d-none', !visible);
    }

    function createDetalleManager(options) {
        const opts = options || {};
        const keyField = opts.keyField || 'key';
        const keyFactory = typeof opts.keyFactory === 'function' ? opts.keyFactory : function (index) { return index; };
        const detalles = (opts.initialDetalles || []).map(function (detalle, index) {
            if (typeof detalle[keyField] === 'undefined') {
                detalle[keyField] = keyFactory(index, detalle);
            }
            return detalle;
        });

        function add(detalle) {
            const newDetalle = Object.assign({}, detalle);
            if (typeof newDetalle[keyField] === 'undefined') {
                newDetalle[keyField] = keyFactory(detalles.length, newDetalle);
            }
            detalles.push(newDetalle);
            if (typeof opts.onChange === 'function') {
                opts.onChange(detalles);
            }
            return newDetalle;
        }

        function removeByKey(key) {
            const idx = detalles.findIndex(function (detalle) { return String(detalle[keyField]) === String(key); });
            if (idx >= 0) {
                detalles.splice(idx, 1);
                if (typeof opts.onChange === 'function') {
                    opts.onChange(detalles);
                }
            }
        }

        function getAll() {
            return detalles;
        }

        return {
            add: add,
            removeByKey: removeByKey,
            getAll: getAll
        };
    }

    function calcularTotales(options) {
        const { detalles, url, descuentoGeneral, descuentoEsPorcentaje, antiforgeryToken } = options;

        if (!url || !detalles || !detalles.length) {
            return Promise.resolve(null);
        }

        const payload = {
            detalles: detalles.map(function (detalle) {
                return {
                    productoId: detalle.productoId ?? detalle.ProductoId,
                    cantidad: detalle.cantidad ?? detalle.Cantidad,
                    precioUnitario: detalle.precioUnitario ?? detalle.PrecioUnitario,
                    descuento: detalle.descuento ?? detalle.Descuento
                };
            }),
            descuentoGeneral: descuentoGeneral,
            descuentoEsPorcentaje: descuentoEsPorcentaje
        };

        const headers = { 'Content-Type': 'application/json' };
        if (antiforgeryToken) {
            headers.RequestVerificationToken = antiforgeryToken;
        }

        return fetch(url, {
            method: 'POST',
            headers: headers,
            body: JSON.stringify(payload)
        }).then(function (response) {
            if (!response.ok) {
                throw new Error('No se pudieron calcular los totales');
            }
            return response.json();
        });
    }

    function resetTotalesUI(config) {
        const { subtotalSelector, descuentoSelector, ivaSelector, totalSelector, hiddenSubtotal, hiddenIVA, hiddenTotal } = config;

        setText(subtotalSelector, '$0.00');
        setText(descuentoSelector, '$0.00');
        setText(ivaSelector, '$0.00');
        setText(totalSelector, '$0.00');
        setValue(hiddenSubtotal, '0.00');
        setValue(hiddenIVA, '0.00');
        setValue(hiddenTotal, '0.00');
    }

    function aplicarTotalesUI(data, config) {
        if (!data) {
            resetTotalesUI(config);
            return;
        }

        const subtotal = Number(data.subtotal || 0);
        const descuento = Number(data.descuentoGeneralAplicado || 0);
        const iva = Number(data.iva || 0);
        const total = Number(data.total || 0);

        setText(config.subtotalSelector, `$${subtotal.toFixed(2)}`);
        setText(config.descuentoSelector, `$${descuento.toFixed(2)}`);
        setText(config.ivaSelector, `$${iva.toFixed(2)}`);
        setText(config.totalSelector, `$${total.toFixed(2)}`);
        setValue(config.hiddenSubtotal, subtotal.toFixed(2));
        setValue(config.hiddenIVA, iva.toFixed(2));
        setValue(config.hiddenTotal, total.toFixed(2));
    }

    function initTarjetaHandlers(config) {
        const state = { tarjetas: [] };

        const tipoPagoSelect = resolveElement(config.tipoPagoSelect);
        const tarjetaRow = resolveElement(config.tarjetaRow);
        const chequeRow = resolveElement(config.chequeRow);
        const chequeFechaInput = resolveElement(config.chequeFechaInput);
        const tarjetaSelect = resolveElement(config.tarjetaSelect);
        const cuotasSelect = resolveElement(config.cuotasSelect);
        const cuotasDiv = resolveElement(config.cuotasDiv);
        const infoCuotas = resolveElement(config.infoCuotas);
        const totalHidden = resolveElement(config.totalHidden);

        function handleTipoPagoChange(tipoPago) {
            toggleDisplay(tarjetaRow, false);
            toggleDisplay(chequeRow, false);

            if (tipoPago === 'TarjetaDebito' || tipoPago === 'TarjetaCredito') {
                toggleDisplay(tarjetaRow, true);
            } else if (tipoPago === 'Cheque') {
                toggleDisplay(chequeRow, true);
                if (chequeFechaInput) {
                    chequeFechaInput.value = new Date().toISOString().split('T')[0];
                }
            }
        }

        function cargarTarjetas() {
            if (!config.getTarjetasUrl || !tarjetaSelect) return;

            fetch(config.getTarjetasUrl)
                .then(function (response) { return response.ok ? response.json() : []; })
                .then(function (tarjetas) {
                    state.tarjetas = Array.isArray(tarjetas) ? tarjetas : [];
                    const fragment = document.createDocumentFragment();

                    const placeholder = document.createElement('option');
                    placeholder.value = '';
                    placeholder.textContent = 'Seleccione tarjeta...';
                    fragment.appendChild(placeholder);

                    state.tarjetas.forEach(function (tarjeta) {
                        const option = document.createElement('option');
                        option.value = tarjeta.id;
                        const tipo = tarjeta.tipo === 0 ? 'Débito' : 'Crédito';
                        option.textContent = `${tarjeta.nombre} (${tipo})`;
                        fragment.appendChild(option);
                    });

                    tarjetaSelect.innerHTML = '';
                    tarjetaSelect.appendChild(fragment);
                })
                .catch(function () { /* ignore */ });
        }

        function configurarTarjeta(tarjetaId) {
            if (!tarjetaSelect || !cuotasSelect || !cuotasDiv || !infoCuotas) return;
            infoCuotas.classList.add('d-none');

            const tarjeta = state.tarjetas.find(function (t) { return String(t.id) === String(tarjetaId); });
            if (!tarjeta) {
                return;
            }

            if (tarjeta.permiteCuotas && tarjeta.cantidadMaximaCuotas > 1) {
                const fragment = document.createDocumentFragment();
                for (let i = 1; i <= tarjeta.cantidadMaximaCuotas; i++) {
                    const option = document.createElement('option');
                    option.value = i;
                    option.textContent = `${i} cuota${i > 1 ? 's' : ''}`;
                    fragment.appendChild(option);
                }
                cuotasSelect.innerHTML = '';
                cuotasSelect.appendChild(fragment);
                cuotasDiv.classList.remove('d-none');
            } else {
                cuotasDiv.classList.add('d-none');
            }
        }

        function calcularCuotasTarjeta() {
            if (!cuotasSelect || !tarjetaSelect || !totalHidden || !infoCuotas || !config.calcularCuotasUrl) {
                return;
            }

            const tarjetaId = tarjetaSelect.value;
            const cuotas = cuotasSelect.value;
            const total = parseFloat(totalHidden.value) || 0;

            if (!tarjetaId || !cuotas || Number(cuotas) === 1) {
                infoCuotas.classList.add('d-none');
                return;
            }

            const params = new URLSearchParams({ tarjetaId: tarjetaId, monto: total, cuotas: cuotas });
            fetch(`${config.calcularCuotasUrl}?${params.toString()}`)
                .then(function (response) { return response.ok ? response.json() : null; })
                .then(function (resultado) {
                    if (!resultado) {
                        infoCuotas.classList.add('d-none');
                        return;
                    }

                    const montoCuota = document.getElementById('lblMontoCuota');
                    const montoTotal = document.getElementById('lblMontoTotal');
                    const interes = document.getElementById('lblInteres');

                    setText(montoCuota, resultado.montoCuota.toFixed(2));
                    setText(montoTotal, resultado.montoTotal.toFixed(2));
                    setText(interes, resultado.interes.toFixed(2));
                    infoCuotas.classList.remove('d-none');
                })
                .catch(function () {
                    infoCuotas.classList.add('d-none');
                });
        }

        function bindEvents() {
            if (tipoPagoSelect) {
                tipoPagoSelect.addEventListener('change', function (e) {
                    handleTipoPagoChange(e.target.value);
                });
            }

            if (tarjetaSelect) {
                tarjetaSelect.addEventListener('change', function (e) {
                    configurarTarjeta(e.target.value);
                });
            }

            if (cuotasSelect) {
                cuotasSelect.addEventListener('change', calcularCuotasTarjeta);
            }

            cargarTarjetas();
        }

        return {
            bindEvents: bindEvents,
            cargarTarjetas: cargarTarjetas,
            configurarTarjeta: configurarTarjeta,
            calcularCuotasTarjeta: calcularCuotasTarjeta,
            handleTipoPagoChange: handleTipoPagoChange
        };
    }

    window.VentaCommon = {
        createDetalleManager: createDetalleManager,
        calcularTotales: calcularTotales,
        resetTotalesUI: resetTotalesUI,
        aplicarTotalesUI: aplicarTotalesUI,
        initTarjetaHandlers: initTarjetaHandlers
    };
})();
