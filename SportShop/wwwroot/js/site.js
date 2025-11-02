// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Common utility functions for the entire site

// Function to handle login requirement
function handleLoginRequired(message = "Vui lòng đăng nhập để tiếp tục") {
    // Show notification
    if (typeof showNotification === 'function') {
        showNotification(message, 'warning');
    } else {
        alert(message);
    }
    
    // Redirect to login page after a delay
    setTimeout(() => {
        window.location.href = '/Account/Login?returnUrl=' + encodeURIComponent(window.location.pathname);
    }, 2000);
}

// Function to show notification (fallback if not defined elsewhere)
if (typeof showNotification === 'undefined') {
    window.showNotification = function(message, type = 'info') {
        // Simple alert fallback
        const icon = type === 'success' ? '✅' : type === 'error' ? '❌' : type === 'warning' ? '⚠️' : 'ℹ️';
        alert(icon + ' ' + message);
    };
}

// Function to update cart counter
if (typeof updateCartCounter === 'undefined') {
    window.updateCartCounter = function(count) {
        const cartCountElement = document.querySelector('.cart-count');
        if (cartCountElement) {
            cartCountElement.textContent = count || '0';
            
            // Animation effect
            cartCountElement.classList.add('pulse');
            setTimeout(() => {
                cartCountElement.classList.remove('pulse');
            }, 1000);
        }
    };
}

// Format currency to VND
window.formatCurrency = function(value) {
    return new Intl.NumberFormat('vi-VN').format(value) + 'đ';
};

// Check if user is logged in
window.isUserLoggedIn = function() {
    // You can customize this logic based on how you determine if user is logged in
    // For now, we'll check if there's a user menu or login link
    const userMenu = document.querySelector('.user-menu');
    const loginLink = document.querySelector('[href*="/Account/Login"]');
    
    return userMenu && !loginLink;
};

// Common AJAX error handler
window.handleAjaxResponse = function(data, successCallback, errorCallback) {
    if (data.success) {
        if (typeof successCallback === 'function') {
            successCallback(data);
        } else {
            showNotification(data.message || 'Thao tác thành công!', 'success');
        }
    } else {
        if (data.requireLogin) {
            handleLoginRequired(data.message);
        } else {
            if (typeof errorCallback === 'function') {
                errorCallback(data);
            } else {
                showNotification(data.message || 'Có lỗi xảy ra!', 'error');
            }
        }
    }
};

// Write your JavaScript code.

// Scroll to Top Button
document.addEventListener('DOMContentLoaded', function() {
    const scrollToTopBtn = document.getElementById('scrollToTop');
    
    if (scrollToTopBtn) {
        // Show/hide button on scroll
        window.addEventListener('scroll', function() {
            if (window.pageYOffset > 300) {
                scrollToTopBtn.classList.add('show');
            } else {
                scrollToTopBtn.classList.remove('show');
            }
        });
        
        // Scroll to top on click
        scrollToTopBtn.addEventListener('click', function() {
            window.scrollTo({
                top: 0,
                behavior: 'smooth'
            });
        });
    }
});
