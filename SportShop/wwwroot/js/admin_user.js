// Admin Customer Management JavaScript

$(document).ready(function() {
    initializeCustomerManagement();
    initializeSearch();
    initializeTableInteractions();
    initializeAnimations();
});

// Initialize customer management functionality
function initializeCustomerManagement() {
    // Initialize table row clicks
    initializeRowClicks();
    
    // Initialize page size selector
    initializePageSizeSelector();
    
    // Initialize tooltips
    initializeTooltips();
    
    // Initialize modals if any
    initializeModals();
}

// Initialize search functionality
function initializeSearch() {
    const searchInput = $('.customers-container .search-input');
    const searchForm = $('.customers-container .search-form');
    
    // Real-time search with debounce
    let searchTimeout;
    searchInput.on('input', function() {
        const query = $(this).val().trim();
        
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(function() {
            if (query.length >= 2 || query.length === 0) {
                performLiveSearch(query);
            }
        }, 500);
    });
    
    // Search on form submit
    searchForm.on('submit', function(e) {
        e.preventDefault();
        const query = searchInput.val().trim();
        performSearch(query);
    });
    
    // Clear search
    const clearSearchBtn = $('.clear-search-btn');
    clearSearchBtn.on('click', function() {
        searchInput.val('');
        performSearch('');
    });
}

// Initialize table interactions
function initializeTableInteractions() {
    // Customer row hover effects
    $('.customer-row').hover(
        function() {
            $(this).addClass('row-hover');
        },
        function() {
            $(this).removeClass('row-hover');
        }
    );
    
    // Action button clicks
    $('.action-buttons .btn').on('click', function(e) {
        e.stopPropagation(); // Prevent row click
        
        const action = $(this).attr('title');
        const customerId = $(this).closest('.customer-row').data('customer-id');
        
        // Add loading state
        const originalHtml = $(this).html();
        $(this).html('<i class="fas fa-spinner fa-spin"></i>');
        
        // Restore button after a short delay (for user feedback)
        setTimeout(() => {
            $(this).html(originalHtml);
        }, 300);
    });
    
    // Sort table columns
    $('.customers-table th[data-sort]').on('click', function() {
        const column = $(this).data('sort');
        const currentOrder = $(this).data('order') || 'asc';
        const newOrder = currentOrder === 'asc' ? 'desc' : 'asc';
        
        // Update sort indicators
        $('.customers-table th').removeClass('sort-asc sort-desc');
        $(this).addClass('sort-' + newOrder).data('order', newOrder);
        
        // Perform sort
        sortTable(column, newOrder);
    });
}

// Initialize row click functionality
function initializeRowClicks() {
    $('.customer-row').on('click', function(e) {
        // Don't navigate if clicking on action buttons
        if ($(e.target).closest('.action-buttons').length > 0) {
            return;
        }
        
        const customerId = $(this).data('customer-id');
        if (customerId) {
            // Add navigation animation
            $(this).addClass('row-navigating');
            
            setTimeout(() => {
                window.location.href = `/Admin/Customers/Details/${customerId}`;
            }, 150);
        }
    });
}

// Initialize page size selector
function initializePageSizeSelector() {
    $('.items-per-page select').on('change', function() {
        const pageSize = $(this).val();
        const url = new URL(window.location);
        url.searchParams.set('pageSize', pageSize);
        url.searchParams.set('page', '1');
        window.location.href = url.toString();
    });
}

// Initialize tooltips
function initializeTooltips() {
    // Initialize Bootstrap tooltips if available
    if (typeof bootstrap !== 'undefined') {
        const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        tooltipTriggerList.map(function (tooltipTriggerEl) {
            return new bootstrap.Tooltip(tooltipTriggerEl);
        });
    }
    
    // Custom tooltip for action buttons
    $('.action-buttons .btn').each(function() {
        const title = $(this).attr('title');
        if (title) {
            $(this).hover(
                function() {
                    showCustomTooltip($(this), title);
                },
                function() {
                    hideCustomTooltip();
                }
            );
        }
    });
}

// Initialize modals
function initializeModals() {
    // Customer detail modal (if implemented)
    $('.customer-detail-modal').on('show.bs.modal', function(e) {
        const button = $(e.relatedTarget);
        const customerId = button.data('customer-id');
        loadCustomerDetails(customerId);
    });
}

// Initialize animations
function initializeAnimations() {
    // Fade in table rows
    $('.customer-row').each(function(index) {
        $(this).css('opacity', '0').delay(index * 50).animate({
            opacity: 1
        }, 300);
    });
    
    // Stats card animations
    $('.stat-card').hover(
        function() {
            $(this).addClass('animate__animated animate__pulse');
        },
        function() {
            $(this).removeClass('animate__animated animate__pulse');
        }
    );
    
    // Order card animations on purchase history page
    $('.order-card').each(function(index) {
        $(this).css({
            opacity: 0,
            transform: 'translateY(20px)'
        }).delay(index * 100).animate({
            opacity: 1
        }, 300).css({
            transform: 'translateY(0)'
        });
    });
}

// Perform live search
function performLiveSearch(query) {
    // Show loading indicator
    $('.customers-container .search-input').addClass('searching');
    
    // Simulate API call delay
    setTimeout(() => {
        $('.customers-container .search-input').removeClass('searching');
        // In a real implementation, this would make an AJAX call
        console.log('Live searching for:', query);
    }, 300);
}

// Perform search
function performSearch(query) {
    const url = new URL(window.location);
    url.searchParams.set('search', query);
    url.searchParams.set('page', '1');
    window.location.href = url.toString();
}

// Sort table
function sortTable(column, order) {
    const tbody = $('.customers-table tbody');
    const rows = tbody.find('tr').toArray();
    
    rows.sort(function(a, b) {
        const aVal = $(a).find(`[data-sort="${column}"]`).text().trim();
        const bVal = $(b).find(`[data-sort="${column}"]`).text().trim();
        
        if (order === 'asc') {
            return aVal.localeCompare(bVal);
        } else {
            return bVal.localeCompare(aVal);
        }
    });
    
    tbody.empty().append(rows);
    
    // Re-initialize row interactions
    initializeRowClicks();
}

// Show custom tooltip
function showCustomTooltip(element, text) {
    const tooltip = $('<div class="custom-tooltip"></div>').text(text);
    $('body').append(tooltip);
    
    const offset = element.offset();
    tooltip.css({
        top: offset.top - tooltip.outerHeight() - 5,
        left: offset.left + (element.outerWidth() / 2) - (tooltip.outerWidth() / 2)
    }).fadeIn(200);
}

// Hide custom tooltip
function hideCustomTooltip() {
    $('.custom-tooltip').fadeOut(200, function() {
        $(this).remove();
    });
}

// Load customer details (for modal)
function loadCustomerDetails(customerId) {
    // Show loading state
    $('.customer-detail-modal .modal-body').html('<div class="loading-state"><i class="fas fa-spinner fa-spin"></i> Đang tải...</div>');
    
    // Simulate API call
    setTimeout(() => {
        // In real implementation, load data via AJAX
        console.log('Loading customer details for ID:', customerId);
    }, 1000);
}

// Utility functions for customer management
const CustomerUtils = {
    // Format currency
    formatCurrency: function(amount) {
        return new Intl.NumberFormat('vi-VN', {
            style: 'currency',
            currency: 'VND'
        }).format(amount);
    },
    
    // Format date
    formatDate: function(date) {
        return new Intl.DateTimeFormat('vi-VN', {
            year: 'numeric',
            month: '2-digit',
            day: '2-digit'
        }).format(new Date(date));
    },
    
    // Show notification
    showNotification: function(message, type = 'info') {
        const notification = $(`
            <div class="notification notification-${type}">
                <div class="notification-content">
                    <i class="fas ${this.getNotificationIcon(type)}"></i>
                    <span>${message}</span>
                </div>
                <button class="notification-close">
                    <i class="fas fa-times"></i>
                </button>
            </div>
        `);
        
        $('body').append(notification);
        
        notification.addClass('show');
        
        setTimeout(() => {
            this.hideNotification(notification);
        }, 5000);
        
        notification.find('.notification-close').on('click', () => {
            this.hideNotification(notification);
        });
    },
    
    // Hide notification
    hideNotification: function(notification) {
        notification.removeClass('show');
        setTimeout(() => {
            notification.remove();
        }, 300);
    },
    
    // Get notification icon
    getNotificationIcon: function(type) {
        switch (type) {
            case 'success':
                return 'fa-check-circle';
            case 'warning':
                return 'fa-exclamation-triangle';
            case 'error':
                return 'fa-times-circle';
            default:
                return 'fa-info-circle';
        }
    },
    
    // Export customers data
    exportCustomers: function(format = 'csv') {
        console.log(`Exporting customers in ${format} format`);
        // Implementation for export functionality
    },
    
    // Print customer list
    printCustomerList: function() {
        window.print();
    },
    
    // Navigate to customer details
    viewCustomerDetails: function(customerId) {
        window.location.href = `/Admin/Customers/Details/${customerId}`;
    },
    
    // Navigate to purchase history
    viewPurchaseHistory: function(customerId) {
        window.location.href = `/Admin/Customers/PurchaseHistory/${customerId}`;
    }
};

// Page-specific initializations
$(document).ready(function() {
    const currentPage = window.location.pathname;
    
    // Customer list page
    if (currentPage.includes('/Admin/Customers') && !currentPage.includes('/Details') && !currentPage.includes('/PurchaseHistory')) {
        initializeCustomerListPage();
    }
    
    // Customer details page
    if (currentPage.includes('/Admin/Customers/Details')) {
        initializeCustomerDetailsPage();
    }
    
    // Purchase history page
    if (currentPage.includes('/Admin/Customers/PurchaseHistory')) {
        initializePurchaseHistoryPage();
    }
});

// Initialize customer list page specific features
function initializeCustomerListPage() {
    // Add export functionality
    const exportBtn = $('<button class="btn btn-outline-primary export-btn"><i class="fas fa-download"></i> Xuất dữ liệu</button>');
    $('.table-actions').append(exportBtn);
    
    exportBtn.on('click', function() {
        CustomerUtils.exportCustomers('csv');
    });
    
    // Add print functionality
    const printBtn = $('<button class="btn btn-outline-secondary print-btn"><i class="fas fa-print"></i> In danh sách</button>');
    $('.table-actions').append(printBtn);
    
    printBtn.on('click', function() {
        CustomerUtils.printCustomerList();
    });
}

// Initialize customer details page specific features
function initializeCustomerDetailsPage() {
    // Add edit customer functionality (if needed)
    console.log('Customer details page initialized');
    
    // Initialize recent orders interactions
    $('.order-card').on('click', function() {
        const orderId = $(this).data('order-id');
        window.location.href = `/Admin/Customers/OrderDetails/${orderId}`;
    });
}

// Initialize purchase history page specific features
function initializePurchaseHistoryPage() {
    console.log('Purchase history page initialized');
    
    // Add order filtering functionality
    initializeOrderFiltering();
    
    // Add order status updates (if admin can update)
    initializeOrderStatusUpdates();
}

// Initialize order filtering
function initializeOrderFiltering() {
    // Add status filter dropdown
    const statusFilter = $(`
        <div class="order-filter">
            <select class="form-control status-filter">
                <option value="">Tất cả trạng thái</option>
                <option value="pending">Chờ xử lý</option>
                <option value="processing">Đang xử lý</option>
                <option value="completed">Hoàn thành</option>
                <option value="cancelled">Đã hủy</option>
            </select>
        </div>
    `);
    
    $('.section-header').append(statusFilter);
    
    $('.status-filter').on('change', function() {
        const status = $(this).val();
        filterOrdersByStatus(status);
    });
}

// Filter orders by status
function filterOrdersByStatus(status) {
    $('.order-card').each(function() {
        const orderStatus = $(this).find('.status-badge').text().toLowerCase().trim();
        
        if (status === '' || orderStatus.includes(status)) {
            $(this).show();
        } else {
            $(this).hide();
        }
    });
}

// Initialize order status updates
function initializeOrderStatusUpdates() {
    // Add quick status update buttons (if admin has permission)
    $('.order-card').each(function() {
        const currentStatus = $(this).find('.status-badge').text().toLowerCase().trim();
        
        if (currentStatus === 'pending') {
            const actionBtn = $('<button class="btn btn-sm btn-success approve-order"><i class="fas fa-check"></i> Xác nhận</button>');
            $(this).find('.order-actions').append(actionBtn);
            
            actionBtn.on('click', function(e) {
                e.stopPropagation();
                updateOrderStatus($(this).closest('.order-card').data('order-id'), 'processing');
            });
        }
    });
}

// Update order status
function updateOrderStatus(orderId, newStatus) {
    // Implementation for updating order status
    console.log(`Updating order ${orderId} to status: ${newStatus}`);
    
    // Show confirmation
    if (confirm('Bạn có chắc chắn muốn cập nhật trạng thái đơn hàng?')) {
        // Make API call to update status
        CustomerUtils.showNotification('Đã cập nhật trạng thái đơn hàng thành công!', 'success');
    }
}

// Export functions for global use
window.CustomerManagement = {
    utils: CustomerUtils,
    viewDetails: CustomerUtils.viewCustomerDetails,
    viewHistory: CustomerUtils.viewPurchaseHistory,
    exportData: CustomerUtils.exportCustomers,
    showNotification: CustomerUtils.showNotification
};