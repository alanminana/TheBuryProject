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

    function getConfirmModal() {
        return document.getElementById('confirmModal');
    }

    function applyConfirmModalContent(message, options) {
        var modal = getConfirmModal();
        if (!modal) return null;

        options = options || {};

        var titleEl = document.getElementById('confirmModalTitle');
        var messageEl = document.getElementById('confirmModalMessage');
        var confirmBtn = document.getElementById('confirmModalConfirmBtn');
        var cancelBtn = document.getElementById('confirmModalCancelBtn');
        var iconEl = document.getElementById('confirmModalIcon');

        if (titleEl) titleEl.textContent = options.title || 'Confirmar acción';
        if (messageEl) messageEl.innerHTML = message.replace(/\n/g, '<br>');
        if (confirmBtn) {
            confirmBtn.textContent = options.confirmText || 'Confirmar';
            confirmBtn.className = 'btn btn-' + (options.variant || 'primary');
        }
        if (cancelBtn) cancelBtn.textContent = options.cancelText || 'Cancelar';
        if (iconEl) {
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

        if (!confirmBtn) return null;

        return { modal: modal, confirmBtn: confirmBtn };
    }

    function showConfirmModal(message, options) {
        var modalContext = applyConfirmModalContent(message, options);
        if (!modalContext) {
            return Promise.resolve(confirm(message));
        }

        var modal = modalContext.modal;
        var confirmBtn = modalContext.confirmBtn;

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

    function submitFormWithBypass(form, submitter) {
        if (typeof form.requestSubmit === 'function') {
            if (submitter && submitter.form === form) {
                form.requestSubmit(submitter);
            } else {
                form.requestSubmit();
            }
            return;
        }
        if (typeof form.submit === 'function') {
            form.submit();
        }
    }

    function bindConfirmations() {
        if (window.__confirmModalBound) return;
        window.__confirmModalBound = true;

        // Handler para clicks en elementos con data-confirm (no submit buttons)
        document.addEventListener('click', function (event) {
            const trigger = event.target.closest('[data-confirm]');
            if (!trigger) return;

            // Si el data-confirm está en un formulario, el submit handler se encarga
            if (trigger instanceof HTMLFormElement) {
                return;
            }

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

            if (form.dataset.confirmBypass === '1') {
                delete form.dataset.confirmBypass;
                console.log('[ConfirmModal] bypass submit OK', {
                    action: form.getAttribute('action'),
                    method: form.getAttribute('method'),
                    id: form.id || null
                });
                return;
            }

            const submitter = event.submitter;
            const submitterInfo = {
                id: submitter?.id || null,
                name: submitter?.name || null,
                type: submitter?.getAttribute?.('type') || null
            };

            const rawMessage = submitter?.getAttribute?.('data-confirm') || form.getAttribute('data-confirm');
            const message = normalizeConfirmMessage(rawMessage);

            if (message) {
                event.preventDefault();
                event.stopPropagation();

                console.log('[ConfirmModal] intercept submit', {
                    action: form.getAttribute('action'),
                    method: form.getAttribute('method'),
                    id: form.id || null,
                    submitter: submitterInfo
                });

                const options = getConfirmOptions(submitter || form);
                var modalContext = applyConfirmModalContent(message, options);

                if (!modalContext) {
                    if (confirm(message)) {
                        console.log('[ConfirmModal] confirm click (fallback confirm)', submitterInfo);
                        form.dataset.confirmBypass = '1';
                        submitFormWithBypass(form, submitter);
                    }
                    return;
                }

                var modal = modalContext.modal;
                var confirmBtn = modalContext.confirmBtn;
                var bsModal = bootstrap.Modal.getOrCreateInstance(modal);
                var confirmed = false;
                var submitted = false;
                var timeoutId = null;

                // IMPORTANTE: misma referencia para add/remove
                var hiddenListenerOptions = { once: true };

                function cleanup() {
                    confirmBtn.removeEventListener('click', onConfirm);
                    // SOLO este remove (sin duplicados)
                    modal.removeEventListener('hidden.bs.modal', onHidden, hiddenListenerOptions);
                }

                function submitWithBypass(logLabel) {
                    if (submitted) return;
                    submitted = true;

                    if (timeoutId) {
                        clearTimeout(timeoutId);
                        timeoutId = null;
                    }

                    console.log('[ConfirmModal] ' + logLabel, {
                        action: form.getAttribute('action'),
                        method: form.getAttribute('method'),
                        id: form.id || null
                    });

                    form.dataset.confirmBypass = '1';

                    console.log('[ConfirmModal] bypass submit OK', {
                        action: form.getAttribute('action'),
                        method: form.getAttribute('method'),
                        id: form.id || null
                    });

                    submitFormWithBypass(form, submitter);
                }

                function onConfirm() {
                    confirmed = true;
                    console.log('[ConfirmModal] confirm click', submitterInfo);
                    bsModal.hide();

                    timeoutId = setTimeout(function () {
                        if (!confirmed || submitted) return;
                        cleanup();
                        submitWithBypass('timeout fallback -> submit');
                    }, 800);
                }

                function onHidden() {
                    cleanup();
                    if (!confirmed) return;
                    submitWithBypass('hidden -> submit');
                }

                confirmBtn.addEventListener('click', onConfirm);

                // SOLO este add (sin duplicados)
                modal.addEventListener('hidden.bs.modal', onHidden, hiddenListenerOptions);

                bsModal.show();
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

    function cleanupModalArtifacts() {
        var openModals = document.querySelectorAll('.modal.show').length;
        var backdrops = document.querySelectorAll('.modal-backdrop');

        if (openModals === 0) {
            backdrops.forEach(function (backdrop) { backdrop.remove(); });
            document.body.classList.remove('modal-open');
            document.body.style.overflow = '';
            document.body.style.paddingRight = '';
        } else if (backdrops.length > 1) {
            backdrops.forEach(function (backdrop, index) {
                if (index > 0) backdrop.remove();
            });
        }
    }

    document.addEventListener('DOMContentLoaded', function () {
        enableTooltips();
        bindConfirmations();
        bindAutoSubmit();
        bindTriggerClicks();
        cleanupModalArtifacts();
    });

    document.addEventListener('hidden.bs.modal', cleanupModalArtifacts);
})();
