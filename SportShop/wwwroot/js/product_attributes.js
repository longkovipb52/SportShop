// Product Attributes Management
// Quản lý thuộc tính sản phẩm trong trang Edit

let currentProductId = 0;
let currentAttributeId = 0;
let attributeModal = null;

// Initialize when document is ready
document.addEventListener('DOMContentLoaded', function() {
    // Get product ID from hidden input
    const productIdInput = document.getElementById('productId');
    if (productIdInput) {
        currentProductId = parseInt(productIdInput.value);
        loadAttributes();
    }
    
    // Initialize Bootstrap modal
    const modalElement = document.getElementById('attributeModal');
    if (modalElement) {
        attributeModal = new bootstrap.Modal(modalElement);
    }
    
    // Image preview for attribute
    const attributeImageInput = document.getElementById('attributeImage');
    if (attributeImageInput) {
        attributeImageInput.addEventListener('change', function(e) {
            const file = e.target.files[0];
            if (file) {
                const reader = new FileReader();
                reader.onload = function(e) {
                    document.getElementById('attributePreviewImg').src = e.target.result;
                    document.getElementById('attributeImagePreview').style.display = 'block';
                };
                reader.readAsDataURL(file);
            }
        });
    }
});

// Load all attributes for the product
function loadAttributes() {
    const loadingElement = document.getElementById('attributesLoading');
    const listElement = document.getElementById('attributesList');
    const noDataElement = document.getElementById('noAttributes');
    
    if (loadingElement) loadingElement.style.display = 'block';
    if (listElement) listElement.style.display = 'none';
    if (noDataElement) noDataElement.style.display = 'none';
    
    fetch(`/Admin/Products/GetAttributes?productId=${currentProductId}`)
        .then(response => response.json())
        .then(data => {
            if (loadingElement) loadingElement.style.display = 'none';
            
            if (data.success && data.data && data.data.length > 0) {
                displayAttributes(data.data);
                if (listElement) listElement.style.display = 'block';
            } else {
                if (noDataElement) noDataElement.style.display = 'block';
            }
        })
        .catch(error => {
            console.error('Error loading attributes:', error);
            if (loadingElement) loadingElement.style.display = 'none';
            if (noDataElement) {
                noDataElement.innerHTML = `
                    <i class="fas fa-exclamation-triangle text-danger"></i>
                    <p class="text-danger">Lỗi khi tải thuộc tính</p>
                `;
                noDataElement.style.display = 'block';
            }
        });
}

// Display attributes in the list
function displayAttributes(attributes) {
    const listElement = document.getElementById('attributesList');
    if (!listElement) return;
    
    let html = '<div class="attributes-grid">';
    
    attributes.forEach(attr => {
        const stockStatus = attr.stock > 10 ? 'in-stock' : (attr.stock > 0 ? 'low-stock' : 'out-of-stock');
        const stockBadge = attr.stock > 10 ? 
            '<span class="badge bg-success">Còn hàng</span>' :
            (attr.stock > 0 ? 
                '<span class="badge bg-warning text-dark">Sắp hết</span>' :
                '<span class="badge bg-danger">Hết hàng</span>');
        
        const imageUrl = attr.imageURL ? attr.imageURL : '/upload/product/no-image.svg';
        const priceDisplay = attr.price ? formatCurrency(attr.price) : '<span class="text-muted">Giá mặc định</span>';
        
        html += `
            <div class="attribute-card ${stockStatus}" data-attribute-id="${attr.attributeID}">
                <div class="attribute-image">
                    <img src="${imageUrl}" alt="${attr.size} - ${attr.color}" onerror="this.src='/upload/product/no-image.svg'" />
                </div>
                <div class="attribute-info">
                    <div class="attribute-header">
                        <h4>${attr.size} - ${attr.color}</h4>
                        ${stockBadge}
                    </div>
                    <div class="attribute-details">
                        <div class="detail-item">
                            <i class="fas fa-cube"></i>
                            <span>Tồn kho: <strong>${attr.stock}</strong></span>
                        </div>
                        <div class="detail-item">
                            <i class="fas fa-tag"></i>
                            <span>Giá: ${priceDisplay}</span>
                        </div>
                    </div>
                </div>
                <div class="attribute-actions">
                    <button type="button" class="btn btn-sm btn-primary" onclick="editAttribute(${attr.attributeID})" title="Chỉnh sửa">
                        <i class="fas fa-edit"></i>
                    </button>
                    <button type="button" class="btn btn-sm btn-danger" onclick="deleteAttribute(${attr.attributeID})" title="Xóa">
                        <i class="fas fa-trash"></i>
                    </button>
                </div>
            </div>
        `;
    });
    
    html += '</div>';
    listElement.innerHTML = html;
}

// Open modal to add new attribute
function openAttributeModal() {
    currentAttributeId = 0;
    
    // Reset form
    document.getElementById('attributeForm').reset();
    document.getElementById('attributeId').value = '0';
    document.getElementById('attributeImagePreview').style.display = 'none';
    document.getElementById('currentImageURL').value = '';
    
    // Update modal title
    document.getElementById('modalTitle').textContent = 'Thêm thuộc tính mới';
    
    // Show modal
    if (attributeModal) {
        attributeModal.show();
    }
}

// Edit existing attribute
function editAttribute(attributeId) {
    currentAttributeId = attributeId;
    
    // Fetch attribute data
    fetch(`/Admin/Products/GetAttribute?id=${attributeId}`)
        .then(response => response.json())
        .then(data => {
            if (data.success && data.data) {
                const attr = data.data;
                
                // Fill form with data
                document.getElementById('attributeId').value = attr.attributeID;
                document.getElementById('attributeSize').value = attr.size;
                document.getElementById('attributeColor').value = attr.color;
                document.getElementById('attributeStock').value = attr.stock;
                document.getElementById('attributePrice').value = attr.price || '';
                document.getElementById('currentImageURL').value = attr.imageURL || '';
                
                // Show current image if exists
                if (attr.imageURL) {
                    document.getElementById('attributePreviewImg').src = attr.imageURL;
                    document.getElementById('attributeImagePreview').style.display = 'block';
                } else {
                    document.getElementById('attributeImagePreview').style.display = 'none';
                }
                
                // Update modal title
                document.getElementById('modalTitle').textContent = 'Chỉnh sửa thuộc tính';
                
                // Show modal
                if (attributeModal) {
                    attributeModal.show();
                }
            } else {
                showAttributeToast(data.message || 'Lỗi khi tải thông tin thuộc tính', 'error');
            }
        })
        .catch(error => {
            console.error('Error:', error);
            showAttributeToast('Lỗi khi tải thông tin thuộc tính', 'error');
        });
}

// Save attribute (create or update)
function saveAttribute() {
    const form = document.getElementById('attributeForm');
    
    // Validate form
    if (!form.checkValidity()) {
        form.classList.add('was-validated');
        return;
    }
    
    const formData = new FormData(form);
    formData.append('__RequestVerificationToken', getAntiForgeryToken());
    
    const attributeId = parseInt(document.getElementById('attributeId').value);
    const url = attributeId > 0 ? '/Admin/Products/UpdateAttribute' : '/Admin/Products/CreateAttribute';
    
    // Show loading
    const submitBtn = event.target;
    const originalText = submitBtn.innerHTML;
    submitBtn.disabled = true;
    submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Đang lưu...';
    
    fetch(url, {
        method: 'POST',
        body: formData
    })
    .then(response => response.json())
    .then(data => {
        submitBtn.disabled = false;
        submitBtn.innerHTML = originalText;
        
        if (data.success) {
            showAttributeToast(data.message, 'success');
            
            // Hide modal
            if (attributeModal) {
                attributeModal.hide();
            }
            
            // Reload attributes list
            loadAttributes();
        } else {
            showAttributeToast(data.message || 'Có lỗi xảy ra', 'error');
        }
    })
    .catch(error => {
        console.error('Error:', error);
        submitBtn.disabled = false;
        submitBtn.innerHTML = originalText;
        showAttributeToast('Lỗi khi lưu thuộc tính', 'error');
    });
}

// Delete attribute
function deleteAttribute(attributeId) {
    if (!confirm('Bạn có chắc chắn muốn xóa thuộc tính này?\n\nHành động này không thể hoàn tác!')) {
        return;
    }
    
    const formData = new FormData();
    formData.append('__RequestVerificationToken', getAntiForgeryToken());
    
    fetch(`/Admin/Products/DeleteAttribute?id=${attributeId}`, {
        method: 'POST',
        body: formData
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            showAttributeToast(data.message, 'success');
            loadAttributes();
        } else {
            showAttributeToast(data.message || 'Có lỗi xảy ra', 'error');
        }
    })
    .catch(error => {
        console.error('Error:', error);
        showAttributeToast('Lỗi khi xóa thuộc tính', 'error');
    });
}

// Remove attribute image from preview
function removeAttributeImage() {
    document.getElementById('attributeImage').value = '';
    document.getElementById('attributeImagePreview').style.display = 'none';
    document.getElementById('currentImageURL').value = '';
}

// Format currency
function formatCurrency(amount) {
    return new Intl.NumberFormat('vi-VN', { 
        style: 'currency', 
        currency: 'VND' 
    }).format(amount);
}

// Get anti-forgery token
function getAntiForgeryToken() {
    const token = document.querySelector('input[name="__RequestVerificationToken"]');
    return token ? token.value : '';
}

// Show toast notification
function showAttributeToast(message, type = 'success') {
    // Try to use global toast function if it exists
    if (typeof showToast === 'function' && showToast.toString().indexOf('showAttributeToast') === -1) {
        showToast(message, type);
        return;
    }
    
    // Create a simple toast notification
    const toast = document.createElement('div');
    toast.className = `alert alert-${type === 'success' ? 'success' : 'danger'} position-fixed`;
    toast.style.cssText = 'top: 20px; right: 20px; z-index: 9999; min-width: 300px; animation: slideIn 0.3s ease-out;';
    toast.innerHTML = `
        <i class="fas fa-${type === 'success' ? 'check-circle' : 'exclamation-circle'}"></i>
        ${message}
    `;
    
    document.body.appendChild(toast);
    
    // Auto remove after 4 seconds
    setTimeout(() => {
        toast.style.animation = 'slideOut 0.3s ease-out';
        setTimeout(() => toast.remove(), 300);
    }, 4000);
}
