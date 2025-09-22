// ===== Admin Reviews JavaScript =====

document.addEventListener('DOMContentLoaded', function() {
    initializeReviewsPage();
});

function initializeReviewsPage() {
    initializeCheckboxes();
    initializeBulkActions();
    initializeDeleteButtons();
    initializeQuickActions();
    initializeTooltips();
    initializeModals();
}

// ===== Checkbox Management =====
function initializeCheckboxes() {
    const selectAllCheckbox = document.getElementById('selectAll');
    const reviewCheckboxes = document.querySelectorAll('.review-checkbox');

    if (selectAllCheckbox) {
        selectAllCheckbox.addEventListener('change', function() {
            const isChecked = this.checked;
            reviewCheckboxes.forEach(checkbox => {
                checkbox.checked = isChecked;
                updateRowSelection(checkbox);
            });
            updateBulkActionButton();
        });
    }

    reviewCheckboxes.forEach(checkbox => {
        checkbox.addEventListener('change', function() {
            updateRowSelection(this);
            updateSelectAllCheckbox();
            updateBulkActionButton();
        });
    });
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

function updateSelectAllCheckbox() {
    const selectAllCheckbox = document.getElementById('selectAll');
    const reviewCheckboxes = document.querySelectorAll('.review-checkbox');
    const checkedCheckboxes = document.querySelectorAll('.review-checkbox:checked');

    if (selectAllCheckbox) {
        if (checkedCheckboxes.length === 0) {
            selectAllCheckbox.indeterminate = false;
            selectAllCheckbox.checked = false;
        } else if (checkedCheckboxes.length === reviewCheckboxes.length) {
            selectAllCheckbox.indeterminate = false;
            selectAllCheckbox.checked = true;
        } else {
            selectAllCheckbox.indeterminate = true;
        }
    }
}

function updateBulkActionButton() {
    const applyButton = document.getElementById('applyBulkAction');
    const checkedCheckboxes = document.querySelectorAll('.review-checkbox:checked');
    
    if (applyButton) {
        if (checkedCheckboxes.length > 0) {
            applyButton.removeAttribute('disabled');
            applyButton.innerHTML = `<i class="fas fa-play"></i> Áp dụng (${checkedCheckboxes.length})`;
        } else {
            applyButton.setAttribute('disabled', 'disabled');
            applyButton.innerHTML = '<i class="fas fa-play"></i> Áp dụng';
        }
    }
}

// ===== Bulk Actions =====
function initializeBulkActions() {
    const applyButton = document.getElementById('applyBulkAction');
    
    if (applyButton) {
        applyButton.addEventListener('click', function() {
            const selectedAction = document.getElementById('bulkAction').value;
            const checkedCheckboxes = document.querySelectorAll('.review-checkbox:checked');
            
            if (!selectedAction) {
                showAlert('Vui lòng chọn một hành động!', 'warning');
                return;
            }
            
            if (checkedCheckboxes.length === 0) {
                showAlert('Vui lòng chọn ít nhất một đánh giá!', 'warning');
                return;
            }
            
            const selectedIds = Array.from(checkedCheckboxes).map(cb => cb.value);
            showBulkActionConfirmation(selectedAction, selectedIds);
        });
    }
}

function showBulkActionConfirmation(action, selectedIds) {
    const modal = document.getElementById('bulkActionModal');
    const messageElement = document.getElementById('bulkActionMessage');
    const confirmButton = document.getElementById('confirmBulkAction');
    
    if (!modal || !messageElement || !confirmButton) return;
    
    let message = '';
    let buttonClass = 'btn-primary';
    let buttonText = 'Xác nhận';
    
    switch (action) {
        case 'approve':
            message = `Bạn có chắc chắn muốn duyệt ${selectedIds.length} đánh giá đã chọn?`;
            buttonClass = 'btn-success';
            buttonText = 'Duyệt đánh giá';
            break;
        case 'reject':
            message = `Bạn có chắc chắn muốn từ chối ${selectedIds.length} đánh giá đã chọn?`;
            buttonClass = 'btn-warning';
            buttonText = 'Từ chối đánh giá';
            break;
        case 'delete':
            message = `Bạn có chắc chắn muốn xóa ${selectedIds.length} đánh giá đã chọn? Hành động này không thể hoàn tác!`;
            buttonClass = 'btn-danger';
            buttonText = 'Xóa đánh giá';
            break;
        default:
            return;
    }
    
    messageElement.textContent = message;
    confirmButton.className = `btn ${buttonClass}`;
    confirmButton.innerHTML = `<i class="fas fa-check"></i> ${buttonText}`;
    
    // Remove old event listeners
    const newConfirmButton = confirmButton.cloneNode(true);
    confirmButton.parentNode.replaceChild(newConfirmButton, confirmButton);
    
    // Add new event listener
    newConfirmButton.addEventListener('click', function() {
        executeBulkAction(action, selectedIds);
        bootstrap.Modal.getInstance(modal).hide();
    });
    
    const modalInstance = new bootstrap.Modal(modal);
    modalInstance.show();
}

function executeBulkAction(action, selectedIds) {
    const formData = new FormData();
    formData.append('action', action);
    selectedIds.forEach(id => formData.append('selectedIds', id));
    formData.append('__RequestVerificationToken', getAntiForgeryToken());
    
    showLoadingState(true);
    
    fetch('/Admin/Reviews/BulkAction', {
        method: 'POST',
        body: formData
    })
    .then(response => response.json())
    .then(data => {
        showLoadingState(false);
        
        if (data.success) {
            showAlert(data.message, 'success');
            setTimeout(() => {
                window.location.reload();
            }, 1500);
        } else {
            showAlert(data.message || 'Có lỗi xảy ra!', 'error');
        }
    })
    .catch(error => {
        showLoadingState(false);
        console.error('Error:', error);
        showAlert('Có lỗi xảy ra khi xử lý yêu cầu!', 'error');
    });
}

// ===== Delete Actions =====
function initializeDeleteButtons() {
    const deleteButtons = document.querySelectorAll('.delete-review');
    
    deleteButtons.forEach(button => {
        button.addEventListener('click', function() {
            const reviewId = this.getAttribute('data-review-id');
            const reviewUser = this.getAttribute('data-review-user');
            showDeleteConfirmation(reviewId, reviewUser);
        });
    });
}

function showDeleteConfirmation(reviewId, reviewUser) {
    const modal = document.getElementById('deleteModal');
    const userElement = document.getElementById('reviewUser');
    const confirmButton = document.getElementById('confirmDelete');
    
    if (!modal || !userElement || !confirmButton) return;
    
    userElement.textContent = reviewUser || 'người dùng này';
    
    // Remove old event listeners
    const newConfirmButton = confirmButton.cloneNode(true);
    confirmButton.parentNode.replaceChild(newConfirmButton, confirmButton);
    
    // Add new event listener
    newConfirmButton.addEventListener('click', function() {
        executeDelete(reviewId);
        bootstrap.Modal.getInstance(modal).hide();
    });
    
    const modalInstance = new bootstrap.Modal(modal);
    modalInstance.show();
}

function executeDelete(reviewId) {
    const formData = new FormData();
    formData.append('__RequestVerificationToken', getAntiForgeryToken());
    
    showLoadingState(true);
    
    fetch(`/Admin/Reviews/Delete/${reviewId}`, {
        method: 'POST',
        body: formData
    })
    .then(response => response.json())
    .then(data => {
        showLoadingState(false);
        
        if (data.success) {
            showAlert(data.message, 'success');
            setTimeout(() => {
                window.location.reload();
            }, 1500);
        } else {
            showAlert(data.message || 'Có lỗi xảy ra!', 'error');
        }
    })
    .catch(error => {
        showLoadingState(false);
        console.error('Error:', error);
        showAlert('Có lỗi xảy ra khi xóa đánh giá!', 'error');
    });
}

// ===== Quick Actions =====
function initializeQuickActions() {
    const approveButtons = document.querySelectorAll('.quick-approve');
    const rejectButtons = document.querySelectorAll('.quick-reject');
    
    approveButtons.forEach(button => {
        button.addEventListener('click', function() {
            const reviewId = this.getAttribute('data-review-id');
            showQuickActionConfirmation('approve', [reviewId]);
        });
    });
    
    rejectButtons.forEach(button => {
        button.addEventListener('click', function() {
            const reviewId = this.getAttribute('data-review-id');
            showQuickActionConfirmation('reject', [reviewId]);
        });
    });
}

function showQuickActionConfirmation(action, selectedIds) {
    const modal = document.getElementById('quickActionModal');
    const messageElement = document.getElementById('quickActionMessage');
    const confirmButton = document.getElementById('confirmQuickAction');
    
    if (!modal || !messageElement || !confirmButton) return;
    
    let message = '';
    let buttonClass = 'btn-primary';
    let buttonText = 'Xác nhận';
    
    switch (action) {
        case 'approve':
            message = 'Bạn có chắc chắn muốn duyệt đánh giá này?';
            buttonClass = 'btn-success';
            buttonText = 'Duyệt đánh giá';
            break;
        case 'reject':
            message = 'Bạn có chắc chắn muốn từ chối đánh giá này?';
            buttonClass = 'btn-warning';
            buttonText = 'Từ chối đánh giá';
            break;
        default:
            return;
    }
    
    messageElement.textContent = message;
    confirmButton.className = `btn ${buttonClass}`;
    confirmButton.innerHTML = `<i class="fas fa-check"></i> ${buttonText}`;
    
    // Remove old event listeners
    const newConfirmButton = confirmButton.cloneNode(true);
    confirmButton.parentNode.replaceChild(newConfirmButton, confirmButton);
    
    // Add new event listener
    newConfirmButton.addEventListener('click', function() {
        executeBulkAction(action, selectedIds);
        bootstrap.Modal.getInstance(modal).hide();
    });
    
    const modalInstance = new bootstrap.Modal(modal);
    modalInstance.show();
}

// ===== Utility Functions =====
function getAntiForgeryToken() {
    const token = document.querySelector('input[name="__RequestVerificationToken"]');
    return token ? token.value : '';
}

function showLoadingState(loading) {
    const buttons = document.querySelectorAll('.btn');
    
    if (loading) {
        buttons.forEach(btn => {
            if (!btn.hasAttribute('data-original-html')) {
                btn.setAttribute('data-original-html', btn.innerHTML);
            }
            btn.disabled = true;
            if (btn.classList.contains('btn-primary') || btn.classList.contains('btn-danger') || btn.classList.contains('btn-success')) {
                btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Đang xử lý...';
            }
        });
    } else {
        buttons.forEach(btn => {
            btn.disabled = false;
            const originalHtml = btn.getAttribute('data-original-html');
            if (originalHtml) {
                btn.innerHTML = originalHtml;
                btn.removeAttribute('data-original-html');
            }
        });
    }
}

function showAlert(message, type = 'info') {
    // Remove existing alerts
    const existingAlerts = document.querySelectorAll('.custom-alert');
    existingAlerts.forEach(alert => alert.remove());
    
    // Create new alert
    const alertDiv = document.createElement('div');
    alertDiv.className = `alert alert-${getBootstrapAlertType(type)} alert-dismissible fade show custom-alert`;
    alertDiv.style.position = 'fixed';
    alertDiv.style.top = '20px';
    alertDiv.style.right = '20px';
    alertDiv.style.zIndex = '9999';
    alertDiv.style.minWidth = '300px';
    alertDiv.style.maxWidth = '500px';
    
    const icon = getAlertIcon(type);
    
    alertDiv.innerHTML = `
        <div class="d-flex align-items-center">
            <i class="${icon} me-2"></i>
            <span>${message}</span>
        </div>
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;
    
    document.body.appendChild(alertDiv);
    
    // Auto-dismiss after 5 seconds
    setTimeout(() => {
        if (alertDiv && alertDiv.parentNode) {
            alertDiv.remove();
        }
    }, 5000);
}

function getBootstrapAlertType(type) {
    switch (type) {
        case 'success': return 'success';
        case 'error': return 'danger';
        case 'warning': return 'warning';
        default: return 'info';
    }
}

function getAlertIcon(type) {
    switch (type) {
        case 'success': return 'fas fa-check-circle';
        case 'error': return 'fas fa-exclamation-circle';
        case 'warning': return 'fas fa-exclamation-triangle';
        default: return 'fas fa-info-circle';
    }
}

function initializeTooltips() {
    // Initialize Bootstrap tooltips if available
    if (typeof bootstrap !== 'undefined' && bootstrap.Tooltip) {
        const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        tooltipTriggerList.map(function(tooltipTriggerEl) {
            return new bootstrap.Tooltip(tooltipTriggerEl);
        });
    }
}

function initializeModals() {
    // Ensure modals are properly initialized
    const modals = document.querySelectorAll('.modal');
    modals.forEach(modal => {
        modal.addEventListener('hidden.bs.modal', function() {
            // Reset modal content when closed
            const form = modal.querySelector('form');
            if (form) {
                form.reset();
            }
        });
    });
}

// ===== Table Sorting =====
function initializeTableSorting() {
    const sortableHeaders = document.querySelectorAll('.sortable');
    
    sortableHeaders.forEach(header => {
        header.style.cursor = 'pointer';
        header.addEventListener('click', function() {
            const sortLink = this.querySelector('a');
            if (sortLink) {
                window.location.href = sortLink.href;
            }
        });
    });
}

// ===== Auto-refresh for pending reviews =====
function startAutoRefresh() {
    const pendingCount = document.querySelector('.stat-card.pending h3');
    if (pendingCount && parseInt(pendingCount.textContent) > 0) {
        // Refresh every 30 seconds if there are pending reviews
        setInterval(() => {
            const currentUrl = new URL(window.location);
            if (!currentUrl.searchParams.get('statusFilter')) {
                // Only auto-refresh if no specific filter is applied
                window.location.reload();
            }
        }, 30000);
    }
}

// Initialize additional features
document.addEventListener('DOMContentLoaded', function() {
    initializeTableSorting();
    startAutoRefresh();
});