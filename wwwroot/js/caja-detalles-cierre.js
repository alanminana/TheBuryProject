document.addEventListener('DOMContentLoaded', () => {
    const printButton = document.getElementById('imprimirCierre');
    if (!printButton) return;

    printButton.addEventListener('click', () => {
        window.print();
    });
});
