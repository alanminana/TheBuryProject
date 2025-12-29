(function () {
    function getConfig() {
        var configElement = document.querySelector('[data-alerta-stock-config]');
        if (!configElement) {
            return null;
        }

        return {
            resolverUrl: configElement.dataset.resolverUrl,
            ignorarUrl: configElement.dataset.ignorarUrl,
            token: document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
        };
    }

    function createForm(url, id, observaciones, rowVersion, token) {
        var form = document.createElement('form');
        form.method = 'POST';
        form.action = url + '/' + id;

        var tokenInput = document.createElement('input');
        tokenInput.type = 'hidden';
        tokenInput.name = '__RequestVerificationToken';
        tokenInput.value = token;
        form.appendChild(tokenInput);

        if (observaciones) {
            var obsInput = document.createElement('input');
            obsInput.type = 'hidden';
            obsInput.name = 'observaciones';
            obsInput.value = observaciones;
            form.appendChild(obsInput);
        }

        if (rowVersion) {
            var rowVersionInput = document.createElement('input');
            rowVersionInput.type = 'hidden';
            rowVersionInput.name = 'rowVersion';
            rowVersionInput.value = rowVersion;
            form.appendChild(rowVersionInput);
        }

        document.body.appendChild(form);
        form.submit();
    }

    function resolverAlerta(config, id, producto, rowVersion) {
        var observaciones = window.prompt('Resolver alerta de "' + producto + '".\n\nObservaciones (opcional):');
        if (observaciones !== null) {
            createForm(config.resolverUrl, id, observaciones, rowVersion, config.token);
        }
    }

    function ignorarAlerta(config, id, producto, rowVersion) {
        if (window.confirm('¿Ignorar alerta de "' + producto + '"?')) {
            var observaciones = window.prompt('Motivo (opcional):');
            if (observaciones !== null) {
                createForm(config.ignorarUrl, id, observaciones, rowVersion, config.token);
            }
        }
    }

    function bindEvents() {
        var config = getConfig();
        if (!config) {
            return;
        }

        document.querySelectorAll('.js-resolver-alerta').forEach(function (button) {
            button.addEventListener('click', function () {
                resolverAlerta(config, button.dataset.id, button.dataset.producto, button.dataset.rowversion);
            });
        });

        document.querySelectorAll('.js-ignorar-alerta').forEach(function (button) {
            button.addEventListener('click', function () {
                ignorarAlerta(config, button.dataset.id, button.dataset.producto, button.dataset.rowversion);
            });
        });
    }

    document.addEventListener('DOMContentLoaded', bindEvents);
})();
