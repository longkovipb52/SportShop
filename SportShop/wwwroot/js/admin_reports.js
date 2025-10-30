// Admin Reports JavaScript
class ReportsManager {
    constructor() {
        this.charts = {};
        this.filters = {};
        this.isLoading = false;
        
        this.init();
    }

    init() {
        // Check if Chart.js is loaded
        if (typeof Chart === 'undefined') {
            console.error('Chart.js is not loaded!');
            return;
        }
        
        this.initializeDatePicker();
        this.initializeFilters();
        this.initializeCharts();
        this.bindEvents();
        
        // Load initial data after a short delay to ensure everything is ready
        setTimeout(() => {
            this.loadInitialData();
        }, 100);
    }

    // Date Picker Initialization
    initializeDatePicker() {
        $('#dateRange').daterangepicker({
            opens: 'left',
            locale: {
                format: 'DD/MM/YYYY',
                separator: ' - ',
                applyLabel: 'Áp dụng',
                cancelLabel: 'Hủy',
                fromLabel: 'Từ',
                toLabel: 'Đến',
                customRangeLabel: 'Tùy chọn',
                weekLabel: 'T',
                daysOfWeek: ['CN', 'T2', 'T3', 'T4', 'T5', 'T6', 'T7'],
                monthNames: [
                    'Tháng 1', 'Tháng 2', 'Tháng 3', 'Tháng 4', 'Tháng 5', 'Tháng 6',
                    'Tháng 7', 'Tháng 8', 'Tháng 9', 'Tháng 10', 'Tháng 11', 'Tháng 12'
                ],
                firstDay: 1
            },
            ranges: {
                'Hôm nay': [moment(), moment()],
                'Hôm qua': [moment().subtract(1, 'days'), moment().subtract(1, 'days')],
                '7 ngày qua': [moment().subtract(6, 'days'), moment()],
                '30 ngày qua': [moment().subtract(29, 'days'), moment()],
                'Tháng này': [moment().startOf('month'), moment().endOf('month')],
                'Tháng trước': [moment().subtract(1, 'month').startOf('month'), moment().subtract(1, 'month').endOf('month')],
                'Quý này': [moment().startOf('quarter'), moment().endOf('quarter')],
                'Năm này': [moment().startOf('year'), moment().endOf('year')]
            }
        });
    }

    // Initialize Filters
    initializeFilters() {
        this.filters = this.getFilterValues();
    }

    getFilterValues() {
        const dateRange = $('#dateRange').val().split(' - ');
        return {
            startDate: moment(dateRange[0], 'DD/MM/YYYY').format('YYYY-MM-DD'),
            endDate: moment(dateRange[1], 'DD/MM/YYYY').format('YYYY-MM-DD'),
            period: $('#period').val(),
            categoryID: $('#categoryFilter').val() || null,
            brandID: $('#brandFilter').val() || null,
            pageSize: 10
        };
    }

    // Charts Initialization
    initializeCharts() {
        try {
            this.initializeRevenueChart();
            this.initializeProductChart();
            this.initializePaymentChart();
            console.log('Charts initialized successfully');
        } catch (error) {
            console.error('Error initializing charts:', error);
            this.hideLoadingState();
        }
    }

    initializeRevenueChart() {
        const ctx = document.getElementById('revenueChart');
        if (!ctx) {
            console.error('Revenue chart canvas not found');
            return;
        }

        try {
            this.charts.revenue = new Chart(ctx, {
            type: 'line',
            data: {
                labels: [],
                datasets: [{
                    label: 'Doanh thu',
                    data: [],
                    borderColor: '#5E72E4',
                    backgroundColor: 'rgba(94, 114, 228, 0.1)',
                    borderWidth: 3,
                    fill: true,
                    tension: 0.4,
                    pointBackgroundColor: '#5E72E4',
                    pointBorderColor: '#ffffff',
                    pointBorderWidth: 2,
                    pointRadius: 6,
                    pointHoverRadius: 8
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                interaction: {
                    intersect: false,
                    mode: 'index'
                },
                plugins: {
                    legend: {
                        display: false
                    },
                    tooltip: {
                        backgroundColor: 'rgba(0, 0, 0, 0.8)',
                        titleColor: '#ffffff',
                        bodyColor: '#ffffff',
                        borderColor: '#5E72E4',
                        borderWidth: 1,
                        cornerRadius: 8,
                        callbacks: {
                            label: function(context) {
                                return 'Doanh thu: ' + new Intl.NumberFormat('vi-VN').format(context.parsed.y) + ' VNĐ';
                            }
                        }
                    }
                },
                scales: {
                    x: {
                        grid: {
                            display: false
                        },
                        ticks: {
                            color: '#8898aa',
                            font: {
                                size: 12
                            }
                        }
                    },
                    y: {
                        grid: {
                            color: 'rgba(136, 152, 170, 0.1)'
                        },
                        ticks: {
                            color: '#8898aa',
                            font: {
                                size: 12
                            },
                            callback: function(value) {
                                return new Intl.NumberFormat('vi-VN', {
                                    notation: 'compact',
                                    compactDisplay: 'short'
                                }).format(value) + ' VNĐ';
                            }
                        }
                    }
                },
                animation: {
                    duration: 2000,
                    easing: 'easeInOutQuart'
                }
            }
        });
        } catch (error) {
            console.error('Error initializing revenue chart:', error);
        }
    }

    initializeProductChart() {
        const ctx = document.getElementById('productChart');
        if (!ctx) {
            console.error('Product chart canvas not found');
            return;
        }

        try {

        this.charts.product = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: [],
                datasets: [{
                    label: 'Doanh thu sản phẩm',
                    data: [],
                    backgroundColor: [
                        '#2dce89', '#5e72e4', '#fb6340', '#f5365c', '#11cdef',
                        '#ffd600', '#8965e0', '#f3a4b5', '#20bf6b', '#fd7f00'
                    ],
                    borderColor: 'rgba(255, 255, 255, 0.8)',
                    borderWidth: 2,
                    borderRadius: 8,
                    borderSkipped: false
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: false
                    },
                    tooltip: {
                        backgroundColor: 'rgba(0, 0, 0, 0.8)',
                        titleColor: '#ffffff',
                        bodyColor: '#ffffff',
                        borderColor: '#2dce89',
                        borderWidth: 1,
                        cornerRadius: 8,
                        callbacks: {
                            label: function(context) {
                                return 'Doanh thu: ' + new Intl.NumberFormat('vi-VN').format(context.parsed.y) + ' VNĐ';
                            }
                        }
                    }
                },
                scales: {
                    x: {
                        grid: {
                            display: false
                        },
                        ticks: {
                            color: '#8898aa',
                            font: {
                                size: 11
                            },
                            maxRotation: 45
                        }
                    },
                    y: {
                        grid: {
                            color: 'rgba(136, 152, 170, 0.1)'
                        },
                        ticks: {
                            color: '#8898aa',
                            font: {
                                size: 12
                            },
                            callback: function(value) {
                                return new Intl.NumberFormat('vi-VN', {
                                    notation: 'compact',
                                    compactDisplay: 'short'
                                }).format(value) + ' VNĐ';
                            }
                        }
                    }
                },
                animation: {
                    duration: 2000,
                    easing: 'easeInOutQuart'
                }
            }
        });
        } catch (error) {
            console.error('Error initializing product chart:', error);
        }
    }

    initializePaymentChart() {
        const ctx = document.getElementById('paymentChart');
        if (!ctx) {
            console.error('Payment chart canvas not found');
            return;
        }

        try {

        this.charts.payment = new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: [],
                datasets: [{
                    data: [],
                    backgroundColor: [
                        '#e91e63', // MoMo - Pink
                        '#2196f3', // VNPay - Blue  
                        '#4caf50'  // COD - Green
                    ],
                    borderColor: '#ffffff',
                    borderWidth: 3,
                    hoverBorderWidth: 4
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                cutout: '60%',
                plugins: {
                    legend: {
                        position: 'bottom',
                        labels: {
                            padding: 20,
                            usePointStyle: true,
                            font: {
                                size: 12,
                                weight: '600'
                            }
                        }
                    },
                    tooltip: {
                        backgroundColor: 'rgba(0, 0, 0, 0.8)',
                        titleColor: '#ffffff',
                        bodyColor: '#ffffff',
                        borderWidth: 1,
                        cornerRadius: 8,
                        callbacks: {
                            label: function(context) {
                                const total = context.dataset.data.reduce((a, b) => a + b, 0);
                                const percentage = ((context.parsed / total) * 100).toFixed(1);
                                return context.label + ': ' + 
                                       new Intl.NumberFormat('vi-VN').format(context.parsed) + ' VNĐ' +
                                       ' (' + percentage + '%)';
                            }
                        }
                    }
                },
                animation: {
                    animateRotate: true,
                    duration: 2000
                }
            }
        });
        } catch (error) {
            console.error('Error initializing payment chart:', error);
        }
    }

    // Event Bindings
    bindEvents() {
        // Filter Events
        $('#applyFilter').on('click', () => this.applyFilters());
        $('#refreshData').on('click', () => this.refreshData());
        $('#exportReport').on('click', () => this.exportReport());

        // Chart Type Toggle
        $('[data-chart="revenue"]').on('click', (e) => {
            const type = $(e.currentTarget).data('type');
            this.changeChartType('revenue', type);
            $('[data-chart="revenue"]').removeClass('active');
            $(e.currentTarget).addClass('active');
        });

        // Product Chart Limit
        $('#productChartLimit').on('change', () => this.loadReportsData());

        // Export Buttons
        $('#exportProducts').on('click', () => this.exportTableData('products'));
        $('#exportCustomers').on('click', () => this.exportTableData('customers'));

        // Keyboard Shortcuts
        $(document).on('keydown', (e) => {
            if (e.ctrlKey || e.metaKey) {
                switch(e.key) {
                    case 'r':
                        e.preventDefault();
                        this.refreshData();
                        break;
                    case 'e':
                        e.preventDefault();
                        this.exportReport();
                        break;
                }
            }
        });

        // Auto-refresh every 5 minutes
        setInterval(() => {
            if (!this.isLoading) {
                this.refreshData(true); // Silent refresh
            }
        }, 300000);
    }

    // Load Initial Data
    loadInitialData() {
        console.log('Loading initial data...', window.reportsData);
        
        if (window.reportsData) {
            try {
                // Ensure data has the required structure
                const revenueData = window.reportsData.revenueData || this.getEmptyChartData();
                const productData = window.reportsData.productData || this.getEmptyChartData();
                const paymentData = window.reportsData.paymentData || this.getEmptyChartData();
                
                this.updateRevenueChart(revenueData);
                this.updateProductChart(productData);
                this.updatePaymentChart(paymentData);
                this.hideLoadingState();
            } catch (error) {
                console.error('Error loading initial data:', error);
                this.loadEmptyCharts();
                this.hideLoadingState();
            }
        } else {
            console.warn('No initial data found, loading empty charts...');
            this.loadEmptyCharts();
            this.hideLoadingState();
        }
    }
    
    // Load empty charts with default data
    loadEmptyCharts() {
        const emptyData = this.getEmptyChartData();
        this.updateRevenueChart(emptyData);
        this.updateProductChart(emptyData);
        this.updatePaymentChart(emptyData);
    }
    
    // Get empty chart data structure
    getEmptyChartData() {
        return {
            labels: ['Không có dữ liệu'],
            data: [0],
            label: 'Không có dữ liệu',
            color: '#dee2e6',
            type: 'line'
        };
    }

    // Apply Filters
    applyFilters() {
        this.filters = this.getFilterValues();
        this.loadReportsData(true); // Pass true to indicate this is from filter apply
    }

    // Refresh Data
    refreshData(silent = false) {
        if (!silent) {
            this.showToast('Đang làm mới dữ liệu...', 'info');
        }
        this.loadReportsData();
    }

    // Load Reports Data
    async loadReportsData(fromFilter = false) {
        if (this.isLoading) return;
        
        this.isLoading = true;
        this.showLoadingState();

        try {
            // Load all data in parallel
            const [revenueData, productData, customerData, paymentData, overviewData] = await Promise.all([
                this.loadRevenueData(),
                this.loadProductData(),
                this.loadCustomerData(),
                this.loadPaymentData(),
                this.loadOverviewData()
            ]);

            // Update charts
            this.updateRevenueChart(revenueData.data);
            this.updateProductChart(productData.data);
            this.updatePaymentChart(paymentData.data);

            // Update tables
            this.updateProductsTable(productData.details);
            this.updateCustomersTable(customerData.data);
            this.updatePaymentTable(paymentData.details);

            // Update overview cards
            this.updateOverviewCards(overviewData.data);

            this.hideLoadingState();
            
            // Show appropriate success message
            if (fromFilter) {
                this.showToast('Đã áp dụng bộ lọc thành công', 'success');
            } else {
                this.showToast('Đã cập nhật dữ liệu thành công', 'success');
            }

        } catch (error) {
            console.error('Error loading reports data:', error);
            this.hideLoadingState();
            this.showToast('Có lỗi xảy ra khi tải dữ liệu', 'error');
        } finally {
            this.isLoading = false;
        }
    }

    // Load Revenue Data
    async loadRevenueData() {
        const response = await fetch('/Admin/Reports/GetRevenueData?' + new URLSearchParams(this.filters));
        return await response.json();
    }

    // Load Product Data
    async loadProductData() {
        const limit = $('#productChartLimit').val();
        const params = { ...this.filters, pageSize: limit };
        const response = await fetch('/Admin/Reports/GetProductData?' + new URLSearchParams(params));
        return await response.json();
    }

    // Load Customer Data
    async loadCustomerData() {
        const response = await fetch('/Admin/Reports/GetCustomerData?' + new URLSearchParams(this.filters));
        return await response.json();
    }

    // Load Payment Data
    async loadPaymentData() {
        const response = await fetch('/Admin/Reports/GetPaymentData?' + new URLSearchParams(this.filters));
        return await response.json();
    }

    // Load Overview Data
    async loadOverviewData() {
        const response = await fetch('/Admin/Reports/GetOverviewData');
        return await response.json();
    }

    // Update Charts
    updateRevenueChart(chartData) {
        if (!this.charts.revenue) {
            console.error('Revenue chart not initialized');
            return;
        }
        
        if (!chartData || !chartData.labels || !chartData.data) {
            console.warn('Invalid revenue chart data:', chartData);
            $('#revenueLoading').hide();
            return;
        }

        this.charts.revenue.data.labels = chartData.labels;
        this.charts.revenue.data.datasets[0].data = chartData.data;
        this.charts.revenue.update('active');
        $('#revenueLoading').hide();
    }

    updateProductChart(chartData) {
        if (!this.charts.product) {
            console.error('Product chart not initialized');
            return;
        }
        
        if (!chartData || !chartData.labels || !chartData.data) {
            console.warn('Invalid product chart data:', chartData);
            $('#productLoading').hide();
            return;
        }

        this.charts.product.data.labels = chartData.labels;
        this.charts.product.data.datasets[0].data = chartData.data;
        this.charts.product.update('active');
        $('#productLoading').hide();
    }

    updatePaymentChart(chartData) {
        if (!this.charts.payment) {
            console.error('Payment chart not initialized');
            return;
        }
        
        if (!chartData || !chartData.labels || !chartData.data) {
            console.warn('Invalid payment chart data:', chartData);
            return;
        }

        this.charts.payment.data.labels = chartData.labels;
        this.charts.payment.data.datasets[0].data = chartData.data;
        this.charts.payment.update('active');
    }

    // Change Chart Type
    changeChartType(chartName, type) {
        if (!this.charts[chartName]) return;

        this.charts[chartName].config.type = type;
        this.charts[chartName].update('none');
    }

    // Update Tables
    updateProductsTable(products) {
        if (!products || !products.length) return;

        const tbody = $('#productsTable tbody');
        tbody.empty();

        products.forEach((product, index) => {
            const row = this.createProductRow(product, index + 1);
            tbody.append(row);
        });
    }

    createProductRow(product, index) {
        const stars = this.generateStars(product.averageRating);
        
        // Smart image URL handling
        let imageUrl = '/upload/product/no-image.svg';
        if (product.imageURL) {
            imageUrl = product.imageURL.startsWith('/') || product.imageURL.startsWith('~') 
                ? product.imageURL 
                : `/upload/product/${product.imageURL}`;
        }
        
        return `
            <tr class="fade-in-up">
                <td>${index}</td>
                <td>
                    <div class="product-info">
                        <img src="${imageUrl}" 
                             alt="${product.productName}" 
                             class="product-image"
                             onerror="this.src='/upload/product/no-image.svg'">
                        <div class="product-details">
                            <strong>${product.productName || 'Không xác định'}</strong>
                            <small class="text-muted">${this.formatCurrency(product.price)}</small>
                        </div>
                    </div>
                </td>
                <td>${product.categoryName || 'Không xác định'}</td>
                <td>${product.brandName || 'Không xác định'}</td>
                <td><span class="badge bg-primary">${product.quantitySold}</span></td>
                <td><strong class="text-success">${this.formatCurrency(product.revenue)}</strong></td>
                <td>
                    <div class="rating-info">
                        ${product.reviewCount > 0 ? 
                            `<span class="rating-stars">${stars}</span>
                             <small class="text-muted">(${product.reviewCount})</small>` : 
                            '<small class="text-muted">Chưa có đánh giá</small>'
                        }
                    </div>
                </td>
                <td>
                    <a href="/Admin/Products/Details/${product.productID}" class="btn btn-sm btn-outline-primary" title="Xem chi tiết">
                        <i class="fas fa-eye"></i>
                    </a>
                </td>
            </tr>
        `;
    }

    updateCustomersTable(customers) {
        if (!customers || !customers.length) return;

        const tbody = $('#customersTable tbody');
        tbody.empty();

        customers.forEach((customer, index) => {
            const row = this.createCustomerRow(customer, index + 1);
            tbody.append(row);
        });
    }

    createCustomerRow(customer, index) {
        const typeClass = customer.customerType === 'VIP' ? 'bg-danger' : 
                         customer.customerType === 'Regular' ? 'bg-warning' : 'bg-info';
        const statusClass = customer.status === 'Active' ? 'bg-success' : 
                           customer.status === 'Potential' ? 'bg-warning' : 'bg-secondary';

        return `
            <tr class="fade-in-up">
                <td>${index}</td>
                <td>
                    <div class="customer-info">
                        <strong>${customer.userName}</strong>
                        <small class="text-muted d-block">${customer.email}</small>
                        ${customer.phone ? `<small class="text-muted d-block">${customer.phone}</small>` : ''}
                    </div>
                </td>
                <td><span class="badge ${typeClass}">${customer.customerType}</span></td>
                <td>${customer.totalOrders}</td>
                <td><strong class="text-success">${this.formatCurrency(customer.totalSpent)}</strong></td>
                <td>${this.formatCurrency(customer.averageOrderValue)}</td>
                <td>
                    ${this.formatDate(customer.lastOrderDate)}
                    <small class="text-muted d-block">${customer.daysSinceLastOrder} ngày trước</small>
                </td>
                <td><span class="badge ${statusClass}">${customer.status}</span></td>
            </tr>
        `;
    }

    updatePaymentTable(payments) {
        if (!payments || !payments.length) return;

        const tbody = $('#paymentTable tbody');
        tbody.empty();

        payments.forEach((payment) => {
            const row = this.createPaymentRow(payment);
            tbody.append(row);
        });
    }

    createPaymentRow(payment) {
        const icon = payment.paymentMethod === 'MoMo' ? 'fas fa-mobile-alt text-pink' :
                    payment.paymentMethod === 'VNPay' ? 'fas fa-credit-card text-blue' :
                    'fas fa-money-bill-wave text-green';
        
        const progressClass = payment.successRate >= 90 ? 'bg-success' :
                             payment.successRate >= 70 ? 'bg-warning' : 'bg-danger';

        return `
            <tr class="fade-in-up">
                <td>
                    <div class="payment-method">
                        <i class="${icon}"></i>
                        <strong>${payment.paymentMethod}</strong>
                    </div>
                </td>
                <td>${payment.totalTransactions}</td>
                <td><span class="badge bg-success">${payment.successfulTransactions}</span></td>
                <td><span class="badge bg-danger">${payment.failedTransactions}</span></td>
                <td>
                    <div class="progress-wrapper">
                        <div class="progress" style="height: 20px;">
                            <div class="progress-bar ${progressClass}" role="progressbar" 
                                 style="width: ${payment.successRate}%">
                                ${payment.successRate.toFixed(1)}%
                            </div>
                        </div>
                    </div>
                </td>
                <td><strong class="text-success">${this.formatCurrency(payment.totalAmount)}</strong></td>
                <td>${this.formatCurrency(payment.averageAmount)}</td>
            </tr>
        `;
    }

    updateOverviewCards(overview) {
        if (!overview) return;

        $('#monthRevenue').text(this.formatCurrency(overview.monthRevenue));
        $('#monthOrders').text(overview.monthOrders);
        $('#totalCustomers').text(overview.totalCustomers);
        $('#conversionRate').text(overview.conversionRate.toFixed(1) + '%');
    }

    // Loading States
    showLoadingState() {
        $('.chart-loading').show();
        $('.table-body').addClass('loading');
    }

    hideLoadingState() {
        $('.chart-loading').hide();
        $('.table-body').removeClass('loading');
    }

    // Export Functions
    exportReport() {
        this.showToast('Đang xuất báo cáo...', 'info');
        
        // Create export data
        const exportData = {
            filters: this.filters,
            timestamp: new Date().toISOString(),
            data: {
                revenue: this.charts.revenue?.data,
                products: this.charts.product?.data,
                payments: this.charts.payment?.data
            }
        };

        // Download as JSON (you can implement Excel export later)
        this.downloadJSON(exportData, `bao-cao-${moment().format('YYYY-MM-DD')}.json`);
        this.showToast('Đã xuất báo cáo thành công', 'success');
    }

    exportTableData(tableType) {
        this.showToast(`Đang xuất dữ liệu ${tableType}...`, 'info');
        
        // Get table data
        let data = [];
        const table = tableType === 'products' ? '#productsTable' : '#customersTable';
        
        $(table + ' tbody tr').each(function() {
            const row = [];
            $(this).find('td').each(function() {
                row.push($(this).text().trim());
            });
            data.push(row);
        });

        // Convert to CSV
        const csv = this.arrayToCSV(data);
        this.downloadCSV(csv, `${tableType}-${moment().format('YYYY-MM-DD')}.csv`);
        this.showToast(`Đã xuất dữ liệu ${tableType} thành công`, 'success');
    }

    // Utility Functions
    formatCurrency(amount) {
        return new Intl.NumberFormat('vi-VN').format(amount) + ' VNĐ';
    }

    formatDate(dateString) {
        return moment(dateString).format('DD/MM/YYYY');
    }

    generateStars(rating) {
        let stars = '';
        for (let i = 1; i <= 5; i++) {
            if (i <= rating) {
                stars += '<i class="fas fa-star text-warning"></i>';
            } else if (i - 0.5 <= rating) {
                stars += '<i class="fas fa-star-half-alt text-warning"></i>';
            } else {
                stars += '<i class="far fa-star text-muted"></i>';
            }
        }
        return stars;
    }

    downloadJSON(data, filename) {
        const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = filename;
        a.click();
        URL.revokeObjectURL(url);
    }

    downloadCSV(csvContent, filename) {
        const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = filename;
        a.click();
        URL.revokeObjectURL(url);
    }

    arrayToCSV(data) {
        return data.map(row => 
            row.map(field => `"${field.replace(/"/g, '""')}"`).join(',')
        ).join('\n');
    }

    showToast(message, type = 'info') {
        // Use existing toast system from dashboard.js
        if (window.DashboardUtils && window.DashboardUtils.showToast) {
            window.DashboardUtils.showToast(message, type);
        } else {
            console.log(`${type.toUpperCase()}: ${message}`);
        }
    }
}

// Initialize when document is ready
$(document).ready(function() {
    window.reportsManager = new ReportsManager();
});

// Export for global access
window.ReportsManager = ReportsManager;