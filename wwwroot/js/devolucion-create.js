const initDevolucionCreate = () => {
    const loadButton = document.getElementById('btnCargarVenta');
    const ventaSelect = document.getElementById('ventaSelect');
    const createUrl = loadButton?.dataset?.createUrl || '';

    if (loadButton && ventaSelect) {
        loadButton.addEventListener('click', () => {
            const ventaId = ventaSelect.value;
            if (!ventaId) {
                alert('Debe seleccionar una venta');
                return;
            }

            const url = createUrl ? `${createUrl}?ventaId=${encodeURIComponent(ventaId)}` : `?ventaId=${encodeURIComponent(ventaId)}`;
            window.location.href = url;
        });
    }

    const cantidadInputs = Array.from(document.querySelectorAll('.cantidad-devolver'));
    const totalElement = document.getElementById('totalDevolucion');
    const guardarButton = document.getElementById('btnGuardar');
    const form = document.querySelector('form');

    const calcularTotales = () => {
        let total = 0;
        let hayProductos = false;

        cantidadInputs.forEach(input => {
            const index = input.dataset.index;
            const cantidad = parseInt(input.value, 10) || 0;
            const precioInput = document.querySelector(`input[name="Productos[${index}].PrecioUnitario"]`);
            const precio = parseFloat(precioInput?.value || '0') || 0;
            const subtotal = cantidad * precio;

            const subtotalElement = document.getElementById(`subtotal_${index}`);
            if (subtotalElement) {
                subtotalElement.textContent = `$${subtotal.toFixed(2)}`;
            }

            total += subtotal;
            if (cantidad > 0) {
                hayProductos = true;
            }
        });

        if (totalElement) {
            totalElement.textContent = `$${total.toFixed(2)}`;
        }

        if (guardarButton) {
            guardarButton.disabled = !hayProductos;
        }
    };

    cantidadInputs.forEach(input => {
        input.addEventListener('input', calcularTotales);
    });

    calcularTotales();

    if (form) {
        form.addEventListener('submit', event => {
            const descripcion = form.querySelector('textarea[name="Descripcion"]');
            const texto = descripcion?.value.trim() || '';

            if (texto.length < 20) {
                event.preventDefault();
                alert('La descripción debe tener al menos 20 caracteres.');
                return;
            }

            const tieneProductos = cantidadInputs.some(input => (parseInt(input.value, 10) || 0) > 0);
            if (!tieneProductos) {
                event.preventDefault();
                alert('Debe seleccionar al menos un producto para devolver.');
            }
        });
    }
};

document.addEventListener('DOMContentLoaded', initDevolucionCreate);
