document.addEventListener('DOMContentLoaded', function() {
    // Form validation nâng cao
    const forms = document.querySelectorAll('.needs-validation');
    
    Array.from(forms).forEach(form => {
        form.addEventListener('submit', event => {
            if (!form.checkValidity()) {
                event.preventDefault();
                event.stopPropagation();
            }
            
            form.classList.add('was-validated');
        }, false);
    });
    
    // Handle register form submission
    const registerForm = document.querySelector('form[action*="Register"]');
    const registerBtn = document.getElementById('registerBtn');
    
    if (registerForm && registerBtn) {
        // Reset button state when page loads (in case of errors from server)
        const btnText = registerBtn.querySelector('.btn-text');
        const btnSpinner = registerBtn.querySelector('.btn-spinner');
        if (btnText && btnSpinner) {
            btnText.classList.remove('d-none');
            btnSpinner.classList.add('d-none');
            registerBtn.disabled = false;
        }
        
        registerForm.addEventListener('submit', function(e) {
            // Check all validation rules
            const allInputs = registerForm.querySelectorAll('input[required], input[type="email"], input[type="tel"]');
            let isValid = true;
            
            allInputs.forEach(input => {
                if (!input.checkValidity()) {
                    isValid = false;
                }
            });
            
            // Only show loading if form is valid
            if (isValid && registerForm.checkValidity()) {
                // Show loading state
                if (btnText && btnSpinner) {
                    btnText.classList.add('d-none');
                    btnSpinner.classList.remove('d-none');
                    registerBtn.disabled = true;
                }
            } else {
                e.preventDefault();
                e.stopPropagation();
            }
        });
    }

    // Handle login form submission
    const loginForm = document.querySelector('form[action*="Login"]');
    const loginBtn = document.getElementById('loginBtn');
    
    if (loginForm && loginBtn) {
        // Reset button state when page loads
        const btnText = loginBtn.querySelector('.btn-text');
        const btnSpinner = loginBtn.querySelector('.btn-spinner');
        if (btnText && btnSpinner) {
            btnText.classList.remove('d-none');
            btnSpinner.classList.add('d-none');
            loginBtn.disabled = false;
        }
        
        loginForm.addEventListener('submit', function(e) {
            // Check all validation rules
            const allInputs = loginForm.querySelectorAll('input[required]');
            let isValid = true;
            
            allInputs.forEach(input => {
                if (!input.checkValidity()) {
                    isValid = false;
                }
            });
            
            // Only show loading if form is valid
            if (isValid && loginForm.checkValidity()) {
                // Show loading state
                if (btnText && btnSpinner) {
                    btnText.classList.add('d-none');
                    btnSpinner.classList.remove('d-none');
                    loginBtn.disabled = true;
                }
            } else {
                e.preventDefault();
                e.stopPropagation();
            }
        });
    }
    
    // Real-time validation for Username (alphanumeric and underscore only)
    const usernameField = document.getElementById('Username');
    if (usernameField) {
        usernameField.addEventListener('input', function() {
            const value = this.value;
            const isValid = /^[a-zA-Z0-9_]*$/.test(value);
            
            if (!isValid && value.length > 0) {
                this.setCustomValidity('Chỉ được chứa chữ cái, số và dấu gạch dưới');
            } else if (value.length > 0 && value.length < 3) {
                this.setCustomValidity('Tên đăng nhập phải có ít nhất 3 ký tự');
            } else {
                this.setCustomValidity('');
            }
        });
    }
    
    // Real-time validation for FullName (letters and spaces only)
    const fullNameField = document.getElementById('FullName');
    if (fullNameField) {
        fullNameField.addEventListener('input', function() {
            const value = this.value;
            const isValid = /^[\p{L}\s]*$/u.test(value);
            
            if (!isValid && value.length > 0) {
                this.setCustomValidity('Chỉ được chứa chữ cái và khoảng trắng');
            } else {
                this.setCustomValidity('');
            }
        });
    }
    
    // Real-time validation for Phone
    const phoneField = document.getElementById('Phone');
    if (phoneField) {
        phoneField.addEventListener('input', function() {
            const value = this.value;
            const isValid = /^(0|\+84)[0-9]{0,10}$/.test(value);
            
            if (!isValid && value.length > 0) {
                this.setCustomValidity('Số điện thoại phải bắt đầu bằng 0 hoặc +84 và chỉ chứa số');
            } else if (value.length > 0 && value.length < 10) {
                this.setCustomValidity('Số điện thoại phải có ít nhất 10 số');
            } else {
                this.setCustomValidity('');
            }
        });
    }
    
    // Password requirements validation
    const passwordField = document.getElementById('Password');
    
    if (passwordField) {
        // Create password requirements display
        const requirementsHtml = `
            <div class="password-requirements mt-2">
                <div class="requirements-title">Yêu cầu mật khẩu:</div>
                <ul class="requirements-list">
                    <li id="req-length"><i class="fas fa-circle"></i> Ít nhất 8 ký tự</li>
                    <li id="req-uppercase"><i class="fas fa-circle"></i> Chứa chữ hoa (A-Z)</li>
                    <li id="req-lowercase"><i class="fas fa-circle"></i> Chứa chữ thường (a-z)</li>
                    <li id="req-number"><i class="fas fa-circle"></i> Chứa số (0-9)</li>
                    <li id="req-special"><i class="fas fa-circle"></i> Chứa ký tự đặc biệt (@$!%*?&)</li>
                </ul>
            </div>
        `;
        
        // Insert requirements after the ConfirmPassword field
        const confirmPasswordField = document.getElementById('ConfirmPassword');
        if (confirmPasswordField) {
            const confirmPasswordContainer = confirmPasswordField.closest('.form-floating');
            if (confirmPasswordContainer) {
                confirmPasswordContainer.insertAdjacentHTML('afterend', requirementsHtml);
            }
        }
        
        passwordField.addEventListener('input', function() {
            const password = this.value;
            updatePasswordRequirements(password);
            
            // Validate password format
            if (password.length > 0) {
                const hasMinLength = password.length >= 8;
                const hasUpperCase = /[A-Z]/.test(password);
                const hasLowerCase = /[a-z]/.test(password);
                const hasNumber = /\d/.test(password);
                const hasSpecialChar = /[@$!%*?&]/.test(password);
                
                const isValid = hasMinLength && hasUpperCase && hasLowerCase && hasNumber && hasSpecialChar;
                
                if (!isValid) {
                    this.setCustomValidity('Mật khẩu chưa đáp ứng đầy đủ yêu cầu');
                } else {
                    this.setCustomValidity('');
                }
            } else {
                this.setCustomValidity('');
            }
        });
        
        // Kiểm tra ngay khi trang tải xong nếu đã có giá trị
        if (passwordField.value) {
            updatePasswordRequirements(passwordField.value);
        }
    }
    
    function updatePasswordRequirements(password) {
        const requirements = {
            'req-length': password.length >= 8,
            'req-uppercase': /[A-Z]/.test(password),
            'req-lowercase': /[a-z]/.test(password),
            'req-number': /\d/.test(password),
            'req-special': /[@$!%*?&]/.test(password)
        };
        
        for (const [id, isMet] of Object.entries(requirements)) {
            const element = document.getElementById(id);
            if (element) {
                if (isMet) {
                    element.classList.add('valid');
                    element.querySelector('i').className = 'fas fa-check-circle';
                } else {
                    element.classList.remove('valid');
                    element.querySelector('i').className = 'fas fa-circle';
                }
            }
        }
    }
    
    // Xử lý đặc biệt cho floating labels
    const formFloatingInputs = document.querySelectorAll('.form-floating input');
    
    // Áp dụng class khi có giá trị ban đầu
    formFloatingInputs.forEach(input => {
        // Nếu input đã có giá trị khi trang tải
        if (input.value) {
            input.classList.add('has-value');
        }
        
        // Thêm sự kiện khi nhập dữ liệu
        input.addEventListener('input', function() {
            if (this.value) {
                this.classList.add('has-value');
            } else {
                this.classList.remove('has-value');
            }
        });
    });
    
    // Đảm bảo nhãn không bị đè lên khi trang tải
    setTimeout(function() {
        document.querySelectorAll('.form-floating input').forEach(input => {
            if (input.value) {
                const label = input.nextElementSibling;
                if (label && label.tagName === 'LABEL') {
                    label.style.opacity = '0.65';
                    label.style.transform = 'scale(0.85) translateY(-0.5rem) translateX(0.15rem)';
                }
            }
        });
    }, 100);
    
    // Toggle password visibility - Universal for all password fields
    document.querySelectorAll('.toggle-password').forEach(toggle => {
        toggle.addEventListener('click', function() {
            const targetId = this.getAttribute('data-target');
            const input = document.getElementById(targetId);
            const icon = this.querySelector('i');
            
            if (input && icon) {
                if (input.type === 'password') {
                    input.type = 'text';
                    icon.classList.remove('fa-eye');
                    icon.classList.add('fa-eye-slash');
                } else {
                    input.type = 'password';
                    icon.classList.remove('fa-eye-slash');
                    icon.classList.add('fa-eye');
                }
            }
        });
    });
});