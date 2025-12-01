document.addEventListener('DOMContentLoaded', () => {
    const container = document.getElementById('permisosApp');
    if (!container) return;

    const toggleUrl = container.dataset.toggleUrl;
    if (!toggleUrl) return;

    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

    container.querySelectorAll('.permiso-checkbox').forEach((checkbox) => {
        checkbox.addEventListener('change', () => {
            const roleId = checkbox.dataset.roleId;
            const moduloId = checkbox.dataset.moduloId;
            const accionId = checkbox.dataset.accionId;
            const asignar = checkbox.checked;

            checkbox.disabled = true;

            const formData = new URLSearchParams();
            formData.append('roleId', roleId);
            formData.append('moduloId', moduloId);
            formData.append('accionId', accionId);
            formData.append('asignar', asignar);
            if (token) {
                formData.append('__RequestVerificationToken', token);
            }

            fetch(toggleUrl, {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: formData
            })
                .then((response) => response.ok ? response.json() : Promise.reject())
                .then((response) => {
                    if (response.success) {
                        const label = checkbox.parentElement?.querySelector('label');
                        if (label) {
                            label.classList.add('text-success');
                            setTimeout(() => label.classList.remove('text-success'), 1000);
                        }
                    } else {
                        checkbox.checked = !asignar;
                        alert('Error: ' + (response.message || 'No se pudo actualizar el permiso'));
                    }
                })
                .catch(() => {
                    checkbox.checked = !asignar;
                    alert('Error de conexión. Por favor, intente nuevamente.');
                })
                .finally(() => {
                    checkbox.disabled = false;
                });
        });
    });
});
