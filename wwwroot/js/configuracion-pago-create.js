(function () {
    document.addEventListener("DOMContentLoaded", () => {
        const form = document.querySelector("[data-configuracion-pago-form]");
        if (!form) return;

        const descuentoCheck = form.querySelector("#permiteDescuentoCheck");
        const descuentoDiv = form.querySelector("#descuentoMaxDiv");
        const recargoCheck = form.querySelector("#tieneRecargoCheck");
        const recargoDiv = form.querySelector("#recargoDiv");
        const tipoPagoSelect = form.querySelector("#tipoPagoSelect");
        const tasaCreditoDiv = form.querySelector("#creditoPersonalTasaDiv");
        const creditoPersonalValue = form.dataset.creditoPersonalValue;

        const toggle = (check, target, inputId) => {
            if (!check || !target) return;

            if (check.checked) {
                target.style.display = "block";
                return;
            }

            target.style.display = "none";

            if (!inputId) return;
            const input = document.getElementById(inputId);
            if (input) {
                input.value = "";
            }
        };

        toggle(descuentoCheck, descuentoDiv, form.dataset.porcentajeDescuentoId);
        toggle(recargoCheck, recargoDiv, form.dataset.porcentajeRecargoId);

        const toggleCreditoPersonal = () => {
            if (!tipoPagoSelect || !tasaCreditoDiv) return;

            const isCreditoPersonal = tipoPagoSelect.value === creditoPersonalValue;
            tasaCreditoDiv.style.display = isCreditoPersonal ? "block" : "none";

            if (!isCreditoPersonal && form.dataset.tasaCreditoId) {
                const tasaInput = document.getElementById(form.dataset.tasaCreditoId);
                if (tasaInput) {
                    tasaInput.value = "";
                }
            }
        };

        toggleCreditoPersonal();

        if (descuentoCheck && descuentoDiv) {
            descuentoCheck.addEventListener("change", () =>
                toggle(descuentoCheck, descuentoDiv, form.dataset.porcentajeDescuentoId)
            );
        }

        if (recargoCheck && recargoDiv) {
            recargoCheck.addEventListener("change", () =>
                toggle(recargoCheck, recargoDiv, form.dataset.porcentajeRecargoId)
            );
        }

        if (tipoPagoSelect && tasaCreditoDiv) {
            tipoPagoSelect.addEventListener("change", toggleCreditoPersonal);
        }
    });
})();
