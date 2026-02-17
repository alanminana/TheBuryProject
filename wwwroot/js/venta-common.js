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

        function normalizeTipoPago(value) {
            const v = String(value ?? '').trim();

            if (v === '2' || v === 'TarjetaDebito') return 'TarjetaDebito';
            if (v === '3' || v === 'TarjetaCredito') return 'TarjetaCredito';
            if (v === '4' || v === 'Cheque') return 'Cheque';

            return v;
        }

        function setTodayOnDateInput(inputEl) {
            if (!inputEl) return;

            if ('valueAsDate' in inputEl) {
                inputEl.valueAsDate = new Date();
                return;
            }

            const d = new Date();
            const yyyy = d.getFullYear();
            const mm = String(d.getMonth() + 1).padStart(2, '0');
            const dd = String(d.getDate()).padStart(2, '0');
            inputEl.value = `${yyyy}-${mm}-${dd}`;
        }

        function handleTipoPagoChange(tipoPago) {
            const normalizado = normalizeTipoPago(tipoPago);

            toggleDisplay(tarjetaRow, false);
            toggleDisplay(chequeRow, false);

            if (normalizado === 'TarjetaDebito' || normalizado === 'TarjetaCredito') {
                toggleDisplay(tarjetaRow, true);
            } else if (normalizado === 'Cheque') {
                toggleDisplay(chequeRow, true);
                setTodayOnDateInput(chequeFechaInput);
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

    function showToast(message, options) {
        const opts = options || {};
        const level = opts.level || 'warning';
        const delay = Number(opts.delay) > 0 ? Number(opts.delay) : 3500;
        const strong = opts.title || 'Atención';

        let container = document.getElementById('ventaToastContainer');
        if (!container) {
            container = document.createElement('div');
            container.id = 'ventaToastContainer';
            container.className = 'toast-container position-fixed top-0 end-0 p-3';
            container.style.zIndex = '1090';
            document.body.appendChild(container);
        }

        const toastEl = document.createElement('div');
        toastEl.className = `toast align-items-center text-bg-${level} border-0`;
        toastEl.setAttribute('role', 'status');
        toastEl.setAttribute('aria-live', 'polite');
        toastEl.setAttribute('aria-atomic', 'true');
        toastEl.innerHTML = `
          <div class="d-flex">
            <div class="toast-body">
              <strong class="me-1">${strong}:</strong>${message}
            </div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Cerrar"></button>
          </div>
        `;

        container.appendChild(toastEl);

        if (window.bootstrap && typeof window.bootstrap.Toast === 'function') {
            const toast = new window.bootstrap.Toast(toastEl, { delay: delay, autohide: true });
            toastEl.addEventListener('hidden.bs.toast', function () {
                toastEl.remove();
            }, { once: true });
            toast.show();
            return;
        }

        toastEl.classList.add('show');
        setTimeout(function () {
            toastEl.remove();
        }, delay);
    }

    function initBuscadorProductos(config) {
        const cfg = config || {};
        const input = resolveElement(cfg.input);
        const results = resolveElement(cfg.results);
        const url = cfg.url;
        const filtros = cfg.filtros || {};

        if (!input || !results || !url) {
            return null;
        }

        const onSelect = typeof cfg.onSelect === 'function' ? cfg.onSelect : function () { };
        const onEnterWhenClosed = typeof cfg.onEnterWhenClosed === 'function' ? cfg.onEnterWhenClosed : function () { };
        const minChars = cfg.minChars ?? 2;
        const debounceMs = cfg.debounceMs ?? 250;
        const take = String(cfg.take ?? 20);
        const resultsId = results.id || `ventaSearchResults-${Math.random().toString(36).slice(2)}`;
        results.id = resultsId;
        const optionIdPrefix = `${resultsId}-option-`;

        results.setAttribute('role', 'listbox');
        input.setAttribute('aria-controls', resultsId);
        input.setAttribute('aria-expanded', 'false');
        input.setAttribute('aria-autocomplete', 'list');
        input.setAttribute('aria-activedescendant', '');

        let sugeridos = [];
        let indiceActivo = -1;
        let debounceId = null;
        let abortCtrl = null;

        function updateAriaState() {
            const abierto = !results.classList.contains('d-none');
            input.setAttribute('aria-expanded', abierto ? 'true' : 'false');

            if (abierto && indiceActivo >= 0 && sugeridos[indiceActivo]) {
                input.setAttribute('aria-activedescendant', `${optionIdPrefix}${indiceActivo}`);
            } else {
                input.setAttribute('aria-activedescendant', '');
            }
        }

        function ocultar() {
            results.classList.add('d-none');
            results.innerHTML = '';
            indiceActivo = -1;
            updateAriaState();
        }

        function getParams(term) {
            const params = new URLSearchParams({ term: term, take: take });

            if (filtros.categoria?.value) params.set('categoriaId', filtros.categoria.value);
            if (filtros.marca?.value) params.set('marcaId', filtros.marca.value);

            params.set('soloConStock', filtros.soloStock?.checked === false ? 'false' : 'true');

            if (filtros.precioMin?.value) params.set('precioMin', filtros.precioMin.value);
            if (filtros.precioMax?.value) params.set('precioMax', filtros.precioMax.value);

            return params;
        }

        function render() {
            if (!sugeridos.length) {
                                results.innerHTML = '<div class="list-group-item small text-muted" role="option" aria-disabled="true">Sin resultados</div>';
                results.classList.remove('d-none');
                                updateAriaState();
                return;
            }

            results.innerHTML = sugeridos.map(function (producto, index) {
                const marcaCategoria = [producto.marca, producto.categoria].filter(Boolean).join(' / ');
                const caracteristicas = producto.caracteristicasResumen || '';
                const precio = Number(producto.precioVenta || 0).toFixed(2);
                return `
          <button type="button"
                                    id="${optionIdPrefix}${index}"
                                    role="option"
                                    aria-selected="${index === indiceActivo ? 'true' : 'false'}"
                  class="list-group-item list-group-item-action producto-suggestion ${index === indiceActivo ? 'active' : ''}"
                  data-index="${index}">
            <div class="d-flex justify-content-between">
              <strong>${producto.codigo} - ${producto.nombre}</strong>
              <span>$${precio}</span>
            </div>
            <small class="d-block text-muted">${marcaCategoria || 'Sin marca/categoría'} · Stock: ${producto.stockActual}</small>
            ${caracteristicas ? `<small class="d-block text-info">${caracteristicas}</small>` : ''}
          </button>
        `;
            }).join('');

            results.classList.remove('d-none');
            updateAriaState();
        }

        function buscar(term) {
            if (abortCtrl) abortCtrl.abort();
            abortCtrl = new AbortController();

            const params = getParams(term);
            fetch(`${url}?${params.toString()}`, { signal: abortCtrl.signal })
                .then(function (r) { return r.ok ? r.json() : []; })
                .then(function (data) {
                    sugeridos = Array.isArray(data) ? data : [];
                    indiceActivo = -1;
                    render();
                })
                .catch(function (err) {
                    if (err && err.name === 'AbortError') return;
                    sugeridos = [];
                    ocultar();
                });
        }

        function mover(delta) {
            if (!sugeridos.length) return;
            indiceActivo += delta;
            if (indiceActivo < 0) indiceActivo = sugeridos.length - 1;
            if (indiceActivo >= sugeridos.length) indiceActivo = 0;
            render();
        }

        function seleccionarPorIndice(idx) {
            const i = parseInt(idx, 10);
            if (!Number.isFinite(i) || !sugeridos[i]) return;
            onSelect(sugeridos[i]);
            ocultar();
        }

        results.addEventListener('click', function (ev) {
            const btn = ev.target.closest('.producto-suggestion');
            if (!btn) return;
            seleccionarPorIndice(btn.dataset.index);
        });

        input.addEventListener('input', function () {
            const term = input.value.trim();

            if (debounceId) clearTimeout(debounceId);

            if (term.length < minChars) {
                ocultar();
                return;
            }

            debounceId = setTimeout(function () {
                buscar(term);
            }, debounceMs);
        });

        input.addEventListener('keydown', function (event) {
            const term = input.value.trim();
            const abierto = !results.classList.contains('d-none');

            if (!abierto) {
                if (event.key === 'Enter') {
                    event.preventDefault();
                    onEnterWhenClosed(term, sugeridos);
                }
                return;
            }

            if (event.key === 'ArrowDown') {
                event.preventDefault();
                mover(1);
                return;
            }
            if (event.key === 'ArrowUp') {
                event.preventDefault();
                mover(-1);
                return;
            }
            if (event.key === 'Enter') {
                event.preventDefault();
                if (indiceActivo >= 0 && sugeridos[indiceActivo]) {
                    onSelect(sugeridos[indiceActivo]);
                    ocultar();
                    return;
                }
                onEnterWhenClosed(term, sugeridos);
                return;
            }
            if (event.key === 'Escape') {
                ocultar();
            }
        });

        document.addEventListener('click', function (event) {
            if (event.target === input || results.contains(event.target)) return;
            ocultar();
        });

        [filtros.categoria, filtros.marca, filtros.soloStock].forEach(function (el) {
            el?.addEventListener('change', function () {
                const term = input.value.trim();
                if (!term || term.length < minChars) {
                    ocultar();
                    return;
                }
                buscar(term);
            });
        });

        [filtros.precioMin, filtros.precioMax].forEach(function (el) {
            el?.addEventListener('input', function () {
                const term = input.value.trim();
                if (!term || term.length < minChars) {
                    ocultar();
                    return;
                }
                if (debounceId) clearTimeout(debounceId);
                debounceId = setTimeout(function () { buscar(term); }, debounceMs);
            });
        });

        return { ocultar: ocultar };
    }

    window.VentaCommon = {
        createDetalleManager: createDetalleManager,
        calcularTotales: calcularTotales,
        resetTotalesUI: resetTotalesUI,
        aplicarTotalesUI: aplicarTotalesUI,
        initTarjetaHandlers: initTarjetaHandlers,
        initBuscadorProductos: initBuscadorProductos,
        showToast: showToast
    };
})();
