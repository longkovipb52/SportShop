// Product Attributes Management
// Quản lý thuộc tính sản phẩm trong trang Edit

let currentProductId = 0;
let currentAttributeId = 0;
let attributeModal = null;
let sizeOptions = [];
let colorOptions = [];
let currentCategoryId = 0;
let allAttributes = []; // Store all loaded attributes for stock calculation

// Initialize when document is ready
document.addEventListener('DOMContentLoaded', function() {
    // Get product ID from hidden input (try both lowercase and uppercase)
    const productIdInput = document.getElementById('productId') || document.getElementById('ProductID');
    if (productIdInput) {
        currentProductId = parseInt(productIdInput.value);
        console.log('Product ID found:', currentProductId);
        if (currentProductId > 0) {
            loadAttributes();
        } else {
            console.warn('Invalid product ID:', currentProductId);
        }
    } else {
        console.error('Product ID input not found');
    }
    
    // Get category ID
    const categorySelect = document.querySelector('select[name="CategoryID"]');
    if (categorySelect) {
        currentCategoryId = parseInt(categorySelect.value) || 0;
        // Load master data when category changes
        categorySelect.addEventListener('change', function() {
            currentCategoryId = parseInt(this.value) || 0;
            loadMasterData();
        });
    }
    
    // Initialize Bootstrap modal
    const modalElement = document.getElementById('attributeModal');
    if (modalElement) {
        attributeModal = new bootstrap.Modal(modalElement);
        // Load master data when modal is opened
        modalElement.addEventListener('show.bs.modal', function() {
            loadMasterData();
        });
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
    console.log('loadAttributes called for product ID:', currentProductId);
    
    const loadingElement = document.getElementById('attributesLoading');
    const listElement = document.getElementById('attributesList');
    const noDataElement = document.getElementById('noAttributes');
    
    console.log('Elements found:', { 
        loading: !!loadingElement, 
        list: !!listElement, 
        noData: !!noDataElement 
    });
    
    if (loadingElement) loadingElement.style.display = 'block';
    if (listElement) listElement.style.display = 'none';
    if (noDataElement) noDataElement.style.display = 'none';
    
    const url = `/Admin/Products/GetAttributes?productId=${currentProductId}`;
    console.log('Fetching from:', url);
    
    fetch(url)
        .then(response => {
            console.log('Response status:', response.status);
            return response.json();
        })
        .then(data => {
            console.log('Data received:', data);
            
            if (loadingElement) loadingElement.style.display = 'none';
            
            if (data.success && data.data && data.data.length > 0) {
                console.log('Displaying', data.data.length, 'attributes');
                allAttributes = data.data; // Store for stock calculation
                displayAttributes(data.data);
                if (listElement) listElement.style.display = 'block';
            } else {
                console.log('No attributes found or error');
                allAttributes = []; // Reset if no attributes
                if (noDataElement) noDataElement.style.display = 'block';
            }
        })
        .catch(error => {
            console.error('Error loading attributes:', error);
            if (loadingElement) loadingElement.style.display = 'none';
            if (noDataElement) {
                noDataElement.innerHTML = `
                    <i class="fas fa-exclamation-triangle text-danger"></i>
                    <p class="text-danger">Lỗi khi tải thuộc tính: ${error.message}</p>
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
    document.getElementById('sizeOptionId').value = '';
    document.getElementById('colorOptionId').value = '';
    
    // Reset dropdowns and inputs
    document.getElementById('attributeSizeSelect').value = '';
    document.getElementById('attributeColorSelect').value = '';
    document.getElementById('attributeSize').style.display = 'none';
    document.getElementById('attributeColor').style.display = 'none';
    document.getElementById('attributeSizeSelect').style.display = 'block';
    document.getElementById('attributeColorSelect').style.display = 'block';
    document.getElementById('colorPreview').style.display = 'none';
    
    // Set modal title
    document.getElementById('modalTitle').textContent = 'Thêm thuộc tính mới';
    
    // Update stock summary
    updateStockSummary();
    
    // Load master data
    loadMasterData();
    
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
                
                // Load master data first
                loadMasterData().then(() => {
                    // Fill form with data
                    document.getElementById('attributeId').value = attr.attributeID;
                    document.getElementById('attributeStock').value = attr.stock;
                    document.getElementById('attributePrice').value = attr.price || '';
                    document.getElementById('currentImageURL').value = attr.imageURL || '';
                    document.getElementById('sizeOptionId').value = attr.sizeOptionID || '';
                    document.getElementById('colorOptionId').value = attr.colorOptionID || '';
                    
                    // Set size - check if it exists in dropdown
                    const sizeSelect = document.getElementById('attributeSizeSelect');
                    const sizeInput = document.getElementById('attributeSize');
                    const sizeOption = Array.from(sizeSelect.options).find(opt => opt.value === attr.size);
                    
                    if (sizeOption) {
                        // Size exists in master data
                        sizeSelect.value = attr.size;
                        sizeInput.style.display = 'none';
                        sizeInput.value = attr.size;
                        sizeSelect.style.display = 'block';
                    } else {
                        // Custom size - show input
                        sizeSelect.style.display = 'none';
                        sizeInput.style.display = 'block';
                        sizeInput.value = attr.size;
                    }
                    
                    // Set color - check if it exists in dropdown
                    const colorSelect = document.getElementById('attributeColorSelect');
                    const colorInput = document.getElementById('attributeColor');
                    const colorOption = Array.from(colorSelect.options).find(opt => opt.value === attr.color);
                    
                    if (colorOption) {
                        // Color exists in master data
                        colorSelect.value = attr.color;
                        colorInput.style.display = 'none';
                        colorInput.value = attr.color;
                        colorSelect.style.display = 'block';
                        
                        // Show color preview
                        const hexCode = colorOption.dataset.hexCode;
                        if (hexCode) {
                            document.getElementById('colorBox').style.backgroundColor = hexCode;
                            document.getElementById('colorPreview').style.display = 'block';
                        }
                    } else {
                        // Custom color - show input
                        colorSelect.style.display = 'none';
                        colorInput.style.display = 'block';
                        colorInput.value = attr.color;
                    }
                    
                    // Show current image if exists
                    if (attr.imageURL) {
                        document.getElementById('attributePreviewImg').src = attr.imageURL;
                        document.getElementById('attributeImagePreview').style.display = 'block';
                    } else {
                        document.getElementById('attributeImagePreview').style.display = 'none';
                    }
                    
                    // Update modal title
                    document.getElementById('modalTitle').textContent = 'Chỉnh sửa thuộc tính';
                    
                    // Update stock summary (exclude current attribute being edited)
                    updateStockSummary(attributeId);
                    
                    // Show modal
                    if (attributeModal) {
                        attributeModal.show();
                    }
                });
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
    
    // Get size value from either select or input
    const sizeSelect = document.getElementById('attributeSizeSelect');
    const sizeInput = document.getElementById('attributeSize');
    const sizeValue = sizeInput.style.display !== 'none' ? sizeInput.value : sizeSelect.value;
    
    // Get color value from either select or input
    const colorSelect = document.getElementById('attributeColorSelect');
    const colorInput = document.getElementById('attributeColor');
    const colorValue = colorInput.style.display !== 'none' ? colorInput.value : colorSelect.value;
    
    // Validate
    if (!sizeValue || sizeValue === '__other__') {
        showAttributeToast('Vui lòng chọn hoặc nhập kích thước', 'error');
        return;
    }
    
    if (!colorValue || colorValue === '__other__') {
        showAttributeToast('Vui lòng chọn hoặc nhập màu sắc', 'error');
        return;
    }
    
    // Ensure hidden inputs have the actual values
    sizeInput.value = sizeValue;
    colorInput.value = colorValue;
    
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

// Update stock summary in modal
function updateStockSummary(excludeAttributeId = null) {
    const stockSummary = document.getElementById('stockSummary');
    const productTotalStockEl = document.getElementById('productTotalStock');
    const allocatedStockEl = document.getElementById('allocatedStock');
    const remainingStockEl = document.getElementById('remainingStock');
    const productTotalStockValue = parseInt(document.getElementById('productTotalStockValue').value) || 0;
    
    if (!stockSummary || !allAttributes) return;
    
    // Calculate allocated stock (excluding current attribute if editing)
    let allocatedStock = 0;
    allAttributes.forEach(attr => {
        if (excludeAttributeId === null || attr.attributeID !== excludeAttributeId) {
            allocatedStock += attr.stock || 0;
        }
    });
    
    const remainingStock = productTotalStockValue - allocatedStock;
    
    // Update display
    if (productTotalStockEl) productTotalStockEl.textContent = productTotalStockValue;
    if (allocatedStockEl) allocatedStockEl.textContent = allocatedStock;
    if (remainingStockEl) {
        remainingStockEl.textContent = remainingStock;
        
        // Change color based on remaining stock
        if (remainingStock < 0) {
            remainingStockEl.classList.remove('text-success', 'text-warning');
            remainingStockEl.classList.add('text-danger');
        } else if (remainingStock === 0) {
            remainingStockEl.classList.remove('text-success', 'text-danger');
            remainingStockEl.classList.add('text-warning');
        } else {
            remainingStockEl.classList.remove('text-warning', 'text-danger');
            remainingStockEl.classList.add('text-success');
        }
    }
    
    // Show the summary
    stockSummary.style.display = 'flex';
}

// ==========================================
// Master Data Functions (Size & Color Options)
// ==========================================

// Load master data from database
async function loadMasterData() {
    await Promise.all([
        loadSizeOptions(),
        loadColorOptions()
    ]);
}

// Load size options for current category
async function loadSizeOptions() {
    try {
        const url = currentCategoryId > 0 
            ? `/Admin/AttributeManagement/GetSizes?categoryId=${currentCategoryId}`
            : '/Admin/AttributeManagement/GetSizes';
        
        const response = await fetch(url);
        const result = await response.json();
        
        if (result.success) {
            sizeOptions = result.data || [];
            populateSizeDropdown();
        } else {
            console.error('Failed to load sizes:', result.message);
        }
    } catch (error) {
        console.error('Error loading sizes:', error);
    }
}

// Load color options
async function loadColorOptions() {
    try {
        const response = await fetch('/Admin/AttributeManagement/GetColors');
        const result = await response.json();
        
        if (result.success) {
            colorOptions = result.data || [];
            populateColorDropdown();
        } else {
            console.error('Failed to load colors:', result.message);
        }
    } catch (error) {
        console.error('Error loading colors:', error);
    }
}

// Populate size dropdown
function populateSizeDropdown() {
    const select = document.getElementById('attributeSizeSelect');
    if (!select) return;
    
    // Clear existing options except first
    select.innerHTML = '<option value="">-- Chọn kích thước --</option>';
    
    // Add options from master data
    sizeOptions.forEach(size => {
        const option = document.createElement('option');
        option.value = size.sizeName;
        option.textContent = size.sizeName;
        option.dataset.sizeOptionId = size.sizeOptionID;
        select.appendChild(option);
    });
    
    // Add "Other" option
    const otherOption = document.createElement('option');
    otherOption.value = '__other__';
    otherOption.textContent = '➕ Nhập kích thước khác...';
    select.appendChild(otherOption);
}

// Populate color dropdown
function populateColorDropdown() {
    const select = document.getElementById('attributeColorSelect');
    if (!select) return;
    
    // Clear existing options except first
    select.innerHTML = '<option value="">-- Chọn màu sắc --</option>';
    
    // Add options from master data
    colorOptions.forEach(color => {
        const option = document.createElement('option');
        option.value = color.colorName;
        option.textContent = color.colorName;
        option.dataset.colorOptionId = color.colorOptionID;
        option.dataset.hexCode = color.hexCode || '';
        select.appendChild(option);
    });
    
    // Add "Other" option
    const otherOption = document.createElement('option');
    otherOption.value = '__other__';
    otherOption.textContent = '➕ Nhập màu sắc khác...';
    select.appendChild(otherOption);
}

// Handle size selection change
function handleSizeChange(select) {
    const sizeInput = document.getElementById('attributeSize');
    const sizeOptionIdInput = document.getElementById('sizeOptionId');
    
    if (select.value === '__other__') {
        // Show input for custom size
        sizeInput.style.display = 'block';
        sizeInput.value = '';
        sizeInput.focus();
        sizeOptionIdInput.value = '';
    } else if (select.value) {
        // Use selected size from master data
        sizeInput.style.display = 'none';
        sizeInput.value = select.value;
        
        // Store SizeOptionID
        const selectedOption = select.options[select.selectedIndex];
        sizeOptionIdInput.value = selectedOption.dataset.sizeOptionId || '';
    } else {
        sizeInput.style.display = 'none';
        sizeInput.value = '';
        sizeOptionIdInput.value = '';
    }
}

// Handle color selection change
function handleColorChange(select) {
    const colorInput = document.getElementById('attributeColor');
    const colorOptionIdInput = document.getElementById('colorOptionId');
    const colorPreview = document.getElementById('colorPreview');
    const colorBox = document.getElementById('colorBox');
    
    if (select.value === '__other__') {
        // Show input for custom color
        colorInput.style.display = 'block';
        colorInput.value = '';
        colorInput.focus();
        colorOptionIdInput.value = '';
        colorPreview.style.display = 'none';
    } else if (select.value) {
        // Use selected color from master data
        colorInput.style.display = 'none';
        colorInput.value = select.value;
        
        // Store ColorOptionID
        const selectedOption = select.options[select.selectedIndex];
        colorOptionIdInput.value = selectedOption.dataset.colorOptionId || '';
        
        // Show color preview if hex code available
        const hexCode = selectedOption.dataset.hexCode;
        if (hexCode) {
            colorBox.style.backgroundColor = hexCode;
            colorPreview.style.display = 'block';
        } else {
            colorPreview.style.display = 'none';
        }
    } else {
        colorInput.style.display = 'none';
        colorInput.value = '';
        colorOptionIdInput.value = '';
        colorPreview.style.display = 'none';
    }
}

// Toggle between dropdown and text input for size
function toggleSizeInput() {
    const select = document.getElementById('attributeSizeSelect');
    const input = document.getElementById('attributeSize');
    
    if (input.style.display === 'none' || !input.style.display) {
        // Switch to text input mode
        select.style.display = 'none';
        input.style.display = 'block';
        input.focus();
        document.getElementById('sizeOptionId').value = '';
    } else {
        // Switch back to dropdown
        select.style.display = 'block';
        input.style.display = 'none';
        input.value = '';
        select.value = '';
    }
}

// Toggle between dropdown and text input for color
function toggleColorInput() {
    const select = document.getElementById('attributeColorSelect');
    const input = document.getElementById('attributeColor');
    
    if (input.style.display === 'none' || !input.style.display) {
        // Switch to text input mode
        select.style.display = 'none';
        input.style.display = 'block';
        input.focus();
        document.getElementById('colorOptionId').value = '';
        document.getElementById('colorPreview').style.display = 'none';
    } else {
        // Switch back to dropdown
        select.style.display = 'block';
        input.style.display = 'none';
        input.value = '';
        select.value = '';
    }
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
