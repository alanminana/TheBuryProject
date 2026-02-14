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
            // Resetear prevalidación cuando cambian los productos
            if (tipoPagoSelect?.value === TIPO_PAGO_CREDITO_PERSONAL) {
                resetEstadoPrevalidacion();
            }
        }
    });

    const getTarjetasUrl = form.dataset.getTarjetasUrl;
    const calcularCuotasUrl = form.dataset.calcularCuotasUrl;
    const buscarProductosUrl = form.dataset.buscarProductosUrl;
    const calcularTotalesUrl = form.dataset.calcularTotalesUrl;
    const prevalidarCreditoUrl = form.dataset.prevalidarCreditoUrl;
    const descuentoEsPorcentaje = form.dataset.descuentoEsPorcentaje === 'true';
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
    const tarjetaRow = document.getElementById('tarjetaRow');
    const chequeRow = document.getElementById('chequeRow');
    const tarjetaSelect = document.getElementById('tarjetaSelect');
    const cuotasSelect = document.getElementById('cuotasSelect');
    const cuotasDiv = document.getElementById('cuotasDiv');
    const infoCuotas = document.getElementById('infoCuotas');
    const tipoPagoSelect = document.getElementById('tipoPagoSelect');
    const TIPO_PAGO_CREDITO_PERSONAL = '5';
    const productosBody = document.getElementById('productosBody');
    const descuentoGeneralInput = document.getElementById('descuentoGeneral');
    const clienteSelect = document.getElementById('clienteSelect');
    
    // Elementos de prevalidación de crédito
    const prevalidacionRow = document.getElementById('prevalidacionRow');
    const btnVerificarAptitud = document.getElementById('btnVerificarAptitud');
    const prevalidacionPendiente = document.getElementById('prevalidacionPendiente');
    const prevalidacionCargando = document.getElementById('prevalidacionCargando');
    const prevalidacionResultado = document.getElementById('prevalidacionResultado');
    const prevalidacionBadge = document.getElementById('prevalidacionBadge');
    const prevalidacionTexto = document.getElementById('prevalidacionTexto');

    let productosSugeridos = [];
    let indiceSugeridoActivo = -1;
    let productoSeleccionado = null;
    let debounceBusquedaId = null;
    
    // Estado de prevalidación
    let estadoPrevalidacion = {
        verificado: false,
        permiteGuardar: false,
        resultado: null
    };

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
        bindBusquedaProductosEvents();
        bindPrevalidacionEvents();
        handleTipoPagoChangeForPrevalidacion(tipoPagoSelect?.value);
    }

    function bindEvents() {
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
                return;
            }
            
            // Validar prevalidación para Crédito Personal
            if (tipoPagoSelect?.value === TIPO_PAGO_CREDITO_PERSONAL) {
                if (!estadoPrevalidacion.verificado) {
                    e.preventDefault();
                    alert('Debe verificar la aptitud crediticia del cliente antes de continuar.');
                    return;
                }
                if (!estadoPrevalidacion.permiteGuardar) {
                    e.preventDefault();
                    alert('El cliente no tiene aptitud crediticia para esta operación. Revise los motivos en el panel de verificación.');
                    return;
                }
            }
        });
        
        // Cuando cambia el tipo de pago, resetear prevalidación si es necesario
        tipoPagoSelect?.addEventListener('change', function () {
            handleTipoPagoChangeForPrevalidacion(this.value);
        });
        
        // Cuando cambia el cliente, resetear prevalidación
        clienteSelect?.addEventListener('change', function () {
            resetEstadoPrevalidacion();
        });

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
        stockDisponible[producto.id] = Number(producto.stockActual || 0);
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
            agregarProducto();
        }
    }
    
    function bindPrevalidacionEvents() {
        btnVerificarAptitud?.addEventListener('click', verificarAptitudCrediticia);
    }
    
    function handleTipoPagoChangeForPrevalidacion(tipoPago) {
        const avisoCreditoPersonal = document.getElementById('avisoCreditoPersonal');
        
        if (tipoPago === TIPO_PAGO_CREDITO_PERSONAL) {
            prevalidacionRow?.classList.remove('d-none');
            avisoCreditoPersonal?.classList.remove('d-none');
            actualizarBotonVerificar();
            actualizarBotonGuardar();
        } else {
            prevalidacionRow?.classList.add('d-none');
            avisoCreditoPersonal?.classList.add('d-none');
            resetEstadoPrevalidacion();
            actualizarBotonGuardar();
        }
    }
    
    function actualizarBotonVerificar() {
        const tieneCliente = clienteSelect?.value && parseInt(clienteSelect.value) > 0;
        const tieneProductos = detalleManager.getAll().length > 0;
        const totalVenta = parseFloat(document.getElementById('hiddenTotal')?.value) || 0;
        
        if (btnVerificarAptitud) {
            btnVerificarAptitud.disabled = !tieneCliente || !tieneProductos || totalVenta <= 0;
        }
    }
    
    function resetEstadoPrevalidacion() {
        estadoPrevalidacion = {
            verificado: false,
            permiteGuardar: false,
            resultado: null
        };
        
        if (prevalidacionPendiente) prevalidacionPendiente.classList.remove('d-none');
        if (prevalidacionCargando) prevalidacionCargando.classList.add('d-none');
        if (prevalidacionResultado) prevalidacionResultado.classList.add('d-none');
        actualizarBotonVerificar();
        actualizarBotonGuardar();
    }
    
    async function verificarAptitudCrediticia() {
        if (!prevalidarCreditoUrl) {
            console.error('URL de prevalidación no configurada');
            return;
        }
        
        const clienteId = clienteSelect?.value;
        const monto = parseFloat(document.getElementById('hiddenTotal')?.value) || 0;
        
        if (!clienteId || monto <= 0) {
            alert('Seleccione un cliente y agregue productos antes de verificar.');
            return;
        }
        
        // Mostrar estado de carga
        if (prevalidacionPendiente) prevalidacionPendiente.classList.add('d-none');
        if (prevalidacionCargando) prevalidacionCargando.classList.remove('d-none');
        if (prevalidacionResultado) prevalidacionResultado.classList.add('d-none');
        
        try {
            const params = new URLSearchParams({ clienteId: clienteId, monto: monto });
            const response = await fetch(`${prevalidarCreditoUrl}?${params.toString()}`);
            
            if (!response.ok) {
                const errorData = await response.json().catch(() => ({}));
                throw new Error(errorData.error || 'Error al verificar aptitud');
            }
            
            const resultado = await response.json();
            mostrarResultadoPrevalidacion(resultado);
            
        } catch (error) {
            console.error('Error en prevalidación:', error);
            if (prevalidacionCargando) prevalidacionCargando.classList.add('d-none');
            if (prevalidacionPendiente) prevalidacionPendiente.classList.remove('d-none');
            alert('Error al verificar aptitud crediticia: ' + error.message);
        }
    }
    
    function mostrarResultadoPrevalidacion(resultado) {
        estadoPrevalidacion = {
            verificado: true,
            permiteGuardar: resultado.permiteGuardar,
            resultado: resultado
        };
        
        if (prevalidacionCargando) prevalidacionCargando.classList.add('d-none');
        if (prevalidacionResultado) prevalidacionResultado.classList.remove('d-none');
        
        // Configurar badge con el color correcto
        if (prevalidacionBadge) {
            const badgeColor = resultado.resultado === 0 ? 'bg-success' : 
                               resultado.resultado === 1 ? 'bg-warning text-dark' : 'bg-danger';
            prevalidacionBadge.className = `badge fs-6 me-3 ${badgeColor}`;
            prevalidacionBadge.innerHTML = `<i class="${resultado.icono || 'bi bi-question-circle'} me-1"></i>${resultado.textoEstado || 'Desconocido'}`;
        }
        
        if (prevalidacionTexto) {
            if (resultado.resultado === 0) { // Aprobable
                prevalidacionTexto.textContent = 'El cliente tiene aptitud para esta operación de crédito.';
            } else if (resultado.resultado === 1) { // RequiereAutorizacion
                prevalidacionTexto.textContent = 'Esta operación requerirá autorización de un supervisor.';
            } else { // NoViable
                prevalidacionTexto.textContent = 'No es posible realizar esta operación de crédito.';
            }
        }
        
        // Mostrar detalles financieros
        const formatCurrency = (n) => '$' + (n || 0).toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
        const limiteEl = document.getElementById('prevalidacionLimite');
        const cupoEl = document.getElementById('prevalidacionCupo');
        const montoEl = document.getElementById('prevalidacionMonto');
        
        if (limiteEl) limiteEl.textContent = formatCurrency(resultado.limiteCredito);
        if (cupoEl) cupoEl.textContent = formatCurrency(resultado.cupoDisponible);
        if (montoEl) montoEl.textContent = formatCurrency(parseFloat(document.getElementById('hiddenTotal')?.value) || 0);
        
        // Mostrar mora si existe
        const moraDiv = document.getElementById('prevalidacionMora');
        if (resultado.tieneMora && moraDiv) {
            moraDiv.classList.remove('d-none');
            document.getElementById('prevalidacionMoraTexto').textContent = 
                `El cliente tiene mora activa de ${resultado.diasMora || 0} días.`;
        } else if (moraDiv) {
            moraDiv.classList.add('d-none');
        }
        
        // Mostrar motivos
        const motivosDiv = document.getElementById('prevalidacionMotivos');
        if (motivosDiv && resultado.motivos && resultado.motivos.length > 0) {
            let html = '<div class="mt-2">';
            resultado.motivos.forEach(function (motivo) {
                const alertClass = motivo.esBloqueante ? 'alert-danger' : 'alert-warning';
                const icon = motivo.esBloqueante ? 'bi-x-circle' : 'bi-exclamation-triangle';
                const titulo = motivo.titulo || (motivo.esBloqueante ? 'Bloqueante' : 'Advertencia');
                html += `
                    <div class="alert ${alertClass} py-2 mb-2 d-flex align-items-start">
                        <i class="bi ${icon} me-2 mt-1"></i>
                        <div>
                            <strong>${titulo}</strong>
                            <p class="mb-0 small">${motivo.descripcion || ''}</p>
                            ${motivo.accionSugerida ? `<small class="text-muted"><i class="bi bi-lightbulb me-1"></i>${motivo.accionSugerida}</small>` : ''}
                        </div>
                    </div>
                `;
            });
            html += '</div>';
            motivosDiv.innerHTML = html;
        } else if (motivosDiv) {
            motivosDiv.innerHTML = '';
        }
        
        // Actualizar estado del botón Guardar
        actualizarBotonGuardar();
    }
    
    function actualizarBotonGuardar() {
        const btnGuardar = form?.querySelector('button[type="submit"]');
        if (!btnGuardar) return;
        
        const esCreditoPersonal = tipoPagoSelect?.value === TIPO_PAGO_CREDITO_PERSONAL;
        
        if (esCreditoPersonal) {
            if (!estadoPrevalidacion.verificado) {
                // No verificado aún - deshabilitar con mensaje
                btnGuardar.disabled = true;
                btnGuardar.classList.remove('btn-primary', 'btn-success');
                btnGuardar.classList.add('btn-secondary');
                btnGuardar.title = 'Debe verificar la aptitud crediticia antes de guardar';
            } else if (!estadoPrevalidacion.permiteGuardar) {
                // NoViable - deshabilitar
                btnGuardar.disabled = true;
                btnGuardar.classList.remove('btn-primary', 'btn-success');
                btnGuardar.classList.add('btn-danger');
                btnGuardar.title = 'El cliente no tiene aptitud crediticia para esta operación';
            } else {
                // Aprobable o RequiereAutorizacion - habilitar
                btnGuardar.disabled = false;
                btnGuardar.classList.remove('btn-secondary', 'btn-danger');
                btnGuardar.classList.add('btn-primary');
                btnGuardar.title = '';
            }
        } else {
            // No es crédito personal - habilitar
            btnGuardar.disabled = false;
            btnGuardar.classList.remove('btn-secondary', 'btn-danger');
            btnGuardar.classList.add('btn-primary');
            btnGuardar.title = '';
        }
    }

    function agregarProducto() {
        if (!precioInput || !cantidadInput || !descuentoInput) return;

        if (!productoSeleccionado) {
            alert('Seleccione un producto');
            return;
        }

        const productoId = productoSeleccionado.id;
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

        const codigo = productoSeleccionado.codigo;
        const nombre = productoSeleccionado.nombre;

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
        if (!cantidadInput || !precioInput || !descuentoInput) return;
        if (productoSearchInput) {
            productoSearchInput.value = '';
            productoSearchInput.focus();
        }
        productoSeleccionado = null;
        productosSugeridos = [];
        ocultarSugerenciasProducto();
        cantidadInput.value = 1;
        precioInput.value = '';
        descuentoInput.value = 0;
    }

    init();
})();

