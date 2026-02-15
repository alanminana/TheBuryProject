// venta-create.js
(function () {
  'use strict';

  const form = document.getElementById('formVenta');
  if (!form) return;

  // -----------------------------
  // Helpers locales (sin dependencias)
  // -----------------------------
  function setTodayOnDateInput(inputEl) {
    if (!inputEl) return;

    // 🟢 Estable: valueAsDate
    if ('valueAsDate' in inputEl) {
      inputEl.valueAsDate = new Date();
      return;
    }

    // Fallback local yyyy-mm-dd
    const d = new Date();
    const yyyy = d.getFullYear();
    const mm = String(d.getMonth() + 1).padStart(2, '0');
    const dd = String(d.getDate()).padStart(2, '0');
    inputEl.value = `${yyyy}-${mm}-${dd}`;
  }

  function formatCurrencyInline(value) {
    return '$' + Number(value || 0).toLocaleString('es-AR', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2
    });
  }

  function sanitizeIconClass(value) {
    // evita caracteres raros; conserva solo letras/números/espacios/guiones
    const raw = String(value || '').trim();
    const safe = raw.replace(/[^a-z0-9\- ]/gi, '');
    // si no parece bootstrap-icon, fallback
    return safe.includes('bi') ? safe : 'bi bi-question-circle';
  }

  function normalizeTipoPagoForTarjetaHandlers(value) {
    // Soporta ambos escenarios:
    //  - valores string ("TarjetaDebito", "TarjetaCredito", "Cheque")
    //  - valores numéricos típicos (2 débito, 3 crédito, 4 cheque)
    const v = String(value ?? '');
    if (v === '2') return 'TarjetaDebito';
    if (v === '3') return 'TarjetaCredito';
    if (v === '4') return 'Cheque';
    return v;
  }

  function syncDetallesHiddenInputs() {
    // Genera inputs hidden al final del form (evita HTML inválido dentro de <tr>)
    form.querySelectorAll('input[name^="Detalles["]').forEach(function (i) { i.remove(); });

    detalleManager.getAll().forEach(function (prod, index) {
      const productoId = prod.productoId ?? prod.ProductoId;
      const cantidad = prod.cantidad ?? prod.Cantidad;
      const precioUnitario = prod.precioUnitario ?? prod.PrecioUnitario;
      const descuento = prod.descuento ?? prod.Descuento;
      const subtotal = prod.subtotal ?? prod.Subtotal;

      form.insertAdjacentHTML('beforeend', `
        <input type="hidden" name="Detalles[${index}].ProductoId" value="${productoId}" />
        <input type="hidden" name="Detalles[${index}].Cantidad" value="${cantidad}" />
        <input type="hidden" name="Detalles[${index}].PrecioUnitario" value="${precioUnitario}" />
        <input type="hidden" name="Detalles[${index}].Descuento" value="${descuento}" />
        <input type="hidden" name="Detalles[${index}].Subtotal" value="${subtotal}" />
      `);
    });
  }

  // Fallback local si todavía no existe VentaCommon.initBuscadorProductos
  function initBuscadorProductosFallback(cfg) {
    const input = cfg.input;
    const results = cfg.results;
    const url = cfg.url;
    const filtros = cfg.filtros || {};
    const onSelect = typeof cfg.onSelect === 'function' ? cfg.onSelect : function () { };
    const onEnterWhenClosed = typeof cfg.onEnterWhenClosed === 'function' ? cfg.onEnterWhenClosed : function () { };

    if (!input || !results || !url) return null;

    const minChars = cfg.minChars ?? 2;
    const debounceMs = cfg.debounceMs ?? 250;
    const take = String(cfg.take ?? 20);

    let sugeridos = [];
    let indiceActivo = -1;
    let debounceId = null;
    let abortCtrl = null;

    function ocultar() {
      results.classList.add('d-none');
      results.innerHTML = '';
      indiceActivo = -1;
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
        results.innerHTML = '<div class="list-group-item small text-muted">Sin resultados</div>';
        results.classList.remove('d-none');
        return;
      }

      results.innerHTML = sugeridos.map(function (producto, index) {
        const marcaCategoria = [producto.marca, producto.categoria].filter(Boolean).join(' / ');
        const caracteristicas = producto.caracteristicasResumen || '';
        const precio = Number(producto.precioVenta || 0).toFixed(2);
        return `
          <button type="button"
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

    // Delegación de eventos para click en sugerencias
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
        // si no hay item activo, intentar exacto
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

    // Re-búsqueda por filtros
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

  // -----------------------------
  // Estado + Managers
  // -----------------------------
  const stockDisponible = Object.create(null);

  let detalleKeySeq = 0;
  const detalleManager = VentaCommon.createDetalleManager({
    keyField: 'key',
    keyFactory: function () { detalleKeySeq += 1; return detalleKeySeq; },
    onChange: function () {
      actualizarTablaProductos();
      calcularTotales();

      // Resetear prevalidación cuando cambian los productos
      if (tipoPagoSelect?.value === TIPO_PAGO_CREDITO_PERSONAL) {
        resetEstadoPrevalidacion();
        triggerPrevalidacionAutomatica();
      }
    }
  });

  // -----------------------------
  // Dataset / urls / flags
  // -----------------------------
  const getTarjetasUrl = form.dataset.getTarjetasUrl;
  const calcularCuotasUrl = form.dataset.calcularCuotasUrl;
  const buscarProductosUrl = form.dataset.buscarProductosUrl;
  const calcularTotalesUrl = form.dataset.calcularTotalesUrl;
  const prevalidarCreditoUrl = form.dataset.prevalidarCreditoUrl;

  const puedeExcepcionDocumental = form.dataset.puedeExcepcionDocumental === 'true';
  const descuentoEsPorcentaje = form.dataset.descuentoEsPorcentaje === 'true';

  const antiforgeryToken = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

  // -----------------------------
  // Elementos UI
  // -----------------------------
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

  // Prevalidación crédito
  const prevalidacionRow = document.getElementById('prevalidacionRow');
  const btnVerificarAptitud = document.getElementById('btnVerificarAptitud');
  const prevalidacionPendiente = document.getElementById('prevalidacionPendiente');
  const prevalidacionCargando = document.getElementById('prevalidacionCargando');
  const prevalidacionResultado = document.getElementById('prevalidacionResultado');
  const prevalidacionBadge = document.getElementById('prevalidacionBadge');
  const prevalidacionTexto = document.getElementById('prevalidacionTexto');
  const resumenCreditoDisponibleInline = document.getElementById('resumenCreditoDisponibleInline');
  const inlineCreditoDisponible = document.getElementById('inlineCreditoDisponible');
  const inlineCreditoVenta = document.getElementById('inlineCreditoVenta');
  const inlineCreditoExcedeMensaje = document.getElementById('inlineCreditoExcedeMensaje');
  const inlineCreditoExcedeDisponible = document.getElementById('inlineCreditoExcedeDisponible');

  const btnAplicarExcepcionDocumental = document.getElementById('btnAplicarExcepcionDocumental');
  const estadoExcepcionDocumental = document.getElementById('estadoExcepcionDocumental');
  const motivoExcepcionDocumentalInput = document.getElementById('motivoExcepcionDocumentalInput');
  const aplicarExcepcionDocumentalHidden = document.getElementById('aplicarExcepcionDocumental');
  const motivoExcepcionDocumentalHidden = document.getElementById('motivoExcepcionDocumentalHidden');

  // -----------------------------
  // Estado local
  // -----------------------------
  let productoSeleccionado = null;

  let estadoPrevalidacion = {
    verificado: false,
    permiteGuardar: false,
    resultado: null,
    disponible: 0,
    montoVenta: 0
  };

  let debouncePrevalidacionId = null;
  let excepcionDocumentalActiva = false;

  // -----------------------------
  // Tarjetas/Cheques (reusa VentaCommon, pero controlamos tipoPago con normalización)
  // -----------------------------
  const tarjetaHandlers = VentaCommon.initTarjetaHandlers({
    // Importante: NO pasamos tipoPagoSelect para evitar que el handler se enganche a valores no normalizados
    tipoPagoSelect: null,
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

  // -----------------------------
  // Init
  // -----------------------------
  function init() {
    tarjetaHandlers.bindEvents();

    // Estado inicial: tarjetas/cheque
    applyTipoPagoToTarjetaHandlers(tipoPagoSelect?.value);

    bindEvents();
    initBuscadorProductos();
    bindPrevalidacionEvents();
    handleTipoPagoChangeForPrevalidacion(tipoPagoSelect?.value);

    // Estado inicial prevalidación
    if (tipoPagoSelect?.value === TIPO_PAGO_CREDITO_PERSONAL) {
      triggerPrevalidacionAutomatica();
    }
  }

  function applyTipoPagoToTarjetaHandlers(tipoPagoValue) {
    const normalizado = normalizeTipoPagoForTarjetaHandlers(tipoPagoValue);
    tarjetaHandlers.handleTipoPagoChange(normalizado);

    // Corrige fecha del cheque en zona local si aplica
    if (normalizado === 'Cheque') {
      const chequeFecha = document.getElementById('chequeFechaEmision');
      setTodayOnDateInput(chequeFecha);
    }
  }

  // -----------------------------
  // Eventos generales
  // -----------------------------
  function bindEvents() {
    document.getElementById('btnAgregarProducto')?.addEventListener('click', agregarProducto);

    // Mejor UX: recalcular mientras tipea
    descuentoGeneralInput?.addEventListener('input', calcularTotales);

    productosBody?.addEventListener('click', function (event) {
      const btn = event.target.closest('.btn-eliminar-producto');
      if (!btn) return;
      const key = btn.dataset.key;
      if (key !== undefined) eliminarProducto(key);
    });

    // Tipo de pago: tarjetas/cheque + prevalidación
    tipoPagoSelect?.addEventListener('change', function () {
      applyTipoPagoToTarjetaHandlers(this.value);
      handleTipoPagoChangeForPrevalidacion(this.value);
    });

    // Cliente: reset prevalidación
    clienteSelect?.addEventListener('change', function () {
      resetEstadoPrevalidacion();
      triggerPrevalidacionAutomatica();
    });

    // Submit: validaciones + sync hidden inputs
    form.addEventListener('submit', function (e) {
      if (detalleManager.getAll().length === 0) {
        e.preventDefault();
        alert('Debe agregar al menos un producto a la venta');
        return;
      }

      // Validar prevalidación para Crédito Personal
      if (tipoPagoSelect?.value === TIPO_PAGO_CREDITO_PERSONAL) {
        const excedeDisponible =
          estadoPrevalidacion.verificado && estadoPrevalidacion.montoVenta > estadoPrevalidacion.disponible;

        const excepcionValida =
          puedeExcepcionDocumental &&
          excepcionDocumentalActiva &&
          esNoViableSoloDocumentacion() &&
          ((motivoExcepcionDocumentalInput?.value || '').trim().length > 0) &&
          !excedeDisponible;

        if (!estadoPrevalidacion.verificado) {
          e.preventDefault();
          alert('Debe verificar la aptitud crediticia del cliente antes de continuar.');
          return;
        }

        if (estadoPrevalidacion.montoVenta > estadoPrevalidacion.disponible) {
          e.preventDefault();
          alert(
            `Excede el crédito disponible por puntaje. Disponible: ${formatCurrencyInline(estadoPrevalidacion.disponible)}. ` +
            `Ajuste el monto, cambie método de pago o actualice puntaje/límites.`
          );
          return;
        }

        if (!estadoPrevalidacion.permiteGuardar) {
          if (excepcionValida) {
            sincronizarExcepcionDocumentalHidden();
            // Continúa y guarda
          } else {
            e.preventDefault();
            alert('El cliente no tiene aptitud crediticia para esta operación. Revise los motivos en el panel de verificación.');
            return;
          }
        }
      }

      // Siempre generar hidden inputs justo antes de enviar
      syncDetallesHiddenInputs();
    });
  }

  // -----------------------------
  // Buscador de productos (shared si existe, fallback local si no)
  // -----------------------------
  function initBuscadorProductos() {
    if (!productoSearchInput || !productoSearchResults) return;

    const initFn =
      typeof VentaCommon.initBuscadorProductos === 'function'
        ? VentaCommon.initBuscadorProductos
        : initBuscadorProductosFallback;

    initFn({
      input: productoSearchInput,
      results: productoSearchResults,
      url: buscarProductosUrl,
      filtros: {
        categoria: productoCategoriaFiltro,
        marca: productoMarcaFiltro,
        soloStock: productoSoloStockFiltro,
        precioMin: productoPrecioMinFiltro,
        precioMax: productoPrecioMaxFiltro
      },
      onSelect: function (producto) {
        if (!precioInput) return;

        productoSeleccionado = producto;
        stockDisponible[producto.id] = Number(producto.stockActual || 0);

        precioInput.value = Number(producto.precioVenta || 0).toFixed(2);
        productoSearchInput.value = `${producto.codigo} - ${producto.nombre}`;

        // Sugerencia UX: poner foco en cantidad para acelerar carga
        cantidadInput?.focus();
      },
      onEnterWhenClosed: function (term, sugeridos) {
        const t = String(term || '').trim();
        if (!t || !Array.isArray(sugeridos) || sugeridos.length === 0) return;

        const exacto = sugeridos.find(function (p) {
          return String(p.codigo || '').toLowerCase() === t.toLowerCase() || p.codigoExacto;
        });

        if (exacto) {
          productoSeleccionado = exacto;
          stockDisponible[exacto.id] = Number(exacto.stockActual || 0);
          if (precioInput) precioInput.value = Number(exacto.precioVenta || 0).toFixed(2);
          if (productoSearchInput) productoSearchInput.value = `${exacto.codigo} - ${exacto.nombre}`;
          agregarProducto(); // Create: Enter exacto agrega
        }
      }
    });
  }

  // -----------------------------
  // Prevalidación crédito personal
  // -----------------------------
  function bindPrevalidacionEvents() {
    btnVerificarAptitud?.addEventListener('click', function () {
      verificarAptitudCrediticia({ silencioso: false });
    });

    btnAplicarExcepcionDocumental?.addEventListener('click', toggleExcepcionDocumental);

    motivoExcepcionDocumentalInput?.addEventListener('input', function () {
      sincronizarExcepcionDocumentalHidden();
      actualizarBotonGuardar();
    });
  }

  function esCategoriaDocumentacion(categoria) {
    return categoria === 1 || categoria === 'Documentacion' || categoria === 'Documentación';
  }

  function esNoViableSoloDocumentacion() {
    if (!estadoPrevalidacion?.verificado || !estadoPrevalidacion?.resultado || !Array.isArray(estadoPrevalidacion.resultado.motivos)) {
      return false;
    }

    const bloqueantes = estadoPrevalidacion.resultado.motivos.filter(function (motivo) {
      return motivo && motivo.esBloqueante;
    });

    return bloqueantes.length > 0 && bloqueantes.every(function (motivo) {
      return esCategoriaDocumentacion(motivo.categoria);
    });
  }

  function sincronizarExcepcionDocumentalHidden() {
    if (aplicarExcepcionDocumentalHidden) {
      aplicarExcepcionDocumentalHidden.value = excepcionDocumentalActiva ? 'true' : 'false';
    }

    if (motivoExcepcionDocumentalHidden) {
      const motivo = (motivoExcepcionDocumentalInput?.value || '').trim();
      motivoExcepcionDocumentalHidden.value = excepcionDocumentalActiva ? motivo : '';
    }
  }

  function desactivarExcepcionDocumental() {
    excepcionDocumentalActiva = false;

    if (btnAplicarExcepcionDocumental) {
      btnAplicarExcepcionDocumental.classList.remove('btn-outline-warning');
      btnAplicarExcepcionDocumental.classList.add('btn-warning');
      btnAplicarExcepcionDocumental.innerHTML = '<i class="bi bi-shield-exclamation me-1"></i> Aplicar Excepción Documental';
    }

    if (estadoExcepcionDocumental) {
      estadoExcepcionDocumental.classList.remove('bg-success');
      estadoExcepcionDocumental.classList.add('bg-secondary');
      estadoExcepcionDocumental.textContent = 'No aplicada';
    }

    sincronizarExcepcionDocumentalHidden();
  }

  function activarExcepcionDocumental() {
    excepcionDocumentalActiva = true;

    if (btnAplicarExcepcionDocumental) {
      btnAplicarExcepcionDocumental.classList.remove('btn-warning');
      btnAplicarExcepcionDocumental.classList.add('btn-outline-warning');
      btnAplicarExcepcionDocumental.innerHTML = '<i class="bi bi-shield-check me-1"></i> Quitar Excepción Documental';
    }

    if (estadoExcepcionDocumental) {
      estadoExcepcionDocumental.classList.remove('bg-secondary');
      estadoExcepcionDocumental.classList.add('bg-success');
      estadoExcepcionDocumental.textContent = 'Aplicada';
    }

    sincronizarExcepcionDocumentalHidden();
  }

  function toggleExcepcionDocumental() {
    if (!puedeExcepcionDocumental) {
      alert('No tiene permisos para aplicar excepción documental.');
      return;
    }

    if (!estadoPrevalidacion.verificado) {
      alert('Primero debe verificar la aptitud crediticia.');
      return;
    }

    if (excepcionDocumentalActiva) {
      desactivarExcepcionDocumental();
      actualizarBotonGuardar();
      return;
    }

    if (!esNoViableSoloDocumentacion()) {
      alert('La excepción documental solo aplica cuando el bloqueo es exclusivamente por documentación faltante.');
      return;
    }

    const excedeDisponible = estadoPrevalidacion.montoVenta > estadoPrevalidacion.disponible;
    if (excedeDisponible) {
      alert('No se puede aplicar excepción documental si el monto excede el crédito disponible por puntaje.');
      return;
    }

    const motivo = (motivoExcepcionDocumentalInput?.value || '').trim();
    if (!motivo) {
      alert('Debe ingresar el motivo de excepción documental.');
      return;
    }

    activarExcepcionDocumental();
    actualizarBotonGuardar();
  }

  function handleTipoPagoChangeForPrevalidacion(tipoPago) {
    const avisoCreditoPersonal = document.getElementById('avisoCreditoPersonal');

    if (tipoPago === TIPO_PAGO_CREDITO_PERSONAL) {
      prevalidacionRow?.classList.remove('d-none');
      avisoCreditoPersonal?.classList.remove('d-none');
      resumenCreditoDisponibleInline?.classList.remove('d-none');
      actualizarBotonVerificar();
      actualizarBotonGuardar();
      triggerPrevalidacionAutomatica();
    } else {
      prevalidacionRow?.classList.add('d-none');
      avisoCreditoPersonal?.classList.add('d-none');
      resumenCreditoDisponibleInline?.classList.add('d-none');
      resetEstadoPrevalidacion();
      actualizarBotonGuardar();
    }
  }

  function actualizarBotonVerificar() {
    const tieneCliente = clienteSelect?.value && parseInt(clienteSelect.value, 10) > 0;
    const tieneProductos = detalleManager.getAll().length > 0;
    const totalVenta = parseFloat(document.getElementById('hiddenTotal')?.value) || 0;

    if (btnVerificarAptitud) {
      btnVerificarAptitud.disabled = !tieneCliente || !tieneProductos || totalVenta <= 0;
    }
  }

  function resetEstadoPrevalidacion() {
    const totalVenta = parseFloat(document.getElementById('hiddenTotal')?.value) || 0;
    estadoPrevalidacion = {
      verificado: false,
      permiteGuardar: false,
      resultado: null,
      disponible: 0,
      montoVenta: totalVenta
    };

    if (prevalidacionPendiente) prevalidacionPendiente.classList.remove('d-none');
    if (prevalidacionCargando) prevalidacionCargando.classList.add('d-none');
    if (prevalidacionResultado) prevalidacionResultado.classList.add('d-none');

    desactivarExcepcionDocumental();
    actualizarBotonVerificar();
    actualizarInlineCreditoDisponible();
    actualizarBotonGuardar();
  }

  function triggerPrevalidacionAutomatica() {
    if (tipoPagoSelect?.value !== TIPO_PAGO_CREDITO_PERSONAL) return;

    if (debouncePrevalidacionId) clearTimeout(debouncePrevalidacionId);

    debouncePrevalidacionId = setTimeout(function () {
      verificarAptitudCrediticia({ silencioso: true });
    }, 250);
  }

  function actualizarInlineCreditoDisponible() {
    if (tipoPagoSelect?.value !== TIPO_PAGO_CREDITO_PERSONAL) return;

    const montoVenta = estadoPrevalidacion.montoVenta || (parseFloat(document.getElementById('hiddenTotal')?.value) || 0);
    const disponible = estadoPrevalidacion.disponible || 0;

    if (inlineCreditoDisponible) inlineCreditoDisponible.textContent = formatCurrencyInline(disponible);
    if (inlineCreditoVenta) inlineCreditoVenta.textContent = formatCurrencyInline(montoVenta);

    const excede = estadoPrevalidacion.verificado && montoVenta > disponible;

    if (inlineCreditoExcedeDisponible) {
      inlineCreditoExcedeDisponible.textContent = formatCurrencyInline(disponible);
    }

    if (inlineCreditoExcedeMensaje) {
      inlineCreditoExcedeMensaje.classList.toggle('d-none', !excede);
    }
  }

  async function verificarAptitudCrediticia(options) {
    const opts = options || {};
    const silencioso = opts.silencioso === true;

    if (!prevalidarCreditoUrl) {
      console.error('URL de prevalidación no configurada');
      return;
    }

    const clienteId = clienteSelect?.value;
    const monto = parseFloat(document.getElementById('hiddenTotal')?.value) || 0;
    estadoPrevalidacion.montoVenta = monto;

    if (!clienteId || monto <= 0) {
      if (!silencioso) {
        alert('Seleccione un cliente y agregue productos antes de verificar.');
      }
      actualizarInlineCreditoDisponible();
      return;
    }

    if (prevalidacionPendiente) prevalidacionPendiente.classList.add('d-none');
    if (prevalidacionCargando) prevalidacionCargando.classList.remove('d-none');
    if (prevalidacionResultado) prevalidacionResultado.classList.add('d-none');

    try {
      const params = new URLSearchParams({ clienteId: String(clienteId), monto: String(monto) });
      const response = await fetch(`${prevalidarCreditoUrl}?${params.toString()}`);

      if (!response.ok) {
        const errorData = await response.json().catch(function () { return {}; });
        throw new Error(errorData.error || 'Error al verificar aptitud');
      }

      const resultado = await response.json();
      mostrarResultadoPrevalidacion(resultado);
    } catch (error) {
      console.error('Error en prevalidación:', error);
      if (prevalidacionCargando) prevalidacionCargando.classList.add('d-none');
      if (prevalidacionPendiente) prevalidacionPendiente.classList.remove('d-none');
      if (!silencioso) {
        alert('Error al verificar aptitud crediticia: ' + (error?.message || 'Error'));
      }
    }
  }

  function mostrarResultadoPrevalidacion(resultado) {
    estadoPrevalidacion = {
      verificado: true,
      permiteGuardar: !!resultado?.permiteGuardar,
      resultado: resultado,
      disponible: Number(resultado?.cupoDisponible || 0),
      montoVenta: parseFloat(document.getElementById('hiddenTotal')?.value) || 0
    };

    if (prevalidacionCargando) prevalidacionCargando.classList.add('d-none');
    if (prevalidacionResultado) prevalidacionResultado.classList.remove('d-none');

    // Badge
    if (prevalidacionBadge) {
      const badgeColor =
        resultado?.resultado === 0 ? 'bg-success' :
          resultado?.resultado === 1 ? 'bg-warning text-dark' :
            'bg-danger';

      prevalidacionBadge.className = `badge fs-6 me-3 ${badgeColor}`;

      // Render seguro: icono (class) + texto (textContent)
      prevalidacionBadge.replaceChildren();
      const icon = document.createElement('i');
      icon.className = sanitizeIconClass(resultado?.icono || 'bi bi-question-circle') + ' me-1';
      prevalidacionBadge.appendChild(icon);

      const txt = document.createTextNode(String(resultado?.textoEstado || 'Desconocido'));
      prevalidacionBadge.appendChild(txt);
    }

    if (prevalidacionTexto) {
      if (resultado?.resultado === 0) {
        prevalidacionTexto.textContent = 'El cliente tiene aptitud para esta operación de crédito.';
      } else if (resultado?.resultado === 1) {
        prevalidacionTexto.textContent = 'Esta operación requerirá autorización de un supervisor.';
      } else {
        prevalidacionTexto.textContent = 'No es posible realizar esta operación de crédito.';
      }
    }

    // Detalles financieros
    const limiteEl = document.getElementById('prevalidacionLimite');
    const cupoEl = document.getElementById('prevalidacionCupo');
    const montoEl = document.getElementById('prevalidacionMonto');

    if (limiteEl) limiteEl.textContent = formatCurrencyInline(resultado?.limiteCredito || 0);
    if (cupoEl) cupoEl.textContent = formatCurrencyInline(resultado?.cupoDisponible || 0);
    if (montoEl) montoEl.textContent = formatCurrencyInline(parseFloat(document.getElementById('hiddenTotal')?.value) || 0);

    actualizarInlineCreditoDisponible();

    // Mora
    const moraDiv = document.getElementById('prevalidacionMora');
    const moraTexto = document.getElementById('prevalidacionMoraTexto');
    if (resultado?.tieneMora && moraDiv && moraTexto) {
      moraDiv.classList.remove('d-none');
      moraTexto.textContent = `El cliente tiene mora activa de ${resultado?.diasMora || 0} días.`;
    } else if (moraDiv) {
      moraDiv.classList.add('d-none');
    }

    // Motivos (render seguro, sin innerHTML con data)
    const motivosDiv = document.getElementById('prevalidacionMotivos');
    if (motivosDiv) {
      motivosDiv.replaceChildren();

      const motivos = Array.isArray(resultado?.motivos) ? resultado.motivos : [];
      if (motivos.length > 0) {
        const wrapper = document.createElement('div');
        wrapper.className = 'mt-2';

        motivos.forEach(function (motivo) {
          const esBloqueante = !!motivo?.esBloqueante;

          const alert = document.createElement('div');
          alert.className = `alert ${esBloqueante ? 'alert-danger' : 'alert-warning'} py-2 mb-2 d-flex align-items-start`;

          const icon = document.createElement('i');
          icon.className = `bi ${esBloqueante ? 'bi-x-circle' : 'bi-exclamation-triangle'} me-2 mt-1`;
          alert.appendChild(icon);

          const content = document.createElement('div');

          const strong = document.createElement('strong');
          strong.textContent = String(motivo?.titulo || (esBloqueante ? 'Bloqueante' : 'Advertencia'));
          content.appendChild(strong);

          const p = document.createElement('p');
          p.className = 'mb-0 small';
          p.textContent = String(motivo?.descripcion || '');
          content.appendChild(p);

          if (motivo?.accionSugerida) {
            const small = document.createElement('small');
            small.className = 'text-muted';

            const bulb = document.createElement('i');
            bulb.className = 'bi bi-lightbulb me-1';
            small.appendChild(bulb);

            small.appendChild(document.createTextNode(String(motivo.accionSugerida)));
            content.appendChild(small);
          }

          alert.appendChild(content);
          wrapper.appendChild(alert);
        });

        motivosDiv.appendChild(wrapper);
      }
    }

    if (excepcionDocumentalActiva && !esNoViableSoloDocumentacion()) {
      desactivarExcepcionDocumental();
    } else {
      sincronizarExcepcionDocumentalHidden();
    }

    actualizarBotonGuardar();
  }

  function actualizarBotonGuardar() {
    const btnGuardar = form?.querySelector('button[type="submit"]');
    if (!btnGuardar) return;

    const esCreditoPersonal = tipoPagoSelect?.value === TIPO_PAGO_CREDITO_PERSONAL;

    if (esCreditoPersonal) {
      const excedeDisponible =
        estadoPrevalidacion.verificado && estadoPrevalidacion.montoVenta > estadoPrevalidacion.disponible;

      const excepcionValida =
        puedeExcepcionDocumental &&
        excepcionDocumentalActiva &&
        esNoViableSoloDocumentacion() &&
        ((motivoExcepcionDocumentalInput?.value || '').trim().length > 0) &&
        !excedeDisponible;

      if (!estadoPrevalidacion.verificado) {
        btnGuardar.disabled = true;
        btnGuardar.classList.remove('btn-primary', 'btn-success', 'btn-danger');
        btnGuardar.classList.add('btn-secondary');
        btnGuardar.title = 'Debe verificar la aptitud crediticia antes de guardar';
      } else if (excedeDisponible) {
        btnGuardar.disabled = true;
        btnGuardar.classList.remove('btn-primary', 'btn-success', 'btn-secondary');
        btnGuardar.classList.add('btn-danger');
        btnGuardar.title = `Excede el crédito disponible por puntaje. Disponible: ${formatCurrencyInline(estadoPrevalidacion.disponible)}.`;
      } else if (!estadoPrevalidacion.permiteGuardar) {
        if (excepcionValida) {
          btnGuardar.disabled = false;
          btnGuardar.classList.remove('btn-secondary', 'btn-danger');
          btnGuardar.classList.add('btn-primary');
          btnGuardar.title = 'Excepción documental activa: se permitirá guardar';
        } else {
          btnGuardar.disabled = true;
          btnGuardar.classList.remove('btn-primary', 'btn-success', 'btn-secondary');
          btnGuardar.classList.add('btn-danger');
          btnGuardar.title = 'El cliente no tiene aptitud crediticia para esta operación';
        }
      } else {
        btnGuardar.disabled = false;
        btnGuardar.classList.remove('btn-secondary', 'btn-danger');
        btnGuardar.classList.add('btn-primary');
        btnGuardar.title = '';
      }
    } else {
      btnGuardar.disabled = false;
      btnGuardar.classList.remove('btn-secondary', 'btn-danger');
      btnGuardar.classList.add('btn-primary');
      btnGuardar.title = '';
    }
  }

  // -----------------------------
  // Productos
  // -----------------------------
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

    // Nota: en Create el descuento se interpreta como porcentaje
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

  function eliminarProducto(key) {
    detalleManager.removeByKey(key);
  }

  function actualizarTablaProductos() {
    if (!productosBody) return;

    const list = detalleManager.getAll();

    // Render solo tabla (sin hidden inputs dentro de <tr>)
    productosBody.innerHTML = list.map(function (prod) {
      const key = prod.key;
      const cantidad = Number(prod.cantidad || 0);
      const precioUnitario = Number(prod.precioUnitario || 0);
      const descuento = Number(prod.descuento || 0);
      const subtotal = Number(prod.subtotal || 0);

      return `
        <tr>
          <td>${prod.codigo}</td>
          <td>${prod.nombre}</td>
          <td class="text-center">${cantidad}</td>
          <td class="text-end">$${precioUnitario.toFixed(2)}</td>
          <td class="text-end">$${descuento.toFixed(2)}</td>
          <td class="text-end">$${subtotal.toFixed(2)}</td>
          <td class="text-center">
            <button type="button" class="btn btn-sm btn-danger btn-eliminar-producto" data-key="${key}">
              <i class="bi bi-trash"></i>
            </button>
          </td>
        </tr>
      `;
    }).join('');

    actualizarBotonVerificar();
    actualizarBotonGuardar();
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

        if (tipoPagoSelect?.value === TIPO_PAGO_CREDITO_PERSONAL) {
          estadoPrevalidacion.montoVenta = parseFloat(document.getElementById('hiddenTotal')?.value) || 0;
          actualizarInlineCreditoDisponible();
          triggerPrevalidacionAutomatica();
        }
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

        if (tipoPagoSelect?.value === TIPO_PAGO_CREDITO_PERSONAL) {
          estadoPrevalidacion.montoVenta = 0;
          actualizarInlineCreditoDisponible();
        }
      });
  }

  function resetProductoInputs() {
    if (productoSearchInput) {
      productoSearchInput.value = '';
      productoSearchInput.focus();
    }

    productoSeleccionado = null;

    if (cantidadInput) cantidadInput.value = 1;
    if (precioInput) precioInput.value = '';
    if (descuentoInput) descuentoInput.value = 0;

    // Ocultar sugerencias si el fallback está activo y dejó algo visible
    if (productoSearchResults) {
      productoSearchResults.classList.add('d-none');
      productoSearchResults.innerHTML = '';
    }
  }

  init();
})();
