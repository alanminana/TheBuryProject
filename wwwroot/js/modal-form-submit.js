/**
 * modal-form-submit.js
 * 
 * Maneja formularios dentro de modales Bootstrap 5.
 * Oculta el modal antes de hacer submit para evitar backdrops colgados.
 * También limpia cualquier backdrop huérfano al restaurar desde bfcache o navegar.
 * 
 * IMPORTANTE: Para abrir modales programáticamente, usar:
 *   window.abrirModal('miModalId')  o  window.abrirModal(elementoDOM)
 * Esto evita crear instancias duplicadas que causan backdrops huérfanos.
 */
(function () {
    'use strict';

    // Verificar que Bootstrap esté disponible
    if (typeof bootstrap === 'undefined' || !bootstrap.Modal) {
        console.warn('modal-form-submit.js: Bootstrap no está disponible, script deshabilitado');
        return;
    }

    // === UTILIDAD GLOBAL PARA ABRIR MODALES SIN DUPLICAR INSTANCIAS ===
    /**
     * Abre un modal de forma segura, reutilizando la instancia existente.
     * @param {string|HTMLElement} modalIdOrElement - ID del modal (sin #) o elemento DOM
     * @returns {bootstrap.Modal|null} La instancia del modal o null si no se encontró
     */
    window.abrirModal = function(modalIdOrElement) {
        var modalEl = typeof modalIdOrElement === 'string' 
            ? document.getElementById(modalIdOrElement) 
            : modalIdOrElement;
        
        if (!modalEl) {
            console.warn('abrirModal: No se encontró el elemento:', modalIdOrElement);
            return null;
        }

        // Usar getOrCreateInstance para evitar duplicados
        var instance = bootstrap.Modal.getOrCreateInstance(modalEl);
        instance.show();
        return instance;
    };

    /**
     * Cierra un modal de forma segura y limpia recursos.
     * @param {string|HTMLElement} modalIdOrElement - ID del modal (sin #) o elemento DOM
     */
    window.cerrarModal = function(modalIdOrElement) {
        var modalEl = typeof modalIdOrElement === 'string' 
            ? document.getElementById(modalIdOrElement) 
            : modalIdOrElement;
        
        if (!modalEl) return;

        var instance = bootstrap.Modal.getInstance(modalEl);
        if (instance) {
            instance.hide();
        }
    };

    // === LIMPIEZA DE BACKDROPS HUÉRFANOS ===
    function limpiarBackdropsHuerfanos() {
        // Solo limpiar si NO hay un modal visible
        var modalAbierto = document.querySelector('.modal.show');
        if (modalAbierto) return;

        // Remover clase modal-open del body
        document.body.classList.remove('modal-open');
        document.body.style.removeProperty('overflow');
        document.body.style.removeProperty('padding-right');

        // Remover backdrops huérfanos
        document.querySelectorAll('.modal-backdrop').forEach(function (backdrop) {
            backdrop.remove();
        });
    }

    // === DISPOSE DE INSTANCIAS HUÉRFANAS ===
    function disposeModalesOcultos() {
        document.querySelectorAll('.modal').forEach(function(modalEl) {
            // Si el modal no está visible pero tiene instancia, hacer dispose
            if (!modalEl.classList.contains('show')) {
                var instance = bootstrap.Modal.getInstance(modalEl);
                if (instance && instance._isShown === false) {
                    // No hacer dispose aquí porque Bootstrap lo maneja internamente
                    // Solo asegurar que el backdrop se limpió
                }
            }
        });
    }

    // Limpiar al cierre de cualquier modal (por si Bootstrap falla)
    document.addEventListener('hidden.bs.modal', function (event) {
        // Pequeño delay para que Bootstrap termine su limpieza
        setTimeout(function() {
            limpiarBackdropsHuerfanos();
            disposeModalesOcultos();
        }, 100);
    });

    // Limpiar al cargar la página (por si quedó algo de una navegación anterior)
    document.addEventListener('DOMContentLoaded', function () {
        setTimeout(limpiarBackdropsHuerfanos, 100);
    });

    // === INTERCEPTAR NAVEGACIÓN PARA CERRAR MODALES ===
    // Antes de navegar (click en links), cerrar modales abiertos
    document.addEventListener('click', function(event) {
        var link = event.target.closest('a[href]');
        if (!link) return;
        
        // Ignorar links que abren modales, tabs, o javascript:
        var href = link.getAttribute('href');
        if (!href || href.startsWith('#') || href.startsWith('javascript:')) return;
        if (link.hasAttribute('data-bs-toggle')) return;
        if (link.target === '_blank') return;
        
        // Si hay un modal abierto, cerrarlo antes de navegar
        var modalAbierto = document.querySelector('.modal.show');
        if (modalAbierto) {
            var instance = bootstrap.Modal.getInstance(modalAbierto);
            if (instance) {
                event.preventDefault();
                instance.hide();
                
                // Navegar después de que el modal se cierre
                modalAbierto.addEventListener('hidden.bs.modal', function() {
                    limpiarBackdropsHuerfanos();
                    window.location.href = href;
                }, { once: true });
                
                // Fallback por si el evento no dispara
                setTimeout(function() {
                    limpiarBackdropsHuerfanos();
                    window.location.href = href;
                }, 400);
            }
        }
    }, true);

    // Interceptar submit de formularios dentro de modales
    document.addEventListener('submit', function (event) {
        var form = event.target;
        if (!form || !(form instanceof HTMLFormElement)) return;

        // Verificar si el form está dentro de un modal
        var modal = form.closest('.modal');
        if (!modal) return;

        // Obtener la instancia del modal de Bootstrap
        var modalInstance = bootstrap.Modal.getInstance(modal);
        if (!modalInstance) {
            // No hay instancia, dejar que Bootstrap maneje normalmente
            return;
        }

        // Marcar el form para evitar doble submit
        if (form.dataset.submitting === 'true') {
            event.preventDefault();
            return;
        }
        form.dataset.submitting = 'true';

        // Prevenir el submit normal
        event.preventDefault();

        // Ocultar el modal primero
        modalInstance.hide();

        // Esperar a que termine la animación y luego hacer submit
        var submitted = false;
        modal.addEventListener('hidden.bs.modal', function submitAfterHide() {
            if (submitted) return;
            submitted = true;
            modal.removeEventListener('hidden.bs.modal', submitAfterHide);
            limpiarBackdropsHuerfanos();
            form.submit();
        }, { once: true });

        // Fallback: si el modal no se cierra en 400ms, hacer submit de todas formas
        setTimeout(function () {
            if (!submitted && form.isConnected) {
                submitted = true;
                limpiarBackdropsHuerfanos();
                form.submit();
            }
        }, 400);

    }, true);

    // Solo limpiar en pageshow cuando viene de bfcache (no en carga normal)
    window.addEventListener('pageshow', function (event) {
        if (event.persisted) {
            limpiarBackdropsHuerfanos();
        }
    });

    // Limpiar antes de descargar la página (beforeunload)
    window.addEventListener('beforeunload', function() {
        // Cerrar todos los modales abiertos para evitar problemas de bfcache
        document.querySelectorAll('.modal.show').forEach(function(modalEl) {
            var instance = bootstrap.Modal.getInstance(modalEl);
            if (instance) {
                try {
                    instance.hide();
                } catch (e) {
                    // Ignorar errores en cleanup
                }
            }
        });
        limpiarBackdropsHuerfanos();
    });

    // Limpiar en popstate (navegación con botón atrás/adelante)
    window.addEventListener('popstate', function() {
        setTimeout(limpiarBackdropsHuerfanos, 100);
    });

})();
