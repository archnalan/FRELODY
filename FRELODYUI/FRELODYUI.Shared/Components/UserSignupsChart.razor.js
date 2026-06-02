const _charts = {};

// mode: "daily" → new accounts per day (columns); "cumulative" → running total (smooth area)
export function renderChart(elementId, labels, values, mode) {
    if (_charts[elementId]) {
        _charts[elementId].destroy();
        delete _charts[elementId];
    }

    const el = document.getElementById(elementId);
    if (!el || typeof ApexCharts === 'undefined') return;

    const isDark = document.documentElement.getAttribute('data-bs-theme') === 'dark';
    const s = getComputedStyle(document.documentElement);
    const primaryColor = s.getPropertyValue('--bg-active').trim() || '#0d6efd';
    const accentColor = s.getPropertyValue('--k-accent').trim() || '#4f46e5';
    const textColor = isDark ? '#9ca3af' : '#6c757d';
    const gridColor = isDark ? '#374151' : '#e9ecef';
    const markerBg = isDark ? '#1f2937' : '#ffffff';

    const isCumulative = mode === 'cumulative';
    const color = isCumulative ? accentColor : primaryColor;
    const intFmt = v => (v == null ? '' : Math.round(v).toLocaleString('en-US'));

    // Thin the date axis so 30 days don't crush together — show ~7 evenly-spaced
    // labels on desktop, ~4 on phones; ApexCharts hides the rest.
    const tickCount = Math.min(labels.length, 7);
    const tickCountMobile = Math.min(labels.length, 4);

    const options = {
        series: [{
            name: isCumulative ? 'Total accounts' : 'New accounts',
            data: values
        }],

        chart: {
            id: elementId,
            type: isCumulative ? 'area' : 'bar',
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

        stroke: isCumulative
            ? { curve: 'smooth', width: 2.5 }
            : { show: false, width: 0 },

        plotOptions: {
            bar: {
                borderRadius: 4,
                columnWidth: '55%',
                borderRadiusApplication: 'end'
            }
        },

        fill: isCumulative
            ? {
                type: 'gradient',
                gradient: {
                    shadeIntensity: 1,
                    opacityFrom: 0.4,
                    opacityTo: 0.02,
                    stops: [0, 90, 100]
                }
            }
            : { type: 'solid', opacity: 0.9 },

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
            x: { show: true },
            y: {
                formatter: v => {
                    const n = intFmt(v);
                    return isCumulative
                        ? `${n} total`
                        : `${n} new account${v !== 1 ? 's' : ''}`;
                }
            },
            marker: { show: true }
        },

        markers: isCumulative
            ? { size: 4, colors: [color], strokeColors: markerBg, strokeWidth: 2, hover: { size: 7, sizeOffset: 3 } }
            : { size: 0 },

        colors: [color],

        theme: { mode: isDark ? 'dark' : 'light' },

        responsive: [{
            breakpoint: 576,
            options: {
                chart: { height: 200 },
                stroke: isCumulative ? { width: 2 } : { width: 0 },
                plotOptions: { bar: { columnWidth: '65%' } },
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
