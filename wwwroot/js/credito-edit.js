(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', () => {
        const requiereCheckbox = document.getElementById('chkRequiereGarante');
        const garanteField = document.getElementById('garanteField');
        const garanteSelect = document.getElementById('GaranteId');

        if (!requiereCheckbox || !garanteField || !garanteSelect) {
            return;
        }

        const toggleGarante = () => {
            if (requiereCheckbox.checked) {
                garanteField.classList.remove('d-none');
                requiereCheckbox.setAttribute('aria-expanded', 'true');
                garanteSelect.setAttribute('required', 'required');
            } else {
                garanteField.classList.add('d-none');
                requiereCheckbox.setAttribute('aria-expanded', 'false');
                garanteSelect.value = '';
                garanteSelect.removeAttribute('required');
            }
        };

        requiereCheckbox.addEventListener('change', toggleGarante);
        toggleGarante();
    });
})();
