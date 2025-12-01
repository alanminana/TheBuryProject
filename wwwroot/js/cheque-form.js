(function () {
    function setupChequeForm() {
        const proveedorSelect = document.getElementById('proveedorSelect');
        const ordenCompraSelect = document.getElementById('ordenCompraSelect');

        if (!proveedorSelect || !ordenCompraSelect) {
            return;
        }

        const resetOrdenes = () => {
            ordenCompraSelect.innerHTML = '<option value="">Ninguna (Opcional)</option>';
        };

        const cargarOrdenesProveedor = async () => {
            const proveedorId = proveedorSelect.value;
            const ordenesUrl = proveedorSelect.dataset.ordenesUrl;
            const seleccionActual = ordenCompraSelect.dataset.seleccionada;
            ordenCompraSelect.dataset.seleccionada = '';

            resetOrdenes();

            if (!proveedorId || !ordenesUrl) {
                return;
            }

            ordenCompraSelect.innerHTML = '<option value="">Cargando órdenes...</option>';

            try {
                const response = await fetch(`${ordenesUrl}?proveedorId=${encodeURIComponent(proveedorId)}`);

                if (!response.ok) {
                    throw new Error('No se pudieron cargar las órdenes.');
                }

                const ordenes = await response.json();
                resetOrdenes();

                ordenes.forEach(({ id, numero }) => {
                    const option = document.createElement('option');
                    option.value = id;
                    option.textContent = numero;

                    if (seleccionActual && seleccionActual === String(id)) {
                        option.selected = true;
                    }

                    ordenCompraSelect.appendChild(option);
                });
            } catch (error) {
                resetOrdenes();
                console.error(error);
                const option = document.createElement('option');
                option.value = '';
                option.textContent = 'Error al cargar órdenes';
                option.disabled = true;
                option.selected = true;
                ordenCompraSelect.appendChild(option);
            }
        };

        proveedorSelect.addEventListener('change', cargarOrdenesProveedor);

        cargarOrdenesProveedor();
    }

    document.addEventListener('DOMContentLoaded', setupChequeForm);
})();
