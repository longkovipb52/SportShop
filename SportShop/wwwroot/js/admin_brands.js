
document.addEventListener('DOMContentLoaded', function() {
    initializeBrandsPage();
});


function initializeBrandsPage() {
    initializeDeleteButtons();
    initializeLogoPreview();
    initializeLogoActions();
    loadProductCounts();
    initializeFormValidation();
}

/**
 * Initialize delete brand functionality
 */
function initializeDeleteButtons() {
    const deleteButtons = document.querySelectorAll('.delete-brand-btn');
    const deleteModal = document.getElementById('deleteBrandModal');
    const confirmDeleteBtn = document.getElementById('confirmDeleteBtn');
    const brandNameElement = document.getElementById('brandNameToDelete');
    
    let brandIdToDelete = null;

    deleteButtons.forEach(button => {
        button.addEventListener('click', function() {
            brandIdToDelete = this.getAttribute('data-brand-id');
            const brandName = this.getAttribute('data-brand-name');
            
            if (brandNameElement) {
                brandNameElement.textContent = brandName;
            }
            
            if (deleteModal) {
                const modal = new bootstrap.Modal(deleteModal);
                modal.show();
            }
        });
    });

    if (confirmDeleteBtn) {
        confirmDeleteBtn.addEventListener('click', function() {
            if (brandIdToDelete) {
                deleteBrand(brandIdToDelete);
            }
        });
    }
}

/**
 * Delete brand via AJAX
 * @param {number} brandId - The ID of the brand to delete
 */
function deleteBrand(brandId) {
    const confirmBtn = document.getElementById('confirmDeleteBtn');
    const originalText = confirmBtn.innerHTML;
    
    // Show loading state
    confirmBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Đang xóa...';
    confirmBtn.disabled = true;

    // Create form data for anti-forgery token
    const formData = new FormData();
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    if (token) {
        formData.append('__RequestVerificationToken', token);
    }

    fetch(`/Admin/Brands/Delete/${brandId}`, {
        method: 'POST',
        body: formData,
        headers: {
            'X-Requested-With': 'XMLHttpRequest'
        }
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            showAlert('success', data.message);
            
            // Close modal
            const modal = bootstrap.Modal.getInstance(document.getElementById('deleteBrandModal'));
            if (modal) {
                modal.hide();
            }
            
            // Redirect to brands index page after successful deletion
            setTimeout(() => {
                // Check if we're on a details page and redirect to index
                const currentPath = window.location.pathname;
                if (currentPath.includes('/Details/') || currentPath.includes('/Edit/')) {
                    window.location.href = '/Admin/Brands';
                } else {
                    window.location.reload();
                }
            }, 1500);
        } else {
            showAlert('danger', data.message);
        }
    })
    .catch(error => {
        console.error('Error:', error);
        showAlert('danger', 'Có lỗi xảy ra khi xóa thương hiệu. Vui lòng thử lại.');
    })
    .finally(() => {
        // Restore button state
        confirmBtn.innerHTML = originalText;
        confirmBtn.disabled = false;
    });
}

/**
 * Initialize logo preview functionality
 */
function initializeLogoPreview() {
    const logoInput = document.getElementById('logoFile');
    if (logoInput) {
        logoInput.addEventListener('change', function(e) {
            previewLogo(this);
        });
    }
}

/**
 * Preview selected logo
 * @param {HTMLInputElement} input - The file input element
 */
function previewLogo(input) {
    const preview = document.getElementById('logoPreview');
    const actions = document.getElementById('logoActions');
    
    if (input.files && input.files[0]) {
        const reader = new FileReader();
        
        reader.onload = function(e) {
            preview.innerHTML = `
                <img src="${e.target.result}" alt="Preview" class="current-logo">
                <div class="logo-overlay">
                    <button type="button" class="btn btn-sm btn-outline-light" onclick="changeLogo()">
                        <i class="fas fa-camera"></i>
                        Thay đổi
                    </button>
                </div>
            `;
            
            if (actions) {
                actions.style.display = 'flex';
            }
        };
        
        reader.readAsDataURL(input.files[0]);
    }
}

/**
 * Change logo - trigger file input click
 */
function changeLogo() {
    const logoInput = document.getElementById('logoFile');
    if (logoInput) {
        logoInput.click();
    }
}

/**
 * Confirm logo selection
 */
function confirmLogo() {
    const actions = document.getElementById('logoActions');
    if (actions) {
        actions.style.display = 'none';
    }
    showAlert('info', 'Logo sẽ được lưu khi bạn submit form.');
}

/**
 * Cancel logo selection
 */
function cancelLogo() {
    const preview = document.getElementById('logoPreview');
    const actions = document.getElementById('logoActions');
    const logoInput = document.getElementById('logoFile');
    
    // Reset preview to original state or placeholder
    const currentLogoSrc = preview.querySelector('.current-logo')?.src;
    if (currentLogoSrc && !currentLogoSrc.startsWith('data:')) {
        // Has original logo, restore it
        preview.innerHTML = `
            <img src="${currentLogoSrc}" alt="Current Logo" class="current-logo">
            <div class="logo-overlay">
                <button type="button" class="btn btn-sm btn-outline-light" onclick="changeLogo()">
                    <i class="fas fa-camera"></i>
                    Thay đổi
                </button>
            </div>
        `;
    } else {
        // No original logo, show placeholder
        preview.innerHTML = `
            <div class="preview-placeholder">
                <i class="fas fa-image"></i>
                <p>Chưa có logo</p>
            </div>
        `;
    }
    
    if (actions) {
        actions.style.display = 'none';
    }
    
    if (logoInput) {
        logoInput.value = '';
    }
}

/**
 * Remove logo
 */
function removeLogo() {
    const preview = document.getElementById('logoPreview');
    const actions = document.getElementById('logoActions');
    const logoInput = document.getElementById('logoFile');
    
    preview.innerHTML = `
        <div class="preview-placeholder">
            <i class="fas fa-image"></i>
            <p>Chưa có logo</p>
        </div>
    `;
    
    if (actions) {
        actions.style.display = 'none';
    }
    
    if (logoInput) {
        logoInput.value = '';
    }
}

/**
 * Initialize logo action buttons
 */
function initializeLogoActions() {
    // Functions are now defined globally above
    // No need to assign to window object
}

/**
 * Load product counts for each brand
 */
function loadProductCounts() {
    const productCountElements = document.querySelectorAll('.product-count[data-brand-id]');
    
    productCountElements.forEach(element => {
        const brandId = element.getAttribute('data-brand-id');
        loadBrandStats(brandId, element);
    });
}

/**
 * Load statistics for a specific brand
 * @param {number} brandId - The brand ID
 * @param {HTMLElement} targetElement - Element to update with count (optional)
 */
function loadBrandStats(brandId, targetElement = null) {
    fetch(`/Admin/Brands/GetBrandStats/${brandId}`, {
        method: 'GET',
        headers: {
            'X-Requested-With': 'XMLHttpRequest',
            'Accept': 'application/json'
        }
    })
    .then(response => response.json())
    .then(data => {
        if (data.error) {
            console.error('Error loading stats:', data.error);
            if (targetElement) {
                targetElement.innerHTML = '<span class="text-muted">N/A</span>';
            }
            return;
        }

        // Update product count in table
        if (targetElement) {
            targetElement.innerHTML = `
                <span class="badge bg-primary">${data.totalProducts}</span>
            `;
        }

        // Update detailed stats if elements exist
        updateStatElement('totalProducts', data.totalProducts);
        updateStatElement('totalStock', data.totalStock);
        updateStatElement('inStock', data.inStock);
        updateStatElement('outOfStock', data.outOfStock);
    })
    .catch(error => {
        console.error('Error loading brand stats:', error);
        if (targetElement) {
            targetElement.innerHTML = '<span class="text-muted">Error</span>';
        }
    });
}

/**
 * Update a stat element with new value
 * @param {string} elementId - The ID of the element to update
 * @param {number} value - The new value
 */
function updateStatElement(elementId, value) {
    const element = document.getElementById(elementId);
    if (element) {
        element.innerHTML = value.toLocaleString('vi-VN');
    }
}

/**
 * Initialize form validation
 */
function initializeFormValidation() {
    const forms = document.querySelectorAll('form[action*="/Admin/Brands/"]');
    
    forms.forEach(form => {
        form.addEventListener('submit', function(e) {
            if (!validateForm(this)) {
                e.preventDefault();
                return false;
            }
            
            // Show loading state on submit button
            const submitBtn = this.querySelector('button[type="submit"]');
            if (submitBtn) {
                const originalText = submitBtn.innerHTML;
                submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Đang xử lý...';
                submitBtn.disabled = true;
                
                // Restore button if form validation fails
                setTimeout(() => {
                    if (this.querySelector('.validation-summary') || this.querySelector('.text-danger')) {
                        submitBtn.innerHTML = originalText;
                        submitBtn.disabled = false;
                    }
                }, 100);
            }
        });
    });
}

/**
 * Validate form fields
 * @param {HTMLFormElement} form - The form to validate
 * @returns {boolean} - True if form is valid
 */
function validateForm(form) {
    let isValid = true;
    const requiredFields = form.querySelectorAll('input[required], textarea[required], select[required]');
    
    requiredFields.forEach(field => {
        if (!field.value.trim()) {
            showFieldError(field, 'Trường này là bắt buộc');
            isValid = false;
        } else {
            clearFieldError(field);
        }
    });
    
    // Validate file upload
    const fileInput = form.querySelector('input[type="file"]');
    if (fileInput && fileInput.files.length > 0) {
        const file = fileInput.files[0];
        const maxSize = 5 * 1024 * 1024; // 5MB
        const allowedTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif'];
        
        if (file.size > maxSize) {
            showFieldError(fileInput, 'Kích thước file không được vượt quá 5MB');
            isValid = false;
        } else if (!allowedTypes.includes(file.type)) {
            showFieldError(fileInput, 'Chỉ chấp nhận file hình ảnh (JPG, PNG, GIF)');
            isValid = false;
        } else {
            clearFieldError(fileInput);
        }
    }
    
    return isValid;
}

/**
 * Show error message for a field
 * @param {HTMLElement} field - The form field
 * @param {string} message - Error message
 */
function showFieldError(field, message) {
    clearFieldError(field);
    
    field.classList.add('is-invalid');
    
    const errorElement = document.createElement('div');
    errorElement.className = 'text-danger field-error';
    errorElement.textContent = message;
    
    field.parentNode.appendChild(errorElement);
}

/**
 * Clear error message for a field
 * @param {HTMLElement} field - The form field
 */
function clearFieldError(field) {
    field.classList.remove('is-invalid');
    
    const existingError = field.parentNode.querySelector('.field-error');
    if (existingError) {
        existingError.remove();
    }
}

/**
 * Show alert message
 * @param {string} type - Alert type (success, danger, warning, info)
 * @param {string} message - Alert message
 */
function showAlert(type, message) {
    // Remove existing alerts
    const existingAlerts = document.querySelectorAll('.dynamic-alert');
    existingAlerts.forEach(alert => alert.remove());
    
    const alertHtml = `
        <div class="alert alert-${type} alert-dismissible fade show dynamic-alert" role="alert">
            <i class="fas fa-${getAlertIcon(type)}"></i>
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>
    `;
    
    // Insert at the top of the main container
    const container = document.querySelector('.brands-container, .brand-form-container, .brand-details-container');
    if (container) {
        container.insertAdjacentHTML('afterbegin', alertHtml);
        
        // Auto-hide success alerts after 5 seconds
        if (type === 'success') {
            setTimeout(() => {
                const alert = container.querySelector('.dynamic-alert');
                if (alert) {
                    const bsAlert = new bootstrap.Alert(alert);
                    bsAlert.close();
                }
            }, 5000);
        }
    }
}

/**
 * Get icon for alert type
 * @param {string} type - Alert type
 * @returns {string} - Icon class
 */
function getAlertIcon(type) {
    const icons = {
        'success': 'check-circle',
        'danger': 'exclamation-triangle', 
        'warning': 'exclamation-triangle',
        'info': 'info-circle'
    };
    return icons[type] || 'info-circle';
}

/**
 * Initialize search functionality
 */
function initializeSearch() {
    const searchForm = document.querySelector('.search-form');
    const searchInput = searchForm?.querySelector('input[name="search"]');
    
    if (searchInput) {
        // Add real-time search with debounce
        let searchTimeout;
        searchInput.addEventListener('input', function() {
            clearTimeout(searchTimeout);
            searchTimeout = setTimeout(() => {
                if (this.value.length >= 2 || this.value.length === 0) {
                    searchForm.submit();
                }
            }, 500);
        });
    }
}

/**
 * Initialize tooltips
 */
function initializeTooltips() {
    const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    const tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });
}

/**
 * Utility function to format numbers
 * @param {number} num - Number to format
 * @returns {string} - Formatted number
 */
function formatNumber(num) {
    return new Intl.NumberFormat('vi-VN').format(num);
}

/**
 * Utility function to format currency
 * @param {number} amount - Amount to format
 * @returns {string} - Formatted currency
 */
function formatCurrency(amount) {
    return new Intl.NumberFormat('vi-VN', {
        style: 'currency',
        currency: 'VND'
    }).format(amount);
}

// Initialize additional features when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    initializeSearch();
    initializeTooltips();
});

// Handle page visibility change to refresh data
document.addEventListener('visibilitychange', function() {
    if (!document.hidden) {
        // Refresh product counts when page becomes visible
        loadProductCounts();
    }
});

// Export functions for global access
window.BrandManager = {
    loadBrandStats,
    showAlert,
    previewLogo,
    changeLogo,
    confirmLogo,
    cancelLogo,
    removeLogo,
    deleteBrand
};

// Make functions available globally for onclick handlers
window.changeLogo = changeLogo;
window.confirmLogo = confirmLogo;
window.cancelLogo = cancelLogo;
window.removeLogo = removeLogo;
window.previewLogo = previewLogo;