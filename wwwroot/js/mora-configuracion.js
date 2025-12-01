(() => {
    const jobActivo = document.getElementById('JobActivo');
    if (!jobActivo) {
        return;
    }

    const statusTargetId = jobActivo.dataset.statusTarget;
    const statusText = statusTargetId ? document.getElementById(statusTargetId) : null;

    const updateStatus = () => {
        if (statusText) {
            statusText.textContent = jobActivo.checked ? 'Activo' : 'Inactivo';
        }
    };

    jobActivo.addEventListener('change', updateStatus);
    updateStatus();
})();
