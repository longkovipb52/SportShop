

document.addEventListener('DOMContentLoaded', function() {
    initializeCategoriesPage();
});

/**
 * Initialize all categories page functionality
 */
function initializeCategoriesPage() {
    initializeDeleteButtons();
    initializeImagePreview();
    initializeImageActions();
    loadProductCounts();
    initializeFormValidation();
}

/**
 * Initialize delete category functionality
 */
function initializeDeleteButtons() {
    const deleteButtons = document.querySelectorAll('.delete-category-btn');
    const deleteModal = document.getElementById('deleteCategoryModal');
    const confirmDeleteBtn = document.getElementById('confirmDeleteBtn');
    const categoryNameElement = document.getElementById('categoryNameToDelete');
    
    let categoryIdToDelete = null;

    deleteButtons.forEach(button => {
        button.addEventListener('click', function() {
            categoryIdToDelete = this.getAttribute('data-category-id');
            const categoryName = this.getAttribute('data-category-name');
            
            if (categoryNameElement) {
                categoryNameElement.textContent = categoryName;
            }
            
            if (deleteModal) {
                const modal = new bootstrap.Modal(deleteModal);
                modal.show();
            }
        });
    });

    if (confirmDeleteBtn) {
        confirmDeleteBtn.addEventListener('click', function() {
            if (categoryIdToDelete) {
                deleteCategory(categoryIdToDelete);
            }
        });
    }
}

/**
 * Delete category via AJAX
 * @param {number} categoryId - The ID of the category to delete
 */
function deleteCategory(categoryId) {
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

    fetch(`/Admin/Categories/Delete/${categoryId}`, {
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
            const modal = bootstrap.Modal.getInstance(document.getElementById('deleteCategoryModal'));
            if (modal) {
                modal.hide();
            }
            
            // Redirect to categories index page after successful deletion
            setTimeout(() => {
                // Check if we're on a details page and redirect to index
                const currentPath = window.location.pathname;
                if (currentPath.includes('/Details/') || currentPath.includes('/Edit/')) {
                    window.location.href = '/Admin/Categories';
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
        showAlert('danger', 'Có lỗi xảy ra khi xóa danh mục. Vui lòng thử lại.');
    })
    .finally(() => {
        // Restore button state
        confirmBtn.innerHTML = originalText;
        confirmBtn.disabled = false;
    });
}

/**
 * Initialize image preview functionality
 */
function initializeImagePreview() {
    const imageInput = document.getElementById('imageFile');
    if (imageInput) {
        imageInput.addEventListener('change', function(e) {
            previewImage(this);
        });
    }
}

/**
 * Preview selected image
 * @param {HTMLInputElement} input - The file input element
 */
function previewImage(input) {
    const preview = document.getElementById('imagePreview');
    const actions = document.getElementById('imageActions');
    
    if (input.files && input.files[0]) {
        const reader = new FileReader();
        
        reader.onload = function(e) {
            preview.innerHTML = `
                <img src="${e.target.result}" alt="Preview" class="current-image">
                <div class="image-overlay">
                    <button type="button" class="btn btn-sm btn-outline-light" onclick="changeImage()">
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
 * Initialize image action buttons
 */
function initializeImageActions() {
    // These functions are called from HTML onclick handlers
    window.changeImage = function() {
        const imageInput = document.getElementById('imageFile');
        if (imageInput) {
            imageInput.click();
        }
    };

    window.confirmImage = function() {
        const actions = document.getElementById('imageActions');
        if (actions) {
            actions.style.display = 'none';
        }
        showAlert('info', 'Hình ảnh sẽ được lưu khi bạn submit form.');
    };

    window.cancelImage = function() {
        const preview = document.getElementById('imagePreview');
        const actions = document.getElementById('imageActions');
        const imageInput = document.getElementById('imageFile');
        
        // Reset preview to original state or placeholder
        const currentImageSrc = preview.querySelector('.current-image')?.src;
        if (currentImageSrc && !currentImageSrc.startsWith('data:')) {
            // Has original image, restore it
            preview.innerHTML = `
                <img src="${currentImageSrc}" alt="Current Image" class="current-image">
                <div class="image-overlay">
                    <button type="button" class="btn btn-sm btn-outline-light" onclick="changeImage()">
                        <i class="fas fa-camera"></i>
                        Thay đổi
                    </button>
                </div>
            `;
        } else {
            // No original image, show placeholder
            preview.innerHTML = `
                <div class="preview-placeholder">
                    <i class="fas fa-image"></i>
                    <p>Chưa có hình ảnh</p>
                </div>
            `;
        }
        
        if (actions) {
            actions.style.display = 'none';
        }
        
        if (imageInput) {
            imageInput.value = '';
        }
    };

    window.removeImage = function() {
        const preview = document.getElementById('imagePreview');
        const actions = document.getElementById('imageActions');
        const imageInput = document.getElementById('imageFile');
        
        preview.innerHTML = `
            <div class="preview-placeholder">
                <i class="fas fa-image"></i>
                <p>Chưa có hình ảnh</p>
            </div>
        `;
        
        if (actions) {
            actions.style.display = 'none';
        }
        
        if (imageInput) {
            imageInput.value = '';
        }
    };
}

/**
 * Load product counts for each category
 */
function loadProductCounts() {
    const productCountElements = document.querySelectorAll('.product-count[data-category-id]');
    
    productCountElements.forEach(element => {
        const categoryId = element.getAttribute('data-category-id');
        loadCategoryStats(categoryId, element);
    });
}

/**
 * Load statistics for a specific category
 * @param {number} categoryId - The category ID
 * @param {HTMLElement} targetElement - Element to update with count (optional)
 */
function loadCategoryStats(categoryId, targetElement = null) {
    fetch(`/Admin/Categories/GetCategoryStats/${categoryId}`, {
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
        console.error('Error loading category stats:', error);
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
    const forms = document.querySelectorAll('form[asp-action]');
    
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
    const container = document.querySelector('.categories-container, .category-form-container, .category-details-container');
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
window.CategoryManager = {
    loadCategoryStats,
    showAlert,
    previewImage,
    changeImage,
    confirmImage,
    cancelImage,
    removeImage,
    deleteCategory
};