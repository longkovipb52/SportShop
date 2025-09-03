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
    
    // Password strength indicator
    const passwordField = document.getElementById('Password');
    const strengthIndicator = document.getElementById('password-strength');
    
    if (passwordField && strengthIndicator) {
        passwordField.addEventListener('input', () => {
            const strength = checkPasswordStrength(passwordField.value);
            updateStrengthIndicator(strength);
        });
        
        // Kiểm tra ngay khi trang tải xong nếu đã có giá trị
        if (passwordField.value) {
            const strength = checkPasswordStrength(passwordField.value);
            updateStrengthIndicator(strength);
        }
    }
    
    function checkPasswordStrength(password) {
        // 0: Empty, 1: Weak, 2: Medium, 3: Strong
        if (!password) return 0;
        
        let strength = 0;
        
        // Length check
        if (password.length >= 8) strength += 1;
        
        // Contains number
        if (/\d/.test(password)) strength += 1;
        
        // Contains special character
        if (/[!@#$%^&*]/.test(password)) strength += 1;
        
        // Contains uppercase & lowercase
        if (/[a-z]/.test(password) && /[A-Z]/.test(password)) strength += 1;
        
        return Math.min(Math.floor(strength * 3 / 4), 3);
    }
    
    function updateStrengthIndicator(strength) {
        const strengthText = ['Trống', 'Yếu', 'Trung bình', 'Mạnh'];
        const strengthColor = ['', 'danger', 'warning', 'success'];
        
        strengthIndicator.textContent = 'Độ mạnh mật khẩu: ' + strengthText[strength];
        strengthIndicator.className = 'password-strength text-' + strengthColor[strength];
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
});