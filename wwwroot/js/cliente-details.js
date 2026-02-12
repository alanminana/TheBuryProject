(() => {
    const tabsRoot = document.getElementById('clienteTabs');
    if (tabsRoot) {
        const tabButtons = tabsRoot.querySelectorAll('button[data-bs-target]');
        tabButtons.forEach((tab) => {
            tab.addEventListener('shown.bs.tab', (e) => {
                const target = e?.target;
                const bsTarget = target?.getAttribute?.('data-bs-target');
                if (!bsTarget || !bsTarget.startsWith('#')) return;

                const tabId = bsTarget.slice(1);
                const url = new URL(window.location.href);
                url.searchParams.set('tab', tabId);
                history.replaceState(null, '', url.toString());
            });
        });

        const urlParams = new URLSearchParams(window.location.search);
        const tabParam = urlParams.get('tab');
        if (tabParam) {
            const tabButton = tabsRoot.querySelector(`[data-bs-target="#${CSS.escape(tabParam)}"]`);
            if (tabButton && typeof bootstrap !== 'undefined' && bootstrap.Tab) {
                new bootstrap.Tab(tabButton).show();
            }
        }
    }

    const autoOpenTrigger = document.querySelector('[data-abrir-verificar-todos="1"]');
    if (!autoOpenTrigger) return;

    const modalEl = document.getElementById('verificarTodosModal');
    if (!modalEl || typeof bootstrap === 'undefined' || !bootstrap.Modal) return;

    bootstrap.Modal.getOrCreateInstance(modalEl).show();
})();
