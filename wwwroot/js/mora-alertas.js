(function () {
    const form = document.getElementById('filtrosForm');
    if (!form) return;

    const submitForm = () => form.submit();

    const autoSubmitInputs = form.querySelectorAll('[data-auto-submit="true"]');
    autoSubmitInputs.forEach((element) => {
        element.addEventListener('change', submitForm);
    });

    const clienteInput = form.querySelector('input[name="cliente"]');
    if (clienteInput) {
        clienteInput.addEventListener('keypress', (event) => {
            if (event.key === 'Enter') {
                event.preventDefault();
                submitForm();
            }
        });
    }
})();
