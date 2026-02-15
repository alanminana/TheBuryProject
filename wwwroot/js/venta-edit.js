// venta-edit.js
(function () {
  'use strict';

  const form = document.getElementById('formVenta');
  if (!form) return;

  // -----------------------------
  // Data inicial (detalles existentes)
  // -----------------------------
  const dataContainer = document.getElementById('venta-edit-data');
  let detallesIniciales = [];

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
      const raw = JSON.parse(dataContainer.dataset.detalles || '[]');
      detallesIniciales = Array.isArray(raw) ? raw.map(normalizeDetalle) : [];
    } catch {
      detallesIniciales = [];
    }
  }

  // Secuencia de keys (index) para mantener consistencia al editar
  let detalleIndexSeq =
    detallesIniciales.length > 0
      ? Math.max(...detallesIniciales.map((d) => Number(d.index || 0))) + 1
      : 1;

  // -----------------------------
  // Dataset / urls / flags
  // -----------------------------
  const buscarProductosUrl = form.dataset.buscarProductosUrl;
  const calcularTotalesUrl = form.dataset.calcularTotalesUrl;
  const descuentoEsPorcentaje =
    form.dataset.descuentoEsPorcentaje === 'true' || form.dataset.descuentoEsPorcentaje === true;

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

  const detallesBody = document.getElementById('productosBody');
  const descuentoGeneralInput = document.getElementById('descuentoGeneral') || document.getElementById('Descuento');

  const totalHidden = document.getElementById('hiddenTotal') || document.getElementById('totalHidden');
  const subtotalHidden = document.getElementById('hiddenSubtotal') || document.getElementById('subtotalHidden');
  const ivaHidden = document.getElementById('hiddenIVA') || document.getElementById('ivaHidden');

  // -----------------------------
  // Estado local
  // -----------------------------
  let productoSeleccionado = null;

  // -----------------------------
  // Detalle manager (single source of truth)
  // -----------------------------
  const detalleManager = VentaCommon.createDetalleManager({
    keyField: 'index',
    initialDetalles: detallesIniciales,
    keyFactory: function () {
      return detalleIndexSeq++;
    },
    onChange: function () {
      renderTablaDetalles();
      calcularTotales();
    }
  });

  // -----------------------------
  // Hidden inputs (post)
  // -----------------------------
  function syncDetallesHiddenInputs() {
    // Limpia inputs anteriores (si re-submit)
    form.querySelectorAll('input[name^="Detalles["]').forEach(function (i) {
      i.remove();
    });

    detalleManager.getAll().forEach(function (detalle, index) {
      const d = normalizeDetalle(detalle);
      form.insertAdjacentHTML(
        'beforeend',
        `
        <input type="hidden" name="Detalles[${index}].ProductoId" value="${d.ProductoId}" />
        <input type="hidden" name="Detalles[${index}].Cantidad" value="${d.Cantidad}" />
        <input type="hidden" name="Detalles[${index}].PrecioUnitario" value="${d.PrecioUnitario}" />
        <input type="hidden" name="Detalles[${index}].Descuento" value="${d.Descuento}" />
        <input type="hidden" name="Detalles[${index}].Subtotal" value="${d.Subtotal}" />
      `
      );
    });
  }

  // -----------------------------
  // Render tabla (sin mezclar con hidden inputs)
  // -----------------------------
  function renderTablaDetalles() {
    if (!detallesBody) return;

    const list = detalleManager.getAll().map(normalizeDetalle);

    if (list.length === 0) {
      detallesBody.replaceChildren();
      return;
    }

    detallesBody.innerHTML = list
      .map(function (d) {
        const cantidad = Number(d.Cantidad || 0);
        const precio = Number(d.PrecioUnitario || 0);
        const descuento = Number(d.Descuento || 0);
        const subtotal = Number(d.Subtotal || 0);

        return `
        <tr data-index="${d.index}">
          <td>${d.ProductoCodigo || ''}</td>
          <td>${d.ProductoNombre || ''}</td>
          <td class="text-center">${cantidad}</td>
          <td class="text-end">$${precio.toFixed(2)}</td>
          <td class="text-end">$${descuento.toFixed(2)}</td>
          <td class="text-end">$${subtotal.toFixed(2)}</td>
          <td class="text-center">
            <button type="button" class="btn btn-sm btn-danger btn-eliminar-producto" data-index="${d.index}">
              <i class="bi bi-trash"></i>
            </button>
          </td>
        </tr>
      `;
      })
      .join('');
  }

  // -----------------------------
  // Buscador (usa shared si existe, sino fallback local)
  // -----------------------------
  function initBuscadorProductosFallback(cfg) {
    const input = cfg.input;
    const results = cfg.results;
    const url = cfg.url;
    const filtros = cfg.filtros || {};
    const onSelect = typeof cfg.onSelect === 'function' ? cfg.onSelect : function () {};
    const onEnterWhenClosed = typeof cfg.onEnterWhenClosed === 'function' ? cfg.onEnterWhenClosed : function () {};

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

      results.innerHTML = sugeridos
        .map(function (producto, index) {
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
        })
        .join('');

      results.classList.remove('d-none');
    }

    function buscar(term) {
      if (abortCtrl) abortCtrl.abort();
      abortCtrl = new AbortController();

      const params = getParams(term);
      fetch(`${url}?${params.toString()}`, { signal: abortCtrl.signal })
        .then(function (r) {
          return r.ok ? r.json() : [];
        })
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
        debounceId = setTimeout(function () {
          buscar(term);
        }, debounceMs);
      });
    });

    return { ocultar: ocultar };
  }

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
        productoSeleccionado = producto;

        if (precioInput) {
          precioInput.value = Number(producto.precioVenta || 0).toFixed(2);
        }

        productoSearchInput.value = `${producto.codigo} - ${producto.nombre}`;
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
          if (precioInput) precioInput.value = Number(exacto.precioVenta || 0).toFixed(2);
          if (productoSearchInput) productoSearchInput.value = `${exacto.codigo} - ${exacto.nombre}`;
          cantidadInput?.focus();
        }
      }
    });
  }

  // -----------------------------
  // Acciones (agregar/eliminar)
  // -----------------------------
  function resetProductoInputs() {
    if (productoSearchInput) {
      productoSearchInput.value = '';
      productoSearchInput.focus();
    }
    productoSeleccionado = null;

    if (cantidadInput) cantidadInput.value = 1;
    if (precioInput) precioInput.value = '';
    if (descuentoInput) descuentoInput.value = 0;

    // Por si el fallback dejó visible
    if (productoSearchResults) {
      productoSearchResults.classList.add('d-none');
      productoSearchResults.innerHTML = '';
    }
  }

  function agregarProducto() {
    const productoId = productoSeleccionado?.id;
    const cantidad = parseFloat(cantidadInput?.value || '');
    const precio = parseFloat(precioInput?.value || '');
    const descuento = parseFloat(descuentoInput?.value || '0') || 0;

    if (!productoId || !productoSeleccionado || !Number.isFinite(cantidad) || cantidad <= 0 || !Number.isFinite(precio) || precio <= 0) {
      alert('Complete todos los campos correctamente.');
      return;
    }

    const subtotal = cantidad * precio - descuento;

    // Mantiene compatibilidad con naming del edit (PascalCase)
    detalleManager.add({
      ProductoId: productoId,
      ProductoCodigo: productoSeleccionado.codigo,
      ProductoNombre: productoSeleccionado.nombre,
      Cantidad: cantidad,
      PrecioUnitario: precio,
      Descuento: descuento,
      Subtotal: subtotal
    });

    resetProductoInputs();
  }

  function eliminarDetalle(index) {
    detalleManager.removeByKey(index);
  }

  // -----------------------------
  // Totales
  // -----------------------------
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
      .then(function (data) {
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
      .catch(function () {
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

  // -----------------------------
  // Eventos
  // -----------------------------
  function bindEventos() {
    document.getElementById('btnAgregarProducto')?.addEventListener('click', agregarProducto);

    detallesBody?.addEventListener('click', function (event) {
      const btn = event.target.closest('.btn-eliminar-producto');
      if (!btn) return;
      const index = parseInt(btn.dataset.index, 10);
      if (Number.isFinite(index)) eliminarDetalle(index);
    });

    // Mejor UX: recalcular mientras se escribe
    descuentoGeneralInput?.addEventListener('input', calcularTotales);

    form.addEventListener('submit', function () {
      syncDetallesHiddenInputs();
    });
  }

  // -----------------------------
  // Init
  // -----------------------------
  function init() {
    renderTablaDetalles();
    calcularTotales();
    bindEventos();
    initBuscadorProductos();
  }

  init();

  /* ============================================================================
  MEJORAS APLICADAS (venta-edit.js)
  - Single source of truth:
    - La tabla se renderiza desde detalleManager (no se mezcla estado DOM + estado JS). Esto evita inconsistencias.
  - Eliminación de duplicación interna:
    - Se eliminó el patrón de “agregarFilaDetalle” + “inicializarFilasExistentes” y se reemplazó por render completo.
  - Hidden inputs:
    - Se generan solo en submit (más robusto y consistente con Edit original).
  - Buscador:
    - Usa VentaCommon.initBuscadorProductos si existe; si no, incluye fallback local con debounce + AbortController
      para evitar race conditions al tipear rápido. (🟢 Estable: AbortController)
      Ref: https://developer.mozilla.org/en-US/docs/Web/API/AbortController
  - UX:
    - Descuento general recalcula en input.
    - Al seleccionar producto, foco a cantidad.
  NOTA FUNCIONAL:
  - En Edit se mantiene el comportamiento original: el campo descuentoInput se interpreta como “monto” por ítem
    (subtotal = cantidad*precio - descuento). En Create se estaba usando porcentaje.
    Si querés unificar (monto vs %), se debe definir contrato único con backend y UI.
  ============================================================================ */
})();
