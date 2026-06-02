const _charts = {};

export function renderChart(elementId, labels, values, currency) {
    if (_charts[elementId]) {
        _charts[elementId].destroy();
        delete _charts[elementId];
    }

    const el = document.getElementById(elementId);
    if (!el || typeof ApexCharts === 'undefined') return;

    const isDark = document.documentElement.getAttribute('data-bs-theme') === 'dark';
    const s = getComputedStyle(document.documentElement);
    const primaryColor = s.getPropertyValue('--bg-active').trim() || '#0d6efd';
    const textColor = isDark ? '#9ca3af' : '#6c757d';
    const gridColor = isDark ? '#374151' : '#e9ecef';
    const markerBg = isDark ? '#1f2937' : '#ffffff';

    const moneyFormatter = v => {
        if (v == null) return '';
        const n = Math.round(v);
        return `${currency} ${n.toLocaleString('en-US')}`;
    };

    // Thin the date axis so 30 days don't crush together — ~7 labels on desktop,
    // ~4 on phones; ApexCharts hides the rest.
    const tickCount = Math.min(labels.length, 7);
    const tickCountMobile = Math.min(labels.length, 4);

    const options = {
        series: [{ name: currency, data: values }],

        chart: {
            id: elementId,
            type: 'area',
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

        stroke: {
            curve: 'smooth',
            width: 2.5
        },

        fill: {
            type: 'gradient',
            gradient: {
                shadeIntensity: 1,
                opacityFrom: 0.4,
                opacityTo: 0.02,
                stops: [0, 90, 100]
            }
        },

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
                formatter: v => {
                    if (v == null) return '';
                    const n = Math.round(v);
                    return n.toLocaleString('en-US');
                }
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
            x: { show: true },
            y: { formatter: moneyFormatter },
            marker: { show: true }
        },

        markers: {
            size: 4,
            colors: [primaryColor],
            strokeColors: markerBg,
            strokeWidth: 2,
            hover: { size: 7, sizeOffset: 3 }
        },

        colors: [primaryColor],

        theme: {
            mode: isDark ? 'dark' : 'light'
        },

        responsive: [{
            breakpoint: 576,
            options: {
                chart: { height: 200 },
                markers: { size: 3 },
                stroke: { width: 2 },
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
