(function () {
    const productoSelect = $('#productoSelect');
    const productoInfo = $('#productoInfo');
    const productoCodigo = $('#productoCodigo');
    const productoNombre = $('#productoNombre');
    const stockActualBadge = $('#stockActual');
    const tipoSelect = $('#tipoSelect');
    const cantidadInput = $('#cantidadInput');
    const stockResultado = $('#stockResultado');

    let stockActual = 0;

    async function cargarProductoInfo(id) {
        const baseUrl = productoSelect.data('producto-info-url');
        if (!baseUrl) {
            return;
        }

        try {
            const response = await fetch(`${baseUrl}/${id}`);
            if (!response.ok) {
                throw new Error('No se pudo obtener la información del producto');
            }

            const data = await response.json();
            productoCodigo.text(data.codigo || '-');
            productoNombre.text(data.nombre || '-');
            stockActualBadge.text(data.stockActual ?? 0);
            stockActual = parseFloat(data.stockActual) || 0;
            productoInfo.show();
            calcularStock();
        } catch (error) {
            productoInfo.hide();
            stockActual = 0;
            stockResultado.text('');
        }
    }

    function calcularStock() {
        const tipo = tipoSelect.val();
        const cantidad = parseFloat(cantidadInput.val()) || 0;
        if (!tipo || cantidad === 0) {
            stockResultado.text('');
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

        stockResultado.text(mensaje).removeClass().addClass(`form-text ${clase}`);
    }

    productoSelect.on('change', function () {
        const id = $(this).val();
        if (!id) {
            productoInfo.hide();
            stockActual = 0;
            stockResultado.text('');
            return;
        }

        cargarProductoInfo(id);
    });

    tipoSelect.add(cantidadInput).on('change keyup', calcularStock);

    $(function () {
        const idInicial = productoSelect.val();
        if (idInicial) {
            productoSelect.trigger('change');
        }
    });
})();
