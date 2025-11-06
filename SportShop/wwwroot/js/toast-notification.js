// Toast Notification System
// Hệ thống thông báo toast chuyên nghiệp cho LoLoSport

// Flag để đảm bảo chỉ khởi tạo 1 lần
if (typeof window.toastInitialized === 'undefined') {
    window.toastInitialized = true;

    /**
     * Hiển thị toast notification
     * @param {string} message - Nội dung thông báo
     * @param {string} type - Loại thông báo: 'success', 'error', 'warning', 'info'
     * @param {number} duration - Thời gian hiển thị (ms), mặc định 4000ms
     */
    function showToast(message, type = 'success', duration = 4000) {
    const toastContainer = document.getElementById('toastContainer');
    if (!toastContainer) return;

    // Tạo toast element
    const toastId = 'toast-' + Date.now();
    const toast = document.createElement('div');
    toast.id = toastId;
    toast.className = 'toast align-items-center border-0';
    toast.setAttribute('role', 'alert');
    toast.setAttribute('aria-live', 'assertive');
    toast.setAttribute('aria-atomic', 'true');

    // Xác định icon và màu sắc dựa trên type
    let iconClass, bgClass, iconColor;
    switch (type) {
        case 'success':
            iconClass = 'fa-check-circle';
            bgClass = 'toast-success';
            iconColor = '#fff';
            break;
        case 'error':
            iconClass = 'fa-exclamation-circle';
            bgClass = 'toast-error';
            iconColor = '#fff';
            break;
        case 'warning':
            iconClass = 'fa-exclamation-triangle';
            bgClass = 'toast-warning';
            iconColor = '#ffc107';
            break;
        case 'info':
            iconClass = 'fa-info-circle';
            bgClass = 'toast-info';
            iconColor = '#17a2b8';
            break;
        default:
            iconClass = 'fa-info-circle';
            bgClass = 'toast-info';
            iconColor = '#17a2b8';
    }

    toast.innerHTML = `
        <div class="toast-content ${bgClass}">
            <div class="d-flex align-items-center">
                <i class="fas ${iconClass} me-3" style="font-size: 1.5rem; color: ${iconColor};"></i>
                <div class="toast-body flex-grow-1">
                    ${message}
                </div>
                <button type="button" class="btn-close btn-close-white ms-2" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
        </div>
    `;

    toastContainer.appendChild(toast);

    // Khởi tạo Bootstrap toast
    const bsToast = new bootstrap.Toast(toast, {
        autohide: true,
        delay: duration
    });

    // Hiển thị toast
    bsToast.show();

    // Xóa toast khỏi DOM sau khi ẩn
    toast.addEventListener('hidden.bs.toast', function () {
        toast.remove();
    });
}

    /**
     * Hiển thị toast từ ModelState errors
     */
    function showModelStateErrors() {
        const errorsContainer = document.querySelector('[data-modelstate-errors]');
        if (!errorsContainer) return;

        const errors = JSON.parse(errorsContainer.getAttribute('data-modelstate-errors'));
        if (errors && errors.length > 0) {
            // Nếu có nhiều lỗi, hiển thị tổng hợp
            if (errors.length === 1) {
                showToast(errors[0], 'error', 5000);
            } else {
                let errorMessage = '<strong>Có lỗi xảy ra:</strong><ul class="mb-0 mt-2 ps-3">';
                errors.forEach(error => {
                    errorMessage += `<li>${error}</li>`;
                });
                errorMessage += '</ul>';
                showToast(errorMessage, 'error', 6000);
            }
        }
    }

    /**
     * Hiển thị toast từ Session success message
     */
    function showSessionMessage() {
        const messageContainer = document.querySelector('[data-session-message]');
        if (!messageContainer) return;

        const message = messageContainer.getAttribute('data-session-message');
        if (message && message.trim() !== '') {
            showToast(message, 'success', 5000);
        }
    }

    // Tự động hiển thị toast khi trang load
    document.addEventListener('DOMContentLoaded', function() {
        showSessionMessage(); // Hiển thị success message trước (nếu có)
        showModelStateErrors(); // Sau đó hiển thị error message (nếu có)
    });

    // Export function cho global scope
    window.showToast = showToast;
}