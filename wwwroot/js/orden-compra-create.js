(function () {
    const proveedorSelect = document.getElementById('ProveedorId');
    const productoSelect = document.getElementById('productoSelect');
    const cantidadInput = document.getElementById('cantidadInput');
    const precioInput = document.getElementById('precioInput');
    const agregarDetalleBtn = document.getElementById('agregarDetalleBtn');
    const descuentoInput = document.getElementById('Descuento');
    const detallesBody = document.getElementById('detallesBody');
    const subtotalDisplay = document.getElementById('subtotalDisplay');
    const descuentoDisplay = document.getElementById('descuentoDisplay');
    const ivaDisplay = document.getElementById('ivaDisplay');
    const totalDisplay = document.getElementById('totalDisplay');
    const subtotalHidden = document.getElementById('subtotalHidden');
    const ivaHidden = document.getElementById('ivaHidden');
    const totalHidden = document.getElementById('totalHidden');
    const form = document.getElementById('ordenCompraForm');
    const ivaRate = parseFloat(form?.dataset?.ivaRate || '0.21');

    const emptyRowTemplate = detallesBody ? detallesBody.innerHTML : '';

    let detalles = [];
    let detalleIndex = 0;

    async function cargarProductos(proveedorId) {
        if (!productoSelect) return;

        productoSelect.innerHTML = '<option value="">Seleccione un producto</option>';
        productoSelect.disabled = true;

        if (!proveedorId) {
            productoSelect.disabled = false;
            return;
        }

        try {
            const baseUrl = proveedorSelect?.dataset?.productosUrl;
            if (!baseUrl) {
                productoSelect.disabled = false;
                return;
            }
            const response = await fetch(`${baseUrl}/${proveedorId}`);
            if (!response.ok) {
                throw new Error('No se pudo obtener los productos');
            }

            const data = await response.json();
            if (!Array.isArray(data) || data.length === 0) {
                alert('Sin productos para este proveedor.');
                productoSelect.innerHTML = '<option>No disponibles</option>';
                productoSelect.disabled = false;
                return;
            }

            productoSelect.innerHTML = '<option value="">Seleccione un producto</option>';
            data.forEach(p => {
                const option = document.createElement('option');
                option.value = p.id;
                option.textContent = p.nombre;
                option.dataset.precio = p.precio;
                productoSelect.appendChild(option);
            });
        } catch (error) {
            alert('Error al cargar productos: ' + error.message);
        } finally {
            productoSelect.disabled = false;
        }
    }

    function actualizarPrecio() {
        const selected = productoSelect.options[productoSelect.selectedIndex];
        if (selected && selected.dataset.precio) {
            precioInput.value = parseFloat(selected.dataset.precio).toFixed(2);
        }
    }

    function agregarDetalle() {
        const productoId = productoSelect.value;
        const productoNombre = productoSelect.options[productoSelect.selectedIndex]?.text;
        const cantidad = parseFloat(cantidadInput.value);
        const precio = parseFloat(precioInput.value);

        if (!productoId || !productoNombre || isNaN(cantidad) || isNaN(precio) || cantidad <= 0 || precio <= 0) {
            alert('Complete los campos.');
            return;
        }

        if (detalles.some(d => d.productoId === productoId)) {
            alert('Producto duplicado.');
            return;
        }

        const detalle = {
            index: detalleIndex++,
            productoId,
            productoNombre,
            cantidad,
            precioUnitario: precio,
            subtotal: cantidad * precio
        };

        detalles.push(detalle);
        renderDetalles();
        calcularTotales();

        productoSelect.value = '';
        cantidadInput.value = 1;
        precioInput.value = 0;
    }

    function eliminarDetalle(detalleId) {
        detalles = detalles.filter(d => d.index !== detalleId);
        renderDetalles();
        calcularTotales();
    }

    function renderDetalles() {
        if (!detallesBody) return;

        if (detalles.length === 0) {
            detallesBody.innerHTML = emptyRowTemplate;
            return;
        }

        const rows = detalles.map((d, idx) => `
            <tr>
                <td>${d.productoNombre}<input type="hidden" name="Detalles[${idx}].ProductoId" value="${d.productoId}" /></td>
                <td class="text-end">${d.cantidad.toFixed(2)}<input type="hidden" name="Detalles[${idx}].Cantidad" value="${d.cantidad}" /></td>
                <td class="text-end">$${d.precioUnitario.toFixed(2)}<input type="hidden" name="Detalles[${idx}].PrecioUnitario" value="${d.precioUnitario}" /></td>
                <td class="text-end fw-bold">$${d.subtotal.toFixed(2)}<input type="hidden" name="Detalles[${idx}].Subtotal" value="${d.subtotal}" /></td>
                <td class="text-center"><button type="button" class="btn btn-sm btn-danger" data-detalle-index="${d.index}"><i class="bi bi-trash"></i></button></td>
            </tr>
        `);

        detallesBody.innerHTML = rows.join('');
    }

    function calcularTotales() {
        const subtotal = detalles.reduce((acc, d) => acc + d.subtotal, 0);
        const descuento = parseFloat(descuentoInput.value) || 0;
        const iva = (subtotal - descuento) * ivaRate;
        const total = subtotal - descuento + iva;

        subtotalDisplay.textContent = '$' + subtotal.toFixed(2);
        descuentoDisplay.textContent = '$' + descuento.toFixed(2);
        ivaDisplay.textContent = '$' + iva.toFixed(2);
        totalDisplay.textContent = '$' + total.toFixed(2);

        subtotalHidden.value = subtotal.toFixed(2);
        ivaHidden.value = iva.toFixed(2);
        totalHidden.value = total.toFixed(2);
    }

    function handleEliminarClick(event) {
        const target = event.target.closest('[data-detalle-index]');
        if (!target) return;

        const id = parseInt(target.dataset.detalleIndex, 10);
        if (!isNaN(id)) {
            eliminarDetalle(id);
        }
    }

    function inicializar() {
        if (proveedorSelect) {
            proveedorSelect.addEventListener('change', () => cargarProductos(proveedorSelect.value));
            if (proveedorSelect.value) {
                cargarProductos(proveedorSelect.value);
            }
        }

        if (productoSelect) {
            productoSelect.addEventListener('change', actualizarPrecio);
        }

        if (agregarDetalleBtn) {
            agregarDetalleBtn.addEventListener('click', agregarDetalle);
        }

        if (descuentoInput) {
            descuentoInput.addEventListener('input', calcularTotales);
        }

        if (detallesBody) {
            detallesBody.addEventListener('click', handleEliminarClick);
        }

        if (form) {
            form.addEventListener('submit', (event) => {
                if (detalles.length === 0) {
                    event.preventDefault();
                    alert('Agregue al menos un producto.');
                }
            });
        }

        calcularTotales();
    }

    inicializar();
})();
