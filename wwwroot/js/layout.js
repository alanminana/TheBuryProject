(function () {
    const sidebar = document.getElementById("sidebar");
    const overlay = document.getElementById("sidebarOverlay");
    const toggleBtn = document.getElementById("toggleSidebar");

    function isMobile() {
        return window.innerWidth < 992;
    }

    function openMobileSidebar() {
        sidebar?.classList.add("show");
        overlay?.classList.add("show");
        document.body.classList.add("no-scroll");
    }

    function closeMobileSidebar() {
        sidebar?.classList.remove("show");
        overlay?.classList.remove("show");
        document.body.classList.remove("no-scroll");
    }

    toggleBtn?.addEventListener("click", function () {
        if (isMobile()) {
            if (sidebar?.classList.contains("show")) {
                closeMobileSidebar();
            } else {
                openMobileSidebar();
            }
        } else {
            document.body.classList.toggle("sidebar-pinned");
            const pinned = document.body.classList.contains("sidebar-pinned") ? "1" : "0";
            localStorage.setItem("sidebarPinned", pinned);
        }
    });

    overlay?.addEventListener("click", closeMobileSidebar);

    const pinnedState = localStorage.getItem("sidebarPinned");
    if (pinnedState === "1") {
        document.body.classList.add("sidebar-pinned");
    }

    const currentPath = window.location.pathname.toLowerCase();
    document.querySelectorAll(".sidebar-nav .nav-link").forEach(link => {
        const href = link.getAttribute("href");
        if (!href) return;
        try {
            const url = new URL(href, window.location.origin);
            if (currentPath.startsWith(url.pathname.toLowerCase()) && url.pathname !== "/") {
                link.classList.add("active");
            }
        } catch { /* ignore invalid links */ }
    });
})();
