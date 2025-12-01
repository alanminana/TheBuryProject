(function () {
    const form = document.getElementById('formSimular');
    if (!form) {
        return;
    }

    const simularUrl = form.dataset.simularUrl;
    const resultadoCard = document.getElementById('resultadoCard');
    const resultadoContenido = document.getElementById('resultadoContenido');
    const alertasCard = document.getElementById('alertasCard');
    const alertasContenido = document.getElementById('alertasContenido');

    form.addEventListener('submit', async (event) => {
        event.preventDefault();

        const productoId = document.getElementById('productoId')?.value;
        const precioCompraNuevo = parseFloat(document.getElementById('precioCompraNuevo')?.value || '0');
        const precioVentaNuevo = parseFloat(document.getElementById('precioVentaNuevo')?.value || '0');
        const token = form.querySelector('input[name="__RequestVerificationToken"]')?.value || '';

        try {
            const response = await fetch(simularUrl, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    RequestVerificationToken: token
                },
                body: JSON.stringify({
                    productoId: parseInt(productoId, 10),
                    precioCompraPropuesto: precioCompraNuevo,
                    precioVentaPropuesto: precioVentaNuevo
                })
            });

            if (!response.ok) {
                throw new Error('No se pudo realizar la simulación');
            }

            const result = await response.json();

            if (result.success) {
                mostrarResultado(result.data);
            } else {
                alert(`Error: ${result.message}`);
            }
        } catch (error) {
            console.error('Error:', error);
            alert('Error al realizar la simulación');
        }
    });

    function mostrarResultado(data) {
        if (resultadoCard) {
            resultadoCard.style.display = 'block';
        }

        const html = `
            <div class="row mb-3">
                <div class="col-6">
                    <h6 class="text-muted">Cambio en Compra</h6>
                    <h3 class="${data.porcentajeCambioCompra >= 0 ? 'text-danger' : 'text-success'}">
                        ${data.porcentajeCambioCompra >= 0 ? '↑' : '↓'} ${Math.abs(data.porcentajeCambioCompra).toFixed(2)}%
                    </h3>
                    <p class="mb-0">
                        ${data.diferenciaCompra >= 0 ? '+' : ''}$${data.diferenciaCompra.toFixed(2)}
                    </p>
                </div>
                <div class="col-6">
                    <h6 class="text-muted">Cambio en Venta</h6>
                    <h3 class="${data.porcentajeCambioVenta >= 0 ? 'text-success' : 'text-danger'}">
                        ${data.porcentajeCambioVenta >= 0 ? '↑' : '↓'} ${Math.abs(data.porcentajeCambioVenta).toFixed(2)}%
                    </h3>
                    <p class="mb-0">
                        ${data.diferenciaVenta >= 0 ? '+' : ''}$${data.diferenciaVenta.toFixed(2)}
                    </p>
                </div>
            </div>

            <hr />

            <div class="row">
                <div class="col-6">
                    <h6 class="text-muted">Margen Actual</h6>
                    <h4>${data.margenActual.toFixed(2)}%</h4>
                </div>
                <div class="col-6">
                    <h6 class="text-muted">Margen Propuesto</h6>
                    <h4 class="${data.margenPropuesto >= data.margenActual ? 'text-success' : 'text-danger'}">
                        ${data.margenPropuesto.toFixed(2)}%
                    </h4>
                </div>
            </div>

            <div class="mt-3">
                <h6 class="text-muted">Diferencia de Margen</h6>
                <h3 class="${data.diferenciaMargen >= 0 ? 'text-success' : 'text-danger'}">
                    ${data.diferenciaMargen >= 0 ? '+' : ''}${data.diferenciaMargen.toFixed(2)}%
                </h3>
            </div>
        `;

        if (resultadoContenido) {
            resultadoContenido.innerHTML = html;
        }

        if (data.alertas && data.alertas.length > 0) {
            if (alertasCard) {
                alertasCard.style.display = 'block';
            }

            let alertasHtml = '<ul class="list-unstyled mb-0">';
            data.alertas.forEach((alerta) => {
                alertasHtml += `<li class="mb-2"><i class="bi bi-exclamation-circle"></i> ${alerta}</li>`;
            });
            alertasHtml += '</ul>';

            if (data.recomendacion) {
                alertasHtml += `<hr /><div class="alert ${data.esRecomendable ? 'alert-success' : 'alert-danger'} mb-0 mt-3">${data.recomendacion}</div>`;
            }

            if (alertasContenido) {
                alertasContenido.innerHTML = alertasHtml;
            }
        } else if (alertasCard) {
            alertasCard.style.display = 'none';
        }
    }
})();

