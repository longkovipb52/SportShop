// Admin Login JavaScript
document.addEventListener('DOMContentLoaded', function() {
    initializeAdminLogin();
});

function initializeAdminLogin() {
    // Enhanced form interactions
    enhanceFormInputs();
    
    // Initialize form validation
    initializeValidation();
    
    // Add keyboard shortcuts
    addKeyboardShortcuts();
    
    // Focus first input
    focusFirstInput();
    
    // Add loading state to form submission
    handleFormSubmission();
}

function enhanceFormInputs() {
    const inputs = document.querySelectorAll('.form-control');
    
    inputs.forEach(input => {
        // Add floating label effect
        const label = input.previousElementSibling;
        
        // Check if input has value on load
        if (input.value.trim() !== '') {
            input.classList.add('has-value');
        }
        
        // Add event listeners
        input.addEventListener('focus', function() {
            this.classList.add('focused');
        });
        
        input.addEventListener('blur', function() {
            this.classList.remove('focused');
            if (this.value.trim() !== '') {
                this.classList.add('has-value');
            } else {
                this.classList.remove('has-value');
            }
        });
        
        input.addEventListener('input', function() {
            if (this.value.trim() !== '') {
                this.classList.add('has-value');
            } else {
                this.classList.remove('has-value');
            }
            
            // Clear validation errors on input
            clearValidationError(this);
        });
    });
}

function togglePassword() {
    const passwordInput = document.getElementById('Password');
    const toggleIcon = document.getElementById('toggleIcon');
    
    if (passwordInput.type === 'password') {
        passwordInput.type = 'text';
        toggleIcon.classList.remove('fa-eye');
        toggleIcon.classList.add('fa-eye-slash');
    } else {
        passwordInput.type = 'password';
        toggleIcon.classList.remove('fa-eye-slash');
        toggleIcon.classList.add('fa-eye');
    }
}

function initializeValidation() {
    const form = document.querySelector('.admin-login-form');
    const usernameInput = document.getElementById('Username');
    const passwordInput = document.getElementById('Password');
    
    // Real-time validation
    usernameInput.addEventListener('blur', function() {
        validateUsername(this);
    });
    
    passwordInput.addEventListener('blur', function() {
        validatePassword(this);
    });
    
    // Form submission validation
    form.addEventListener('submit', function(e) {
        const isValid = validateForm();
        if (!isValid) {
            e.preventDefault();
        }
    });
}

function validateUsername(input) {
    const value = input.value.trim();
    let isValid = true;
    let message = '';
    
    if (value === '') {
        isValid = false;
        message = 'Vui lòng nhập tên đăng nhập hoặc email.';
    } else if (value.length < 3) {
        isValid = false;
        message = 'Tên đăng nhập phải có ít nhất 3 ký tự.';
    }
    
    showValidationResult(input, isValid, message);
    return isValid;
}

function validatePassword(input) {
    const value = input.value;
    let isValid = true;
    let message = '';
    
    if (value === '') {
        isValid = false;
        message = 'Vui lòng nhập mật khẩu.';
    } else if (value.length < 6) {
        isValid = false;
        message = 'Mật khẩu phải có ít nhất 6 ký tự.';
    }
    
    showValidationResult(input, isValid, message);
    return isValid;
}

function validateForm() {
    const usernameInput = document.getElementById('Username');
    const passwordInput = document.getElementById('Password');
    
    const isUsernameValid = validateUsername(usernameInput);
    const isPasswordValid = validatePassword(passwordInput);
    
    return isUsernameValid && isPasswordValid;
}

function showValidationResult(input, isValid, message) {
    const existingError = input.parentNode.querySelector('.text-danger');
    
    if (isValid) {
        input.classList.remove('error');
        if (existingError) {
            existingError.remove();
        }
    } else {
        input.classList.add('error');
        if (!existingError) {
            const errorElement = document.createElement('span');
            errorElement.className = 'text-danger';
            errorElement.textContent = message;
            input.parentNode.appendChild(errorElement);
        } else {
            existingError.textContent = message;
        }
    }
}

function clearValidationError(input) {
    input.classList.remove('error');
    const errorElement = input.parentNode.querySelector('.text-danger');
    if (errorElement) {
        errorElement.remove();
    }
}

function addKeyboardShortcuts() {
    document.addEventListener('keydown', function(e) {
        // Enter key to submit form
        if (e.key === 'Enter' && !e.shiftKey) {
            const form = document.querySelector('.admin-login-form');
            const activeElement = document.activeElement;
            
            if (activeElement.tagName === 'INPUT' && activeElement.type !== 'submit') {
                e.preventDefault();
                
                // Move to next input or submit
                const inputs = Array.from(form.querySelectorAll('input[type="text"], input[type="password"], input[type="email"]'));
                const currentIndex = inputs.indexOf(activeElement);
                
                if (currentIndex < inputs.length - 1) {
                    inputs[currentIndex + 1].focus();
                } else {
                    form.querySelector('button[type="submit"]').click();
                }
            }
        }
        
        // Escape key to clear form
        if (e.key === 'Escape') {
            clearForm();
        }
    });
}

function focusFirstInput() {
    const firstInput = document.querySelector('.form-control');
    if (firstInput) {
        setTimeout(() => {
            firstInput.focus();
        }, 100);
    }
}

function handleFormSubmission() {
    const form = document.querySelector('.admin-login-form');
    const submitButton = form.querySelector('button[type="submit"]');
    const originalText = submitButton.innerHTML;
    
    form.addEventListener('submit', function() {
        // Add loading state
        submitButton.disabled = true;
        submitButton.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Đang đăng nhập...';
        
        // Re-enable button after 5 seconds (fallback)
        setTimeout(() => {
            submitButton.disabled = false;
            submitButton.innerHTML = originalText;
        }, 5000);
    });
}

function clearForm() {
    const inputs = document.querySelectorAll('.form-control');
    inputs.forEach(input => {
        input.value = '';
        input.classList.remove('has-value', 'focused', 'error');
        clearValidationError(input);
    });
    
    const checkbox = document.getElementById('RememberMe');
    if (checkbox) {
        checkbox.checked = false;
    }
    
    focusFirstInput();
}

// Utility functions
function showNotification(message, type = 'info') {
    const notification = document.createElement('div');
    notification.className = `notification notification-${type}`;
    notification.innerHTML = `
        <i class="fas fa-${type === 'error' ? 'exclamation-triangle' : type === 'success' ? 'check-circle' : 'info-circle'}"></i>
        <span>${message}</span>
    `;
    
    document.body.appendChild(notification);
    
    // Show notification
    setTimeout(() => {
        notification.classList.add('show');
    }, 10);
    
    // Hide notification
    setTimeout(() => {
        notification.classList.remove('show');
        setTimeout(() => {
            notification.remove();
        }, 300);
    }, 3000);
}

// Password strength checker (optional)
function checkPasswordStrength(password) {
    let strength = 0;
    const checks = {
        length: password.length >= 8,
        lowercase: /[a-z]/.test(password),
        uppercase: /[A-Z]/.test(password),
        numbers: /\d/.test(password),
        special: /[!@#$%^&*(),.?":{}|<>]/.test(password)
    };
    
    strength = Object.values(checks).filter(Boolean).length;
    
    return {
        score: strength,
        checks: checks,
        level: strength < 2 ? 'weak' : strength < 4 ? 'medium' : 'strong'
    };
}

// Auto-save form data (optional, for better UX)
function autoSaveFormData() {
    const inputs = document.querySelectorAll('.form-control');
    
    inputs.forEach(input => {
        if (input.type !== 'password') {
            // Load saved data
            const savedValue = localStorage.getItem(`admin-login-${input.name}`);
            if (savedValue) {
                input.value = savedValue;
                input.classList.add('has-value');
            }
            
            // Save on input
            input.addEventListener('input', function() {
                localStorage.setItem(`admin-login-${this.name}`, this.value);
            });
        }
    });
}

// Initialize auto-save (commented out for security reasons)
// autoSaveFormData();
