function waitForChart(callback) {
    if (typeof Chart !== 'undefined') {
        callback();
    } else {
        setTimeout(() => waitForChart(callback), 100);
    }
}

function initializeCharts() {
    Chart.defaults.color = '#dcd6f7';
    Chart.defaults.borderColor = 'rgba(255, 255, 255, 0.1)';

    try {
        // Datos recibidos desde la vista
        const usersRoleLabels = window.usersRoleLabels || [];
        const usersRoleData = window.usersRoleData || [];
        const competitionsSportLabels = window.competitionsSportLabels || [];
        const competitionsSportData = window.competitionsSportData || [];

        function hasValidData(labels, data) {
            return labels && data && Array.isArray(labels) && Array.isArray(data) &&
                labels.length > 0 && data.length > 0 && labels.length === data.length &&
                data.some(value => value > 0);
        }

        function showNoDataMessage(chartId, noDataElementId) {
            const canvas = document.getElementById(chartId);
            const noDataElement = document.getElementById(noDataElementId);
            if (canvas) canvas.style.display = 'none';
            if (noDataElement) noDataElement.style.display = 'block';
        }

        // === Usuarios por Rol ===
        const usersCanvas = document.getElementById('usersChart');
        if (usersCanvas && hasValidData(usersRoleLabels, usersRoleData)) {
            new Chart(usersCanvas.getContext('2d'), {
                type: 'doughnut',
                data: {
                    labels: usersRoleLabels,
                    datasets: [{
                        data: usersRoleData,
                        backgroundColor: [
                            '#6C5CE7', '#00B8D4', '#00E676',
                            '#FFC107', '#FF5252', '#9C27B0',
                            '#FF9800', '#795548'
                        ],
                        borderWidth: 2,
                        borderColor: 'rgba(255, 255, 255, 0.1)'
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: {
                            position: 'bottom',
                            labels: { color: '#dcd6f7', padding: 20, usePointStyle: true }
                        },
                        tooltip: {
                            callbacks: {
                                label: function (context) {
                                    const label = context.label || '';
                                    const value = context.parsed || 0;
                                    const total = context.dataset.data.reduce((a, b) => a + b, 0);
                                    const percentage = ((value / total) * 100).toFixed(1);
                                    return `${label}: ${value} (${percentage}%)`;
                                }
                            }
                        }
                    }
                }
            });
        } else {
            showNoDataMessage('usersChart', 'usersChartNoData');
        }

        // === Competiciones por Deporte ===
        const competitionsCanvas = document.getElementById('competitionsChart');
        if (competitionsCanvas && hasValidData(competitionsSportLabels, competitionsSportData)) {
            new Chart(competitionsCanvas.getContext('2d'), {
                type: 'pie',
                data: {
                    labels: competitionsSportLabels,
                    datasets: [{
                        data: competitionsSportData,
                        backgroundColor: [
                            '#6C5CE7', '#00B8D4', '#00E676',
                            '#FFC107', '#FF5252', '#9C27B0',
                            '#FF9800', '#795548'
                        ],
                        borderWidth: 2,
                        borderColor: 'rgba(255, 255, 255, 0.1)'
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: {
                            position: 'bottom',
                            labels: { color: '#dcd6f7', padding: 20, usePointStyle: true }
                        },
                        tooltip: {
                            callbacks: {
                                label: function (context) {
                                    const label = context.label || '';
                                    const value = context.parsed || 0;
                                    const total = context.dataset.data.reduce((a, b) => a + b, 0);
                                    const percentage = ((value / total) * 100).toFixed(1);
                                    return `${label}: ${value} (${percentage}%)`;
                                }
                            }
                        }
                    }
                }
            });
        } else {
            showNoDataMessage('competitionsChart', 'competitionsChartNoData');
        }

        // === Animación de contadores ===
        setTimeout(function () {
            document.querySelectorAll('.stat-number').forEach(counter => {
                const target = parseInt(counter.textContent.replace(/[^0-9]/g, '')) || 0;
                const increment = Math.max(target / 50, 1);
                let current = 0;

                const timer = setInterval(() => {
                    current += increment;
                    if (current >= target) {
                        counter.textContent = target.toLocaleString();
                        clearInterval(timer);
                    } else {
                        counter.textContent = Math.ceil(current).toLocaleString();
                    }
                }, 30);
            });
        }, 500);

    } catch (error) {
        console.error('Error inicializando gráficos:', error);
    }
}

document.addEventListener('DOMContentLoaded', () => waitForChart(initializeCharts));
