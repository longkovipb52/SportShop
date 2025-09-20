// Admin Orders Management JavaScript

$(document).ready(function() {
    // Initialize page
    initOrdersPage();
    
    // Load statistics
    loadOrderStatistics();
    
    // Setup real-time updates
    setupRealTimeUpdates();
});

// Initialize orders page
function initOrdersPage() {
    // Setup tooltips
    $('[title]').tooltip();
    
    // Setup page size selector change handler
    $('.page-size-select').on('change', function() {
        const form = $(this).closest('form');
        form.submit();
    });
    
    // Setup search input with debounce
    let searchTimeout;
    $('.search-input').on('input', function() {
        clearTimeout(searchTimeout);
        const searchValue = $(this).val();
        
        searchTimeout = setTimeout(() => {
            if (searchValue.length >= 3 || searchValue.length === 0) {
                const form = $(this).closest('form');
                form.submit();
            }
        }, 500);
    });
    
    // Setup filter change handlers
    $('.filter-select').on('change', function() {
        const form = $(this).closest('form');
        form.submit();
    });
    
    // Setup row click handlers
    setupRowClickHandlers();
    
    // Setup keyboard shortcuts
    setupKeyboardShortcuts();
}

// Setup row click handlers
function setupRowClickHandlers() {
    $('.order-row').on('click', function(e) {
        // Don't trigger if clicking on buttons, selects, or links
        if ($(e.target).is('button, select, a, .btn, .status-select')) {
            return;
        }
        
        const orderId = $(this).data('order-id');
        if (orderId) {
            window.location.href = `/Admin/Orders/Details/${orderId}`;
        }
    });
    
    // Add hover effect
    $('.order-row').on('mouseenter', function() {
        $(this).addClass('table-row-hover');
    }).on('mouseleave', function() {
        $(this).removeClass('table-row-hover');
    });
}

// Update order status
function updateOrderStatus(orderId, status) {
    if (!orderId || !status) {
        showNotification('Thiếu thông tin để cập nhật trạng thái', 'error');
        return;
    }
    
    // Show loading state
    let statusSelect = $(`.status-select[data-order-id="${orderId}"]`);
    
    // For details page, use different selector
    if (!statusSelect.length) {
        statusSelect = $('#statusSelect');
    }
    
    const originalValue = statusSelect.val();
    statusSelect.prop('disabled', true);
    
    // Add loading indicator
    showLoadingIndicator(`Đang cập nhật trạng thái đơn hàng #${orderId}...`);
    
    $.ajax({
        url: '/Admin/Orders/UpdateStatus',
        method: 'POST',
        data: {
            orderId: orderId,
            status: status,
            __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
        },
        success: function(response) {
            hideLoadingIndicator();
            
            if (response.success) {
                showNotification(response.message || 'Cập nhật trạng thái thành công', 'success');
                
                // Update UI
                updateOrderRowStatus(orderId, status);
                
                // Reload statistics
                loadOrderStatistics();
                
                // Add visual feedback
                const row = $(`.order-row[data-order-id="${orderId}"]`);
                row.addClass('status-updated');
                setTimeout(() => row.removeClass('status-updated'), 2000);
                
            } else {
                showNotification(response.message || 'Có lỗi xảy ra khi cập nhật trạng thái', 'error');
                statusSelect.val(originalValue);
            }
        },
        error: function(xhr, status, error) {
            hideLoadingIndicator();
            console.error('Error updating order status:', error);
            showNotification('Có lỗi xảy ra khi cập nhật trạng thái', 'error');
            statusSelect.val(originalValue);
        },
        complete: function() {
            statusSelect.prop('disabled', false);
        }
    });
}

// Update order row status in UI
function updateOrderRowStatus(orderId, status) {
    const row = $(`.order-row[data-order-id="${orderId}"]`);
    
    // Update status badge if exists
    const statusBadge = row.find('.order-status');
    if (statusBadge.length) {
        statusBadge.removeClass().addClass('order-status').addClass(`status-${status.toLowerCase().replace(/\s+/g, '-')}`);
        statusBadge.text(status);
    }

    // Update timeline in details page if exists
    updateStatusTimeline(status);

    // Update page title status badge if exists
    const pageStatusBadge = $('.page-header .order-status');
    if (pageStatusBadge.length) {
        pageStatusBadge.removeClass().addClass('order-status').addClass(`status-${status.toLowerCase().replace(/\s+/g, '-')}`);
        pageStatusBadge.text(status);
    }
}

// Update status timeline in details page
function updateStatusTimeline(currentStatus) {
    const timeline = $('.status-timeline');
    if (!timeline.length) return;

    // Define status order with exact matching
    const statusFlow = [
        { status: 'Chờ xử lý', keywords: ['chờ', 'pending', 'chờ xử lý'] },
        { status: 'Đã xác nhận', keywords: ['xác nhận', 'confirmed', 'đã xác nhận'] },
        { status: 'Đang xử lý', keywords: ['xử lý', 'processing', 'đang xử lý'] },
        { status: 'Đang giao hàng', keywords: ['giao hàng', 'shipping', 'đang giao hàng', 'đang giao'] },
        { status: 'Hoàn thành', keywords: ['hoàn thành', 'completed', 'thành công'] }
    ];

    // Special handling for cancelled status
    if (currentStatus === 'Đã hủy' || currentStatus.toLowerCase().includes('hủy')) {
        timeline.find('.timeline-item').removeClass('completed timeline-animate');
        return;
    }

    // Find current status index
    let currentIndex = -1;
    const normalizedStatus = currentStatus.toLowerCase();
    
    for (let i = 0; i < statusFlow.length; i++) {
        const flow = statusFlow[i];
        if (flow.status === currentStatus || 
            flow.keywords.some(keyword => normalizedStatus.includes(keyword.toLowerCase()))) {
            currentIndex = i;
            break;
        }
    }

    // If status not found in flow, try exact match
    if (currentIndex === -1) {
        const exactMatch = ['Chờ xử lý', 'Đã xác nhận', 'Đang xử lý', 'Đang giao hàng', 'Hoàn thành'];
        currentIndex = exactMatch.indexOf(currentStatus);
    }
    
    // Update timeline items with animation
    timeline.find('.timeline-item').each(function(index) {
        const $item = $(this);
        
        if (index <= currentIndex) {
            if (!$item.hasClass('completed')) {
                // Add with delay for cascading effect
                setTimeout(() => {
                    $item.addClass('completed timeline-animate');
                    setTimeout(() => $item.removeClass('timeline-animate'), 1000);
                }, index * 200);
            }
        } else {
            $item.removeClass('completed timeline-animate');
        }
    });

    // Scroll to current status item if needed
    if (currentIndex >= 0) {
        const currentItem = timeline.find('.timeline-item').eq(currentIndex);
        if (currentItem.length) {
            currentItem[0].scrollIntoView({ behavior: 'smooth', block: 'center' });
        }
    }
}

// Delete order
function deleteOrder(orderId) {
    if (!orderId) {
        showNotification('Không tìm thấy thông tin đơn hàng', 'error');
        return;
    }
    
    // Update modal content
    const modal = $('#deleteModal');
    const form = modal.find('#deleteForm');
    
    if (form.length) {
        form.attr('action', `/Admin/Orders/Delete/${orderId}`);
    } else {
        // Fallback for details page
        form.attr('action', '/Admin/Orders/Delete');
        form.find('input[name="id"]').val(orderId);
    }
    
    // Show modal
    modal.modal('show');
}

// Load order statistics
function loadOrderStatistics() {
    console.log('Loading order statistics...'); // Debug log
    
    $.ajax({
        url: '/Admin/Orders/Statistics',
        method: 'GET',
        success: function(data) {
            console.log('Statistics loaded successfully:', data); // Debug log
            updateStatisticsCards(data);
        },
        error: function(xhr, status, error) {
            console.error('Error loading statistics:', xhr, status, error);
            console.error('Response text:', xhr.responseText);
        }
    });
}

// Update statistics cards
function updateStatisticsCards(data) {
    console.log('Statistics data received:', data); // Debug log
    
    $('#totalOrders').text(data.TotalOrders || data.totalOrders || 0);
    $('#pendingOrders').text(data.PendingOrders || data.pendingOrders || 0);
    $('#completedOrders').text(data.CompletedOrders || data.completedOrders || 0);
    $('#totalRevenue').text(formatCurrency(data.TotalRevenue || data.totalRevenue || 0));
    
    // Animate numbers
    animateNumbers();
}

// Animate statistics numbers
function animateNumbers() {
    $('.stat-number').each(function() {
        const $this = $(this);
        const value = $this.text().replace(/[^\d]/g, '');
        
        if (value && !isNaN(value)) {
            $this.prop('Counter', 0).animate({
                Counter: parseInt(value)
            }, {
                duration: 1000,
                easing: 'swing',
                step: function(now) {
                    if ($this.hasClass('currency')) {
                        $this.text(formatCurrency(Math.ceil(now)));
                    } else {
                        $this.text(Math.ceil(now));
                    }
                }
            });
        }
    });
}

// Refresh orders
function refreshOrders() {
    showLoadingIndicator('Đang tải lại danh sách đơn hàng...');
    
    setTimeout(() => {
        window.location.reload();
    }, 500);
}

// Export orders
function exportOrders() {
    showLoadingIndicator('Đang chuẩn bị xuất báo cáo...');
    
    // Get current filters
    const searchString = $('.search-input').val();
    const statusFilter = $('.filter-select').val();
    
    // Build export URL with filters
    const params = new URLSearchParams();
    if (searchString) params.append('searchString', searchString);
    if (statusFilter && statusFilter !== 'all') params.append('statusFilter', statusFilter);
    
    const exportUrl = `/Admin/Orders/Export?${params.toString()}`;
    
    // Create temporary link and click it
    const link = document.createElement('a');
    link.href = exportUrl;
    link.download = `orders-export-${new Date().toISOString().split('T')[0]}.xlsx`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    
    hideLoadingIndicator();
    showNotification('Báo cáo đã được tải xuống', 'success');
}

// Setup real-time updates
function setupRealTimeUpdates() {
    // Poll for updates every 30 seconds
    setInterval(() => {
        loadOrderStatistics();
    }, 30000);
    
    // Check for new orders
    setInterval(() => {
        checkForNewOrders();
    }, 60000);
}

// Check for new orders
function checkForNewOrders() {
    const lastOrderId = $('.order-row:first').data('order-id');
    
    if (!lastOrderId) return;
    
    $.ajax({
        url: '/Admin/Orders/CheckNewOrders',
        method: 'GET',
        data: { lastOrderId: lastOrderId },
        success: function(data) {
            if (data.hasNewOrders) {
                showNewOrderNotification(data.newOrdersCount);
            }
        },
        error: function(xhr, status, error) {
            console.error('Error checking for new orders:', error);
        }
    });
}

// Show new order notification
function showNewOrderNotification(count) {
    const message = `Có ${count} đơn hàng mới! <a href="#" onclick="refreshOrders()" class="alert-link">Tải lại trang</a>`;
    
    const alertHtml = `
        <div class="alert alert-info alert-dismissible fade show new-order-alert" role="alert">
            <i class="fas fa-bell"></i> ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>
    `;
    
    $('.orders-management').prepend(alertHtml);
    
    // Auto dismiss after 10 seconds
    setTimeout(() => {
        $('.new-order-alert').alert('close');
    }, 10000);
}

// Setup keyboard shortcuts
function setupKeyboardShortcuts() {
    $(document).on('keydown', function(e) {
        // Ctrl + F: Focus search
        if (e.ctrlKey && e.key === 'f') {
            e.preventDefault();
            $('.search-input').focus().select();
        }
        
        // Ctrl + R: Refresh
        if (e.ctrlKey && e.key === 'r') {
            e.preventDefault();
            refreshOrders();
        }
        
        // Escape: Clear search
        if (e.key === 'Escape') {
            $('.search-input').val('').trigger('input');
        }
    });
}

// Utility functions
function formatCurrency(amount) {
    return new Intl.NumberFormat('vi-VN', {
        style: 'currency',
        currency: 'VND',
        minimumFractionDigits: 0
    }).format(amount);
}

function showNotification(message, type = 'info') {
    const alertClass = type === 'error' ? 'alert-danger' : 
                      type === 'success' ? 'alert-success' : 
                      type === 'warning' ? 'alert-warning' : 'alert-info';
    
    const icon = type === 'error' ? 'fas fa-exclamation-triangle' :
                 type === 'success' ? 'fas fa-check-circle' :
                 type === 'warning' ? 'fas fa-exclamation-circle' : 'fas fa-info-circle';
    
    const alertHtml = `
        <div class="alert ${alertClass} alert-dismissible fade show notification-alert" role="alert">
            <i class="${icon}"></i> ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>
    `;
    
    // Remove existing notifications
    $('.notification-alert').remove();
    
    // Add new notification
    $('body').prepend(alertHtml);
    
    // Auto dismiss after 5 seconds
    setTimeout(() => {
        $('.notification-alert').alert('close');
    }, 5000);
}

function showLoadingIndicator(message = 'Đang tải...') {
    const loadingHtml = `
        <div class="loading-overlay" id="loadingOverlay">
            <div class="loading-content">
                <div class="spinner-border text-primary" role="status">
                    <span class="visually-hidden">Loading...</span>
                </div>
                <div class="loading-message">${message}</div>
            </div>
        </div>
    `;
    
    $('body').append(loadingHtml);
}

function hideLoadingIndicator() {
    $('#loadingOverlay').remove();
}

// CSS for loading overlay (inject into head)
$('<style>').text(`
    .loading-overlay {
        position: fixed;
        top: 0;
        left: 0;
        width: 100%;
        height: 100%;
        background: rgba(0, 0, 0, 0.5);
        display: flex;
        align-items: center;
        justify-content: center;
        z-index: 9999;
    }
    
    .loading-content {
        background: white;
        padding: 2rem;
        border-radius: 12px;
        text-align: center;
        box-shadow: 0 10px 30px rgba(0, 0, 0, 0.3);
    }
    
    .loading-message {
        margin-top: 1rem;
        color: #6c757d;
        font-weight: 500;
    }
    
    .notification-alert {
        position: fixed;
        top: 20px;
        right: 20px;
        z-index: 1050;
        min-width: 300px;
        box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
    }
    
    .new-order-alert {
        position: relative;
        margin-bottom: 1rem;
    }
    
    .table-row-hover {
        background-color: #f8f9fc !important;
        cursor: pointer;
    }
    
    .status-updated {
        animation: statusUpdateFlash 2s ease-out;
    }
    
    @keyframes statusUpdateFlash {
        0% { background-color: #d4edda; }
        100% { background-color: transparent; }
    }
`).appendTo('head');

// Print order functionality
function printOrder(orderId) {
    if (!orderId) {
        showNotification('Không tìm thấy thông tin đơn hàng', 'error');
        return;
    }
    
    const printUrl = `/Admin/Orders/Print/${orderId}`;
    window.open(printUrl, '_blank', 'width=800,height=600,scrollbars=yes,resizable=yes');
}

// Advanced search functionality
function setupAdvancedSearch() {
    // Date range picker
    if (typeof flatpickr !== 'undefined') {
        flatpickr('.date-range-picker', {
            mode: 'range',
            dateFormat: 'd/m/Y',
            locale: 'vn'
        });
    }
    
    // Advanced filters modal
    $('#advancedFiltersModal').on('show.bs.modal', function() {
        // Populate current filter values
        const currentFilters = getCurrentFilters();
        populateAdvancedFilters(currentFilters);
    });
}

function getCurrentFilters() {
    return {
        searchString: $('.search-input').val(),
        statusFilter: $('.filter-select').val(),
        // Add more filters as needed
    };
}

function populateAdvancedFilters(filters) {
    // Populate modal form with current filter values
    $('#advancedFiltersModal input[name="searchString"]').val(filters.searchString);
    $('#advancedFiltersModal select[name="statusFilter"]').val(filters.statusFilter);
}

// Bulk actions
function setupBulkActions() {
    // Select all checkbox
    $('#selectAll').on('change', function() {
        const isChecked = $(this).is(':checked');
        $('.order-checkbox').prop('checked', isChecked);
        updateBulkActionButtons();
    });
    
    // Individual checkboxes
    $(document).on('change', '.order-checkbox', function() {
        updateSelectAllState();
        updateBulkActionButtons();
    });
}

function updateSelectAllState() {
    const totalCheckboxes = $('.order-checkbox').length;
    const checkedCheckboxes = $('.order-checkbox:checked').length;
    
    $('#selectAll').prop('indeterminate', checkedCheckboxes > 0 && checkedCheckboxes < totalCheckboxes);
    $('#selectAll').prop('checked', checkedCheckboxes === totalCheckboxes);
}

function updateBulkActionButtons() {
    const checkedCount = $('.order-checkbox:checked').length;
    const bulkActions = $('.bulk-actions');
    
    if (checkedCount > 0) {
        bulkActions.show();
        bulkActions.find('.selected-count').text(checkedCount);
    } else {
        bulkActions.hide();
    }
}

function bulkUpdateStatus(status) {
    const selectedOrders = $('.order-checkbox:checked').map(function() {
        return $(this).val();
    }).get();
    
    if (selectedOrders.length === 0) {
        showNotification('Vui lòng chọn ít nhất một đơn hàng', 'warning');
        return;
    }
    
    if (!confirm(`Bạn có chắc chắn muốn cập nhật trạng thái ${selectedOrders.length} đơn hàng thành "${status}"?`)) {
        return;
    }
    
    showLoadingIndicator(`Đang cập nhật trạng thái ${selectedOrders.length} đơn hàng...`);
    
    $.ajax({
        url: '/Admin/Orders/BulkUpdateStatus',
        method: 'POST',
        data: {
            orderIds: selectedOrders,
            status: status,
            __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
        },
        success: function(response) {
            hideLoadingIndicator();
            
            if (response.success) {
                showNotification(`Đã cập nhật thành công ${response.updatedCount} đơn hàng`, 'success');
                refreshOrders();
            } else {
                showNotification(response.message || 'Có lỗi xảy ra khi cập nhật', 'error');
            }
        },
        error: function(xhr, status, error) {
            hideLoadingIndicator();
            console.error('Error in bulk update:', error);
            showNotification('Có lỗi xảy ra khi cập nhật', 'error');
        }
    });
}