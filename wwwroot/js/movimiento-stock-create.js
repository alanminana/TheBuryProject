(function () {
    const productoSelect = document.getElementById('productoSelect');
    const productoInfo = document.getElementById('productoInfo');
    const productoCodigo = document.getElementById('productoCodigo');
    const productoNombre = document.getElementById('productoNombre');
    const stockActualBadge = document.getElementById('stockActual');
    const tipoSelect = document.getElementById('tipoSelect');
    const cantidadInput = document.getElementById('cantidadInput');
    const stockResultado = document.getElementById('stockResultado');

    let stockActual = 0;

    async function cargarProductoInfo(id) {
        const baseUrl = productoSelect?.dataset?.productoInfoUrl;
        if (!baseUrl) {
            return;
        }

        try {
            const response = await fetch(`${baseUrl}/${id}`);
            if (!response.ok) {
                throw new Error('No se pudo obtener la información del producto');
            }

            const data = await response.json();
            productoCodigo.textContent = data.codigo || '-';
            productoNombre.textContent = data.nombre || '-';
            stockActualBadge.textContent = data.stockActual ?? 0;
            stockActual = parseFloat(data.stockActual) || 0;
            productoInfo?.classList.remove('d-none');
            calcularStock();
        } catch (error) {
            console.error(error);
            productoInfo?.classList.add('d-none');
            stockActual = 0;
            stockResultado.textContent = 'No se pudo cargar la información del producto.';
            stockResultado.className = 'form-text text-danger';
        }
    }

    function calcularStock() {
        const tipo = tipoSelect?.value;
        const cantidad = parseFloat(cantidadInput?.value || '') || 0;
        if (!tipo || cantidad === 0) {
            stockResultado.textContent = '';
            return;
        }

        let nuevo = 0;
        let mensaje = '';
        let clase = '';

        switch (tipo) {
            case '0':
                nuevo = stockActual + cantidad;
                mensaje = `Stock resultante: ${nuevo.toFixed(2)} (actual: ${stockActual} + ${cantidad})`;
                clase = 'text-success';
                break;
            case '1':
                nuevo = stockActual - cantidad;
                if (nuevo < 0) {
                    mensaje = `⚠️ Stock insuficiente (actual: ${stockActual})`;
                    clase = 'text-danger';
                } else {
                    mensaje = `Stock resultante: ${nuevo.toFixed(2)} (actual: ${stockActual} - ${cantidad})`;
                    clase = 'text-warning';
                }
                break;
            case '2':
                nuevo = cantidad;
                const diferencia = cantidad - stockActual;
                mensaje = `Nuevo stock: ${nuevo.toFixed(2)} (dif: ${diferencia >= 0 ? '+' : ''}${diferencia.toFixed(2)})`;
                clase = 'text-info';
                break;
            default:
                mensaje = '';
        }

        stockResultado.textContent = mensaje;
        stockResultado.className = `form-text ${clase}`.trim();
    }

    productoSelect?.addEventListener('change', function () {
        const id = this.value;
        if (!id) {
            productoInfo?.classList.add('d-none');
            stockActual = 0;
            stockResultado.textContent = '';
            return;
        }

        cargarProductoInfo(id);
    });

    tipoSelect?.addEventListener('change', calcularStock);
    cantidadInput?.addEventListener('change', calcularStock);
    cantidadInput?.addEventListener('keyup', calcularStock);

    document.addEventListener('DOMContentLoaded', function () {
        const idInicial = productoSelect?.value;
        if (idInicial) {
            productoSelect.dispatchEvent(new Event('change'));
        }
    });
})();
