document.addEventListener('DOMContentLoaded', function() {
    initializeSubCategoriesPage();
});

/**
 * Initialize all subcategories page functionality
 */
function initializeSubCategoriesPage() {
    initializeDeleteButtons();
    initializeToggleStatusButtons();
    initializeImagePreview();
    initializeFormValidation();
    initializeModals();
}

/**
 * Initialize delete subcategory functionality
 */
function initializeDeleteButtons() {
    const deleteButtons = document.querySelectorAll('[data-action="delete"]');
    
    deleteButtons.forEach(button => {
        button.addEventListener('click', function(e) {
            e.preventDefault();
            const subcategoryId = this.closest('tr')?.querySelector('[data-subcategory-id]')?.dataset.subcategoryId;
            const subcategoryName = this.closest('tr')?.querySelector('strong')?.textContent;
            
            if (subcategoryId && subcategoryName) {
                deleteSubCategory(subcategoryId, subcategoryName);
            }
        });
    });
}

/**
 * Delete subcategory via AJAX
 * @param {number} subcategoryId - The ID of the subcategory to delete
 * @param {string} subcategoryName - The name of the subcategory
 */
function deleteSubCategory(subcategoryId, subcategoryName) {
    if (!confirm(`Bạn có chắc muốn xóa danh mục con "${subcategoryName}"?\n\nLưu ý: Không thể xóa nếu đang có sản phẩm sử dụng danh mục này.`)) {
        return;
    }

    const formData = new FormData();
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    if (token) {
        formData.append('__RequestVerificationToken', token);
    }

    fetch(`/Admin/SubCategory/Delete/${subcategoryId}`, {
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
            setTimeout(() => location.reload(), 1500);
        } else {
            showAlert('danger', data.message);
        }
    })
    .catch(error => {
        console.error('Error:', error);
        showAlert('danger', 'Có lỗi xảy ra khi xóa danh mục con!');
    });
}

/**
 * Initialize toggle status functionality
 */
function initializeToggleStatusButtons() {
    const toggleButtons = document.querySelectorAll('[data-action="toggle-status"]');
    
    toggleButtons.forEach(button => {
        button.addEventListener('click', function(e) {
            e.preventDefault();
            const subcategoryId = this.closest('tr')?.querySelector('[data-subcategory-id]')?.dataset.subcategoryId;
            
            if (subcategoryId) {
                toggleStatus(subcategoryId);
            }
        });
    });
}

/**
 * Toggle subcategory status via AJAX
 * @param {number} subcategoryId - The ID of the subcategory
 */
async function toggleStatus(subcategoryId) {
    if (!confirm('Bạn có chắc muốn thay đổi trạng thái của danh mục con này?')) {
        return;
    }

    try {
        const formData = new FormData();
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        if (token) {
            formData.append('__RequestVerificationToken', token);
        }

        const response = await fetch(`/Admin/SubCategory/ToggleStatus/${subcategoryId}`, {
            method: 'POST',
            body: formData,
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        });

        const result = await response.json();
        
        if (result.success) {
            // Update badge
            const badge = document.getElementById(`status-badge-${subcategoryId}`);
            if (badge) {
                if (result.isActive) {
                    badge.className = 'badge bg-success';
                    badge.textContent = 'Hoạt động';
                } else {
                    badge.className = 'badge bg-secondary';
                    badge.textContent = 'Tạm ngưng';
                }
            }
            
            showAlert('success', result.message);
        } else {
            showAlert('danger', result.message);
        }
    } catch (error) {
        console.error('Error:', error);
        showAlert('danger', 'Có lỗi xảy ra khi thay đổi trạng thái!');
    }
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
    
    // Initialize remove image checkbox handler
    const removeImageCheckbox = document.getElementById('removeImage');
    if (removeImageCheckbox) {
        removeImageCheckbox.addEventListener('change', function() {
            toggleImageUpload();
        });
    }
}

/**
 * Preview selected image
 * @param {HTMLInputElement} input - The file input element
 */
function previewImage(input) {
    if (input.files && input.files[0]) {
        const file = input.files[0];
        
        // Validate file
        if (!validateImageFile(file)) {
            input.value = '';
            return;
        }
        
        const reader = new FileReader();
        
        reader.onload = function(e) {
            const preview = document.getElementById('preview');
            const previewContainer = document.getElementById('imagePreview');
            
            if (preview && previewContainer) {
                preview.src = e.target.result;
                previewContainer.style.display = 'block';
            }
            
            // Uncheck remove image if new image is selected
            const removeCheckbox = document.getElementById('removeImage');
            if (removeCheckbox) {
                removeCheckbox.checked = false;
                const currentImage = document.getElementById('currentImage');
                if (currentImage) {
                    currentImage.style.opacity = '1';
                }
            }
        };
        
        reader.readAsDataURL(file);
    }
}

/**
 * Validate image file
 * @param {File} file - The file to validate
 * @returns {boolean} - True if valid
 */
function validateImageFile(file) {
    const maxSize = 2 * 1024 * 1024; // 2MB
    const allowedTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif'];
    
    if (file.size > maxSize) {
        showAlert('danger', 'Kích thước file không được vượt quá 2MB');
        return false;
    }
    
    if (!allowedTypes.includes(file.type)) {
        showAlert('danger', 'Chỉ chấp nhận file hình ảnh (JPG, PNG, GIF)');
        return false;
    }
    
    return true;
}

/**
 * Toggle image upload controls
 */
function toggleImageUpload() {
    const removeCheckbox = document.getElementById('removeImage');
    const currentImage = document.getElementById('currentImage');
    const imageFile = document.getElementById('imageFile');
    const imagePreview = document.getElementById('imagePreview');
    
    if (removeCheckbox && removeCheckbox.checked) {
        if (currentImage) {
            currentImage.style.opacity = '0.3';
        }
        if (imageFile) {
            imageFile.disabled = true;
        }
        if (imagePreview) {
            imagePreview.style.display = 'none';
        }
    } else {
        if (currentImage) {
            currentImage.style.opacity = '1';
        }
        if (imageFile) {
            imageFile.disabled = false;
        }
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
    
    // Validate category selection
    const categorySelect = form.querySelector('select[name="CategoryID"]');
    if (categorySelect && !categorySelect.value) {
        showFieldError(categorySelect, 'Vui lòng chọn danh mục cha');
        isValid = false;
    }
    
    // Validate file upload if present
    const fileInput = form.querySelector('input[type="file"]');
    if (fileInput && fileInput.files.length > 0 && !fileInput.disabled) {
        const file = fileInput.files[0];
        if (!validateImageFile(file)) {
            isValid = false;
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
    errorElement.className = 'text-danger field-error mt-1';
    errorElement.innerHTML = `<i class="fas fa-exclamation-circle"></i> ${message}`;
    
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
        <div class="alert alert-${type} alert-dismissible fade show dynamic-alert position-fixed top-0 start-50 translate-middle-x mt-3" 
             style="z-index: 9999; min-width: 400px;" role="alert">
            <i class="fas fa-${getAlertIcon(type)} me-2"></i>
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>
    `;
    
    document.body.insertAdjacentHTML('beforeend', alertHtml);
    
    // Auto-hide after 5 seconds
    setTimeout(() => {
        const alert = document.querySelector('.dynamic-alert');
        if (alert) {
            const bsAlert = new bootstrap.Alert(alert);
            bsAlert.close();
        }
    }, 5000);
}

/**
 * Get icon for alert type
 * @param {string} type - Alert type
 * @returns {string} - Icon class
 */
function getAlertIcon(type) {
    const icons = {
        'success': 'check-circle',
        'danger': 'exclamation-circle',
        'warning': 'exclamation-triangle',
        'info': 'info-circle'
    };
    return icons[type] || 'info-circle';
}

/**
 * Initialize modal handlers
 */
function initializeModals() {
    const createModal = document.getElementById('createModal');
    if (createModal) {
        createModal.addEventListener('hidden.bs.modal', function () {
            // Reset form when modal is closed
            const form = this.querySelector('form');
            if (form) {
                form.reset();
                // Clear any validation errors
                form.querySelectorAll('.is-invalid').forEach(field => {
                    field.classList.remove('is-invalid');
                });
                form.querySelectorAll('.field-error').forEach(error => {
                    error.remove();
                });
            }
        });
    }
}

/**
 * Utility function to format numbers
 * @param {number} num - Number to format
 * @returns {string} - Formatted number
 */
function formatNumber(num) {
    return new Intl.NumberFormat('vi-VN').format(num);
}

// Export functions for global access
window.SubCategoryManager = {
    deleteSubCategory,
    toggleStatus,
    showAlert,
    previewImage,
    toggleImageUpload
};
