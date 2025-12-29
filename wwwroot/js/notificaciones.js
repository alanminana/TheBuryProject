/**
 * Sistema de Notificaciones - TheBuryProject
 * Maneja la carga y visualización de notificaciones en tiempo real sin depender de jQuery
 */

(function () {
    'use strict';

    const config = {
        apiUrl: '/api/Notificacion',
        hubUrl: '/hubs/notificaciones',
        refreshInterval: 30000,
        maxNotificaciones: 10
    };

    let intervalId = null;
    let connection = null;

    function init() {
        configurarEventos();
        cargarNotificaciones();
        iniciarConexionTiempoReal();
    }

    function cargarNotificaciones() {
        const url = `${config.apiUrl}?soloNoLeidas=false&limite=${config.maxNotificaciones}`;
        fetch(url)
            .then((response) => response.ok ? response.json() : Promise.reject(response))
            .then((notificaciones) => {
                actualizarBadge(notificaciones);
                renderizarNotificaciones(notificaciones);
            })
            .catch((error) => {
                console.error('Error al cargar notificaciones:', error);
                if (error.status === 401) {
                    detenerActualizacionAutomatica();
                }
            });
    }

    function actualizarBadge(notificaciones) {
        const noLeidas = notificaciones.filter((n) => !n.leida).length;
        const badge = document.getElementById('notificacionesBadge');
        if (!badge) return;

        if (noLeidas > 0) {
            badge.textContent = noLeidas > 99 ? '99+' : noLeidas;
            badge.style.display = '';
        } else {
            badge.style.display = 'none';
        }
    }

    function renderizarNotificaciones(notificaciones) {
        const lista = document.getElementById('notificacionesLista');
        if (!lista) return;

        lista.querySelectorAll('li').forEach((el) => {
            const hasDivider = el.querySelector('hr') !== null;
            const hasVerTodas = el.querySelector('#verTodasNotificaciones') !== null;
            if (!el.classList.contains('dropdown-header') && !hasDivider && !hasVerTodas) {
                el.remove();
            }
        });

        const noNotificacionesMsg = document.getElementById('noNotificacionesMsg');
        const primerDivider = Array.from(lista.querySelectorAll('li')).find((el) => el.querySelector('hr'));

        if (!notificaciones.length) {
            if (!noNotificacionesMsg) {
                const html = document.createElement('li');
                html.className = 'text-center py-3 text-muted';
                html.id = 'noNotificacionesMsg';
                html.innerHTML = `
                    <i class="bi bi-inbox fs-1"></i>
                    <p class="mb-0">No hay notificaciones</p>
                `;
                primerDivider?.insertAdjacentElement('afterend', html);
            } else {
                noNotificacionesMsg.style.display = '';
            }
            return;
        }

        if (noNotificacionesMsg) {
            noNotificacionesMsg.style.display = 'none';
        }

        const fragment = document.createDocumentFragment();
        notificaciones.forEach((notif) => {
            const iconoClase = notif.icono || 'bi-bell';
            const bgClase = notif.leida ? '' : 'bg-primary bg-opacity-10';
            const boldClase = notif.leida ? '' : 'fw-bold';

            const li = document.createElement('li');
            li.innerHTML = `
                <a class="dropdown-item notificacion-item ${bgClase}"
                   href="#"
                   data-notif-id="${notif.id}"
                   data-notif-url="${notif.url || '#'}"
                   data-notif-leida="${notif.leida}"
                   data-notif-rowversion="${notif.rowVersion || ''}">
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
            `;
            fragment.appendChild(li);
        });

        primerDivider?.insertAdjacentElement('afterend', fragment);
    }

    function configurarEventos() {
        document.addEventListener('click', function (event) {
            const item = event.target.closest('.notificacion-item');
            if (item) {
                event.preventDefault();
                const id = item.dataset.notifId;
                const url = item.dataset.notifUrl;
                const leida = item.dataset.notifLeida === 'true';
                const rowVersion = item.dataset.notifRowversion;

                const redirect = function () {
                    if (url && url !== '#') {
                        window.location.href = url;
                    }
                };

                if (!leida) {
                    marcarComoLeida(id, rowVersion, redirect);
                } else {
                    redirect();
                }
                return;
            }

            if (event.target.closest('#marcarTodasLeidasBtn')) {
                event.preventDefault();
                marcarTodasComoLeidas();
                return;
            }

            if (event.target.closest('#verTodasNotificaciones')) {
                event.preventDefault();
                cargarNotificaciones();
            }
        });

        document.getElementById('notificacionesDropdown')?.addEventListener('click', cargarNotificaciones);
    }

    function marcarComoLeida(id, rowVersion, callback) {
        const qs = rowVersion ? `?rowVersion=${encodeURIComponent(rowVersion)}` : '';
        fetch(`${config.apiUrl}/${id}/marcarLeida${qs}`, { method: 'POST' })
            .then((response) => {
                if (!response.ok) throw new Error('Error al marcar como leída');
                cargarNotificaciones();
                if (typeof callback === 'function') {
                    callback();
                }
            })
            .catch((error) => console.error('Error al marcar como leída:', error));
    }

    function marcarTodasComoLeidas() {
        fetch(`${config.apiUrl}/marcarTodasLeidas`, { method: 'POST' })
            .then((response) => {
                if (!response.ok) throw new Error('Error al marcar todas');
                cargarNotificaciones();
                mostrarMensaje('Todas las notificaciones marcadas como leídas', 'success');
            })
            .catch((error) => {
                console.error('Error al marcar todas como leídas:', error);
                mostrarMensaje('Error al marcar notificaciones', 'danger');
            });
    }

    function iniciarActualizacionAutomatica() {
        if (intervalId) {
            clearInterval(intervalId);
        }
        intervalId = setInterval(cargarNotificaciones, config.refreshInterval);
    }

    function detenerActualizacionAutomatica() {
        if (intervalId) {
            clearInterval(intervalId);
            intervalId = null;
        }
    }

    function iniciarConexionTiempoReal() {
        if (typeof signalR === 'undefined') {
            iniciarActualizacionAutomatica();
            return;
        }

        connection = new signalR.HubConnectionBuilder()
            .withUrl(config.hubUrl)
            .withAutomaticReconnect()
            .build();

        connection.on('NotificacionesActualizadas', () => {
            cargarNotificaciones();
        });

        connection.onreconnected(() => {
            detenerActualizacionAutomatica();
            cargarNotificaciones();
        });

        connection.onreconnecting(() => {
            iniciarActualizacionAutomatica();
        });

        connection.onclose(() => {
            iniciarActualizacionAutomatica();
        });

        connection.start()
            .then(() => {
                detenerActualizacionAutomatica();
            })
            .catch((error) => {
                console.warn('SignalR no disponible, se mantiene el polling de notificaciones.', error);
                iniciarActualizacionAutomatica();
            });
    }

    function mostrarMensaje(mensaje, tipo) {
        if (tipo === 'success') {
            console.log('✓ ' + mensaje);
        } else {
            console.error('✗ ' + mensaje);
        }
    }

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

    document.addEventListener('DOMContentLoaded', init);
    window.addEventListener('beforeunload', detenerActualizacionAutomatica);
})();
