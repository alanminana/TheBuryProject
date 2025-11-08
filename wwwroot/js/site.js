document.addEventListener("DOMContentLoaded", () => {
    const sidebar = document.getElementById("sidebar");
    const toggleBtn = document.getElementById("toggleSidebar");
    const overlay = document.getElementById("overlay");
    const mobileBtn = document.getElementById("mobileToggle");

    if (toggleBtn) {
        toggleBtn.addEventListener("click", () => {
            sidebar.classList.toggle("collapsed");
        });
    }

    if (mobileBtn) {
        mobileBtn.addEventListener("click", () => {
            sidebar.classList.toggle("show");
            overlay.classList.toggle("show");
        });
    }

    if (overlay) {
        overlay.addEventListener("click", () => {
            sidebar.classList.remove("show");
            overlay.classList.remove("show");
        });
    }
});
