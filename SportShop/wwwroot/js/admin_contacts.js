// Admin Contacts JavaScript

document.addEventListener('DOMContentLoaded', function() {
    initializeContactsManagement();
});

function initializeContactsManagement() {
    initializeCheckboxes();
    initializeBulkActions();
    initializeSearch();
    initializeConfirmations();
    initializeTooltips();
    initializeResponsiveTable();
}

// Checkbox Management
function initializeCheckboxes() {
    const selectAllCheckbox = document.getElementById('selectAll');
    const headerSelectAllCheckbox = document.getElementById('headerSelectAll');
    const contactCheckboxes = document.querySelectorAll('.contact-checkbox');
    const selectedCountSpan = document.querySelector('.selected-count');
    const bulkActionBtn = document.querySelector('.bulk-action-btn');

    // Sync both select all checkboxes
    if (selectAllCheckbox && headerSelectAllCheckbox) {
        selectAllCheckbox.addEventListener('change', function() {
            headerSelectAllCheckbox.checked = this.checked;
            toggleAllContacts(this.checked);
        });

        headerSelectAllCheckbox.addEventListener('change', function() {
            selectAllCheckbox.checked = this.checked;
            toggleAllContacts(this.checked);
        });
    }

    // Individual checkbox handling
    contactCheckboxes.forEach(checkbox => {
        checkbox.addEventListener('change', updateSelectionState);
    });

    function toggleAllContacts(checked) {
        contactCheckboxes.forEach(checkbox => {
            checkbox.checked = checked;
            updateRowSelection(checkbox);
        });
        updateSelectionState();
    }

    function updateSelectionState() {
        const checkedBoxes = document.querySelectorAll('.contact-checkbox:checked');
        const totalBoxes = document.querySelectorAll('.contact-checkbox');
        
        // Update count display
        if (selectedCountSpan) {
            selectedCountSpan.textContent = `${checkedBoxes.length} đã chọn`;
        }

        // Update select all checkbox state
        if (selectAllCheckbox && headerSelectAllCheckbox) {
            const allChecked = checkedBoxes.length === totalBoxes.length && totalBoxes.length > 0;
            const someChecked = checkedBoxes.length > 0;
            
            selectAllCheckbox.checked = allChecked;
            headerSelectAllCheckbox.checked = allChecked;
            selectAllCheckbox.indeterminate = someChecked && !allChecked;
            headerSelectAllCheckbox.indeterminate = someChecked && !allChecked;
        }

        // Enable/disable bulk action button
        if (bulkActionBtn) {
            bulkActionBtn.disabled = checkedBoxes.length === 0;
        }

        // Update row highlighting
        contactCheckboxes.forEach(updateRowSelection);
    }

    function updateRowSelection(checkbox) {
        const row = checkbox.closest('tr');
        if (row) {
            if (checkbox.checked) {
                row.classList.add('selected');
            } else {
                row.classList.remove('selected');
            }
        }
    }

    // Initialize state
    updateSelectionState();
}

// Bulk Actions
function initializeBulkActions() {
    const bulkActionForm = document.getElementById('bulkActionForm');
    const bulkActionSelect = document.querySelector('.bulk-action-select');
    const bulkActionBtn = document.querySelector('.bulk-action-btn');

    if (bulkActionForm) {
        bulkActionForm.addEventListener('submit', function(e) {
            const selectedAction = bulkActionSelect?.value;
            const checkedBoxes = document.querySelectorAll('.contact-checkbox:checked');

            if (!selectedAction) {
                e.preventDefault();
                showToast('Vui lòng chọn hành động cần thực hiện.', 'warning');
                return;
            }

            if (checkedBoxes.length === 0) {
                e.preventDefault();
                showToast('Vui lòng chọn ít nhất một liên hệ.', 'warning');
                return;
            }

            // Confirmation for destructive actions
            if (selectedAction === 'delete') {
                const confirmMessage = `Bạn có chắc chắn muốn xóa ${checkedBoxes.length} liên hệ đã chọn? Hành động này không thể hoàn tác.`;
                if (!confirm(confirmMessage)) {
                    e.preventDefault();
                    return;
                }
            } else {
                const actionNames = {
                    'mark-new': 'đánh dấu là chưa trả lời',
                    'mark-replied': 'đánh dấu là đã trả lời'
                };
                const actionName = actionNames[selectedAction] || 'thực hiện hành động trên';
                const confirmMessage = `Bạn có chắc chắn muốn ${actionName} ${checkedBoxes.length} liên hệ đã chọn?`;
                if (!confirm(confirmMessage)) {
                    e.preventDefault();
                    return;
                }
            }

            // Show loading state
            if (bulkActionBtn) {
                bulkActionBtn.disabled = true;
                bulkActionBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Đang xử lý...';
            }
        });
    }
}

// Search Enhancement
function initializeSearch() {
    const searchInput = document.querySelector('.contacts-search-input');
    const searchForm = document.querySelector('.filters-form');
    
    if (searchInput && searchForm) {
        let searchTimeout;

        // Auto-submit search after typing stops
        searchInput.addEventListener('input', function() {
            clearTimeout(searchTimeout);
            searchTimeout = setTimeout(() => {
                if (this.value.length >= 2 || this.value.length === 0) {
                    searchForm.submit();
                }
            }, 500);
        });

        // Clear search button
        const clearSearchBtn = document.createElement('button');
        clearSearchBtn.type = 'button';
        clearSearchBtn.className = 'btn btn-outline-secondary btn-sm clear-search-btn';
        clearSearchBtn.innerHTML = '<i class="fas fa-times"></i>';
        clearSearchBtn.title = 'Xóa tìm kiếm';
        clearSearchBtn.style.display = searchInput.value ? 'block' : 'none';

        searchInput.addEventListener('input', function() {
            clearSearchBtn.style.display = this.value ? 'block' : 'none';
        });

        clearSearchBtn.addEventListener('click', function() {
            searchInput.value = '';
            clearSearchBtn.style.display = 'none';
            searchForm.submit();
        });

        // Insert clear button
        const searchContainer = searchInput.parentElement;
        if (searchContainer) {
            searchContainer.style.position = 'relative';
            clearSearchBtn.style.position = 'absolute';
            clearSearchBtn.style.right = '10px';
            clearSearchBtn.style.top = '50%';
            clearSearchBtn.style.transform = 'translateY(-50%)';
            clearSearchBtn.style.zIndex = '10';
            searchContainer.appendChild(clearSearchBtn);
        }
    }
}

// Confirmation Dialogs
function initializeConfirmations() {
    // Delete confirmations
    const deleteLinks = document.querySelectorAll('a[href*="/Delete"]');
    deleteLinks.forEach(link => {
        link.addEventListener('click', function(e) {
            if (!confirm('Bạn có chắc chắn muốn xóa liên hệ này? Hành động này không thể hoàn tác.')) {
                e.preventDefault();
            }
        });
    });

    // Status update confirmations
    const statusForms = document.querySelectorAll('form[action*="UpdateStatus"]');
    statusForms.forEach(form => {
        form.addEventListener('submit', function(e) {
            const statusInput = form.querySelector('input[name="status"]');
            if (statusInput) {
                const newStatus = statusInput.value;
                const statusNames = {
                    'New': 'chưa trả lời',
                    'Replied': 'đã trả lời'
                };
                const statusName = statusNames[newStatus] || newStatus;
                
                if (!confirm(`Bạn có chắc chắn muốn đánh dấu liên hệ này là ${statusName}?`)) {
                    e.preventDefault();
                }
            }
        });
    });
}

// Tooltips
function initializeTooltips() {
    // Initialize Bootstrap tooltips if available
    if (typeof bootstrap !== 'undefined') {
        const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        tooltipTriggerList.map(function(tooltipTriggerEl) {
            return new bootstrap.Tooltip(tooltipTriggerEl);
        });
    }

    // Custom tooltips for action buttons
    const actionButtons = document.querySelectorAll('.action-buttons .btn');
    actionButtons.forEach(btn => {
        btn.addEventListener('mouseenter', function() {
            const title = this.getAttribute('title');
            if (title) {
                showTooltip(this, title);
            }
        });

        btn.addEventListener('mouseleave', function() {
            hideTooltip();
        });
    });
}

// Responsive Table
function initializeResponsiveTable() {
    const table = document.querySelector('.contacts-table');
    const tableContainer = document.querySelector('.table-container');

    if (table && tableContainer) {
        // Add scroll indicators
        function updateScrollIndicators() {
            const scrollLeft = tableContainer.scrollLeft;
            const scrollWidth = tableContainer.scrollWidth;
            const clientWidth = tableContainer.clientWidth;

            if (scrollWidth > clientWidth) {
                tableContainer.classList.add('scrollable');
                
                if (scrollLeft > 0) {
                    tableContainer.classList.add('scrolled-left');
                } else {
                    tableContainer.classList.remove('scrolled-left');
                }

                if (scrollLeft < scrollWidth - clientWidth - 1) {
                    tableContainer.classList.add('can-scroll-right');
                } else {
                    tableContainer.classList.remove('can-scroll-right');
                }
            } else {
                tableContainer.classList.remove('scrollable', 'scrolled-left', 'can-scroll-right');
            }
        }

        tableContainer.addEventListener('scroll', updateScrollIndicators);
        window.addEventListener('resize', updateScrollIndicators);
        updateScrollIndicators();

        // Mobile table enhancements
        if (window.innerWidth <= 768) {
            addMobileTableFeatures();
        }

        window.addEventListener('resize', function() {
            if (window.innerWidth <= 768) {
                addMobileTableFeatures();
            } else {
                removeMobileTableFeatures();
            }
        });
    }
}

function addMobileTableFeatures() {
    // Add mobile-specific classes and features
    const table = document.querySelector('.contacts-table');
    if (table) {
        table.classList.add('mobile-table');
    }
}

function removeMobileTableFeatures() {
    const table = document.querySelector('.contacts-table');
    if (table) {
        table.classList.remove('mobile-table');
    }
}

// Toast Notifications
function showToast(message, type = 'info', duration = 3000) {
    const toastContainer = getOrCreateToastContainer();
    
    const toast = document.createElement('div');
    toast.className = `toast toast-${type} show`;
    toast.setAttribute('role', 'alert');
    toast.innerHTML = `
        <div class="toast-header">
            <i class="fas fa-${getToastIcon(type)} me-2"></i>
            <strong class="me-auto">${getToastTitle(type)}</strong>
            <button type="button" class="btn-close" data-bs-dismiss="toast"></button>
        </div>
        <div class="toast-body">
            ${message}
        </div>
    `;

    toastContainer.appendChild(toast);

    // Auto remove
    setTimeout(() => {
        toast.classList.remove('show');
        setTimeout(() => {
            if (toast.parentNode) {
                toast.parentNode.removeChild(toast);
            }
        }, 300);
    }, duration);

    // Manual close
    const closeBtn = toast.querySelector('.btn-close');
    if (closeBtn) {
        closeBtn.addEventListener('click', () => {
            toast.classList.remove('show');
            setTimeout(() => {
                if (toast.parentNode) {
                    toast.parentNode.removeChild(toast);
                }
            }, 300);
        });
    }
}

function getOrCreateToastContainer() {
    let container = document.querySelector('.toast-container');
    if (!container) {
        container = document.createElement('div');
        container.className = 'toast-container';
        document.body.appendChild(container);
    }
    return container;
}

function getToastIcon(type) {
    const icons = {
        'success': 'check-circle',
        'error': 'exclamation-circle',
        'warning': 'exclamation-triangle',
        'info': 'info-circle'
    };
    return icons[type] || 'info-circle';
}

function getToastTitle(type) {
    const titles = {
        'success': 'Thành công',
        'error': 'Lỗi',
        'warning': 'Cảnh báo',
        'info': 'Thông tin'
    };
    return titles[type] || 'Thông báo';
}

// Tooltip utilities
let currentTooltip = null;

function showTooltip(element, text) {
    hideTooltip();
    
    const tooltip = document.createElement('div');
    tooltip.className = 'custom-tooltip';
    tooltip.textContent = text;
    document.body.appendChild(tooltip);
    
    const rect = element.getBoundingClientRect();
    const tooltipRect = tooltip.getBoundingClientRect();
    
    tooltip.style.left = (rect.left + rect.width / 2 - tooltipRect.width / 2) + 'px';
    tooltip.style.top = (rect.top - tooltipRect.height - 8) + 'px';
    
    currentTooltip = tooltip;
}

function hideTooltip() {
    if (currentTooltip) {
        currentTooltip.remove();
        currentTooltip = null;
    }
}

// Email and Phone utilities
function formatEmail(email) {
    if (email.length > 30) {
        return email.substring(0, 27) + '...';
    }
    return email;
}

function formatPhone(phone) {
    // Remove all non-digit characters
    const cleaned = phone.replace(/\D/g, '');
    
    // Format Vietnamese phone numbers
    if (cleaned.length === 10) {
        return cleaned.replace(/(\d{4})(\d{3})(\d{3})/, '$1 $2 $3');
    } else if (cleaned.length === 11) {
        return cleaned.replace(/(\d{4})(\d{3})(\d{4})/, '$1 $2 $3');
    }
    
    return phone;
}

// Export functions for use in other scripts
window.ContactsAdmin = {
    showToast,
    hideTooltip,
    formatEmail,
    formatPhone
};

// Handle TempData messages
document.addEventListener('DOMContentLoaded', function() {
    // Check for TempData messages and show toasts
    const successMessage = document.querySelector('[data-tempdata="success"]');
    const errorMessage = document.querySelector('[data-tempdata="error"]');
    const warningMessage = document.querySelector('[data-tempdata="warning"]');
    const infoMessage = document.querySelector('[data-tempdata="info"]');

    if (successMessage) {
        showToast(successMessage.textContent, 'success');
    }
    if (errorMessage) {
        showToast(errorMessage.textContent, 'error');
    }
    if (warningMessage) {
        showToast(warningMessage.textContent, 'warning');
    }
    if (infoMessage) {
        showToast(infoMessage.textContent, 'info');
    }
});

// Keyboard shortcuts
document.addEventListener('keydown', function(e) {
    // Ctrl+A to select all
    if (e.ctrlKey && e.key === 'a' && e.target.tagName !== 'INPUT' && e.target.tagName !== 'TEXTAREA') {
        e.preventDefault();
        const selectAllCheckbox = document.getElementById('selectAll');
        if (selectAllCheckbox) {
            selectAllCheckbox.checked = !selectAllCheckbox.checked;
            selectAllCheckbox.dispatchEvent(new Event('change'));
        }
    }

    // Escape to clear selection
    if (e.key === 'Escape') {
        const selectAllCheckbox = document.getElementById('selectAll');
        if (selectAllCheckbox && selectAllCheckbox.checked) {
            selectAllCheckbox.checked = false;
            selectAllCheckbox.dispatchEvent(new Event('change'));
        }
    }

    // F to focus search
    if (e.key === 'f' && !e.ctrlKey && e.target.tagName !== 'INPUT' && e.target.tagName !== 'TEXTAREA') {
        e.preventDefault();
        const searchInput = document.querySelector('.contacts-search-input');
        if (searchInput) {
            searchInput.focus();
        }
    }
});

// Auto-refresh functionality (optional)
function enableAutoRefresh(intervalMinutes = 5) {
    if (intervalMinutes <= 0) return;
    
    setInterval(() => {
        // Only refresh if no bulk actions are selected
        const checkedBoxes = document.querySelectorAll('.contact-checkbox:checked');
        if (checkedBoxes.length === 0) {
            // Preserve current filters and page
            const currentUrl = new URL(window.location);
            window.location.href = currentUrl.href;
        }
    }, intervalMinutes * 60 * 1000);
}

// Initialize auto-refresh if enabled
// enableAutoRefresh(5); // Uncomment to enable 5-minute auto-refresh