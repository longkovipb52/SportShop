
document.addEventListener('DOMContentLoaded', function() {
    // Load statistics on page load
    loadStatistics();
    
    // Auto-hide alerts
    autoHideAlerts();
});

// Load product statistics
function loadStatistics() {
    fetch('/Admin/Products/Statistics')
        .then(response => response.json())
        .then(data => {
            if (data.error) {
                console.error('Error loading statistics:', data.error);
                return;
            }
            
            // Update statistics cards
            updateStatisticsCards(data);
        })
        .catch(error => {
            console.error('Error loading statistics:', error);
        });
}

// Update statistics cards with animation
function updateStatisticsCards(data) {
    animateCounter('totalProducts', data.totalProducts || 0);
    animateCounter('inStockProducts', data.inStockProducts || 0);
    animateCounter('lowStockProducts', data.lowStockProducts || 0);
    animateCounter('outOfStockProducts', data.outOfStockProducts || 0);
}

// Animate counter numbers
function animateCounter(elementId, targetValue) {
    const element = document.getElementById(elementId);
    if (!element) return;
    
    const startValue = 0;
    const duration = 1000; // 1 second
    const steps = 30;
    const stepValue = (targetValue - startValue) / steps;
    const stepDuration = duration / steps;
    
    let currentValue = startValue;
    let step = 0;
    
    const timer = setInterval(() => {
        step++;
        currentValue += stepValue;
        
        if (step >= steps) {
            currentValue = targetValue;
            clearInterval(timer);
        }
        
        element.textContent = Math.round(currentValue).toLocaleString();
    }, stepDuration);
}

// Toggle product out of stock status
function toggleOutOfStock(productId) {
    if (!confirm('Bạn có chắc chắn muốn thay đổi trạng thái tồn kho của sản phẩm này?')) {
        return;
    }
    
    const formData = new FormData();
    formData.append('__RequestVerificationToken', getAntiForgeryToken());
    
    fetch(`/Admin/Products/ToggleOutOfStock/${productId}`, {
        method: 'POST',
        body: formData
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            showAlert('success', data.message);
            // Reload page to update the display
            setTimeout(() => {
                location.reload();
            }, 1500);
        } else {
            showAlert('error', data.message || 'Có lỗi xảy ra khi cập nhật trạng thái sản phẩm!');
        }
    })
    .catch(error => {
        console.error('Error:', error);
        showAlert('error', 'Có lỗi xảy ra khi cập nhật trạng thái sản phẩm!');
    });
}

// Delete product
function deleteProduct(productId) {
    if (!confirm('Bạn có chắc chắn muốn xóa sản phẩm này?\n\nHành động này không thể hoàn tác!')) {
        return;
    }
    
    const formData = new FormData();
    formData.append('__RequestVerificationToken', getAntiForgeryToken());
    
    fetch(`/Admin/Products/Delete/${productId}`, {
        method: 'POST',
        body: formData
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            showAlert('success', data.message);
            // Reload page to update the list
            setTimeout(() => {
                location.reload();
            }, 1500);
        } else {
            showAlert('error', data.message || 'Có lỗi xảy ra khi xóa sản phẩm!');
        }
    })
    .catch(error => {
        console.error('Error:', error);
        showAlert('error', 'Có lỗi xảy ra khi xóa sản phẩm!');
    });
}

// Get anti-forgery token
function getAntiForgeryToken() {
    const token = document.querySelector('input[name="__RequestVerificationToken"]');
    if (token) {
        return token.value;
    }
    
    // Try to get from meta tag
    const metaToken = document.querySelector('meta[name="__RequestVerificationToken"]');
    if (metaToken) {
        return metaToken.content;
    }
    
    // Create a form to get the token if not found
    const form = document.createElement('form');
    form.style.display = 'none';
    document.body.appendChild(form);
    
    const tokenInput = document.createElement('input');
    tokenInput.type = 'hidden';
    tokenInput.name = '__RequestVerificationToken';
    form.appendChild(tokenInput);
    
    // This should trigger the token generation
    const formData = new FormData(form);
    document.body.removeChild(form);
    
    return tokenInput.value || '';
}

// Show alert message
function showAlert(type, message) {
    // Remove existing alerts
    const existingAlerts = document.querySelectorAll('.alert');
    existingAlerts.forEach(alert => alert.remove());
    
    // Create new alert
    const alert = document.createElement('div');
    alert.className = `alert alert-${type === 'success' ? 'success' : 'danger'} auto-hide`;
    
    const icon = type === 'success' ? 'fa-check-circle' : 'fa-exclamation-circle';
    alert.innerHTML = `
        <i class="fas ${icon}"></i>
        ${message}
    `;
    
    document.body.appendChild(alert);
    
    // Auto remove after 4 seconds
    setTimeout(() => {
        if (alert.parentNode) {
            alert.remove();
        }
    }, 4000);
}

// Auto-hide alerts
function autoHideAlerts() {
    const alerts = document.querySelectorAll('.alert.auto-hide');
    alerts.forEach(alert => {
        setTimeout(() => {
            if (alert.parentNode) {
                alert.remove();
            }
        }, 4000);
    });
}

// Real-time search functionality
function setupRealTimeSearch() {
    const searchInput = document.querySelector('input[name="searchString"]');
    if (!searchInput) return;
    
    let searchTimeout;
    
    searchInput.addEventListener('input', function() {
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(() => {
            // Auto-submit form after 500ms of no typing
            const form = this.closest('form');
            if (form) {
                form.submit();
            }
        }, 500);
    });
}

// Initialize real-time search if enabled
// setupRealTimeSearch();

// Handle image preview for product uploads
function setupImagePreview() {
    const imageInputs = document.querySelectorAll('input[type="file"][accept*="image"]');
    
    imageInputs.forEach(input => {
        input.addEventListener('change', function(e) {
            const file = e.target.files[0];
            if (!file) return;
            
            // Validate file type
            if (!file.type.startsWith('image/')) {
                showAlert('error', 'Vui lòng chọn file hình ảnh!');
                this.value = '';
                return;
            }
            
            // Validate file size (max 5MB)
            const maxSize = 5 * 1024 * 1024; // 5MB
            if (file.size > maxSize) {
                showAlert('error', 'Kích thước file không được vượt quá 5MB!');
                this.value = '';
                return;
            }
            
            // Show preview if preview container exists
            const previewContainer = document.querySelector('.image-preview');
            if (previewContainer) {
                const reader = new FileReader();
                reader.onload = function(e) {
                    let img = previewContainer.querySelector('img');
                    if (!img) {
                        img = document.createElement('img');
                        img.style.maxWidth = '200px';
                        img.style.maxHeight = '200px';
                        img.style.objectFit = 'cover';
                        img.style.borderRadius = '8px';
                        previewContainer.appendChild(img);
                    }
                    img.src = e.target.result;
                };
                reader.readAsDataURL(file);
            }
        });
    });
}

// Initialize image preview
setupImagePreview();

// Format currency input
function formatCurrencyInput(input) {
    let value = input.value.replace(/[^\d.]/g, '');
    
    // Ensure only one decimal point
    const parts = value.split('.');
    if (parts.length > 2) {
        value = parts[0] + '.' + parts.slice(1).join('');
    }
    
    // Limit decimal places to 2
    if (parts[1] && parts[1].length > 2) {
        value = parts[0] + '.' + parts[1].substring(0, 2);
    }
    
    input.value = value;
}

// Setup currency formatting for price inputs
function setupCurrencyFormatting() {
    const priceInputs = document.querySelectorAll('input[name*="Price"], input[name*="price"]');
    
    priceInputs.forEach(input => {
        input.addEventListener('input', function() {
            formatCurrencyInput(this);
        });
        
        input.addEventListener('blur', function() {
            if (this.value && !isNaN(this.value)) {
                // Format the display value
                const value = parseFloat(this.value);
                this.value = value.toFixed(2);
            }
        });
    });
}

// Initialize currency formatting
setupCurrencyFormatting();

// Keyboard shortcuts
document.addEventListener('keydown', function(e) {
    // Ctrl/Cmd + N: New product
    if ((e.ctrlKey || e.metaKey) && e.key === 'n') {
        e.preventDefault();
        const createButton = document.querySelector('a[href*="/Create"]');
        if (createButton) {
            window.location.href = createButton.href;
        }
    }
    
    // Escape: Clear search
    if (e.key === 'Escape') {
        const searchInput = document.querySelector('input[name="searchString"]');
        if (searchInput && searchInput.value) {
            searchInput.value = '';
            searchInput.closest('form').submit();
        }
    }
});

// Export functionality (if needed)
function exportProducts(format = 'csv') {
    const currentUrl = new URL(window.location);
    currentUrl.pathname = '/Admin/Products/Export';
    currentUrl.searchParams.set('format', format);
    
    // Open in new tab
    window.open(currentUrl.toString(), '_blank');
}

// Bulk operations (if needed)
function setupBulkOperations() {
    const selectAllCheckbox = document.querySelector('#select-all-products');
    const productCheckboxes = document.querySelectorAll('.product-checkbox');
    
    if (!selectAllCheckbox || !productCheckboxes.length) return;
    
    // Select all functionality
    selectAllCheckbox.addEventListener('change', function() {
        productCheckboxes.forEach(checkbox => {
            checkbox.checked = this.checked;
        });
        updateBulkActionsVisibility();
    });
    
    // Individual checkbox change
    productCheckboxes.forEach(checkbox => {
        checkbox.addEventListener('change', function() {
            updateSelectAllState();
            updateBulkActionsVisibility();
        });
    });
    
    function updateSelectAllState() {
        const checkedCount = document.querySelectorAll('.product-checkbox:checked').length;
        const totalCount = productCheckboxes.length;
        
        selectAllCheckbox.checked = checkedCount === totalCount;
        selectAllCheckbox.indeterminate = checkedCount > 0 && checkedCount < totalCount;
    }
    
    function updateBulkActionsVisibility() {
        const checkedCount = document.querySelectorAll('.product-checkbox:checked').length;
        const bulkActions = document.querySelector('.bulk-actions');
        
        if (bulkActions) {
            bulkActions.style.display = checkedCount > 0 ? 'block' : 'none';
        }
    }
}

// Initialize bulk operations if elements exist
setupBulkOperations();