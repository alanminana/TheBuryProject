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
                garanteField.style.display = '';
            } else {
                garanteField.style.display = 'none';
                garanteSelect.value = '';
            }
        };

        requiereCheckbox.addEventListener('change', toggleGarante);
        toggleGarante();
    });
})();
