/**
 * Configuración de recargos y descuentos por tipo de pago
 */

// Enum de modo de ajuste
const ModoAjuste = {
    SinAjuste: 0,
    Descuento: 1,
    Recargo: 2
};

// Mapeo de nombres de tipos de pago
const tiposPagoNombres = {
    0: 'Efectivo',
    1: 'Transferencia',
    2: 'Tarjeta Débito',
    3: 'Tarjeta Crédito',
    4: 'Cheque',
    5: 'Crédito Personal',
    6: 'MercadoPago',
    7: 'Cuenta Corriente',
    8: 'Tarjeta'
};

// Tipos de pago que se configurarán en el modal (según requerimiento)
const tiposConfigurables = [0, 2, 1, 6, 3]; // Efectivo, Tarjeta Débito, Transferencia, MercadoPago, Tarjeta Crédito

let configuracionesActuales = [];
let configuracionesTarjeta = []; // Para almacenar las configuraciones de tarjetas de crédito
let perfilesCredito = []; // Para almacenar los perfiles de crédito
let defaultsGlobales = {}; // Para almacenar defaults globales de crédito personal

// Función para abrir el modal y cargar datos
document.getElementById('btnConfigPago')?.addEventListener('click', function () {
    const modal = new bootstrap.Modal(document.getElementById('modalConfigPago'));
    modal.show();
    cargarConfiguraciones();
});

// Función para cargar las configuraciones existentes
async function cargarConfiguraciones() {
    const contentDiv = document.getElementById('configPagoContent');

    try {
        // Cargar configuraciones de tipos de pago
        const response = await fetch('/ConfiguracionPago/GetConfiguracionesModal');
        const result = await response.json();

        if (!result.success) {
            throw new Error(result.message || 'Error al cargar configuraciones');
        }

        configuracionesActuales = result.data || [];
        
        // Extraer defaults globales de Crédito Personal (TipoPago = 5)
        const configCreditoPersonal = configuracionesActuales.find(c => c.tipoPago === 5);
        if (configCreditoPersonal) {
            defaultsGlobales = {
                id: configCreditoPersonal.id,
                tasaMensual: configCreditoPersonal.tasaInteresMensualCreditoPersonal || 0,
                gastos: configCreditoPersonal.gastosAdministrativosDefaultCreditoPersonal || 0,
                minCuotas: configCreditoPersonal.minCuotasDefaultCreditoPersonal || 1,
                maxCuotas: configCreditoPersonal.maxCuotasDefaultCreditoPersonal || 24
            };
        }
        
        // Cargar perfiles de crédito
        const perfilesResponse = await fetch('/ConfiguracionPago/GetPerfilesCredito');
        const perfilesResult = await perfilesResponse.json();
        
        if (perfilesResult.success) {
            perfilesCredito = perfilesResult.data || [];
        }
        
        renderizarFormulario();

    } catch (error) {
        console.error('Error al cargar configuraciones:', error);
        contentDiv.innerHTML = `
            <div class="alert alert-danger">
                <i class="bi bi-exclamation-triangle me-2"></i>
                Error al cargar las configuraciones: ${error.message}
            </div>
        `;
    }
}

// Función para renderizar el formulario de configuración
function renderizarFormulario() {
    const contentDiv = document.getElementById('configPagoContent');
    let html = '<form id="formConfigPago" class="row g-4">';

    tiposConfigurables.forEach(tipoPago => {
        const config = configuracionesActuales.find(c => c.tipoPago === tipoPago) || crearConfiguracionVacia(tipoPago);
        html += generarSeccionTipoPago(tipoPago, config);
    });

    // Agregar sección de Crédito Personal (TAREA 7)
    html += generarSeccionCreditoPersonal();

    html += '</form>';
    contentDiv.innerHTML = html;

    // Agregar event listeners para mostrar/ocultar campos
    agregarEventListeners();
    agregarEventListenersCreditoPersonal();
}

// Función para crear una configuración vacía si no existe
function crearConfiguracionVacia(tipoPago) {
    return {
        id: 0,
        tipoPago: tipoPago,
        nombre: tiposPagoNombres[tipoPago],
        activo: true,
        permiteDescuento: false,
        porcentajeDescuentoMaximo: null,
        tieneRecargo: false,
        porcentajeRecargo: null,
        configuracionesTarjeta: []
    };
}

// Función para obtener el modo de ajuste actual de una configuración
function obtenerModoAjuste(config) {
    if (config.tieneRecargo && config.porcentajeRecargo > 0) {
        return ModoAjuste.Recargo;
    } else if (config.permiteDescuento && config.porcentajeDescuentoMaximo > 0) {
        return ModoAjuste.Descuento;
    }
    return ModoAjuste.SinAjuste;
}

// Función para generar HTML de cada sección de tipo de pago
function generarSeccionTipoPago(tipoPago, config) {
    const nombre = tiposPagoNombres[tipoPago];
    const iconClass = obtenerIconoTipoPago(tipoPago);
    const modoActual = obtenerModoAjuste(config);

    let html = `
        <div class="col-12">
            <div class="card bg-body-secondary border-0 shadow-sm">
                <div class="card-header bg-secondary text-light">
                    <h6 class="mb-0">
                        <i class="${iconClass} me-2"></i>${nombre}
                    </h6>
                </div>
                <div class="card-body">
                    <input type="hidden" id="config_${tipoPago}_id" value="${config.id}">
                    <input type="hidden" id="config_${tipoPago}_tipo" value="${tipoPago}">
                    
                    <div class="row g-3">
    `;

    // Configuración específica según tipo de pago
    if (tipoPago === 3) { // Tarjeta Crédito
        html += generarConfiguracionTarjetas(tipoPago, config);
    } else {
        // Selector de modo para otros tipos de pago
        html += generarSelectorModo(tipoPago, modoActual);
        html += generarCamposAjuste(tipoPago, modoActual, config);
    }

    html += `
                    </div>
                </div>
            </div>
        </div>
    `;

    return html;
}

// Función para generar selector de modo
function generarSelectorModo(tipoPago, modoActual) {
    return `
        <div class="col-12">
            <label class="form-label fw-bold">Modo de Ajuste</label>
            <select class="form-select bg-dark text-light" id="config_${tipoPago}_modo" data-tipo="${tipoPago}">
                <option value="${ModoAjuste.SinAjuste}" ${modoActual === ModoAjuste.SinAjuste ? 'selected' : ''}>Sin Ajuste</option>
                <option value="${ModoAjuste.Descuento}" ${modoActual === ModoAjuste.Descuento ? 'selected' : ''}>Descuento</option>
                <option value="${ModoAjuste.Recargo}" ${modoActual === ModoAjuste.Recargo ? 'selected' : ''}>Recargo</option>
            </select>
        </div>
    `;
}

// Función para generar campos de ajuste
function generarCamposAjuste(tipoPago, modoActual, config) {
    const porcentajeDescuento = config.porcentajeDescuentoMaximo || 0;
    const porcentajeRecargo = config.porcentajeRecargo || 0;
    
    return `
        <div class="col-md-6" id="group_${tipoPago}_descuento" style="${modoActual === ModoAjuste.Descuento ? '' : 'display: none;'}">
            <label class="form-label">Porcentaje de Descuento (%)</label>
            <input type="number" class="form-control bg-dark text-light" 
                id="config_${tipoPago}_porcDescuento" 
                min="0" max="100" step="0.01" 
                value="${porcentajeDescuento}"
                ${modoActual !== ModoAjuste.Descuento ? 'disabled' : ''}>
        </div>
        <div class="col-md-6" id="group_${tipoPago}_recargo" style="${modoActual === ModoAjuste.Recargo ? '' : 'display: none;'}">
            <label class="form-label">Porcentaje de Recargo (%)</label>
            <input type="number" class="form-control bg-dark text-light" 
                id="config_${tipoPago}_porcRecargo" 
                min="0" max="100" step="0.01" 
                value="${porcentajeRecargo}"
                ${modoActual !== ModoAjuste.Recargo ? 'disabled' : ''}>
        </div>
    `;
}

// Función para generar configuración de tarjetas (COMPLETA en el modal)
function generarConfiguracionTarjetas(tipoPago, config) {
    const tarjetas = config.configuracionesTarjeta || [];
    
    let html = `
        <div class="col-12">
            <div class="d-flex justify-content-between align-items-center mb-3">
                <h6 class="mb-0">
                    <i class="bi bi-credit-card me-2"></i>
                    Configuraciones de Tarjetas de Crédito
                    <span class="badge bg-primary ms-2">${tarjetas.length} activas</span>
                </h6>
                <button type="button" class="btn btn-sm btn-primary" onclick="agregarReglaTarjeta()">
                    <i class="bi bi-plus-circle me-1"></i> Nueva Regla
                </button>
            </div>
            
            <div class="table-responsive">
                <table class="table table-dark table-hover table-sm mb-0" id="tablaTarjetas">
                    <thead>
                        <tr>
                            <th>Banco</th>
                            <th>Cuotas</th>
                            <th>Modo</th>
                            <th>Porcentaje (%)</th>
                            <th class="text-center">Acciones</th>
                        </tr>
                    </thead>
                    <tbody id="tarjetasBody">
    `;
    
    if (tarjetas.length === 0) {
        html += `
                        <tr>
                            <td colspan="5" class="text-center text-muted py-3">
                                <i class="bi bi-inbox display-6"></i>
                                <p class="mt-2 mb-0">No hay configuraciones. Agregue una nueva regla.</p>
                            </td>
                        </tr>
        `;
    } else {
        tarjetas.forEach((tarjeta, index) => {
            html += generarFilaTarjeta(tarjeta, index);
        });
    }
    
    html += `
                    </tbody>
                </table>
            </div>
        </div>
    `;
    
    return html;
}

// Función para generar una fila de tarjeta
function generarFilaTarjeta(tarjeta, index) {
    const modoTexto = tarjeta.tieneRecargoDebito 
        ? 'Recargo' 
        : (tarjeta.porcentajeDescuento > 0 ? 'Descuento' : 'Sin Ajuste');
    
    const porcentaje = tarjeta.tieneRecargoDebito 
        ? tarjeta.porcentajeRecargoDebito 
        : tarjeta.porcentajeDescuento;
    
    return `
        <tr data-index="${index}" data-id="${tarjeta.id || 0}">
            <td>${tarjeta.nombreTarjeta || tarjeta.banco || 'N/A'}</td>
            <td>${tarjeta.cantidadMaximaCuotas || tarjeta.cuotas || 'N/A'}</td>
            <td><span class="badge bg-${modoTexto === 'Recargo' ? 'danger' : (modoTexto === 'Descuento' ? 'success' : 'secondary')}">${modoTexto}</span></td>
            <td>${porcentaje || 0}%</td>
            <td class="text-center">
                <button type="button" class="btn btn-sm btn-outline-warning" onclick="editarReglaTarjeta(${index})">
                    <i class="bi bi-pencil"></i>
                </button>
                <button type="button" class="btn btn-sm btn-outline-danger" onclick="eliminarReglaTarjeta(${index})">
                    <i class="bi bi-trash"></i>
                </button>
            </td>
        </tr>
    `;
}

// Función para obtener el ícono según el tipo de pago
function obtenerIconoTipoPago(tipoPago) {
    const iconos = {
        0: 'bi bi-cash-coin',
        1: 'bi bi-arrow-left-right',
        2: 'bi bi-credit-card-2-front',
        3: 'bi bi-credit-card',
        6: 'bi bi-phone'
    };
    return iconos[tipoPago] || 'bi bi-currency-dollar';
}

// Función para agregar event listeners
function agregarEventListeners() {
    tiposConfigurables.forEach(tipoPago => {
        if (tipoPago === 3) return; // Tarjeta Crédito se maneja aparte
        
        // Listener para el selector de modo
        const selectModo = document.getElementById(`config_${tipoPago}_modo`);
        if (selectModo) {
            selectModo.addEventListener('change', function () {
                const modo = parseInt(this.value);
                actualizarCamposPorModo(tipoPago, modo);
            });
        }
    });
}

// Función para actualizar campos según el modo seleccionado
function actualizarCamposPorModo(tipoPago, modo) {
    const groupDescuento = document.getElementById(`group_${tipoPago}_descuento`);
    const groupRecargo = document.getElementById(`group_${tipoPago}_recargo`);
    const inputDescuento = document.getElementById(`config_${tipoPago}_porcDescuento`);
    const inputRecargo = document.getElementById(`config_${tipoPago}_porcRecargo`);
    
    // Ocultar/mostrar y habilitar/deshabilitar según el modo
    if (modo === ModoAjuste.Descuento) {
        if (groupDescuento) {
            groupDescuento.style.display = '';
            if (inputDescuento) inputDescuento.disabled = false;
        }
        if (groupRecargo) {
            groupRecargo.style.display = 'none';
            if (inputRecargo) {
                inputRecargo.disabled = true;
                inputRecargo.value = 0;
            }
        }
    } else if (modo === ModoAjuste.Recargo) {
        if (groupDescuento) {
            groupDescuento.style.display = 'none';
            if (inputDescuento) {
                inputDescuento.disabled = true;
                inputDescuento.value = 0;
            }
        }
        if (groupRecargo) {
            groupRecargo.style.display = '';
            if (inputRecargo) inputRecargo.disabled = false;
        }
    } else { // SinAjuste
        if (groupDescuento) {
            groupDescuento.style.display = 'none';
            if (inputDescuento) {
                inputDescuento.disabled = true;
                inputDescuento.value = 0;
            }
        }
        if (groupRecargo) {
            groupRecargo.style.display = 'none';
            if (inputRecargo) {
                inputRecargo.disabled = true;
                inputRecargo.value = 0;
            }
        }
    }
}

// ============================================================================
// FUNCIONES CRUD PARA TARJETAS DE CRÉDITO
// ============================================================================

// Función para agregar una nueva regla de tarjeta
function agregarReglaTarjeta() {
    const config = configuracionesActuales.find(c => c.tipoPago === 3) || crearConfiguracionVacia(3);
    
    // Crear modal inline para nueva regla
    mostrarModalReglaTarjeta(null, (regla) => {
        if (!config.configuracionesTarjeta) {
            config.configuracionesTarjeta = [];
        }
        config.configuracionesTarjeta.push(regla);
        
        // Actualizar la configuración actual
        const index = configuracionesActuales.findIndex(c => c.tipoPago === 3);
        if (index >= 0) {
            configuracionesActuales[index] = config;
        } else {
            configuracionesActuales.push(config);
        }
        
        // Re-renderizar solo la sección de tarjetas
        renderizarFormulario();
    });
}

// Función para editar una regla de tarjeta
function editarReglaTarjeta(index) {
    const config = configuracionesActuales.find(c => c.tipoPago === 3);
    if (!config || !config.configuracionesTarjeta[index]) return;
    
    const regla = config.configuracionesTarjeta[index];
    
    mostrarModalReglaTarjeta(regla, (reglaEditada) => {
        config.configuracionesTarjeta[index] = reglaEditada;
        renderizarFormulario();
    });
}

// Función para eliminar una regla de tarjeta
function eliminarReglaTarjeta(index) {
    if (!confirm('¿Está seguro de eliminar esta regla de tarjeta?')) return;
    
    const config = configuracionesActuales.find(c => c.tipoPago === 3);
    if (!config || !config.configuracionesTarjeta[index]) return;
    
    config.configuracionesTarjeta.splice(index, 1);
    renderizarFormulario();
}

// Función para mostrar modal de edición de regla de tarjeta
function mostrarModalReglaTarjeta(reglaExistente, callback) {
    const isEdit = reglaExistente !== null;
    const regla = reglaExistente || {
        id: 0,
        nombreTarjeta: '',
        banco: '',
        cantidadMaximaCuotas: 1,
        cuotas: 1,
        modo: ModoAjuste.SinAjuste,
        porcentaje: 0,
        tieneRecargoDebito: false,
        porcentajeRecargoDebito: 0,
        porcentajeDescuento: 0
    };
    
    // Determinar modo actual
    let modoActual = ModoAjuste.SinAjuste;
    if (regla.tieneRecargoDebito && regla.porcentajeRecargoDebito > 0) {
        modoActual = ModoAjuste.Recargo;
    } else if (regla.porcentajeDescuento > 0) {
        modoActual = ModoAjuste.Descuento;
    }
    
    const porcentaje = modoActual === ModoAjuste.Recargo 
        ? regla.porcentajeRecargoDebito 
        : regla.porcentajeDescuento;
    
    // Crear modal inline
    const modalHtml = `
        <div class="modal fade" id="modalReglaTarjeta" tabindex="-1">
            <div class="modal-dialog">
                <div class="modal-content bg-dark text-light">
                    <div class="modal-header bg-secondary">
                        <h5 class="modal-title">
                            <i class="bi bi-credit-card me-2"></i>
                            ${isEdit ? 'Editar' : 'Nueva'} Regla de Tarjeta
                        </h5>
                        <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
                    </div>
                    <div class="modal-body">
                        <div class="mb-3">
                            <label class="form-label">Banco / Nombre de Tarjeta</label>
                            <input type="text" class="form-control bg-body-secondary text-light" 
                                id="reglaBanco" value="${regla.nombreTarjeta || regla.banco || ''}" required>
                        </div>
                        <div class="mb-3">
                            <label class="form-label">Cantidad de Cuotas</label>
                            <input type="number" class="form-control bg-body-secondary text-light" 
                                id="reglaCuotas" value="${regla.cantidadMaximaCuotas || regla.cuotas || 1}" 
                                min="1" max="60" required>
                        </div>
                        <div class="mb-3">
                            <label class="form-label">Modo de Ajuste</label>
                            <select class="form-select bg-body-secondary text-light" id="reglaModo">
                                <option value="${ModoAjuste.SinAjuste}" ${modoActual === ModoAjuste.SinAjuste ? 'selected' : ''}>Sin Ajuste</option>
                                <option value="${ModoAjuste.Descuento}" ${modoActual === ModoAjuste.Descuento ? 'selected' : ''}>Descuento</option>
                                <option value="${ModoAjuste.Recargo}" ${modoActual === ModoAjuste.Recargo ? 'selected' : ''}>Recargo</option>
                            </select>
                        </div>
                        <div class="mb-3" id="reglaPorcentajeGroup" style="${modoActual !== ModoAjuste.SinAjuste ? '' : 'display: none;'}">
                            <label class="form-label">Porcentaje (%)</label>
                            <input type="number" class="form-control bg-body-secondary text-light" 
                                id="reglaPorcentaje" value="${porcentaje || 0}" 
                                min="0" max="100" step="0.01">
                        </div>
                    </div>
                    <div class="modal-footer bg-secondary">
                        <button type="button" class="btn btn-outline-secondary" data-bs-dismiss="modal">Cancelar</button>
                        <button type="button" class="btn btn-primary" id="btnGuardarRegla">Guardar</button>
                    </div>
                </div>
            </div>
        </div>
    `;
    
    // Agregar modal al DOM
    const modalDiv = document.createElement('div');
    modalDiv.innerHTML = modalHtml;
    document.body.appendChild(modalDiv.firstElementChild);
    
    const modal = new bootstrap.Modal(document.getElementById('modalReglaTarjeta'));
    modal.show();
    
    // Event listener para el selector de modo
    document.getElementById('reglaModo').addEventListener('change', function() {
        const grupo = document.getElementById('reglaPorcentajeGroup');
        if (parseInt(this.value) === ModoAjuste.SinAjuste) {
            grupo.style.display = 'none';
            document.getElementById('reglaPorcentaje').value = 0;
        } else {
            grupo.style.display = '';
        }
    });
    
    // Event listener para guardar
    document.getElementById('btnGuardarRegla').addEventListener('click', function() {
        const banco = document.getElementById('reglaBanco').value.trim();
        const cuotas = parseInt(document.getElementById('reglaCuotas').value);
        const modo = parseInt(document.getElementById('reglaModo').value);
        const porcentaje = parseFloat(document.getElementById('reglaPorcentaje').value) || 0;
        
        if (!banco) {
            alert('Debe ingresar el nombre del banco o tarjeta');
            return;
        }
        
        if (cuotas < 1) {
            alert('La cantidad de cuotas debe ser al menos 1');
            return;
        }
        
        // Construir la regla
        const nuevaRegla = {
            id: regla.id || 0,
            nombreTarjeta: banco,
            banco: banco,
            cantidadMaximaCuotas: cuotas,
            cuotas: cuotas,
            tieneRecargoDebito: modo === ModoAjuste.Recargo,
            porcentajeRecargoDebito: modo === ModoAjuste.Recargo ? porcentaje : 0,
            porcentajeDescuento: modo === ModoAjuste.Descuento ? porcentaje : 0,
            activa: true,
            permiteCuotas: true,
            tipoCuota: 0,
            tasaInteresesMensual: 0,
            tipoTarjeta: 0
        };
        
        callback(nuevaRegla);
        modal.hide();
        
        // Limpiar modal del DOM después de cerrar
        document.getElementById('modalReglaTarjeta').addEventListener('hidden.bs.modal', function() {
            this.remove();
        });
    });
}

// ============================================================================
// FIN FUNCIONES CRUD TARJETAS
// ============================================================================

// Función para guardar las configuraciones
document.getElementById('btnGuardarConfig')?.addEventListener('click', async function () {
    const btn = this;
    btn.disabled = true;
    const originalText = btn.innerHTML;
    btn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Guardando...';

    try {
        const configuraciones = [];

        tiposConfigurables.forEach(tipoPago => {
            const id = parseInt(document.getElementById(`config_${tipoPago}_id`)?.value || '0');
            const permiteDescuento = document.getElementById(`config_${tipoPago}_descuento`)?.checked || false;
            const tieneRecargo = document.getElementById(`config_${tipoPago}_recargo`)?.checked || false;

            const config = {
                id: id,
                tipoPago: tipoPago,
                nombre: tiposPagoNombres[tipoPago],
                activo: true,
                permiteDescuento: permiteDescuento,
                porcentajeDescuentoMaximo: permiteDescuento 
                    ? parseFloat(document.getElementById(`config_${tipoPago}_porcDescuento`)?.value || '0') 
                    : null,
                tieneRecargo: tieneRecargo,
                porcentajeRecargo: tieneRecargo 
                    ? parseFloat(document.getElementById(`config_${tipoPago}_porcRecargo`)?.value || '0') 
                    : null
            };

            configuraciones.push(config);
        });

        // Guardar configuraciones de tipos de pago
        const response = await fetch('/ConfiguracionPago/GuardarConfiguracionesModal', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(configuraciones)
        });

        const result = await response.json();

        if (!result.success) {
            throw new Error(result.message || 'Error al guardar configuraciones de tipos de pago');
        }
        
        // Guardar configuración de Crédito Personal (TAREA 7)
        const creditoPersonalConfig = {
            defaultsGlobales: {
                tasaMensual: parseFloat(document.getElementById('cp_tasa_default')?.value || '0'),
                gastosAdministrativos: parseFloat(document.getElementById('cp_gastos_default')?.value || '0'),
                minCuotas: parseInt(document.getElementById('cp_min_cuotas_default')?.value || '1'),
                maxCuotas: parseInt(document.getElementById('cp_max_cuotas_default')?.value || '24')
            },
            perfiles: perfilesCredito
        };
        
        const cpResponse = await fetch('/ConfiguracionPago/GuardarCreditoPersonalModal', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(creditoPersonalConfig)
        });
        
        const cpResult = await cpResponse.json();
        
        if (!cpResult.success) {
            throw new Error(cpResult.message || 'Error al guardar configuración de crédito personal');
        }
        
        // Mostrar mensaje de éxito
        mostrarNotificacion('success', 'Todas las configuraciones guardadas exitosamente');
        
        // Cerrar modal
        const modal = bootstrap.Modal.getInstance(document.getElementById('modalConfigPago'));
        modal.hide();

    } catch (error) {
        console.error('Error al guardar:', error);
        mostrarNotificacion('danger', 'Error al guardar las configuraciones: ' + error.message);
    } finally {
        btn.disabled = false;
        btn.innerHTML = originalText;
    }
});

// Función para mostrar notificaciones
function mostrarNotificacion(tipo, mensaje) {
    const alertDiv = document.createElement('div');
    alertDiv.className = `alert alert-${tipo} alert-dismissible fade show position-fixed top-0 start-50 translate-middle-x mt-3`;
    alertDiv.style.zIndex = '9999';
    alertDiv.innerHTML = `
        <i class="bi bi-${tipo === 'success' ? 'check-circle' : 'exclamation-triangle'} me-2"></i>
        ${mensaje}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;
    
    document.body.appendChild(alertDiv);
    
    setTimeout(() => {
        alertDiv.remove();
    }, 5000);
}

// ============================================================
// TAREA 7: CRÉDITO PERSONAL - DEFAULTS GLOBALES + PERFILES
// ============================================================

/**
 * Genera la sección de Crédito Personal con:
 * - 7.1.1 Defaults Globales (fallback)
 * - 7.1.2 Perfiles/Planes de crédito
 */
function generarSeccionCreditoPersonal() {
    return `
        <div class="col-12">
            <div class="card bg-body-secondary border-0 shadow-sm">
                <div class="card-header bg-warning text-dark">
                    <h5 class="mb-0">
                        <i class="bi bi-wallet2 me-2"></i>Crédito Personal
                    </h5>
                </div>
                <div class="card-body">
                    <div class="row g-4">
                        ${generarDefaultsGlobales()}
                        ${generarPerfilesCredito()}
                    </div>
                </div>
            </div>
        </div>
    `;
}

/**
 * 7.1.1 Defaults Globales (fallback)
 */
function generarDefaultsGlobales() {
    return `
        <div class="col-12">
            <div class="card bg-dark border-info">
                <div class="card-header bg-info text-dark">
                    <h6 class="mb-0">
                        <i class="bi bi-globe me-2"></i>Defaults Globales (Fallback)
                        <small class="ms-2 badge bg-dark">Usado cuando no hay perfil seleccionado</small>
                    </h6>
                </div>
                <div class="card-body">
                    <div class="row g-3">
                        <div class="col-md-3">
                            <label class="form-label">Tasa Mensual Default (%)</label>
                            <input type="number" class="form-control bg-body-secondary text-light" 
                                id="cp_tasa_default" 
                                min="0" max="100" step="0.01" 
                                value="${defaultsGlobales.tasaMensual || 0}"
                                placeholder="Ej: 7.50">
                            <small class="text-muted">Tasa de interés mensual por defecto</small>
                        </div>
                        <div class="col-md-3">
                            <label class="form-label">Gastos Admin. Default ($)</label>
                            <input type="number" class="form-control bg-body-secondary text-light" 
                                id="cp_gastos_default" 
                                min="0" step="0.01" 
                                value="${defaultsGlobales.gastos || 0}"
                                placeholder="Ej: 500.00">
                            <small class="text-muted">Monto fijo de gastos administrativos</small>
                        </div>
                        <div class="col-md-3">
                            <label class="form-label">Min Cuotas Default</label>
                            <input type="number" class="form-control bg-body-secondary text-light" 
                                id="cp_min_cuotas_default" 
                                min="1" max="120" 
                                value="${defaultsGlobales.minCuotas || 1}"
                                placeholder="Ej: 1">
                            <small class="text-muted">Mínimo de cuotas permitidas</small>
                        </div>
                        <div class="col-md-3">
                            <label class="form-label">Max Cuotas Default</label>
                            <input type="number" class="form-control bg-body-secondary text-light" 
                                id="cp_max_cuotas_default" 
                                min="1" max="120" 
                                value="${defaultsGlobales.maxCuotas || 24}"
                                placeholder="Ej: 24">
                            <small class="text-muted">Máximo de cuotas permitidas</small>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    `;
}

/**
 * 7.1.2 Perfiles/Planes de crédito
 */
function generarPerfilesCredito() {
    const perfilesOrdenados = perfilesCredito.sort((a, b) => a.orden - b.orden);
    
    let html = `
        <div class="col-12">
            <div class="card bg-dark border-success">
                <div class="card-header bg-success text-dark">
                    <div class="d-flex justify-content-between align-items-center">
                        <h6 class="mb-0">
                            <i class="bi bi-list-stars me-2"></i>Perfiles de Crédito
                            <span class="badge bg-dark ms-2">${perfilesOrdenados.length} perfiles</span>
                        </h6>
                        <button type="button" class="btn btn-sm btn-dark" onclick="agregarPerfilCredito()">
                            <i class="bi bi-plus-circle me-1"></i> Nuevo Perfil
                        </button>
                    </div>
                </div>
                <div class="card-body">
                    <div class="table-responsive">
                        <table class="table table-dark table-hover table-sm mb-0" id="tablaPerfiles">
                            <thead>
                                <tr>
                                    <th style="width: 30px;">#</th>
                                    <th>Nombre</th>
                                    <th>Tasa (%)</th>
                                    <th>Gastos ($)</th>
                                    <th>Min Cuotas</th>
                                    <th>Max Cuotas</th>
                                    <th class="text-center">Activo</th>
                                    <th class="text-center">Acciones</th>
                                </tr>
                            </thead>
                            <tbody id="perfilesBody">
    `;
    
    if (perfilesOrdenados.length === 0) {
        html += `
                                <tr>
                                    <td colspan="8" class="text-center text-muted py-4">
                                        <i class="bi bi-inbox display-6"></i>
                                        <p class="mt-2 mb-0">No hay perfiles configurados. Agregue uno nuevo.</p>
                                    </td>
                                </tr>
        `;
    } else {
        perfilesOrdenados.forEach((perfil, index) => {
            html += generarFilaPerfil(perfil, index);
        });
    }
    
    html += `
                            </tbody>
                        </table>
                    </div>
                </div>
            </div>
        </div>
    `;
    
    return html;
}

/**
 * Genera una fila de perfil de crédito
 */
function generarFilaPerfil(perfil, index) {
    const badgeActivo = perfil.activo 
        ? '<span class="badge bg-success">Sí</span>' 
        : '<span class="badge bg-secondary">No</span>';
    
    return `
        <tr data-perfil-id="${perfil.id}">
            <td>${index + 1}</td>
            <td>
                <strong>${perfil.nombre}</strong>
                ${perfil.descripcion ? `<br><small class="text-muted">${perfil.descripcion}</small>` : ''}
            </td>
            <td>${perfil.tasaMensual.toFixed(2)}%</td>
            <td>$${perfil.gastosAdministrativos.toFixed(2)}</td>
            <td>${perfil.minCuotas}</td>
            <td>${perfil.maxCuotas}</td>
            <td class="text-center">${badgeActivo}</td>
            <td class="text-center">
                <div class="btn-group btn-group-sm">
                    <button type="button" class="btn btn-outline-info" onclick="editarPerfil(${perfil.id})" title="Editar">
                        <i class="bi bi-pencil"></i>
                    </button>
                    <button type="button" class="btn btn-outline-danger" onclick="eliminarPerfil(${perfil.id})" title="Eliminar">
                        <i class="bi bi-trash"></i>
                    </button>
                </div>
            </td>
        </tr>
    `;
}

/**
 * Event listeners para la sección de Crédito Personal
 */
function agregarEventListenersCreditoPersonal() {
    // Validación de rangos de cuotas
    const minCuotasInput = document.getElementById('cp_min_cuotas_default');
    const maxCuotasInput = document.getElementById('cp_max_cuotas_default');
    
    if (minCuotasInput && maxCuotasInput) {
        minCuotasInput.addEventListener('change', function() {
            const min = parseInt(this.value) || 1;
            const max = parseInt(maxCuotasInput.value) || 24;
            if (min > max) {
                maxCuotasInput.value = min;
            }
        });
        
        maxCuotasInput.addEventListener('change', function() {
            const min = parseInt(minCuotasInput.value) || 1;
            const max = parseInt(this.value) || 24;
            if (max < min) {
                minCuotasInput.value = max;
            }
        });
    }
}

/**
 * Agregar nuevo perfil de crédito
 */
function agregarPerfilCredito() {
    // Crear formulario en modal o inline
    const tbody = document.getElementById('perfilesBody');
    
    // Si ya hay una fila de edición, no agregar otra
    if (tbody.querySelector('.perfil-edicion')) {
        mostrarNotificacion('warning', 'Ya hay un perfil en edición. Guárdelo o cancele primero.');
        return;
    }
    
    const nuevaFila = `
        <tr class="perfil-edicion bg-info bg-opacity-10" data-perfil-id="0">
            <td><i class="bi bi-plus-circle text-info"></i></td>
            <td>
                <input type="text" class="form-control form-control-sm bg-dark text-light" 
                    id="nuevo_perfil_nombre" placeholder="Ej: Estándar, Conservador" required>
                <input type="text" class="form-control form-control-sm bg-dark text-light mt-1" 
                    id="nuevo_perfil_desc" placeholder="Descripción (opcional)">
            </td>
            <td><input type="number" class="form-control form-control-sm bg-dark text-light" 
                id="nuevo_perfil_tasa" min="0" max="100" step="0.01" placeholder="7.50" required></td>
            <td><input type="number" class="form-control form-control-sm bg-dark text-light" 
                id="nuevo_perfil_gastos" min="0" step="0.01" placeholder="500.00" required></td>
            <td><input type="number" class="form-control form-control-sm bg-dark text-light" 
                id="nuevo_perfil_min" min="1" max="120" placeholder="1" required></td>
            <td><input type="number" class="form-control form-control-sm bg-dark text-light" 
                id="nuevo_perfil_max" min="1" max="120" placeholder="24" required></td>
            <td class="text-center">
                <div class="form-check form-switch d-inline-block">
                    <input class="form-check-input" type="checkbox" id="nuevo_perfil_activo" checked>
                </div>
            </td>
            <td class="text-center">
                <div class="btn-group btn-group-sm">
                    <button type="button" class="btn btn-success" onclick="guardarNuevoPerfil()" title="Guardar">
                        <i class="bi bi-check-lg"></i>
                    </button>
                    <button type="button" class="btn btn-secondary" onclick="cancelarNuevoPerfil()" title="Cancelar">
                        <i class="bi bi-x-lg"></i>
                    </button>
                </div>
            </td>
        </tr>
    `;
    
    tbody.insertAdjacentHTML('afterbegin', nuevaFila);
}

/**
 * Guardar nuevo perfil de crédito
 */
async function guardarNuevoPerfil() {
    const nombre = document.getElementById('nuevo_perfil_nombre').value.trim();
    const descripcion = document.getElementById('nuevo_perfil_desc').value.trim();
    const tasa = parseFloat(document.getElementById('nuevo_perfil_tasa').value);
    const gastos = parseFloat(document.getElementById('nuevo_perfil_gastos').value);
    const minCuotas = parseInt(document.getElementById('nuevo_perfil_min').value);
    const maxCuotas = parseInt(document.getElementById('nuevo_perfil_max').value);
    const activo = document.getElementById('nuevo_perfil_activo').checked;
    
    if (!nombre) {
        mostrarNotificacion('warning', 'El nombre del perfil es requerido');
        return;
    }
    
    if (isNaN(tasa) || tasa < 0 || tasa > 100) {
        mostrarNotificacion('warning', 'La tasa debe estar entre 0% y 100%');
        return;
    }
    
    if (isNaN(gastos) || gastos < 0) {
        mostrarNotificacion('warning', 'Los gastos deben ser un valor positivo');
        return;
    }
    
    if (isNaN(minCuotas) || minCuotas < 1 || minCuotas > 120) {
        mostrarNotificacion('warning', 'Las cuotas mínimas deben estar entre 1 y 120');
        return;
    }
    
    if (isNaN(maxCuotas) || maxCuotas < 1 || maxCuotas > 120) {
        mostrarNotificacion('warning', 'Las cuotas máximas deben estar entre 1 y 120');
        return;
    }
    
    if (minCuotas > maxCuotas) {
        mostrarNotificacion('warning', 'Las cuotas mínimas no pueden ser mayores a las máximas');
        return;
    }
    
    const nuevoPerfil = {
        id: 0,
        nombre,
        descripcion: descripcion || null,
        tasaMensual: tasa,
        gastosAdministrativos: gastos,
        minCuotas,
        maxCuotas,
        activo,
        orden: perfilesCredito.length
    };
    
    perfilesCredito.push(nuevoPerfil);
    cancelarNuevoPerfil();
    renderizarFormulario();
    mostrarNotificacion('success', `Perfil "${nombre}" agregado (recuerde guardar la configuración)`);
}

/**
 * Cancelar nuevo perfil
 */
function cancelarNuevoPerfil() {
    const fila = document.querySelector('.perfil-edicion');
    if (fila) {
        fila.remove();
    }
}

/**
 * Editar perfil existente
 */
function editarPerfil(id) {
    mostrarNotificacion('info', 'Funcionalidad de edición en desarrollo');
    // TODO: Implementar edición inline similar a agregar
}

/**
 * Eliminar perfil
 */
function eliminarPerfil(id) {
    if (!confirm('¿Está seguro de eliminar este perfil de crédito?')) {
        return;
    }
    
    perfilesCredito = perfilesCredito.filter(p => p.id !== id);
    renderizarFormulario();
    mostrarNotificacion('success', 'Perfil eliminado (recuerde guardar la configuración)');
}

// Fin de la sección de Crédito Personal
