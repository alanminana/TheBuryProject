/**
 * Sistema de Notificaciones - TheBuryProject
 * Maneja la carga y visualización de notificaciones en tiempo real
 */

(function () {
    'use strict';

    // Configuración
    const config = {
        apiUrl: '/api/Notificacion',
        refreshInterval: 30000, // 30 segundos
        maxNotificaciones: 10
    };

    // Estado
    let intervalId = null;

    /**
     * Inicializar sistema de notificaciones
     */
    function init() {
        cargarNotificaciones();
        configurarEventos();
        iniciarActualizacionAutomatica();
    }

    /**
     * Cargar notificaciones desde el servidor
     */
    function cargarNotificaciones() {
        $.ajax({
            url: `${config.apiUrl}?soloNoLeidas=false&limite=${config.maxNotificaciones}`,
            method: 'GET',
            success: function (notificaciones) {
                actualizarBadge(notificaciones);
                renderizarNotificaciones(notificaciones);
            },
            error: function (xhr, status, error) {
                console.error('Error al cargar notificaciones:', error);
                if (xhr.status === 401) {
                    // Usuario no autenticado, detener polling
                    detenerActualizacionAutomatica();
                }
            }
        });
    }

    /**
     * Actualizar badge con cantidad de no leídas
     */
    function actualizarBadge(notificaciones) {
        const noLeidas = notificaciones.filter(n => !n.leida).length;
        const badge = $('#notificacionesBadge');

        if (noLeidas > 0) {
            badge.text(noLeidas > 99 ? '99+' : noLeidas);
            badge.show();
        } else {
            badge.hide();
        }
    }

    /**
     * Renderizar lista de notificaciones
     */
    function renderizarNotificaciones(notificaciones) {
        // Limpiar notificaciones existentes (elementos entre los dividers)
        $('#notificacionesLista > li:not(.dropdown-header):not(:has(hr)):not(:has(#verTodasNotificaciones))').remove();

        const noNotificacionesMsg = $('#noNotificacionesMsg');
        const primerDivider = $('#notificacionesLista > li:has(hr)').first();

        if (notificaciones.length === 0) {
            // Mostrar mensaje de no hay notificaciones
            if (noNotificacionesMsg.length === 0) {
                const html = `
                    <li class="text-center py-3 text-muted" id="noNotificacionesMsg">
                        <i class="bi bi-inbox fs-1"></i>
                        <p class="mb-0">No hay notificaciones</p>
                    </li>
                `;
                primerDivider.after(html);
            } else {
                noNotificacionesMsg.show();
            }
            return;
        }

        // Ocultar mensaje de no hay notificaciones
        noNotificacionesMsg.hide();

        let html = '';
        notificaciones.forEach(notif => {
            const iconoClase = notif.icono || 'bi-bell';
            const bgClase = notif.leida ? '' : 'bg-primary bg-opacity-10';
            const boldClase = notif.leida ? '' : 'fw-bold';

            html += `
                <li>
                    <a class="dropdown-item notificacion-item ${bgClase}"
                       href="#"
                       data-notif-id="${notif.id}"
                       data-notif-url="${notif.url || '#'}"
                       data-notif-leida="${notif.leida}">
                        <div class="d-flex">
                            <div class="flex-shrink-0 me-3">
                                <i class="bi ${iconoClase} fs-4"></i>
                            </div>
                            <div class="flex-grow-1">
                                <h6 class="mb-1 ${boldClase}">${escapeHtml(notif.titulo)}</h6>
                                <p class="mb-1 small text-muted">${escapeHtml(notif.mensaje)}</p>
                                <small class="text-muted">
                                    <i class="bi bi-clock me-1"></i>${notif.tiempoTranscurrido}
                                </small>
                            </div>
                            ${!notif.leida ? '<div class="flex-shrink-0"><span class="badge bg-primary">Nueva</span></div>' : ''}
                        </div>
                    </a>
                </li>
            `;
        });

        // Insertar después del primer divider
        primerDivider.after(html);
    }

    /**
     * Configurar eventos del DOM
     */
    function configurarEventos() {
        // Click en notificación
        $(document).on('click', '.notificacion-item', function (e) {
            e.preventDefault();
            const $item = $(this);
            const id = $item.data('notif-id');
            const url = $item.data('notif-url');
            const leida = $item.data('notif-leida');

            // Marcar como leída si no lo está
            if (!leida) {
                marcarComoLeida(id, function () {
                    // Después de marcar, redirigir si hay URL
                    if (url && url !== '#') {
                        window.location.href = url;
                    }
                });
            } else {
                // Ya está leída, redirigir directamente
                if (url && url !== '#') {
                    window.location.href = url;
                }
            }
        });

        // Marcar todas como leídas
        $('#marcarTodasLeidasBtn').on('click', function (e) {
            e.preventDefault();
            marcarTodasComoLeidas();
        });

        // Ver todas las notificaciones (por ahora solo recarga)
        $('#verTodasNotificaciones').on('click', function (e) {
            e.preventDefault();
            cargarNotificaciones();
            // TODO: Crear vista dedicada para ver todas las notificaciones
        });

        // Recargar al abrir el dropdown
        $('#notificacionesDropdown').on('click', function () {
            cargarNotificaciones();
        });
    }

    /**
     * Marcar una notificación como leída
     */
    function marcarComoLeida(id, callback) {
        $.ajax({
            url: `${config.apiUrl}/${id}/marcarLeida`,
            method: 'POST',
            success: function () {
                cargarNotificaciones();
                if (typeof callback === 'function') {
                    callback();
                }
            },
            error: function (xhr, status, error) {
                console.error('Error al marcar como leída:', error);
            }
        });
    }

    /**
     * Marcar todas las notificaciones como leídas
     */
    function marcarTodasComoLeidas() {
        $.ajax({
            url: `${config.apiUrl}/marcarTodasLeidas`,
            method: 'POST',
            success: function () {
                cargarNotificaciones();
                mostrarMensaje('Todas las notificaciones marcadas como leídas', 'success');
            },
            error: function (xhr, status, error) {
                console.error('Error al marcar todas como leídas:', error);
                mostrarMensaje('Error al marcar notificaciones', 'danger');
            }
        });
    }

    /**
     * Iniciar actualización automática
     */
    function iniciarActualizacionAutomatica() {
        if (intervalId) {
            clearInterval(intervalId);
        }
        intervalId = setInterval(cargarNotificaciones, config.refreshInterval);
    }

    /**
     * Detener actualización automática
     */
    function detenerActualizacionAutomatica() {
        if (intervalId) {
            clearInterval(intervalId);
            intervalId = null;
        }
    }

    /**
     * Mostrar mensaje toast (si existe implementación de toasts)
     */
    function mostrarMensaje(mensaje, tipo) {
        // Implementación básica con alert
        // TODO: Implementar sistema de toasts más elegante
        if (tipo === 'success') {
            console.log('✓ ' + mensaje);
        } else {
            console.error('✗ ' + mensaje);
        }
    }

    /**
     * Escapar HTML para prevenir XSS
     */
    function escapeHtml(text) {
        if (!text) return '';
        const map = {
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            '"': '&quot;',
            "'": '&#039;'
        };
        return text.replace(/[&<>"']/g, function (m) { return map[m]; });
    }

    // Inicializar cuando el DOM esté listo
    $(function () {
        init();
    });

    // Detener polling al salir de la página
    $(window).on('beforeunload', function () {
        detenerActualizacionAutomatica();
    });

})();