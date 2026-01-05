/**
 * Módulo de Selección de Productos del Catálogo
 * Maneja la selección múltiple de productos y la barra de cambio de precios
 * 
 * Fase 2: Comportamiento completo
 * - Seleccionar/deseleccionar filas (click en fila o checkbox)
 * - Checkbox maestro con estado indeterminado
 * - Validación de porcentaje (signo ± y decimales)
 * - Modo "Seleccionados" vs "Filtrados" con envío de filtros JSON
 */
const CatalogoSeleccion = (function () {
    'use strict';

    // Estado interno
    let productosSeleccionados = new Map(); // Map<id, {codigo, nombre, precio}>

    // Elementos DOM (se inicializan en init)
    const elementos = {};

    // Regex para validar porcentaje: permite ±999.99
    const REGEX_PORCENTAJE = /^[+-]?\d{1,3}(\.\d{1,2})?$/;

    /**
     * Inicializa el módulo
     */
    function init() {
        // Cachear elementos DOM - Tabla
        elementos.tabla = document.getElementById('tablaProductosCatalogo');
        elementos.checkMaster = document.getElementById('checkMaster');
        
        // Barra de cambio de precios
        elementos.barraCambioPrecios = document.getElementById('barraCambioPrecios');
        elementos.formCambioPreciosRapido = document.getElementById('formCambioPreciosRapido');
        elementos.selectAlcance = document.getElementById('selectAlcanceCambio');
        elementos.inputPorcentaje = document.getElementById('inputPorcentajeCambio');
        elementos.selectListas = document.getElementById('selectListasCambio');
        elementos.btnAplicar = document.getElementById('btnAplicarRapido');
        elementos.btnHistorial = document.getElementById('btnHistorialRapido');
        elementos.btnLimpiar = document.getElementById('btnLimpiarSeleccion');
        elementos.badgeSeleccionados = document.getElementById('badgeSeleccionados');
        elementos.contadorSeleccionados = document.getElementById('contadorSeleccionados');
        elementos.hiddenProductoIds = document.getElementById('hiddenProductoIds');
        elementos.hiddenFiltrosJson = document.getElementById('hiddenFiltrosJson');
        elementos.hiddenAlcance = document.getElementById('hiddenAlcance');

        // Feedback de validación
        elementos.feedbackPorcentaje = document.getElementById('feedbackPorcentaje');

        // Modal de confirmación
        elementos.modalConfirmar = document.getElementById('modalConfirmarAplicar');
        elementos.confirmarAlcance = document.getElementById('confirmarAlcance');
        elementos.confirmarCantidad = document.getElementById('confirmarCantidad');
        elementos.confirmarPorcentaje = document.getElementById('confirmarPorcentaje');
        elementos.confirmarLista = document.getElementById('confirmarLista');
        elementos.btnConfirmarAplicar = document.getElementById('btnConfirmarAplicar');
        elementos.areaErrorConfirmar = document.getElementById('areaErrorConfirmar');
        elementos.mensajeErrorConfirmar = document.getElementById('mensajeErrorConfirmar');

        // Modal alternativo (detalle)
        elementos.modal = document.getElementById('modalCambiarPrecios');
        elementos.modalContador = document.getElementById('modalContadorProductos');
        elementos.modalListaProductos = document.getElementById('modalListaProductos');
        elementos.modalHiddenProductoIds = document.getElementById('modalHiddenProductoIds');

        if (!elementos.tabla) {
            console.warn('[CatalogoSeleccion] Tabla no encontrada');
            return;
        }

        // Event listeners
        bindEventListeners();

        // Estado inicial
        actualizarUI();

        console.log('[CatalogoSeleccion] Inicializado correctamente');
    }

    /**
     * Vincular event listeners
     */
    function bindEventListeners() {
        // Checkbox master
        elementos.checkMaster?.addEventListener('change', onCheckMasterChange);

        // Checkboxes individuales (delegación de eventos)
        elementos.tabla?.addEventListener('change', onCheckboxChange);

        // Click en fila para seleccionar (delegación)
        elementos.tabla?.addEventListener('click', onRowClick);

        // Barra de cambio de precios
        elementos.selectAlcance?.addEventListener('change', onAlcanceChange);
        elementos.inputPorcentaje?.addEventListener('input', onPorcentajeInput);
        elementos.inputPorcentaje?.addEventListener('blur', onPorcentajeBlur);
        elementos.inputPorcentaje?.addEventListener('keydown', onPorcentajeKeydown);
        elementos.btnHistorial?.addEventListener('click', onHistorialClick);
        elementos.btnLimpiar?.addEventListener('click', limpiarSeleccion);

        // Modal de confirmación: poblar datos cuando se abre
        elementos.modalConfirmar?.addEventListener('show.bs.modal', onModalConfirmarShow);
        
        // Botón confirmar aplicar
        elementos.btnConfirmarAplicar?.addEventListener('click', onConfirmarAplicar);

        // Modal detalle: sincronizar IDs cuando se abre
        elementos.modal?.addEventListener('show.bs.modal', onModalShow);

        // Atajos de teclado
        document.addEventListener('keydown', onGlobalKeydown);
    }

    /**
     * Handler cuando se abre el modal de confirmación
     */
    function onModalConfirmarShow() {
        // Limpiar errores previos
        if (elementos.areaErrorConfirmar) {
            elementos.areaErrorConfirmar.style.display = 'none';
        }

        // Poblar datos del modal
        const alcance = elementos.selectAlcance?.value;
        const porcentaje = elementos.inputPorcentaje?.value || '0';
        const listasSeleccionadas = elementos.selectListas ? 
            Array.from(elementos.selectListas.selectedOptions).map(o => o.text) : [];

        // Alcance
        if (elementos.confirmarAlcance) {
            if (alcance === 'seleccionados') {
                elementos.confirmarAlcance.textContent = 'Seleccionados';
            } else {
                elementos.confirmarAlcance.textContent = 'Filtrados (todos)';
            }
        }

        // Cantidad
        if (elementos.confirmarCantidad) {
            if (alcance === 'seleccionados') {
                elementos.confirmarCantidad.textContent = `${productosSeleccionados.size} producto(s)`;
            } else {
                // Obtener del badge de resultados totales
                const totalBadge = document.querySelector('.card-header .badge');
                const total = totalBadge?.textContent || '?';
                elementos.confirmarCantidad.textContent = `${total} producto(s)`;
            }
        }

        // Porcentaje
        if (elementos.confirmarPorcentaje) {
            const num = parseFloat(porcentaje);
            if (num >= 0) {
                elementos.confirmarPorcentaje.innerHTML = `<span class="text-success">+${porcentaje}%</span>`;
            } else {
                elementos.confirmarPorcentaje.innerHTML = `<span class="text-danger">${porcentaje}%</span>`;
            }
        }

        // Lista
        if (elementos.confirmarLista) {
            elementos.confirmarLista.textContent = listasSeleccionadas.join(', ') || 'Ninguna';
        }

        console.log('[CatalogoSeleccion] Modal confirmación abierto');
    }

    /**
     * Handler para confirmar y aplicar el cambio de precios
     */
    async function onConfirmarAplicar() {
        const alcance = elementos.selectAlcance?.value;
        const porcentaje = elementos.inputPorcentaje?.value?.trim();
        const listasSeleccionadas = elementos.selectListas ? 
            Array.from(elementos.selectListas.selectedOptions).map(o => parseInt(o.value)) : [];

        // Validar
        if (!validarPorcentaje(porcentaje)) {
            mostrarErrorConfirmar('Porcentaje inválido');
            return;
        }

        if (listasSeleccionadas.length === 0) {
            mostrarErrorConfirmar('Seleccione al menos una lista de precios');
            return;
        }

        // Preparar datos
        const requestData = {
            modo: alcance,
            porcentaje: parseFloat(porcentaje),
            listasPrecioIds: listasSeleccionadas
        };

        if (alcance === 'seleccionados') {
            requestData.productoIds = Array.from(productosSeleccionados.keys()).map(id => parseInt(id));
            if (requestData.productoIds.length === 0) {
                mostrarErrorConfirmar('Seleccione al menos un producto');
                return;
            }
        } else {
            // Filtrados: enviar filtros
            const filtrosJson = elementos.hiddenFiltrosJson?.value;
            if (filtrosJson) {
                try {
                    requestData.filtros = JSON.parse(filtrosJson);
                } catch (e) {
                    console.warn('Error parsing filtros:', e);
                }
            }
        }

        // Deshabilitar botón
        const btnOriginalText = elementos.btnConfirmarAplicar.innerHTML;
        elementos.btnConfirmarAplicar.disabled = true;
        elementos.btnConfirmarAplicar.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Aplicando...';

        try {
            // Obtener token CSRF
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

            const response = await fetch('/CambiosPrecios/AplicarRapido', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token || ''
                },
                body: JSON.stringify(requestData)
            });

            const result = await response.json();

            if (result.success) {
                // Cerrar modal
                const modal = bootstrap.Modal.getInstance(elementos.modalConfirmar);
                modal?.hide();

                // Mostrar éxito
                if (typeof toastr !== 'undefined') {
                    toastr.success(result.mensaje || `Cambio aplicado: ${result.productosAfectados} productos actualizados`);
                }

                // Recargar página para ver cambios
                setTimeout(() => {
                    window.location.reload();
                }, 1500);
            } else {
                mostrarErrorConfirmar(result.error || 'Error al aplicar el cambio');
            }
        } catch (error) {
            console.error('Error en AplicarRapido:', error);
            mostrarErrorConfirmar('Error de conexión. Intente nuevamente.');
        } finally {
            elementos.btnConfirmarAplicar.disabled = false;
            elementos.btnConfirmarAplicar.innerHTML = btnOriginalText;
        }
    }

    /**
     * Mostrar error en el modal de confirmación
     */
    function mostrarErrorConfirmar(mensaje) {
        if (elementos.areaErrorConfirmar && elementos.mensajeErrorConfirmar) {
            elementos.mensajeErrorConfirmar.textContent = mensaje;
            elementos.areaErrorConfirmar.style.display = 'block';
        }
    }

    /**
     * Handler cuando se abre el modal de cambio de precios (detalle)
     */
    function onModalShow() {
        // Sincronizar IDs de productos seleccionados al campo hidden del modal
        const ids = Array.from(productosSeleccionados.keys());
        if (elementos.modalHiddenProductoIds) {
            elementos.modalHiddenProductoIds.value = ids.join(',');
        }
        console.log('[CatalogoSeleccion] Modal abierto, IDs sincronizados:', ids.join(','));
    }

    /**
     * Handler para checkbox master
     */
    function onCheckMasterChange(e) {
        const isChecked = e.target.checked;
        const checkboxes = elementos.tabla.querySelectorAll('.check-producto');
        
        checkboxes.forEach(cb => {
            cb.checked = isChecked;
            const row = cb.closest('tr');
            const id = row.dataset.productoId;
            const codigo = row.dataset.productoCodigo;
            const nombre = row.dataset.productoNombre;

            if (isChecked) {
                productosSeleccionados.set(id, { codigo, nombre });
                row.classList.add('table-active');
            } else {
                productosSeleccionados.delete(id);
                row.classList.remove('table-active');
            }
        });

        actualizarUI();
    }

    /**
     * Handler para checkboxes individuales (delegación)
     */
    function onCheckboxChange(e) {
        if (!e.target.classList.contains('check-producto')) return;

        const row = e.target.closest('tr');
        const id = row.dataset.productoId;
        const codigo = row.dataset.productoCodigo;
        const nombre = row.dataset.productoNombre;
        const precio = row.dataset.productoPrecio;

        if (e.target.checked) {
            productosSeleccionados.set(id, { codigo, nombre, precio });
            row.classList.add('table-active');
        } else {
            productosSeleccionados.delete(id);
            row.classList.remove('table-active');
        }

        actualizarCheckMaster();
        actualizarUI();
    }

    /**
     * Handler para click en fila (selecciona/deselecciona)
     * Solo actúa si el click fue en la fila, no en botones o links
     */
    function onRowClick(e) {
        // Ignorar clicks en elementos interactivos
        const target = e.target;
        if (target.closest('a, button, .btn, input, .dropdown, .form-check')) return;

        const row = target.closest('tr[data-producto-id]');
        if (!row) return;

        // Toggle del checkbox
        const checkbox = row.querySelector('.check-producto');
        if (checkbox) {
            checkbox.checked = !checkbox.checked;
            checkbox.dispatchEvent(new Event('change', { bubbles: true }));
        }
    }

    /**
     * Handler para cambio de alcance
     * Sincroniza el hidden y actualiza la UI según el modo
     */
    function onAlcanceChange() {
        const alcance = elementos.selectAlcance?.value;
        
        // Sincronizar hidden para el backend
        if (elementos.hiddenAlcance) {
            elementos.hiddenAlcance.value = alcance;
        }
        
        // Feedback visual según modo
        if (elementos.badgeSeleccionados) {
            if (alcance === 'filtrados') {
                // En modo filtrados, atenuar el badge de seleccionados
                elementos.badgeSeleccionados.classList.add('opacity-50');
                elementos.badgeSeleccionados.title = 'Modo filtrados activo - se ignorará la selección manual';
            } else {
                elementos.badgeSeleccionados.classList.remove('opacity-50');
                elementos.badgeSeleccionados.title = '';
            }
        }

        // Highlight visual en la barra según modo
        if (elementos.barraCambioPrecios) {
            if (alcance === 'filtrados') {
                elementos.barraCambioPrecios.classList.add('border-info');
                elementos.barraCambioPrecios.classList.remove('border-secondary');
            } else {
                elementos.barraCambioPrecios.classList.remove('border-info');
                elementos.barraCambioPrecios.classList.add('border-secondary');
            }
        }

        actualizarBotonSimular();
    }

    /**
     * Handler para input de porcentaje - validación en tiempo real
     */
    function onPorcentajeInput(e) {
        const valor = e.target.value.trim();
        
        // Permitir valores vacíos, signos solos mientras escribe
        if (valor === '' || valor === '-' || valor === '+') {
            setValidacionPorcentaje(null); // neutral
            actualizarBotonSimular();
            return;
        }

        // Validar formato
        const esValido = validarPorcentaje(valor);
        setValidacionPorcentaje(esValido);
        actualizarBotonSimular();
    }

    /**
     * Handler para blur en porcentaje - validación final
     */
    function onPorcentajeBlur(e) {
        const valor = e.target.value.trim();
        
        if (valor === '' || valor === '-' || valor === '+') {
            e.target.value = '';
            setValidacionPorcentaje(null);
            return;
        }

        // Limpiar signo + redundante
        if (valor.startsWith('+')) {
            e.target.value = valor.substring(1);
        }

        const esValido = validarPorcentaje(valor);
        setValidacionPorcentaje(esValido);
    }

    /**
     * Handler para keydown en porcentaje - permite solo caracteres válidos
     */
    function onPorcentajeKeydown(e) {
        const permitidos = ['Backspace', 'Delete', 'Tab', 'Escape', 'Enter', 'ArrowLeft', 'ArrowRight', 'Home', 'End'];
        
        if (permitidos.includes(e.key)) return;
        
        // Permitir Ctrl+A, Ctrl+C, Ctrl+V, Ctrl+X
        if (e.ctrlKey && ['a', 'c', 'v', 'x'].includes(e.key.toLowerCase())) return;

        // Permitir números, punto, signo menos y más
        const validos = /^[0-9.\-+]$/;
        if (!validos.test(e.key)) {
            e.preventDefault();
        }

        // Solo un punto decimal
        if (e.key === '.' && e.target.value.includes('.')) {
            e.preventDefault();
        }

        // Solo un signo al inicio
        if ((e.key === '-' || e.key === '+') && e.target.selectionStart !== 0) {
            e.preventDefault();
        }
    }

    /**
     * Handler para atajos de teclado globales
     */
    function onGlobalKeydown(e) {
        // Escape: limpiar selección
        if (e.key === 'Escape' && productosSeleccionados.size > 0) {
            limpiarSeleccion();
        }

        // Ctrl+A en la tabla: seleccionar todos
        if (e.ctrlKey && e.key === 'a' && document.activeElement?.closest('#tablaProductosCatalogo')) {
            e.preventDefault();
            if (elementos.checkMaster) {
                elementos.checkMaster.checked = true;
                elementos.checkMaster.dispatchEvent(new Event('change'));
            }
        }
    }

    /**
     * Validar formato de porcentaje
     * @param {string} valor - El valor a validar
     * @returns {boolean} - true si es válido
     */
    function validarPorcentaje(valor) {
        if (!valor || valor === '') return false;
        
        const numero = parseFloat(valor);
        if (isNaN(numero)) return false;
        
        // Rango permitido: -100% a +999%
        if (numero < -100 || numero > 999) return false;
        
        // No permitir 0
        if (numero === 0) return false;

        return true;
    }

    /**
     * Establecer estado visual de validación del porcentaje
     * @param {boolean|null} esValido - true=válido, false=inválido, null=neutral
     */
    function setValidacionPorcentaje(esValido) {
        const input = elementos.inputPorcentaje;
        if (!input) return;

        input.classList.remove('is-valid', 'is-invalid');
        
        if (esValido === true) {
            input.classList.add('is-valid');
        } else if (esValido === false) {
            input.classList.add('is-invalid');
        }
        // null = neutral, sin clases
    }

    /**
     * Handler para click en Historial
     */
    function onHistorialClick() {
        const alcance = elementos.selectAlcance?.value;

        if (alcance === 'seleccionados' && productosSeleccionados.size > 0) {
            const ids = Array.from(productosSeleccionados.keys());
            if (ids.length === 1) {
                window.location.href = `/CambiosPrecios/Historial?productoId=${ids[0]}`;
            } else {
                window.location.href = `/CambiosPrecios/Index?productoIdsText=${ids.join(',')}`;
            }
        } else {
            // Ir al historial general
            window.location.href = '/CambiosPrecios/Index';
        }
    }

    /**
     * Handler para submit del formulario
     * Prepara los datos según el modo seleccionado (Seleccionados vs Filtrados)
     */
    function onFormSubmit(e) {
        const alcance = elementos.selectAlcance?.value;
        const valor = elementos.inputPorcentaje?.value?.trim();

        // Validar porcentaje
        if (!validarPorcentaje(valor)) {
            e.preventDefault();
            setValidacionPorcentaje(false);
            mostrarError('Ingrese un porcentaje válido (ej: 10 para aumentar, -5 para descuento)');
            elementos.inputPorcentaje?.focus();
            return false;
        }

        // Validar listas de precios seleccionadas
        const listasSeleccionadas = elementos.selectListas ? 
            Array.from(elementos.selectListas.selectedOptions).map(o => o.value) : [];
        
        if (listasSeleccionadas.length === 0) {
            e.preventDefault();
            mostrarError('Seleccione al menos una lista de precios');
            elementos.selectListas?.focus();
            return false;
        }

        if (alcance === 'seleccionados') {
            // Modo Seleccionados: validar que haya productos
            if (productosSeleccionados.size === 0) {
                e.preventDefault();
                mostrarError('Seleccione al menos un producto o elija "Resultados filtrados"');
                return false;
            }
            
            // Preparar IDs para envío
            const ids = Array.from(productosSeleccionados.keys());
            if (elementos.hiddenProductoIds) {
                elementos.hiddenProductoIds.value = ids.join(',');
            }
            
            // Limpiar filtros (no aplican en este modo)
            if (elementos.hiddenFiltrosJson) {
                elementos.hiddenFiltrosJson.value = '';
            }

            console.log(`[CatalogoSeleccion] Enviando ${ids.length} productos seleccionados`);
        } else {
            // Modo Filtrados: enviar filtros actuales, sin IDs específicos
            if (elementos.hiddenProductoIds) {
                elementos.hiddenProductoIds.value = '';
            }
            
            // El filtrosJson ya está poblado desde el servidor
            // Verificar que existe
            const filtrosJson = elementos.hiddenFiltrosJson?.value;
            if (!filtrosJson) {
                console.warn('[CatalogoSeleccion] Sin filtros JSON, se usará filtro vacío');
            }

            console.log('[CatalogoSeleccion] Enviando con filtros actuales:', filtrosJson);
        }

        // Deshabilitar botón para evitar doble submit
        if (elementos.btnSimular) {
            elementos.btnSimular.disabled = true;
            elementos.btnSimular.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Procesando...';
        }

        // El form se enviará normalmente
        return true;
    }

    /**
     * Mostrar mensaje de error temporal
     * @param {string} mensaje - El mensaje a mostrar
     */
    function mostrarError(mensaje) {
        // Usar toastr si está disponible, sino alert
        if (typeof toastr !== 'undefined') {
            toastr.error(mensaje);
        } else {
            alert(mensaje);
        }
    }

    /**
     * Actualizar estado del checkbox master
     */
    function actualizarCheckMaster() {
        if (!elementos.checkMaster) return;

        const checkboxes = elementos.tabla.querySelectorAll('.check-producto');
        const totalCheckboxes = checkboxes.length;
        const checkedCount = elementos.tabla.querySelectorAll('.check-producto:checked').length;

        elementos.checkMaster.checked = totalCheckboxes > 0 && checkedCount === totalCheckboxes;
        elementos.checkMaster.indeterminate = checkedCount > 0 && checkedCount < totalCheckboxes;
    }

    /**
     * Actualizar UI (contadores, visibilidad, botones)
     */
    function actualizarUI() {
        const count = productosSeleccionados.size;

        // Actualizar contadores
        if (elementos.contadorSeleccionados) {
            elementos.contadorSeleccionados.textContent = count;
        }

        // Actualizar opción del select
        if (elementos.selectAlcance) {
            const opcionSeleccionados = elementos.selectAlcance.querySelector('option[value="seleccionados"]');
            if (opcionSeleccionados) {
                opcionSeleccionados.textContent = `Seleccionados (${count})`;
            }
        }

        // Mostrar/ocultar badge y botón limpiar
        if (elementos.badgeSeleccionados) {
            elementos.badgeSeleccionados.style.display = count > 0 ? 'inline-block' : 'none';
        }
        if (elementos.btnLimpiar) {
            elementos.btnLimpiar.style.display = count > 0 ? 'inline-block' : 'none';
        }

        // Actualizar estado del botón simular
        actualizarBotonSimular();

        // Actualizar modal (si existe)
        actualizarModal();
    }

    /**
     * Actualizar estado del botón Aplicar
     */
    function actualizarBotonAplicar() {
        if (!elementos.btnAplicar) return;

        const alcance = elementos.selectAlcance?.value;
        const valor = elementos.inputPorcentaje?.value;
        const tieneValor = valor && valor !== '0' && validarPorcentaje(valor);
        
        let habilitado = false;

        if (alcance === 'seleccionados') {
            habilitado = tieneValor && productosSeleccionados.size > 0;
        } else {
            // Filtrados siempre tiene productos (según TotalResultados)
            habilitado = tieneValor;
        }

        elementos.btnAplicar.disabled = !habilitado;
    }

    // Alias para compatibilidad
    function actualizarBotonSimular() {
        actualizarBotonAplicar();
    }

    /**
     * Actualizar modal con productos seleccionados
     */
    function actualizarModal() {
        if (elementos.modalContador) {
            elementos.modalContador.textContent = productosSeleccionados.size;
        }

        if (elementos.modalListaProductos) {
            const items = [];
            productosSeleccionados.forEach((data, id) => {
                items.push(`${data.codigo} - ${data.nombre}`);
            });
            
            if (items.length <= 5) {
                elementos.modalListaProductos.textContent = items.join(', ');
            } else {
                elementos.modalListaProductos.textContent = items.slice(0, 5).join(', ') + ` y ${items.length - 5} más...`;
            }
        }
    }

    /**
     * Limpiar toda la selección
     */
    function limpiarSeleccion() {
        productosSeleccionados.clear();

        // Desmarcar todos los checkboxes
        const checkboxes = elementos.tabla?.querySelectorAll('.check-producto');
        checkboxes?.forEach(cb => {
            cb.checked = false;
            cb.closest('tr')?.classList.remove('table-active');
        });

        // Desmarcar master
        if (elementos.checkMaster) {
            elementos.checkMaster.checked = false;
            elementos.checkMaster.indeterminate = false;
        }

        actualizarUI();
    }

    /**
     * Obtener productos seleccionados
     * @returns {Map} Map con id => {codigo, nombre}
     */
    function getProductosSeleccionados() {
        return new Map(productosSeleccionados);
    }

    /**
     * Obtener IDs seleccionados como array
     * @returns {string[]} Array de IDs
     */
    function getIdsSeleccionados() {
        return Array.from(productosSeleccionados.keys());
    }

    // API pública
    return {
        init,
        limpiarSeleccion,
        getProductosSeleccionados,
        getIdsSeleccionados
    };
})();

// Auto-inicializar cuando el DOM está listo
document.addEventListener('DOMContentLoaded', function () {
    CatalogoSeleccion.init();
});
