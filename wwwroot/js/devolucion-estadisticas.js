const parseData = (element, attribute) => {
    const value = element?.getAttribute(attribute);
    if (!value) return [];

    try {
        return JSON.parse(value);
    } catch (error) {
        console.error(`No se pudo parsear el atributo ${attribute}`, error);
        return [];
    }
};

const buildMotivosChart = (dataElement) => {
    const labels = parseData(dataElement, 'data-motivos-labels');
    const values = parseData(dataElement, 'data-motivos-values');
    const canvas = document.getElementById('chartMotivos');

    if (!canvas || !labels.length || !values.length || typeof Chart === 'undefined') return;

    const context = canvas.getContext('2d');
    new Chart(context, {
        type: 'pie',
        data: {
            labels,
            datasets: [{
                label: 'Cantidad',
                data: values,
                backgroundColor: [
                    'rgb(255, 99, 132)',
                    'rgb(54, 162, 235)',
                    'rgb(255, 205, 86)',
                    'rgb(75, 192, 192)',
                    'rgb(153, 102, 255)',
                    'rgb(255, 159, 64)',
                    'rgb(201, 203, 207)'
                ]
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: 'bottom'
                }
            }
        }
    });
};

const buildProductosChart = (dataElement) => {
    const labels = parseData(dataElement, 'data-productos-labels');
    const values = parseData(dataElement, 'data-productos-values');
    const canvas = document.getElementById('chartProductos');

    if (!canvas || !labels.length || !values.length || typeof Chart === 'undefined') return;

    const context = canvas.getContext('2d');
    new Chart(context, {
        type: 'bar',
        data: {
            labels,
            datasets: [{
                label: 'Veces Devuelto',
                data: values,
                backgroundColor: 'rgba(54, 162, 235, 0.8)',
                borderColor: 'rgb(54, 162, 235)',
                borderWidth: 1
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            scales: {
                y: {
                    beginAtZero: true,
                    ticks: {
                        stepSize: 1
                    }
                }
            },
            plugins: {
                legend: {
                    display: false
                }
            }
        }
    });
};

const initDevolucionEstadisticas = () => {
    const dataElement = document.getElementById('estadisticas-data');
    if (!dataElement) return;

    buildMotivosChart(dataElement);
    buildProductosChart(dataElement);
};

document.addEventListener('DOMContentLoaded', initDevolucionEstadisticas);
