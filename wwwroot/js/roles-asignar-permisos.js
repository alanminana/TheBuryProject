document.addEventListener('DOMContentLoaded', () => {
    const container = document.getElementById('permisosApp');
    if (!container) return;

    const toggleUrl = container.dataset.toggleUrl;
    if (!toggleUrl) return;

    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

    $(container).find('.permiso-checkbox').on('change', function () {
        const checkbox = $(this);
        const roleId = checkbox.data('role-id');
        const moduloId = checkbox.data('modulo-id');
        const accionId = checkbox.data('accion-id');
        const asignar = checkbox.is(':checked');

        checkbox.prop('disabled', true);

        $.ajax({
            url: toggleUrl,
            type: 'POST',
            data: {
                roleId: roleId,
                moduloId: moduloId,
                accionId: accionId,
                asignar: asignar,
                __RequestVerificationToken: token
            },
            success: function (response) {
                if (response.success) {
                    const label = checkbox.next('label');
                    label.addClass('text-success');
                    setTimeout(function () {
                        label.removeClass('text-success');
                    }, 1000);
                } else {
                    checkbox.prop('checked', !asignar);
                    alert('Error: ' + (response.message || 'No se pudo actualizar el permiso'));
                }
            },
            error: function () {
                checkbox.prop('checked', !asignar);
                alert('Error de conexión. Por favor, intente nuevamente.');
            },
            complete: function () {
                checkbox.prop('disabled', false);
            }
        });
    });
});
