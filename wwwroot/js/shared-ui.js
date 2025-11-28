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

    document.addEventListener('DOMContentLoaded', enableTooltips);
})();
