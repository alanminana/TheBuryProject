(function () {
    const page = document.querySelector('.mora-page');
    if (!page) return;

    const refreshMs = parseInt(page.dataset.refreshMs, 10);
    if (Number.isNaN(refreshMs) || refreshMs <= 0) return;

    setTimeout(function () {
        window.location.reload();
    }, refreshMs);
})();
