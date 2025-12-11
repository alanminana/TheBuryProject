document.addEventListener('DOMContentLoaded', () => {
    const tipoSelect = document.getElementById('Tipo');
    const conceptoSelect = document.getElementById('Concepto');

    if (!tipoSelect || !conceptoSelect) return;

    // Guardamos todas las opciones originales
    const opciones = Array.from(conceptoSelect.options);

    const filtrarConceptos = () => {
        const tipoValor = tipoSelect.value; // "" | "0" | "1"
        const tipoTexto =
            tipoValor === '0'
                ? 'Ingreso'
                : tipoValor === '1'
                    ? 'Egreso'
                    : null;

        const valorActual = conceptoSelect.value;

        opciones.forEach(opt => {
            const dataTipo = opt.getAttribute('data-tipo');

            // Placeholder (sin data-tipo)
            if (!dataTipo) {
                opt.hidden = false;
                opt.disabled = false;
                return;
            }

            // Si no hay tipo seleccionado, ocultamos todos los conceptos
            if (!tipoTexto) {
                opt.hidden = true;
                opt.disabled = true;
                return;
            }

            // Visible si es del tipo seleccionado o de ambos
            const visible = dataTipo === tipoTexto || dataTipo === 'Ambos';

            opt.hidden = !visible;
            opt.disabled = !visible;
        });

        // Si el concepto actual ya no es válido, lo reseteamos
        const opcionSeleccionada = opciones.find(o => o.value === valorActual);
        if (!opcionSeleccionada || opcionSeleccionada.disabled) {
            conceptoSelect.value = '';
        }
    };

    tipoSelect.addEventListener('change', filtrarConceptos);

    // Ejecutar al cargar por si el modelo viene con Tipo/Concepto ya establecidos
    filtrarConceptos();
});
