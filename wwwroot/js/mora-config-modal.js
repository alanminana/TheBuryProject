// ============================================================================
// mora-config-modal.js - Gestión de configuración de mora y alertas
// ============================================================================

let configuracionActual = null;
let alertas = [];
let editandoIndex = -1;

const modalConfiguracion = new bootstrap.Modal(document.getElementById('modalConfiguracionMora'));
const modalAlerta = new bootstrap.Modal(document.getElementById('modalAlerta'));

// ============================================================================
// Inicialización
// ============================================================================

document.addEventListener('DOMContentLoaded', function () {
    inicializarEventos();
});

function inicializarEventos() {
    // Botón para abrir modal de configuración
    const btnConfiguracion = document.querySelector('[data-bs-target="#modalConfiguracionMora"]');
    if (btnConfiguracion) {
        btnConfiguracion.addEventListener('click', cargarConfiguracion);
    }

    // Botones del modal principal
    document.getElementById('btnGuardarConfiguracion')?.addEventListener('click', guardarConfiguracion);
    document.getElementById('btnAgregarAlerta')?.addEventListener('click', mostrarModalAlerta);

    // Botones del modal de alerta
    document.getElementById('btnGuardarAlerta')?.addEventListener('click', guardarAlerta);

    // Sincronización de color picker y hex input
    const colorPicker = document.getElementById('alertaColor');
    const colorHex = document.getElementById('alertaColorHex');

    colorPicker?.addEventListener('input', function () {
        colorHex.value = this.value.toUpperCase();
    });

    colorHex?.addEventListener('input', function () {
        const hex = this.value;
        if (/^#[0-9A-Fa-f]{6}$/.test(hex)) {
            colorPicker.value = hex;
        }
    });
}

// ============================================================================
// Carga de configuración
// ============================================================================

async function cargarConfiguracion() {
    try {
        const response = await fetch('/ConfiguracionMora/GetConfiguracion');
        const result = await response.json();

        if (result.success) {
            configuracionActual = result.data;
            alertas = result.data.alertas || [];

            // Llenar campos de configuración base
            document.getElementById('tasaMoraDiaria').value = configuracionActual.tasaMoraDiaria || 0;
            document.getElementById('diasGracia').value = configuracionActual.diasGracia || 0;
            document.getElementById('procesoAutomaticoActivo').checked = configuracionActual.procesoAutomaticoActivo || false;

            // Renderizar alertas
            renderizarTablaAlertas();
        } else {
            mostrarError(result.message || 'Error al cargar la configuración');
        }
    } catch (error) {
        console.error('Error al cargar configuración:', error);
        mostrarError('Error de conexión al cargar la configuración');
    }
}

// ============================================================================
// Renderizado de tabla de alertas
// ============================================================================

function renderizarTablaAlertas() {
    const tbody = document.getElementById('tbodyAlertas');
    if (!tbody) return;

    // Ordenar alertas por días (antes del vencimiento primero, luego después)
    const alertasOrdenadas = [...alertas].sort((a, b) => a.diasRelativoVencimiento - b.diasRelativoVencimiento);

    tbody.innerHTML = alertasOrdenadas.map((alerta, index) => {
        const diasTexto = formatearDias(alerta.diasRelativoVencimiento);
        const prioridadTexto = obtenerTextoPrioridad(alerta.nivelPrioridad);
        const activaIcon = alerta.activa ? '<i class="bi bi-check-circle-fill text-success"></i>' : '<i class="bi bi-x-circle text-secondary"></i>';

        return `
            <tr data-index="${index}">
                <td class="fw-semibold">${diasTexto}</td>
                <td>${alerta.descripcion || '-'}</td>
                <td>
                    <span class="badge" style="background-color: ${alerta.colorAlerta}; color: ${obtenerColorTexto(alerta.colorAlerta)};">
                        ${alerta.colorAlerta}
                    </span>
                </td>
                <td>
                    <span class="badge bg-secondary">${alerta.nivelPrioridad} - ${prioridadTexto}</span>
                </td>
                <td class="text-center">${activaIcon}</td>
                <td>
                    <button class="btn btn-sm btn-outline-warning me-1" onclick="editarAlerta(${index})" title="Editar">
                        <i class="bi bi-pencil-fill"></i>
                    </button>
                    <button class="btn btn-sm btn-outline-danger" onclick="eliminarAlerta(${index})" title="Eliminar">
                        <i class="bi bi-trash-fill"></i>
                    </button>
                </td>
            </tr>
        `;
    }).join('');
}

function formatearDias(dias) {
    if (dias === 0) {
        return '<span class="badge bg-danger">Día del vencimiento</span>';
    } else if (dias < 0) {
        return `<span class="badge bg-warning text-dark">${Math.abs(dias)} días antes</span>`;
    } else {
        return `<span class="badge bg-danger">${dias} días después</span>`;
    }
}

function obtenerTextoPrioridad(nivel) {
    const prioridades = {
        1: 'Baja',
        2: 'Media-Baja',
        3: 'Media',
        4: 'Media-Alta',
        5: 'Alta'
    };
    return prioridades[nivel] || 'Media';
}

function obtenerColorTexto(colorHex) {
    // Convertir hex a RGB y calcular luminancia para decidir si usar texto blanco o negro
    const hex = colorHex.replace('#', '');
    const r = parseInt(hex.substr(0, 2), 16);
    const g = parseInt(hex.substr(2, 2), 16);
    const b = parseInt(hex.substr(4, 2), 16);
    const luminancia = (0.299 * r + 0.587 * g + 0.114 * b) / 255;
    return luminancia > 0.5 ? '#000000' : '#FFFFFF';
}

// ============================================================================
// Modal de alerta (agregar/editar)
// ============================================================================

function mostrarModalAlerta(index = -1) {
    editandoIndex = index;

    if (index >= 0 && alertas[index]) {
        // Modo edición
        const alerta = alertas[index];
        document.getElementById('tituloModalAlerta').textContent = 'Editar Alerta';
        document.getElementById('alertaIndex').value = index;
        document.getElementById('alertaDias').value = alerta.diasRelativoVencimiento;
        document.getElementById('alertaDescripcion').value = alerta.descripcion || '';
        document.getElementById('alertaColor').value = alerta.colorAlerta;
        document.getElementById('alertaColorHex').value = alerta.colorAlerta.toUpperCase();
        document.getElementById('alertaPrioridad').value = alerta.nivelPrioridad;
        document.getElementById('alertaActiva').checked = alerta.activa;
    } else {
        // Modo creación
        document.getElementById('tituloModalAlerta').textContent = 'Agregar Alerta';
        document.getElementById('alertaIndex').value = '';
        document.getElementById('alertaDias').value = 0;
        document.getElementById('alertaDescripcion').value = '';
        document.getElementById('alertaColor').value = '#FF0000';
        document.getElementById('alertaColorHex').value = '#FF0000';
        document.getElementById('alertaPrioridad').value = 3;
        document.getElementById('alertaActiva').checked = true;
    }

    modalAlerta.show();
}

function editarAlerta(index) {
    mostrarModalAlerta(index);
}

function guardarAlerta() {
    const dias = parseInt(document.getElementById('alertaDias').value);
    const descripcion = document.getElementById('alertaDescripcion').value.trim();
    const color = document.getElementById('alertaColorHex').value.toUpperCase();
    const prioridad = parseInt(document.getElementById('alertaPrioridad').value);
    const activa = document.getElementById('alertaActiva').checked;

    // Validaciones
    if (isNaN(dias)) {
        mostrarError('Debe especificar los días');
        return;
    }

    if (!descripcion) {
        mostrarError('Debe especificar una descripción');
        return;
    }

    if (!/^#[0-9A-Fa-f]{6}$/.test(color)) {
        mostrarError('Color inválido. Debe ser formato #RRGGBB');
        return;
    }

    const alerta = {
        diasRelativoVencimiento: dias,
        descripcion: descripcion,
        colorAlerta: color,
        nivelPrioridad: prioridad,
        activa: activa,
        orden: alertas.length // El orden se establece por defecto
    };

    if (editandoIndex >= 0) {
        // Editar existente
        alertas[editandoIndex] = { ...alertas[editandoIndex], ...alerta };
    } else {
        // Agregar nueva
        alertas.push(alerta);
    }

    renderizarTablaAlertas();
    modalAlerta.hide();
    editandoIndex = -1;
}

function eliminarAlerta(index) {
    if (confirm('¿Está seguro de eliminar esta alerta?')) {
        alertas.splice(index, 1);
        renderizarTablaAlertas();
    }
}

// ============================================================================
// Guardar configuración completa
// ============================================================================

async function guardarConfiguracion() {
    const tasaMoraDiaria = parseFloat(document.getElementById('tasaMoraDiaria').value);
    const diasGracia = parseInt(document.getElementById('diasGracia').value);
    const procesoAutomatico = document.getElementById('procesoAutomaticoActivo').checked;

    // Validaciones
    if (isNaN(tasaMoraDiaria) || tasaMoraDiaria < 0) {
        mostrarError('Tasa de mora diaria inválida');
        return;
    }

    if (isNaN(diasGracia) || diasGracia < 0) {
        mostrarError('Días de gracia inválidos');
        return;
    }

    if (alertas.length === 0) {
        if (!confirm('No ha configurado ninguna alerta. ¿Desea continuar?')) {
            return;
        }
    }

    const modelo = {
        tasaMoraDiaria: tasaMoraDiaria,
        diasGracia: diasGracia,
        procesoAutomaticoActivo: procesoAutomatico,
        alertas: alertas.map((a, index) => ({
            diasRelativoVencimiento: a.diasRelativoVencimiento,
            descripcion: a.descripcion,
            colorAlerta: a.colorAlerta,
            nivelPrioridad: a.nivelPrioridad,
            activa: a.activa,
            orden: index
        }))
    };

    try {
        const response = await fetch('/ConfiguracionMora/SaveConfiguracion', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(modelo)
        });

        const result = await response.json();

        if (result.success) {
            mostrarExito('Configuración guardada exitosamente');
            modalConfiguracion.hide();
            // Recargar la página para reflejar cambios si es necesario
            setTimeout(() => {
                location.reload();
            }, 1500);
        } else {
            mostrarError(result.message || 'Error al guardar la configuración');
        }
    } catch (error) {
        console.error('Error al guardar configuración:', error);
        mostrarError('Error de conexión al guardar la configuración');
    }
}

// ============================================================================
// Utilidades
// ============================================================================

function mostrarExito(mensaje) {
    // Usar toastr si está disponible, sino alert
    if (typeof toastr !== 'undefined') {
        toastr.success(mensaje);
    } else {
        alert(mensaje);
    }
}

function mostrarError(mensaje) {
    if (typeof toastr !== 'undefined') {
        toastr.error(mensaje);
    } else {
        alert('Error: ' + mensaje);
    }
}
