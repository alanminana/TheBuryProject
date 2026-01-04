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

    function bindConfirmations() {
        if (document.documentElement.dataset.confirmBound === 'true') return;
        document.documentElement.dataset.confirmBound = 'true';

        document.addEventListener('click', function (event) {
            const trigger = event.target.closest('[data-confirm]');
            if (!trigger) return;

            if ((trigger instanceof HTMLButtonElement || trigger instanceof HTMLInputElement)
                && String(trigger.getAttribute('type') || '').toLowerCase() === 'submit'
                && trigger.form) {
                return;
            }

            const message = normalizeConfirmMessage(trigger.getAttribute('data-confirm'));
            if (!message) return;

            if (!confirm(message)) {
                event.preventDefault();
                event.stopPropagation();
            }
        }, true);

        document.addEventListener('submit', function (event) {
            const form = event.target;
            if (!form || !(form instanceof HTMLFormElement)) return;

            const submitter = event.submitter;

            const rawMessage = submitter?.getAttribute?.('data-confirm') || form.getAttribute('data-confirm');
            const message = normalizeConfirmMessage(rawMessage);
            if (message) {
                if (!confirm(message)) {
                    event.preventDefault();
                    event.stopPropagation();
                    return;
                }
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
