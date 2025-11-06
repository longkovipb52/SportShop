/**
 * Admin Attribute Management JavaScript
 * Handles Sizes and Colors attribute pages
 */

// Initialize page when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    initializeAttributePages();
    handleTempDataMessages();
});

/**
 * Handle TempData messages on page load
 */
function handleTempDataMessages() {
    // Check for success message
    const successElement = document.querySelector('[data-tempdata-success]');
    if (successElement) {
        const message = successElement.getAttribute('data-tempdata-success');
        if (message) {
            showAlert('success', message);
            successElement.remove();
        }
    }
    
    // Check for error message
    const errorElement = document.querySelector('[data-tempdata-error]');
    if (errorElement) {
        const message = errorElement.getAttribute('data-tempdata-error');
        if (message) {
            showAlert('danger', message);
            errorElement.remove();
        }
    }
    
    // Check for warning message
    const warningElement = document.querySelector('[data-tempdata-warning]');
    if (warningElement) {
        const message = warningElement.getAttribute('data-tempdata-warning');
        if (message) {
            showAlert('warning', message);
            warningElement.remove();
        }
    }
    
    // Check for info message
    const infoElement = document.querySelector('[data-tempdata-info]');
    if (infoElement) {
        const message = infoElement.getAttribute('data-tempdata-info');
        if (message) {
            showAlert('info', message);
            infoElement.remove();
        }
    }
}

/**
 * Initialize attribute management pages
 */
function initializeAttributePages() {
    initializeModals();
    initializeColorPicker();
    initializeImagePreview();
}

/**
 * Initialize modal handlers
 */
function initializeModals() {
    const sizeModal = document.getElementById('sizeModal');
    const colorModal = document.getElementById('colorModal');
    
    if (sizeModal) {
        sizeModal.addEventListener('hidden.bs.modal', function () {
            const form = this.querySelector('form');
            if (form) {
                form.reset();
                clearValidationErrors(form);
            }
        });
    }
    
    if (colorModal) {
        colorModal.addEventListener('hidden.bs.modal', function () {
            const form = this.querySelector('form');
            if (form) {
                form.reset();
                clearValidationErrors(form);
                resetColorPreview();
            }
        });
    }
}

/**
 * Initialize color picker synchronization
 */
function initializeColorPicker() {
    const colorPicker = document.getElementById('colorPicker');
    const hexCode = document.getElementById('hexCode');
    const colorPreview = document.getElementById('colorPreview');
    
    if (colorPicker && hexCode && colorPreview) {
        // Sync color picker with hex input
        colorPicker.addEventListener('input', function() {
            hexCode.value = this.value;
            colorPreview.style.backgroundColor = this.value;
        });
        
        hexCode.addEventListener('input', function() {
            if (/^#[0-9A-Fa-f]{6}$/.test(this.value)) {
                colorPicker.value = this.value;
                colorPreview.style.backgroundColor = this.value;
            }
        });
    }
}

/**
 * Initialize image preview functionality
 */
function initializeImagePreview() {
    const imageInput = document.querySelector('input[type="file"][accept="image/*"]');
    if (imageInput) {
        imageInput.addEventListener('change', function() {
            previewImage(this);
        });
    }
}

/**
 * Preview image before upload
 * @param {HTMLInputElement} input - File input element
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
            const preview = document.getElementById('imagePreview');
            if (preview) {
                preview.src = e.target.result;
                preview.style.display = 'block';
            }
        };
        reader.readAsDataURL(file);
    }
}

/**
 * Validate image file
 * @param {File} file - Image file to validate
 * @returns {boolean} - Validation result
 */
function validateImageFile(file) {
    const maxSize = 2 * 1024 * 1024; // 2MB
    const allowedTypes = ['image/jpeg', 'image/png', 'image/gif'];
    
    if (!allowedTypes.includes(file.type)) {
        showAlert('danger', 'Chỉ chấp nhận file ảnh định dạng JPG, PNG hoặc GIF!');
        return false;
    }
    
    if (file.size > maxSize) {
        showAlert('danger', 'Kích thước ảnh không được vượt quá 2MB!');
        return false;
    }
    
    return true;
}

/**
 * Reset color preview to default
 */
function resetColorPreview() {
    const colorPicker = document.getElementById('colorPicker');
    const hexCode = document.getElementById('hexCode');
    const colorPreview = document.getElementById('colorPreview');
    
    if (colorPicker && hexCode && colorPreview) {
        colorPicker.value = '#000000';
        hexCode.value = '#000000';
        colorPreview.style.backgroundColor = '#000000';
    }
}

/**
 * Clear validation errors from form
 * @param {HTMLFormElement} form - Form element
 */
function clearValidationErrors(form) {
    form.querySelectorAll('.is-invalid').forEach(field => {
        field.classList.remove('is-invalid');
    });
    form.querySelectorAll('.invalid-feedback').forEach(error => {
        error.remove();
    });
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
        <div class="alert alert-${type} alert-dismissible fade show dynamic-alert position-fixed" 
             style="top: 20px; right: 20px; z-index: 9999; min-width: 350px; max-width: 500px; box-shadow: 0 4px 20px rgba(0,0,0,0.15);" 
             role="alert">
            <div class="d-flex align-items-center">
                <i class="fas fa-${getAlertIcon(type)} me-2" style="font-size: 1.2rem;"></i>
                <span style="flex: 1;">${message}</span>
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            </div>
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
 * Open add size modal
 */
function openAddModal() {
    const modalTitle = document.getElementById('modalTitle');
    const form = document.getElementById('sizeForm') || document.getElementById('colorForm');
    const idInput = document.getElementById('sizeOptionId') || document.getElementById('colorOptionId');
    const isActiveInput = document.getElementById('isActive');
    
    if (modalTitle) {
        modalTitle.textContent = document.getElementById('sizeModal') ? 'Thêm kích thước mới' : 'Thêm màu mới';
    }
    
    if (form) {
        form.reset();
    }
    
    if (idInput) {
        idInput.value = '0';
    }
    
    if (isActiveInput) {
        isActiveInput.checked = true;
    }
    
    resetColorPreview();
    
    const modalElement = document.getElementById('sizeModal') || document.getElementById('colorModal');
    if (modalElement) {
        const modal = new bootstrap.Modal(modalElement);
        modal.show();
    }
}

/**
 * Edit size
 * @param {number} id - Size ID
 * @param {string} name - Size name
 * @param {number} subCategoryId - SubCategory ID
 * @param {number} displayOrder - Display order
 * @param {boolean} isActive - Active status
 */
function editSize(id, name, subCategoryId, displayOrder, isActive) {
    document.getElementById('modalTitle').textContent = 'Chỉnh sửa kích thước';
    document.getElementById('sizeOptionId').value = id;
    document.getElementById('sizeName').value = name;
    document.getElementById('subCategoryId').value = subCategoryId || '';
    document.getElementById('displayOrder').value = displayOrder;
    document.getElementById('isActive').checked = isActive;
    
    const modal = new bootstrap.Modal(document.getElementById('sizeModal'));
    modal.show();
}

/**
 * Edit color
 * @param {number} id - Color ID
 * @param {string} name - Color name
 * @param {string} hexCode - Hex color code
 * @param {boolean} isActive - Active status
 */
function editColor(id, name, hexCode, isActive) {
    document.getElementById('modalTitle').textContent = 'Chỉnh sửa màu';
    document.getElementById('colorOptionId').value = id;
    document.getElementById('colorName').value = name;
    document.getElementById('hexCode').value = hexCode || '#000000';
    document.getElementById('colorPicker').value = hexCode || '#000000';
    document.getElementById('colorPreview').style.backgroundColor = hexCode || '#000000';
    document.getElementById('isActive').checked = isActive;
    
    const modal = new bootstrap.Modal(document.getElementById('colorModal'));
    modal.show();
}

/**
 * Save size
 */
async function saveSize() {
    const id = parseInt(document.getElementById('sizeOptionId').value);
    const data = {
        SizeOptionID: id,
        SizeName: document.getElementById('sizeName').value.trim(),
        SubCategoryID: parseInt(document.getElementById('subCategoryId').value) || null,
        DisplayOrder: parseInt(document.getElementById('displayOrder').value) || 0,
        IsActive: document.getElementById('isActive').checked
    };

    if (!data.SizeName) {
        showAlert('danger', 'Vui lòng nhập tên kích thước!');
        return;
    }

    try {
        const url = id === 0 ? '/Admin/AttributeManagement/CreateSize' : '/Admin/AttributeManagement/UpdateSize';
        const response = await fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
            },
            body: JSON.stringify(data)
        });

        const result = await response.json();
        
        if (result.success) {
            showAlert('success', result.message);
            // Close modal
            const modal = bootstrap.Modal.getInstance(document.getElementById('sizeModal'));
            if (modal) {
                modal.hide();
            }
            // Reload page after short delay
            setTimeout(() => {
                location.reload();
            }, 1000);
        } else {
            showAlert('danger', result.message || 'Có lỗi xảy ra!');
        }
    } catch (error) {
        console.error('Error:', error);
        showAlert('danger', 'Có lỗi xảy ra khi lưu dữ liệu!');
    }
}

/**
 * Save color
 */
async function saveColor() {
    const id = parseInt(document.getElementById('colorOptionId').value);
    const data = {
        ColorOptionID: id,
        ColorName: document.getElementById('colorName').value.trim(),
        HexCode: document.getElementById('hexCode').value.trim() || null,
        IsActive: document.getElementById('isActive').checked
    };

    if (!data.ColorName) {
        showAlert('danger', 'Vui lòng nhập tên màu!');
        return;
    }

    try {
        const url = id === 0 ? '/Admin/AttributeManagement/CreateColor' : '/Admin/AttributeManagement/UpdateColor';
        const response = await fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
            },
            body: JSON.stringify(data)
        });

        const result = await response.json();
        
        if (result.success) {
            showAlert('success', result.message);
            // Close modal
            const modal = bootstrap.Modal.getInstance(document.getElementById('colorModal'));
            if (modal) {
                modal.hide();
            }
            // Reload page after short delay
            setTimeout(() => {
                location.reload();
            }, 1000);
        } else {
            showAlert('danger', result.message || 'Có lỗi xảy ra!');
        }
    } catch (error) {
        console.error('Error:', error);
        showAlert('danger', 'Có lỗi xảy ra khi lưu dữ liệu!');
    }
}

/**
 * Delete size
 * @param {number} id - Size ID
 * @param {string} name - Size name
 */
async function deleteSize(id, name) {
    if (!confirm(`Bạn có chắc muốn xóa kích thước "${name}"?\n\nLưu ý: Thao tác này không thể hoàn tác!`)) {
        return;
    }

    try {
        const response = await fetch(`/Admin/AttributeManagement/DeleteSize/${id}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            }
        });

        const result = await response.json();
        
        if (result.success) {
            showAlert('success', result.message);
            setTimeout(() => {
                location.reload();
            }, 1000);
        } else {
            showAlert('danger', result.message || 'Có lỗi xảy ra!');
        }
    } catch (error) {
        console.error('Error:', error);
        showAlert('danger', 'Có lỗi xảy ra khi xóa!');
    }
}

/**
 * Delete color
 * @param {number} id - Color ID
 * @param {string} name - Color name
 */
async function deleteColor(id, name) {
    if (!confirm(`Bạn có chắc muốn xóa màu "${name}"?\n\nLưu ý: Thao tác này không thể hoàn tác!`)) {
        return;
    }

    try {
        const response = await fetch(`/Admin/AttributeManagement/DeleteColor/${id}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            }
        });

        const result = await response.json();
        
        if (result.success) {
            showAlert('success', result.message);
            setTimeout(() => {
                location.reload();
            }, 1000);
        } else {
            showAlert('danger', result.message || 'Có lỗi xảy ra!');
        }
    } catch (error) {
        console.error('Error:', error);
        showAlert('danger', 'Có lỗi xảy ra khi xóa!');
    }
}

// Export functions for global access
window.AttributeManager = {
    openAddModal,
    editSize,
    editColor,
    saveSize,
    saveColor,
    deleteSize,
    deleteColor,
    showAlert
};
