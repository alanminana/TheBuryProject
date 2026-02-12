/**
 * Módulo de Acciones Masivas del Catálogo
 * Maneja el workflow de 4 pasos para cambio masivo de precios
 */
const CatalogoAcciones = (function () {
    'use strict';

    // Estado interno
    let pasoActual = 1;
    let datosSimulacion = null;
    let antiForgeryToken = null;
    let inicializado = false;

    // Elementos DOM (se inicializan en init)
    const elementos = {};

    function leerInitConfig() {
        const el = document.getElementById('catalogo-init');
        if (!el) return null;
        try {
            return JSON.parse(el.textContent || '{}');
        } catch {
            return null;
        }
    }

    function obtenerCsrfToken() {
        const input = document.querySelector('input[name="__RequestVerificationToken"]');
        return input ? input.value : '';
    }

    /**
     * Inicializa el módulo con datos del ViewModel
     * @param {Object} config - Configuración con listas, categorías, marcas y token
     */
    function init(config) {
        if (inicializado) return;

        // Cachear elementos DOM
        elementos.offcanvas = document.getElementById('offcanvasAccionesMasivas');
        if (!elementos.offcanvas) return;

        inicializado = true;
        antiForgeryToken = (config && config.antiForgeryToken) || obtenerCsrfToken();

        elementos.nombreCambio = document.getElementById('nombreCambio');
        elementos.categorias = document.getElementById('categorias');
        elementos.marcas = document.getElementById('marcas');
        elementos.productosIds = document.getElementById('productosIds');
        elementos.tipoCambio = document.getElementById('tipoCambio');
        elementos.valorCambio = document.getElementById('valorCambio');
        elementos.valorSuffix = document.getElementById('valorSuffix');
        elementos.btnAnterior = document.getElementById('btnAnterior');
        elementos.btnSiguiente = document.getElementById('btnSiguiente');
        elementos.btnAplicar = document.getElementById('btnAplicar');
        elementos.btnVolverPreview = document.getElementById('btnVolverPreview');

        // Poblar dropdowns
        poblarDropdown(elementos.categorias, (config && config.categorias) || []);
        poblarDropdown(elementos.marcas, (config && config.marcas) || []);

        // Event listeners
        elementos.tipoCambio?.addEventListener('change', actualizarSufijo);
        elementos.valorCambio?.addEventListener('input', actualizarEjemplo);

        // Reset al abrir offcanvas
        elementos.offcanvas?.addEventListener('show.bs.offcanvas', resetearFormulario);
        elementos.btnAnterior?.addEventListener('click', pasoAnterior);
        elementos.btnSiguiente?.addEventListener('click', pasoSiguiente);
        elementos.btnAplicar?.addEventListener('click', aplicarCambios);
        elementos.btnVolverPreview?.addEventListener('click', () => volverPaso(3));

        console.log('[CatalogoAcciones] Inicializado');
    }

    /**
     * Poblar un select con opciones
     */
    function poblarDropdown(select, opciones) {
        if (!select) return;
        select.innerHTML = '';
        opciones.forEach(op => {
            const option = document.createElement('option');
            option.value = op.id;
            option.textContent = op.nombre;
            select.appendChild(option);
        });
    }

    /**
     * Actualizar sufijo según tipo de cambio
     */
    function actualizarSufijo() {
        const tipo = elementos.tipoCambio?.value;
        const sufijo = (tipo === 'absoluto' || tipo === 'directo') ? '$' : '%';
        if (elementos.valorSuffix) {
            elementos.valorSuffix.textContent = sufijo;
        }
        actualizarEjemplo();
    }

    /**
     * Actualizar ejemplo de cálculo
     */
    function actualizarEjemplo() {
        const tipo = elementos.tipoCambio?.value;
        const valor = parseFloat(elementos.valorCambio?.value) || 0;
        const precioBase = 1000;
        let precioNuevo = precioBase;
        let descripcion = '';

        switch (tipo) {
            case 'porcentaje':
                precioNuevo = precioBase * (1 + valor / 100);
                descripcion = `Con ${valor >= 0 ? '+' : ''}${valor}% sobre un precio de $1,000 → nuevo precio: $${precioNuevo.toLocaleString('es-AR', { minimumFractionDigits: 2 })}`;
                break;
            case 'porcentajecosto':
                const costoBase = 700;
                precioNuevo = costoBase * (1 + valor / 100);
                descripcion = `Con ${valor >= 0 ? '+' : ''}${valor}% sobre un costo de $700 → nuevo precio: $${precioNuevo.toLocaleString('es-AR', { minimumFractionDigits: 2 })}`;
                break;
            case 'absoluto':
                precioNuevo = precioBase + valor;
                descripcion = `Sumando ${valor >= 0 ? '+' : ''}$${valor} a un precio de $1,000 → nuevo precio: $${precioNuevo.toLocaleString('es-AR', { minimumFractionDigits: 2 })}`;
                break;
            case 'directo':
                precioNuevo = valor;
                descripcion = `Asignación directa: todos los productos tendrán precio $${valor.toLocaleString('es-AR', { minimumFractionDigits: 2 })}`;
                break;
        }

        const ejemploDiv = document.getElementById('ejemploCalculo');
        if (ejemploDiv) {
            ejemploDiv.innerHTML = `<i class="bi bi-lightbulb me-2"></i><strong>Ejemplo:</strong> ${descripcion}`;
        }
    }

    /**
     * Resetear formulario al abrir
     */
    function resetearFormulario() {
        pasoActual = 1;
        datosSimulacion = null;

        // Limpiar campos
        if (elementos.nombreCambio) elementos.nombreCambio.value = '';
        if (elementos.productosIds) elementos.productosIds.value = '';
        if (elementos.valorCambio) elementos.valorCambio.value = '';
        if (elementos.tipoCambio) elementos.tipoCambio.value = 'porcentaje';

        // Deseleccionar dropdowns
        [elementos.categorias, elementos.marcas].forEach(sel => {
            if (sel) {
                Array.from(sel.options).forEach(opt => opt.selected = false);
            }
        });

        // Limpiar preview
        const tbody = document.getElementById('previewTableBody');
        if (tbody) tbody.innerHTML = '';

        // Mostrar paso 1
        mostrarPaso(1);
        actualizarSufijo();
    }

    /**
     * Mostrar un paso específico
     */
    function mostrarPaso(numero) {
        pasoActual = numero;

        // Ocultar todos los pasos
        document.querySelectorAll('.step-content').forEach(el => el.style.display = 'none');
        
        // Mostrar paso actual
        const pasoDiv = document.getElementById(`step${numero}`);
        if (pasoDiv) pasoDiv.style.display = 'block';

        // Actualizar indicadores
        document.querySelectorAll('.step-indicator').forEach(ind => {
            const stepNum = parseInt(ind.dataset.step);
            ind.classList.remove('active', 'completed');
            ind.querySelector('.step-number').classList.remove('bg-primary', 'bg-success', 'bg-secondary');
            ind.querySelector('.step-label').classList.remove('text-muted');

            if (stepNum < numero) {
                ind.classList.add('completed');
                ind.querySelector('.step-number').classList.add('bg-success');
            } else if (stepNum === numero) {
                ind.classList.add('active');
                ind.querySelector('.step-number').classList.add('bg-primary');
            } else {
                ind.querySelector('.step-number').classList.add('bg-secondary');
                ind.querySelector('.step-label').classList.add('text-muted');
            }
        });

        // Actualizar botones
        if (elementos.btnAnterior) {
            elementos.btnAnterior.style.display = numero > 1 ? 'inline-block' : 'none';
        }
        if (elementos.btnSiguiente) {
            elementos.btnSiguiente.style.display = numero < 4 ? 'inline-block' : 'none';
            elementos.btnSiguiente.textContent = numero === 2 ? 'Simular' : 'Siguiente';
            if (numero === 2) {
                elementos.btnSiguiente.innerHTML = '<i class="bi bi-play-fill me-1"></i>Simular';
            } else {
                elementos.btnSiguiente.innerHTML = 'Siguiente<i class="bi bi-arrow-right ms-1"></i>';
            }
        }
        if (elementos.btnAplicar) {
            elementos.btnAplicar.style.display = numero === 4 ? 'inline-block' : 'none';
        }
    }

    /**
     * Ir al paso anterior
     */
    function pasoAnterior() {
        if (pasoActual > 1) {
            mostrarPaso(pasoActual - 1);
        }
    }

    /**
     * Ir al paso siguiente (con validaciones)
     */
    async function pasoSiguiente() {
        if (!validarPasoActual()) return;

        if (pasoActual === 2) {
            // Ejecutar simulación antes de ir a paso 3
            await ejecutarSimulacion();
        } else if (pasoActual === 3) {
            // Preparar resumen antes de confirmar
            prepararResumen();
            mostrarPaso(4);
        } else {
            mostrarPaso(pasoActual + 1);
        }
    }

    /**
     * Validar el paso actual
     */
    function validarPasoActual() {
        if (pasoActual === 1) {
            const nombre = elementos.nombreCambio?.value?.trim();
            if (!nombre) {
                alert('Por favor, ingrese un nombre para el cambio');
                elementos.nombreCambio?.focus();
                return false;
            }
        }

        if (pasoActual === 2) {
            const valor = elementos.valorCambio?.value;
            if (!valor || isNaN(parseFloat(valor))) {
                alert('Por favor, ingrese un valor válido para el cambio');
                elementos.valorCambio?.focus();
                return false;
            }
        }

        return true;
    }

    /**
     * Obtener IDs seleccionados de un select multiple
     */
    function getSelectedIds(select) {
        if (!select) return [];
        return Array.from(select.selectedOptions).map(opt => parseInt(opt.value));
    }

    /**
     * Parsear IDs de texto (separados por coma)
     */
    function parseProductoIds(texto) {
        if (!texto || !texto.trim()) return null;
        return texto.split(',')
            .map(id => parseInt(id.trim()))
            .filter(id => !isNaN(id));
    }

    /**
     * Ejecutar simulación
     */
    async function ejecutarSimulacion() {
        const loadingDiv = document.getElementById('previewLoading');
        const tableContainer = document.getElementById('previewTableContainer');
        
        // Mostrar loading
        if (loadingDiv) loadingDiv.style.display = 'block';
        if (tableContainer) tableContainer.style.display = 'none';
        mostrarPaso(3);

        const solicitud = {
            nombre: elementos.nombreCambio?.value?.trim() || 'Cambio masivo',
            tipoCambio: elementos.tipoCambio?.value || 'porcentaje',
            valor: parseFloat(elementos.valorCambio?.value) || 0,
            listasIds: null,
            categoriasIds: getSelectedIds(elementos.categorias),
            marcasIds: getSelectedIds(elementos.marcas),
            productosIds: parseProductoIds(elementos.productosIds?.value)
        };

        if (solicitud.categoriasIds.length === 0) solicitud.categoriasIds = null;
        if (solicitud.marcasIds.length === 0) solicitud.marcasIds = null;

        try {
            const token = obtenerCsrfToken() || antiForgeryToken || '';
            const response = await fetch('/Catalogo/SimularCambioPrecios', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify(solicitud)
            });

            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.mensaje || error.error || 'Error al simular');
            }

            datosSimulacion = await response.json();
            mostrarPreview(datosSimulacion);

        } catch (error) {
            console.error('[CatalogoAcciones] Error en simulación:', error);
            alert('Error al simular: ' + error.message);
            mostrarPaso(2);
        } finally {
            if (loadingDiv) loadingDiv.style.display = 'none';
            if (tableContainer) tableContainer.style.display = 'block';
        }
    }

    /**
     * Mostrar preview de simulación
     */
    function mostrarPreview(datos) {
        // Métricas
        document.getElementById('previewTotal').textContent = datos.totalProductos || 0;
        document.getElementById('previewAumentos').textContent = datos.productosConAumento || 0;
        document.getElementById('previewDescuentos').textContent = datos.productosConDescuento || 0;

        // Alerta de autorización
        const alertaAuth = document.getElementById('alertaAutorizacion');
        if (alertaAuth) {
            alertaAuth.style.display = datos.requiereAutorizacion ? 'block' : 'none';
        }

        // Tabla de filas
        const tbody = document.getElementById('previewTableBody');
        if (!tbody) return;

        tbody.innerHTML = '';

        (datos.filas || []).forEach(fila => {
            const tr = document.createElement('tr');
            const diffClass = fila.esAumento ? 'text-success' : 'text-danger';
            const diffIcon = fila.esAumento ? '↑' : '↓';

            tr.innerHTML = `
                <td>
                    <div class="fw-semibold">${escapeHtml(fila.nombre)}</div>
                    <small class="text-muted">${escapeHtml(fila.codigo)} · ${escapeHtml(fila.listaNombre)}</small>
                </td>
                <td class="text-end">$${formatearNumero(fila.precioActual)}</td>
                <td class="text-end fw-bold">$${formatearNumero(fila.precioNuevo)}</td>
                <td class="text-end ${diffClass}">
                    ${diffIcon} ${formatearNumero(fila.diferenciaPorcentaje)}%
                </td>
            `;
            tbody.appendChild(tr);
        });
    }

    /**
     * Preparar resumen para confirmación
     */
    function prepararResumen() {
        if (!datosSimulacion) return;

        document.getElementById('resumenNombre').textContent = datosSimulacion.nombre;
        document.getElementById('resumenTipo').textContent = elementos.tipoCambio?.options[elementos.tipoCambio.selectedIndex]?.text || '-';
        document.getElementById('resumenValor').textContent = `${datosSimulacion.valor >= 0 ? '+' : ''}${datosSimulacion.valor}${elementos.valorSuffix?.textContent || '%'}`;
        document.getElementById('resumenProductos').textContent = `${datosSimulacion.totalProductos} productos`;
    }

    /**
     * Aplicar cambios definitivamente
     */
    async function aplicarCambios() {
        if (!datosSimulacion) {
            alert('No hay simulación para aplicar');
            return;
        }

        if (!confirm('¿Está seguro de aplicar estos cambios? Esta acción no se puede deshacer fácilmente.')) {
            return;
        }

        const btnAplicar = elementos.btnAplicar;
        if (btnAplicar) {
            btnAplicar.disabled = true;
            btnAplicar.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Aplicando...';
        }

        const solicitud = {
            batchId: datosSimulacion.batchId,
            rowVersion: datosSimulacion.rowVersion,
            fechaVigencia: document.getElementById('fechaVigencia')?.value || null,
            notas: document.getElementById('notasCambio')?.value || null
        };

        try {
            const token = obtenerCsrfToken() || antiForgeryToken || '';
            const response = await fetch('/Catalogo/AplicarCambioPrecios', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify(solicitud)
            });

            const resultado = await response.json();

            // Mostrar resultado
            document.querySelectorAll('.step-content').forEach(el => el.style.display = 'none');
            document.getElementById('stepResultado').style.display = 'block';

            if (resultado.exitoso) {
                document.getElementById('resultadoExito').style.display = 'block';
                document.getElementById('resultadoError').style.display = 'none';
                document.getElementById('resultadoMensaje').textContent = resultado.mensaje;
            } else {
                document.getElementById('resultadoExito').style.display = 'none';
                document.getElementById('resultadoError').style.display = 'block';
                document.getElementById('errorMensaje').textContent = resultado.mensaje || resultado.error;
            }

            // Ocultar botones de navegación
            if (elementos.btnAnterior) elementos.btnAnterior.style.display = 'none';
            if (elementos.btnSiguiente) elementos.btnSiguiente.style.display = 'none';
            if (elementos.btnAplicar) elementos.btnAplicar.style.display = 'none';

        } catch (error) {
            console.error('[CatalogoAcciones] Error al aplicar:', error);
            alert('Error al aplicar los cambios: ' + error.message);
        } finally {
            if (btnAplicar) {
                btnAplicar.disabled = false;
                btnAplicar.innerHTML = '<i class="bi bi-check-lg me-1"></i>Aplicar Cambios';
            }
        }
    }

    /**
     * Volver a un paso específico (usado desde botón en error)
     */
    function volverPaso(numero) {
        mostrarPaso(numero);
    }

    // Utilidades
    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text || '';
        return div.innerHTML;
    }

    function formatearNumero(num) {
        return (num || 0).toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }

    document.addEventListener('DOMContentLoaded', function () {
        const config = leerInitConfig() || {};
        if (config.esAdminPrecios === false) return;
        init(config);
    });

    // API pública
    return {
        init,
        pasoAnterior,
        pasoSiguiente,
        aplicarCambios,
        volverPaso
    };

})();
