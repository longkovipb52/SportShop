/**
 * Admin Profile Module JavaScript
 * Handles profile form interactions and password visibility
 */

document.addEventListener('DOMContentLoaded', function() {
    // Initialize profile module
    initProfileModule();
});

function initProfileModule() {
    initPasswordToggle();
    initFormValidation();
    initAlertDismiss();
    initFormAnimations();
}

/**
 * Password visibility toggle functionality
 */
function initPasswordToggle() {
    const passwordToggles = document.querySelectorAll('.password-toggle');
    
    passwordToggles.forEach(toggle => {
        toggle.addEventListener('click', function(e) {
            e.preventDefault();
            
            // Find the input field - check both old and new structure
            let passwordField = null;
            
            // New structure: input-with-icon container
            const inputWithIcon = this.closest('.input-with-icon');
            if (inputWithIcon) {
                passwordField = inputWithIcon.querySelector('input[type="password"], input[type="text"]');
            } else {
                // Old structure: password-input-group
                const passwordGroup = this.closest('.password-input-group');
                passwordField = passwordGroup ? passwordGroup.querySelector('input[type="password"], input[type="text"]') : null;
            }
            
            const icon = this.querySelector('i');
            
            if (passwordField) {
                if (passwordField.type === 'password') {
                    passwordField.type = 'text';
                    icon.classList.remove('fa-eye');
                    icon.classList.add('fa-eye-slash');
                    this.setAttribute('title', 'Ẩn mật khẩu');
                    this.setAttribute('aria-label', 'Ẩn mật khẩu');
                } else {
                    passwordField.type = 'password';
                    icon.classList.remove('fa-eye-slash');
                    icon.classList.add('fa-eye');
                    this.setAttribute('title', 'Hiện mật khẩu');
                    this.setAttribute('aria-label', 'Hiện mật khẩu');
                }
            }
        });
        
        // Set initial attributes
        toggle.setAttribute('title', 'Hiện mật khẩu');
        toggle.setAttribute('aria-label', 'Hiện mật khẩu');
        toggle.setAttribute('type', 'button'); // Prevent form submission
    });
}

/**
 * Form validation enhancements
 */
function initFormValidation() {
    const forms = document.querySelectorAll('.profile-form, .change-password-form');
    
    forms.forEach(form => {
        const inputs = form.querySelectorAll('input[type="text"], input[type="email"], input[type="password"], input[type="tel"]');
        
        inputs.forEach(input => {
            // Real-time validation feedback
            input.addEventListener('blur', function() {
                validateField(this);
            });
            
            // Clear validation on focus
            input.addEventListener('focus', function() {
                clearFieldValidation(this);
            });
            
            // Enhanced input styling
            input.addEventListener('input', function() {
                this.classList.add('touched');
            });
        });
        
        // Form submission handling
        form.addEventListener('submit', function(e) {
            if (!validateForm(this)) {
                e.preventDefault();
                showFormErrors();
            } else {
                showFormSubmitting(this);
            }
        });
    });
}

/**
 * Validate individual field
 */
function validateField(field) {
    const value = field.value.trim();
    const isRequired = field.hasAttribute('data-val-required') || field.required;
    const fieldType = field.type;
    let isValid = true;
    let errorMessage = '';
    
    // Required validation
    if (isRequired && !value) {
        isValid = false;
        errorMessage = 'Trường này là bắt buộc.';
    }
    
    // Email validation
    if (fieldType === 'email' && value) {
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        if (!emailRegex.test(value)) {
            isValid = false;
            errorMessage = 'Email không hợp lệ.';
        }
    }
    
    // Phone validation
    if (fieldType === 'tel' && value) {
        const phoneRegex = /^[0-9+\-\s()]+$/;
        if (!phoneRegex.test(value)) {
            isValid = false;
            errorMessage = 'Số điện thoại không hợp lệ.';
        }
    }
    
    // Password validation
    if (fieldType === 'password' && value) {
        if (value.length < 6) {
            isValid = false;
            errorMessage = 'Mật khẩu phải có ít nhất 6 ký tự.';
        }
    }
    
    // Username validation
    if (field.name === 'Username' && value) {
        const usernameRegex = /^[a-zA-Z0-9_]+$/;
        if (!usernameRegex.test(value)) {
            isValid = false;
            errorMessage = 'Tên đăng nhập chỉ được chứa chữ, số và dấu gạch dưới.';
        }
    }
    
    // Apply validation styling
    if (isValid) {
        field.classList.remove('is-invalid');
        field.classList.add('is-valid');
    } else {
        field.classList.remove('is-valid');
        field.classList.add('is-invalid');
        showFieldError(field, errorMessage);
    }
    
    return isValid;
}

/**
 * Clear field validation styling
 */
function clearFieldValidation(field) {
    field.classList.remove('is-valid', 'is-invalid');
    const errorElement = field.parentNode.querySelector('.field-validation-error');
    if (errorElement) {
        errorElement.textContent = '';
    }
}

/**
 * Show field error message
 */
function showFieldError(field, message) {
    let errorElement = field.parentNode.querySelector('.field-validation-error');
    if (!errorElement) {
        errorElement = document.createElement('span');
        errorElement.className = 'text-danger field-validation-error';
        field.parentNode.appendChild(errorElement);
    }
    errorElement.textContent = message;
}

/**
 * Validate entire form
 */
function validateForm(form) {
    const inputs = form.querySelectorAll('input[type="text"], input[type="email"], input[type="password"], input[type="tel"]');
    let isFormValid = true;
    
    inputs.forEach(input => {
        if (!validateField(input)) {
            isFormValid = false;
        }
    });
    
    // Password confirmation validation
    const newPassword = form.querySelector('#NewPassword');
    const confirmPassword = form.querySelector('#ConfirmPassword');
    
    if (newPassword && confirmPassword) {
        if (newPassword.value !== confirmPassword.value) {
            isFormValid = false;
            showFieldError(confirmPassword, 'Mật khẩu xác nhận không khớp.');
            confirmPassword.classList.add('is-invalid');
        }
    }
    
    return isFormValid;
}

/**
 * Show form submission state
 */
function showFormSubmitting(form) {
    const submitButton = form.querySelector('button[type="submit"]');
    if (submitButton) {
        const originalText = submitButton.innerHTML;
        submitButton.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Đang xử lý...';
        submitButton.disabled = true;
        
        // Re-enable after a delay (in case of server error)
        setTimeout(() => {
            submitButton.innerHTML = originalText;
            submitButton.disabled = false;
        }, 10000);
    }
}

/**
 * Show form errors
 */
function showFormErrors() {
    const firstError = document.querySelector('.is-invalid');
    if (firstError) {
        firstError.focus();
        firstError.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }
    
    // Show error notification
    showNotification('Vui lòng kiểm tra lại thông tin đã nhập.', 'error');
}

/**
 * Alert dismiss functionality
 */
function initAlertDismiss() {
    const alertDismissButtons = document.querySelectorAll('.btn-close');
    
    alertDismissButtons.forEach(button => {
        button.addEventListener('click', function() {
            const alert = this.closest('.alert');
            if (alert) {
                alert.style.opacity = '0';
                alert.style.transform = 'translateY(-10px)';
                setTimeout(() => {
                    alert.remove();
                }, 300);
            }
        });
    });
    
    // Auto dismiss alerts after 5 seconds
    const alerts = document.querySelectorAll('.alert');
    alerts.forEach(alert => {
        setTimeout(() => {
            const closeButton = alert.querySelector('.btn-close');
            if (closeButton) {
                closeButton.click();
            }
        }, 5000);
    });
}

/**
 * Form animations
 */
function initFormAnimations() {
    // Animate form cards on load
    const profileCards = document.querySelectorAll('.profile-card, .stat-card');
    
    profileCards.forEach((card, index) => {
        card.style.opacity = '0';
        card.style.transform = 'translateY(20px)';
        
        setTimeout(() => {
            card.style.transition = 'all 0.6s ease';
            card.style.opacity = '1';
            card.style.transform = 'translateY(0)';
        }, index * 100);
    });
    
    // Input focus animations
    const inputs = document.querySelectorAll('.form-control');
    inputs.forEach(input => {
        input.addEventListener('focus', function() {
            this.parentNode.classList.add('focused');
        });
        
        input.addEventListener('blur', function() {
            this.parentNode.classList.remove('focused');
        });
    });
}

/**
 * Show notification
 */
function showNotification(message, type = 'info') {
    const notification = document.createElement('div');
    notification.className = `alert alert-${type === 'error' ? 'danger' : 'success'} alert-dismissible fade show notification-toast`;
    notification.style.cssText = `
        position: fixed;
        top: 20px;
        right: 20px;
        z-index: 9999;
        min-width: 300px;
        max-width: 400px;
    `;
    
    notification.innerHTML = `
        <i class="fas fa-${type === 'error' ? 'exclamation-circle' : 'check-circle'}"></i>
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;
    
    document.body.appendChild(notification);
    
    // Auto dismiss
    setTimeout(() => {
        notification.remove();
    }, 5000);
}

/**
 * Utility function to format phone number
 */
function formatPhoneNumber(phoneNumber) {
    const cleaned = phoneNumber.replace(/\D/g, '');
    const match = cleaned.match(/^(\d{3})(\d{3})(\d{4})$/);
    if (match) {
        return `(${match[1]}) ${match[2]}-${match[3]}`;
    }
    return phoneNumber;
}

/**
 * Profile image preview (for future enhancement)
 */
function initImagePreview() {
    const imageInput = document.querySelector('#profileImage');
    const imagePreview = document.querySelector('.user-avatar');
    
    if (imageInput && imagePreview) {
        imageInput.addEventListener('change', function(e) {
            const file = e.target.files[0];
            if (file) {
                const reader = new FileReader();
                reader.onload = function(e) {
                    imagePreview.style.backgroundImage = `url(${e.target.result})`;
                    imagePreview.style.backgroundSize = 'cover';
                    imagePreview.style.backgroundPosition = 'center';
                    imagePreview.innerHTML = '';
                };
                reader.readAsDataURL(file);
            }
        });
    }
}

// Export functions for potential external use
window.ProfileModule = {
    validateField,
    validateForm,
    showNotification,
    formatPhoneNumber
};