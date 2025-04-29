/**
 * Report functionality for TimeCardSystem
 */
document.addEventListener('DOMContentLoaded', function () {
    // Elements
    const reportTypeSelect = document.getElementById('reportType');
    const dateRangeSelect = document.getElementById('dateRange');
    const customDateRange = document.querySelector('.custom-date-range');
    const employeeFilterRow = document.getElementById('employeeFilterRow');
    const groupByContainer = document.getElementById('groupByContainer');
    const clearFiltersBtn = document.getElementById('clearFiltersBtn');
    const exportReportBtn = document.getElementById('exportReportBtn');
    const confirmExportBtn = document.getElementById('confirmExportBtn');
    const searchReportDataInput = document.getElementById('searchReportData');
    const reportFiltersForm = document.getElementById('reportFiltersForm');

    // Bootstrap modal instances
    let exportOptionsModal;

    // Initialize Bootstrap modals
    if (typeof bootstrap !== 'undefined') {
        exportOptionsModal = new bootstrap.Modal(document.getElementById('exportOptionsModal'));
    }

    // Initialize Chart if needed
    let reportChart;
    initializeChart();

    /**
     * Show/hide filters based on report type
     */
    function toggleFilterVisibility() {
        const reportType = reportTypeSelect.value;

        // Reset the display for all filter rows
        employeeFilterRow.style.display = 'none';
        groupByContainer.style.display = 'none';

        // Show relevant filters based on report type
        switch (reportType) {
            case 'individual':
                employeeFilterRow.style.display = '';
                break;
            case 'overtime':
                groupByContainer.style.display = '';
                break;
        }
    }

    /**
     * Show/hide custom date range inputs
     */
    function toggleCustomDateRange() {
        const isCustom = dateRangeSelect.value === 'custom';
        customDateRange.style.display = isCustom ? '' : 'none';
    }

    /**
     * Initialize Chart.js chart
     */
    function initializeChart() {
        const chartElement = document.getElementById('reportChart');
        if (!chartElement) return;

        // Get chart data from the chart container's data attributes
        const chartContainer = chartElement.parentElement;
        let chartLabels = [];
        let regularHoursData = [];
        let overtimeHoursData = [];

        try {
            // Parse the data from data attributes
            if (chartContainer.dataset.labels) {
                chartLabels = JSON.parse(chartContainer.dataset.labels);
            }
            if (chartContainer.dataset.regularHours) {
                regularHoursData = JSON.parse(chartContainer.dataset.regularHours);
            }
            if (chartContainer.dataset.overtimeHours) {
                overtimeHoursData = JSON.parse(chartContainer.dataset.overtimeHours);
            }
        } catch (error) {
            console.error('Error parsing chart data:', error);
        }

        // Create chart
        reportChart = new Chart(chartElement, {
            type: 'bar',
            data: {
                labels: chartLabels,
                datasets: [
                    {
                        label: 'Regular Hours',
                        data: regularHoursData,
                        backgroundColor: 'rgba(54, 162, 235, 0.6)',
                        borderColor: 'rgba(54, 162, 235, 1)',
                        borderWidth: 1
                    },
                    {
                        label: 'Overtime Hours',
                        data: overtimeHoursData,
                        backgroundColor: 'rgba(255, 99, 132, 0.6)',
                        borderColor: 'rgba(255, 99, 132, 1)',
                        borderWidth: 1
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                scales: {
                    y: {
                        beginAtZero: true,
                        title: {
                            display: true,
                            text: 'Hours'
                        }
                    },
                    x: {
                        title: {
                            display: true,
                            text: 'Date/Period'
                        }
                    }
                },
                plugins: {
                    legend: {
                        position: 'top',
                    },
                    tooltip: {
                        callbacks: {
                            label: function (context) {
                                return context.dataset.label + ': ' + context.raw.toFixed(1) + ' hrs';
                            }
                        }
                    }
                }
            }
        });
    }

    /**
     * Filter report table data
     */
    function filterReportData() {
        const searchTerm = searchReportDataInput.value.toLowerCase();
        const rows = document.querySelectorAll('#reportTable tbody tr');

        rows.forEach(row => {
            let found = false;
            const cells = row.querySelectorAll('td');

            cells.forEach(cell => {
                if (cell.textContent.toLowerCase().includes(searchTerm)) {
                    found = true;
                }
            });

            row.style.display = found ? '' : 'none';
        });
    }

    /**
     * Clear all filters
     */
    function clearFilters() {
        reportTypeSelect.value = 'individual';
        dateRangeSelect.value = 'thisWeek';
        document.getElementById('startDate').value = '';
        document.getElementById('endDate').value = '';
        document.getElementById('employeeId').value = '';
        document.getElementById('groupBy').value = 'day';
        document.getElementById('includeDetails').checked = false;

        // Update form layout based on new values
        toggleFilterVisibility();
        toggleCustomDateRange();
    }

    /**
     * Export report
     */
    function exportReport() {
        const format = document.getElementById('exportFormat').value;
        const includeCharts = document.getElementById('includeCharts').checked;
        const includeSummary = document.getElementById('includeSummary').checked;

        // Build export URL with current filter parameters
        const url = new URL(window.location.href);
        url.pathname = url.pathname.replace('Reports', 'Reports/ExportReport');
        url.searchParams.append('format', format);
        url.searchParams.append('includeCharts', includeCharts);
        url.searchParams.append('includeSummary', includeSummary);

        // Redirect to the export handler
        window.location.href = url.toString();
    }

    /**
     * Event Listeners
     */
    if (reportTypeSelect) {
        reportTypeSelect.addEventListener('change', toggleFilterVisibility);
    }

    if (dateRangeSelect) {
        dateRangeSelect.addEventListener('change', toggleCustomDateRange);
    }

    if (clearFiltersBtn) {
        clearFiltersBtn.addEventListener('click', clearFilters);
    }

    if (exportReportBtn) {
        exportReportBtn.addEventListener('click', function () {
            exportOptionsModal.show();
        });
    }

    if (confirmExportBtn) {
        confirmExportBtn.addEventListener('click', function () {
            exportReport();
            exportOptionsModal.hide();
        });
    }

    if (searchReportDataInput) {
        searchReportDataInput.addEventListener('input', filterReportData);
    }

    // Initialize filter visibility based on current selections
    toggleFilterVisibility();
    toggleCustomDateRange();

    /**
     * Show status message
     */
    function showStatusMessage(message, isError = false) {
        const container = document.getElementById('statusMessageContainer');
        const messageText = document.getElementById('statusMessageText');

        if (container && messageText) {
            container.classList.remove('d-none');
            container.querySelector('.alert').className = `alert ${isError ? 'alert-danger' : 'alert-success'} alert-dismissible fade show`;
            messageText.textContent = message;

            // Auto-hide after 5 seconds
            setTimeout(() => {
                container.classList.add('d-none');
            }, 5000);
        }
    }
});