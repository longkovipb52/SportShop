// Dashboard JavaScript

$(document).ready(function() {
    initializeSidebar();
    initializeNotifications();
    initializeSearch();
    initializeUserDropdown();
    initializeAnimations();
});

// Sidebar functionality
function initializeSidebar() {
    const sidebar = $('#sidebar');
    const sidebarToggle = $('#sidebarToggle, #sidebarToggleTop');
    const mainContent = $('.main-content');

    // Toggle sidebar on mobile
    sidebarToggle.on('click', function(e) {
        e.preventDefault();
        
        if (window.innerWidth <= 991.98) {
            sidebar.toggleClass('show');
        }
    });

    // Close sidebar when clicking outside on mobile
    $(document).on('click', function(e) {
        if (window.innerWidth <= 991.98) {
            if (!sidebar.is(e.target) && sidebar.has(e.target).length === 0 && 
                !sidebarToggle.is(e.target) && sidebarToggle.has(e.target).length === 0) {
                sidebar.removeClass('show');
            }
        }
    });

    // Handle window resize
    $(window).on('resize', function() {
        if (window.innerWidth > 991.98) {
            sidebar.removeClass('show');
        }
    });

    // Highlight active nav item
    highlightActiveNavItem();
}

function highlightActiveNavItem() {
    const currentPath = window.location.pathname;
    $('.sidebar-nav .nav-link').removeClass('active');
    
    $('.sidebar-nav .nav-link').each(function() {
        const href = $(this).attr('href');
        if (href && currentPath.includes(href)) {
            $(this).addClass('active');
        }
    });
}

// Notifications functionality
function initializeNotifications() {
    const notificationBell = $('.notification-bell');
    
    notificationBell.on('click', function(e) {
        e.preventDefault();
        // TODO: Show notifications dropdown
        console.log('Show notifications');
    });

    // Animate notification badge
    animateNotificationBadge();
}

function animateNotificationBadge() {
    const badge = $('.notification-badge');
    if (badge.length > 0) {
        setInterval(function() {
            badge.animate({
                opacity: 0.5
            }, 1000).animate({
                opacity: 1
            }, 1000);
        }, 3000);
    }
}

// Search functionality
function initializeSearch() {
    const searchInput = $('.topbar-search input');
    
    searchInput.on('input', function() {
        const query = $(this).val().trim();
        if (query.length >= 2) {
            performSearch(query);
        }
    });

    searchInput.on('keypress', function(e) {
        if (e.which === 13) { // Enter key
            e.preventDefault();
            const query = $(this).val().trim();
            if (query.length >= 2) {
                performGlobalSearch(query);
            }
        }
    });
}

function performSearch(query) {
    // TODO: Implement live search
    console.log('Searching for:', query);
}

function performGlobalSearch(query) {
    // TODO: Navigate to search results page
    console.log('Global search for:', query);
}

// User dropdown functionality
function initializeUserDropdown() {
    const topbarUser = $('.topbar-user');
    const userDropdown = $('.user-dropdown');
    const dropdownMenu = $('.dropdown-menu');
    
    // Show dropdown on hover (desktop only)
    if (window.innerWidth >= 992) {
        topbarUser.on('mouseenter', function() {
            dropdownMenu.addClass('show');
        });
        
        // Hide dropdown when mouse leaves the entire topbar-user area
        topbarUser.on('mouseleave', function() {
            // Add a small delay to prevent flickering
            setTimeout(function() {
                if (!topbarUser.is(':hover')) {
                    dropdownMenu.removeClass('show');
                }
            }, 150);
        });
    }
    
    // Toggle dropdown on click (works on all devices)
    userDropdown.on('click', function(e) {
        e.preventDefault();
        e.stopPropagation();
        
        // Close other dropdowns first
        $('.dropdown-menu').not(dropdownMenu).removeClass('show');
        
        // Toggle current dropdown
        dropdownMenu.toggleClass('show');
    });
    
    // Close dropdown when clicking outside
    $(document).on('click', function(e) {
        const $target = $(e.target);
        const isTopbarUserClick = $target.closest('.topbar-user').length > 0;
        
        if (!isTopbarUserClick) {
            dropdownMenu.removeClass('show');
        }
    });
    
    // Don't close dropdown when clicking inside the menu (except logout)
    dropdownMenu.on('click', function(e) {
        if (!$(e.target).hasClass('logout-btn') && !$(e.target).closest('.logout-btn').length) {
            e.stopPropagation();
        }
    });
    
    // Handle logout button confirmation
    $('.logout-btn').on('click', function(e) {
        e.preventDefault(); // Prevent immediate form submission
        
        const confirmed = confirm('Bạn có chắc chắn muốn đăng xuất không?');
        if (!confirmed) {
            return false;
        }
        
        // Show loading state
        const $button = $(this);
        const originalText = $button.html();
        $button.html('<i class="fas fa-spinner fa-spin"></i> Đang đăng xuất...');
        $button.prop('disabled', true);
        
        // Submit the form after showing loading state
        setTimeout(function() {
            $button.closest('form').submit();
        }, 100);
    });
}

// Animations and effects
function initializeAnimations() {
    // Animate stats cards on load
    animateStatsCards();
    
    // Animate charts
    animateCharts();
    
    // Add hover effects
    addHoverEffects();
}

function animateStatsCards() {
    $('.stats-card').each(function(index) {
        const $card = $(this);
        setTimeout(function() {
            $card.addClass('animate__animated animate__fadeInUp');
        }, index * 100);
    });
}

function animateCharts() {
    // Charts will be animated by Chart.js
    $('.chart-container').addClass('animate__animated animate__fadeIn');
}

function addHoverEffects() {
    // Stats cards hover effect
    $('.stats-card').hover(
        function() {
            $(this).find('.stats-icon').addClass('animate__animated animate__pulse');
        },
        function() {
            $(this).find('.stats-icon').removeClass('animate__animated animate__pulse');
        }
    );

    // Navigation hover effect
    $('.nav-link').hover(
        function() {
            $(this).find('i').addClass('animate__animated animate__bounce');
        },
        function() {
            $(this).find('i').removeClass('animate__animated animate__bounce');
        }
    );
}

// Utility functions
function formatCurrency(amount) {
    return new Intl.NumberFormat('vi-VN', {
        style: 'currency',
        currency: 'VND'
    }).format(amount);
}

function formatNumber(number) {
    return new Intl.NumberFormat('vi-VN').format(number);
}

function formatDate(date) {
    return new Intl.DateTimeFormat('vi-VN', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit'
    }).format(new Date(date));
}

// Loading states
function showLoading(element) {
    const $element = $(element);
    $element.html('<div class="loading"><i class="fas fa-spinner"></i> Đang tải...</div>');
}

function hideLoading() {
    $('.loading').remove();
}

// Toast notifications
function showToast(message, type = 'info') {
    const toast = $(`
        <div class="toast-notification toast-${type}">
            <div class="toast-content">
                <i class="fas ${getToastIcon(type)}"></i>
                <span>${message}</span>
            </div>
            <button class="toast-close">
                <i class="fas fa-times"></i>
            </button>
        </div>
    `);

    $('body').append(toast);
    
    setTimeout(function() {
        toast.addClass('show');
    }, 100);

    setTimeout(function() {
        hideToast(toast);
    }, 5000);

    toast.find('.toast-close').on('click', function() {
        hideToast(toast);
    });
}

function hideToast(toast) {
    toast.removeClass('show');
    setTimeout(function() {
        toast.remove();
    }, 300);
}

function getToastIcon(type) {
    switch (type) {
        case 'success':
            return 'fa-check-circle';
        case 'warning':
            return 'fa-exclamation-triangle';
        case 'error':
            return 'fa-times-circle';
        default:
            return 'fa-info-circle';
    }
}

// Export functions for global use
window.DashboardUtils = {
    formatCurrency,
    formatNumber,
    formatDate,
    showLoading,
    hideLoading,
    showToast,
    hideToast
};
