(function () {
    function enableTooltips() {
        const tooltipElements = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        tooltipElements.forEach(function (el) {
            if (!el.dataset.tooltipBound) {
                new bootstrap.Tooltip(el);
                el.dataset.tooltipBound = 'true';
            }
        });
    }

    function normalizeConfirmMessage(message) {
        if (!message) return '';
        return String(message).replace(/\\n/g, '\n');
    }

    function ensureHiddenInput(form, name) {
        if (!form || !(form instanceof HTMLFormElement) || !name) return null;

        var existing = form.querySelector('input[type="hidden"][name="' + CSS.escape(name) + '"]');
        if (existing) return existing;

        var input = document.createElement('input');
        input.type = 'hidden';
        input.name = name;
        form.appendChild(input);
        return input;
    }

    // Variables para gestionar el estado del modal de confirmación
    var pendingConfirmation = null;

    function getConfirmModal() {
        return document.getElementById('confirmModal');
    }

    function showConfirmModal(message, options) {
        var modal = getConfirmModal();
        if (!modal) {
            // Fallback a confirm() nativo si el modal no existe
            return Promise.resolve(confirm(message));
        }

        options = options || {};

        // Configurar el contenido del modal
        var titleEl = document.getElementById('confirmModalTitle');
        var messageEl = document.getElementById('confirmModalMessage');
        var confirmBtn = document.getElementById('confirmModalConfirmBtn');
        var cancelBtn = document.getElementById('confirmModalCancelBtn');
        var iconEl = document.getElementById('confirmModalIcon');

        if (titleEl) titleEl.textContent = options.title || 'Confirmar acción';
        if (messageEl) messageEl.innerHTML = message.replace(/\n/g, '<br>');
        if (confirmBtn) {
            confirmBtn.textContent = options.confirmText || 'Confirmar';
            // Actualizar clase del botón según variante
            confirmBtn.className = 'btn btn-' + (options.variant || 'primary');
        }
        if (cancelBtn) cancelBtn.textContent = options.cancelText || 'Cancelar';
        if (iconEl) {
            // Actualizar icono según variante
            var iconClass = 'bi bi-question-circle';
            var colorClass = 'text-primary';
            if (options.variant === 'danger') {
                iconClass = 'bi bi-exclamation-triangle-fill';
                colorClass = 'text-danger';
            } else if (options.variant === 'warning') {
                iconClass = 'bi bi-exclamation-triangle';
                colorClass = 'text-warning';
            }
            iconEl.className = iconClass + ' ' + colorClass;
        }

        return new Promise(function (resolve) {
            var bsModal = bootstrap.Modal.getOrCreateInstance(modal);

            function cleanup() {
                confirmBtn.removeEventListener('click', onConfirm);
                modal.removeEventListener('hidden.bs.modal', onHidden);
            }

            function onConfirm() {
                cleanup();
                bsModal.hide();
                resolve(true);
            }

            function onHidden() {
                cleanup();
                resolve(false);
            }

            confirmBtn.addEventListener('click', onConfirm);
            modal.addEventListener('hidden.bs.modal', onHidden);

            bsModal.show();
        });
    }

    function getConfirmOptions(element) {
        if (!element) return {};
        return {
            title: element.getAttribute('data-confirm-title'),
            variant: element.getAttribute('data-confirm-variant'),
            confirmText: element.getAttribute('data-confirm-confirm-text'),
            cancelText: element.getAttribute('data-confirm-cancel-text')
        };
    }

    function bindConfirmations() {
        if (document.documentElement.dataset.confirmBound === 'true') return;
        document.documentElement.dataset.confirmBound = 'true';

        // Handler para clicks en elementos con data-confirm (no submit buttons)
        document.addEventListener('click', function (event) {
            const trigger = event.target.closest('[data-confirm]');
            if (!trigger) return;

            // Los submit buttons se manejan en el handler de submit
            if ((trigger instanceof HTMLButtonElement || trigger instanceof HTMLInputElement)
                && String(trigger.getAttribute('type') || '').toLowerCase() === 'submit'
                && trigger.form) {
                return;
            }

            const message = normalizeConfirmMessage(trigger.getAttribute('data-confirm'));
            if (!message) return;

            event.preventDefault();
            event.stopPropagation();

            const options = getConfirmOptions(trigger);
            showConfirmModal(message, options).then(function (confirmed) {
                if (confirmed) {
                    // Re-trigger el click sin el data-confirm
                    trigger.removeAttribute('data-confirm');
                    trigger.click();
                    trigger.setAttribute('data-confirm', message);
                }
            });
        }, true);

        // Handler para submit de formularios
        document.addEventListener('submit', function (event) {
            const form = event.target;
            if (!form || !(form instanceof HTMLFormElement)) return;

            // Evitar doble procesamiento
            if (form.dataset.confirmPending === 'true') {
                form.dataset.confirmPending = 'false';
                return;
            }

            const submitter = event.submitter;

            const rawMessage = submitter?.getAttribute?.('data-confirm') || form.getAttribute('data-confirm');
            const message = normalizeConfirmMessage(rawMessage);
            
            if (message) {
                event.preventDefault();
                event.stopPropagation();

                const options = getConfirmOptions(submitter || form);
                showConfirmModal(message, options).then(function (confirmed) {
                    if (confirmed) {
                        form.dataset.confirmPending = 'true';
                        // Re-submit el formulario
                        if (submitter && submitter.click) {
                            submitter.click();
                        } else {
                            form.requestSubmit(submitter);
                        }
                    }
                });
                return;
            }

            const rawPrompt = submitter?.getAttribute?.('data-prompt') || form.getAttribute('data-prompt');
            const promptMessage = normalizeConfirmMessage(rawPrompt);
            if (promptMessage) {
                const promptName = submitter?.getAttribute?.('data-prompt-name')
                    || form.getAttribute('data-prompt-name')
                    || 'observaciones';

                const rawRequired = submitter?.getAttribute?.('data-prompt-required')
                    || form.getAttribute('data-prompt-required')
                    || 'false';
                const required = String(rawRequired).toLowerCase() === 'true';

                const value = prompt(promptMessage);
                if (value === null) {
                    event.preventDefault();
                    event.stopPropagation();
                    return;
                }

                if (required && !String(value).trim()) {
                    event.preventDefault();
                    event.stopPropagation();
                    return;
                }

                const input = ensureHiddenInput(form, promptName);
                if (input) {
                    input.value = value;
                }
            }
        }, true);
    }

    function bindAutoSubmit() {
        if (document.documentElement.dataset.autoSubmitBound === 'true') return;
        document.documentElement.dataset.autoSubmitBound = 'true';

        document.addEventListener('change', function (event) {
            const el = event.target;
            if (!el || !(el instanceof Element)) return;

            const trigger = el.closest('[data-auto-submit="true"]');
            if (!trigger) return;

            const form = trigger.closest('form');
            if (!form || !(form instanceof HTMLFormElement)) return;

            form.submit();
        }, true);
    }

    function bindTriggerClicks() {
        if (document.documentElement.dataset.triggerClickBound === 'true') return;
        document.documentElement.dataset.triggerClickBound = 'true';

        document.addEventListener('click', function (event) {
            const el = event.target;
            if (!el || !(el instanceof Element)) return;

            const trigger = el.closest('[data-trigger-click]');
            if (!trigger) return;

            const selector = trigger.getAttribute('data-trigger-click');
            if (!selector) return;

            const target = document.querySelector(selector);
            if (!target) return;

            event.preventDefault();
            target.dispatchEvent(new MouseEvent('click', { bubbles: true, cancelable: true, view: window }));
        }, true);
    }

    document.addEventListener('DOMContentLoaded', function () {
        enableTooltips();
        bindConfirmations();
        bindAutoSubmit();
        bindTriggerClicks();
    });
})();
