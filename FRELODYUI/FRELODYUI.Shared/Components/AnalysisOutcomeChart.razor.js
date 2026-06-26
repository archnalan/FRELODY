const _charts = {};

// Daily success-vs-denied columns: analyzed songs (success/green) against requests
// turned away at the access gate (danger/red).
export function renderChart(elementId, labels, analyzed, denied) {
    if (_charts[elementId]) {
        _charts[elementId].destroy();
        delete _charts[elementId];
    }

    const el = document.getElementById(elementId);
    if (!el || typeof ApexCharts === 'undefined') return;

    const isDark = document.documentElement.getAttribute('data-bs-theme') === 'dark';
    const s = getComputedStyle(document.documentElement);
    const successColor = s.getPropertyValue('--bs-success').trim() || '#198754';
    const dangerColor = s.getPropertyValue('--bs-danger').trim() || '#dc3545';
    const textColor = isDark ? '#9ca3af' : '#6c757d';
    const gridColor = isDark ? '#374151' : '#e9ecef';

    const intFmt = v => (v == null ? '' : Math.round(v).toLocaleString('en-US'));

    const tickCount = Math.min(labels.length, 7);
    const tickCountMobile = Math.min(labels.length, 4);

    const options = {
        series: [
            { name: 'Analyzed', data: analyzed },
            { name: 'Denied', data: denied }
        ],

        chart: {
            id: elementId,
            type: 'bar',
            stacked: true,
            height: '100%',
            toolbar: { show: false },
            zoom: { enabled: false },
            background: 'transparent',
            fontFamily: 'inherit',
            animations: {
                enabled: true,
                easing: 'easeinout',
                speed: 700,
                animateGradually: { enabled: true, delay: 80 },
                dynamicAnimation: { enabled: true, speed: 350 }
            }
        },

        dataLabels: { enabled: false },
        stroke: { show: false, width: 0 },

        plotOptions: {
            bar: {
                borderRadius: 3,
                columnWidth: '60%',
                borderRadiusApplication: 'end'
            }
        },

        fill: { type: 'solid', opacity: 0.9 },

        legend: { show: false },

        xaxis: {
            categories: labels,
            tickAmount: tickCount,
            labels: {
                style: { colors: textColor, fontSize: '0.72rem' },
                rotate: 0,
                rotateAlways: false,
                hideOverlappingLabels: true
            },
            axisBorder: { show: false },
            axisTicks: { show: false },
            tooltip: { enabled: false }
        },

        yaxis: {
            min: 0,
            tickAmount: 4,
            labels: {
                style: { colors: textColor, fontSize: '0.72rem' },
                formatter: v => (Number.isInteger(v) ? v : '')
            }
        },

        grid: {
            borderColor: gridColor,
            strokeDashArray: 4,
            xaxis: { lines: { show: false } },
            padding: { left: 4, right: 8, top: 0, bottom: 0 }
        },

        tooltip: {
            theme: isDark ? 'dark' : 'light',
            shared: true,
            intersect: false,
            y: { formatter: v => intFmt(v) }
        },

        colors: [successColor, dangerColor],
        theme: { mode: isDark ? 'dark' : 'light' },

        responsive: [{
            breakpoint: 576,
            options: {
                chart: { height: 200 },
                plotOptions: { bar: { columnWidth: '70%' } },
                xaxis: { tickAmount: tickCountMobile }
            }
        }]
    };

    const chart = new ApexCharts(el, options);
    chart.render();
    _charts[elementId] = chart;
}

export function destroyChart(elementId) {
    if (_charts[elementId]) {
        _charts[elementId].destroy();
        delete _charts[elementId];
    }
}
