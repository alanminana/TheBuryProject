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

  function notify(message, level, title) {
    if (window.VentaCommon && typeof window.VentaCommon.showToast === 'function') {
      window.VentaCommon.showToast(message, { level: level || 'warning', title: title || 'Atención' });
      return;
    }
    alert(message);
  }
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
  // Buscador (centralizado en VentaCommon)
  // -----------------------------

  function initBuscadorProductos() {
    if (!productoSearchInput || !productoSearchResults) return;

    if (typeof VentaCommon.initBuscadorProductos !== 'function') return;

    VentaCommon.initBuscadorProductos({
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

    // Limpia estado visual del listado de sugerencias
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
      notify('Complete todos los campos correctamente.', 'warning');
      return;
    }

    const subtotal = Math.max(0, (cantidad * precio) - descuento);

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
    - Usa implementación centralizada en VentaCommon (debounce + AbortController + navegación teclado).
  - UX:
    - Descuento general recalcula en input.
    - Al seleccionar producto, foco a cantidad.
  NOTA FUNCIONAL:
  - El campo descuentoInput se interpreta como “monto” por ítem en Create y Edit.
    (subtotal = max(0, cantidad*precio - descuento)).
  ============================================================================ */
})();
