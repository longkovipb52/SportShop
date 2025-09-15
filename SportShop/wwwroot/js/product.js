document.addEventListener('DOMContentLoaded', function() {
    // Initialize product filters
    initProductFilters();
    
    // Initialize view switcher
    initViewSwitcher();
    
    // Initialize quick view
    initQuickView();
    
    // Initialize animations
    initAnimations();
    
    // Initialize add to cart
    initAddToCart();
    
    // Initialize wishlist
    initWishlist();
    
    // Initialize price range slider
    initPriceRange();
    
    // Initialize sidebar scroll
    initSidebarScroll();
    
    // Set initial filter values from URL params
    setInitialFilterValues();
});

// Hàm mới để đặt giá trị ban đầu từ URL
function setInitialFilterValues() {
    const urlParams = new URLSearchParams(window.location.search);
    
    // Set min price if in URL
    const minPrice = urlParams.get('minPrice');
    if (minPrice) {
        const minPriceInput = document.getElementById('min-price-input');
        if (minPriceInput) {
            minPriceInput.value = minPrice;
            
            // Update label
            const priceMinLabel = document.getElementById('price-min');
            if (priceMinLabel) {
                priceMinLabel.textContent = formatCurrency(minPrice);
            }
        }
    }
    
    // Set max price if in URL
    const maxPrice = urlParams.get('maxPrice');
    if (maxPrice) {
        const maxPriceInput = document.getElementById('max-price-input');
        if (maxPriceInput) {
            maxPriceInput.value = maxPrice;
            
            // Update label
            const priceMaxLabel = document.getElementById('price-max');
            if (priceMaxLabel) {
                priceMaxLabel.textContent = formatCurrency(maxPrice);
            }
        }
    }
    
    // Set price slider if both min and max are in URL
    if (minPrice && maxPrice && window.noUiSlider) {
        const priceRange = document.getElementById('price-range');
        if (priceRange && priceRange.noUiSlider) {
            priceRange.noUiSlider.set([minPrice, maxPrice]);
        }
    }

    

    
    // Khôi phục vị trí cuộn sau khi trang tải xong
    setTimeout(function() {
        const savedScrollPosition = localStorage.getItem('productListScrollPosition');
        if (savedScrollPosition) {
            window.scrollTo(0, parseInt(savedScrollPosition));
            localStorage.removeItem('productListScrollPosition');
        }
    }, 100);
}

// Product filters
function initProductFilters() {
    // Category filters
    const categoryCheckboxes = document.querySelectorAll('.category-checkbox');
    categoryCheckboxes.forEach(checkbox => {
        checkbox.addEventListener('change', function() {
            if (this.checked) {
                applyFilters();
            }
        });
    });



    // Brand filters
    const brandCheckboxes = document.querySelectorAll('.brand-checkbox');
    brandCheckboxes.forEach(checkbox => {
        checkbox.addEventListener('change', function() {
            if (this.checked) {
                applyFilters();
            }
        });
    });

    const ratingLabels = document.querySelectorAll('.form-check-label[for^="rating-"]');
    ratingLabels.forEach(label => {
        label.addEventListener('click', function() {
            // Lấy id của label
            const ratingId = this.getAttribute('for');
            if (ratingId) {
                // Tìm radio button tương ứng
                const radioButton = document.getElementById(ratingId);
                if (radioButton) {
                    radioButton.checked = true;
                    // Kích hoạt sự kiện thay đổi để áp dụng bộ lọc
                    radioButton.dispatchEvent(new Event('change'));
                }
            }
        });
    });
    // Rating filters
    const ratingOptions = document.querySelectorAll('input[name="rating"]');
    ratingOptions.forEach(option => {
        option.addEventListener('change', function() {
            applyFilters();
        });
    });

    // Sort by change
    const sortSelect = document.getElementById('sort-by');
    if (sortSelect) {
        sortSelect.addEventListener('change', function() {
            applyFilters();
        });
    }

    // Clear filters button
    const clearFiltersBtn = document.getElementById('clear-filters');
    if (clearFiltersBtn) {
        clearFiltersBtn.addEventListener('click', function() {
            window.location.href = '/Product';
        });
    }
}

// Apply filters function
function applyFilters() {
    // Lưu vị trí cuộn hiện tại
    const scrollPosition = window.scrollY;
    localStorage.setItem('productListScrollPosition', scrollPosition);
    
    // Get selected category
    const selectedCategory = document.querySelector('.category-checkbox:checked');
    const categoryId = selectedCategory ? selectedCategory.value : '';
    
    // Get selected brand
    const selectedBrand = document.querySelector('.brand-checkbox:checked');
    const brandId = selectedBrand ? selectedBrand.value : '';
    
    // Get sort order
    const sortSelect = document.getElementById('sort-by');
    const sortOrder = sortSelect ? sortSelect.value : '';
    
    // Get selected rating
    const selectedRating = document.querySelector('input[name="rating"]:checked');
    const ratingValue = selectedRating ? selectedRating.value : '';
    
    // Get price range values - Sửa phần này
    const minPriceInput = document.getElementById('min-price-input');
    const maxPriceInput = document.getElementById('max-price-input');
    
    // Đảm bảo rằng giá trị không rỗng và là số hợp lệ
    const minPrice = minPriceInput && minPriceInput.value.trim() !== '' ? parseInt(minPriceInput.value.replace(/\D/g,'')) : '';
    const maxPrice = maxPriceInput && maxPriceInput.value.trim() !== '' ? parseInt(maxPriceInput.value.replace(/\D/g,'')) : '';
    
    // Build URL
    let url = '/Product?';
    
    if (categoryId) {
        url += `categoryId=${categoryId}&`;
    }
    
    if (brandId) {
        url += `brandId=${brandId}&`;
    }
    
    if (sortOrder) {
        url += `sortOrder=${sortOrder}&`;
    }
    
    if (ratingValue) {
        url += `rating=${ratingValue}&`;
    }
    
    // Chỉ thêm tham số giá nếu có giá trị
    if (minPrice || minPrice === 0) {
        url += `minPrice=${minPrice}&`;
    }
    
    if (maxPrice) {
        url += `maxPrice=${maxPrice}&`;
    }
    
    // Remove trailing '&' if exists
    if (url.endsWith('&')) {
        url = url.slice(0, -1);
    }
    
    // Log the URL for debugging
    console.log("Filter URL:", url);
    
    // Navigate to filtered URL
    window.location.href = url;
}

// View switcher (Grid vs List)
function initViewSwitcher() {
    const gridViewBtn = document.getElementById('grid-view');
    const listViewBtn = document.getElementById('list-view');
    const productsContainer = document.getElementById('products-container');
    
    if (gridViewBtn && listViewBtn && productsContainer) {
        gridViewBtn.addEventListener('click', function() {
            // Remove list-view class and add grid-view class
            productsContainer.classList.remove('list-view');
            productsContainer.classList.add('grid-view');
            
            // Update button states
            gridViewBtn.classList.add('active');
            listViewBtn.classList.remove('active');
            
            // Save preference
            localStorage.setItem('product-view', 'grid');
            
            // Update container classes for proper grid layout
            productsContainer.className = 'row row-cols-1 row-cols-md-2 row-cols-lg-3 g-4 grid-view';
        });
        
        listViewBtn.addEventListener('click', function() {
            // Remove grid-view class and add list-view class
            productsContainer.classList.remove('grid-view');
            productsContainer.classList.add('list-view');
            
            // Update button states
            listViewBtn.classList.add('active');
            gridViewBtn.classList.remove('active');
            
            // Save preference
            localStorage.setItem('product-view', 'list');
            
            // Update container classes for list layout
            productsContainer.className = 'row g-3 list-view';
        });
        
        // Apply saved view preference if exists
        const savedView = localStorage.getItem('product-view');
        if (savedView === 'list') {
            // Set list view
            productsContainer.classList.remove('grid-view');
            productsContainer.classList.add('list-view');
            productsContainer.className = 'row g-3 list-view';
            listViewBtn.classList.add('active');
            gridViewBtn.classList.remove('active');
        } else {
            // Set grid view (default)
            productsContainer.classList.remove('list-view');
            productsContainer.classList.add('grid-view');
            productsContainer.className = 'row row-cols-1 row-cols-md-2 row-cols-lg-3 g-4 grid-view';
            gridViewBtn.classList.add('active');
            listViewBtn.classList.remove('active');
        }
    }
}

// Quick view product modal
function initQuickView() {
    const quickViewButtons = document.querySelectorAll('.quick-view');
    
    quickViewButtons.forEach(button => {
        button.addEventListener('click', function(e) {
            e.preventDefault();
            e.stopPropagation();
            
            const productId = this.getAttribute('data-id');
            
            // Hiển thị trạng thái loading
            showQuickViewLoading();
            
            // Thay đổi URL để gọi đến action GetProductJson trong ProductController
            fetch(`/Product/GetProductJson?id=${productId}`)
                .then(response => {
                    if (!response.ok) {
                        throw new Error('Network response was not ok');
                    }
                    return response.json();
                })
                .then(data => {
                    console.log("Product data:", data); // Log để debug
                    populateQuickView(data);
                    
                    // Hiển thị modal
                    const quickViewModal = new bootstrap.Modal(document.getElementById('quickViewModal'));
                    quickViewModal.show();
                })
                .catch(error => {
                    console.error('Error fetching product data:', error);
                    showNotification('Không thể tải thông tin sản phẩm. Vui lòng thử lại sau.', 'error');
                });
        });
    });
}

// Show loading state in quick view modal
function showQuickViewLoading() {
    document.getElementById('quick-view-image').src = '/image/loading-placeholder.png';
    document.getElementById('quick-view-title').textContent = 'Đang tải...';
    document.getElementById('quick-view-price').textContent = '';
    document.getElementById('quick-view-description').textContent = 'Đang tải thông tin sản phẩm...';
    document.getElementById('quick-view-colors').innerHTML = '';
    document.getElementById('quick-view-sizes').innerHTML = '';
    document.getElementById('quick-view-category').textContent = '';
    document.getElementById('quick-view-brand').textContent = '';
}


function populateQuickView(product) {
    // Product image - Sử dụng đường dẫn đúng
    const imageElement = document.getElementById('quick-view-image');
    if (imageElement) {
        imageElement.src = product.imageURL 
            ? `/upload/product/${product.imageURL}` 
            : '/images/product-placeholder.jpg';
    }
    
    // Product title
    const titleElement = document.getElementById('quick-view-title');
    if (titleElement) {
        titleElement.textContent = product.name;
    }
    
    // Product price
    const priceElement = document.getElementById('quick-view-price');
    if (priceElement) {
        priceElement.textContent = new Intl.NumberFormat('vi-VN').format(product.price) + 'đ';
    }
    
    // Product description
    const descriptionElement = document.getElementById('quick-view-description');
    if (descriptionElement) {
        // Giới hạn độ dài mô tả
        const maxLength = 150;
        descriptionElement.textContent = product.description && product.description.length > maxLength 
            ? product.description.substring(0, maxLength) + '...' 
            : product.description || 'Không có mô tả';
    }
    
    // Category and brand
    const categoryElement = document.getElementById('quick-view-category');
    if (categoryElement) {
        categoryElement.textContent = product.categoryName;
    }
    
    const brandElement = document.getElementById('quick-view-brand');
    if (brandElement) {
        brandElement.textContent = product.brandName;
    }
    
    // Rating information if available
    const ratingElement = document.getElementById('quick-view-rating');
    if (ratingElement) {
        ratingElement.innerHTML = '';
        
        if (product.averageRating > 0) {
            // Create star rating based on actual average rating
            const starCount = 5;
            for (let i = 1; i <= starCount; i++) {
                const star = document.createElement('i');
                if (i <= Math.floor(product.averageRating)) {
                    star.className = 'fas fa-star text-warning';
                } else if (i - product.averageRating < 1 && i - product.averageRating > 0) {
                    star.className = 'fas fa-star-half-alt text-warning';
                } else {
                    star.className = 'far fa-star text-warning';
                }
                ratingElement.appendChild(star);
            }
            
            // Add rating text
            const ratingText = document.createElement('span');
            ratingText.className = 'ms-2';
            ratingText.textContent = `${product.averageRating.toFixed(1)} (${product.reviewCount} đánh giá)`;
            ratingElement.appendChild(ratingText);
        } else {
            // Show "No ratings yet"
            const noRatingText = document.createElement('span');
            noRatingText.textContent = 'Chưa có đánh giá';
            ratingElement.appendChild(noRatingText);
        }
    }
    
    // Lưu trữ map của thuộc tính vào một biến toàn cục để có thể tham chiếu sau này
    window.productAttributes = product.attributes || [];
    
    // Colors and sizes are populated if product has attributes
    const colorsContainer = document.getElementById('quick-view-colors');
    const sizesContainer = document.getElementById('quick-view-sizes');
    
    if (colorsContainer) colorsContainer.innerHTML = '';
    if (sizesContainer) sizesContainer.innerHTML = '';
    
    if (product.attributes && product.attributes.length > 0) {
        // Extract unique colors and sizes
        const uniqueColors = [...new Set(product.attributes
            .filter(attr => attr.color)
            .map(attr => attr.color))];
            
        const uniqueSizes = [...new Set(product.attributes
            .filter(attr => attr.size)
            .map(attr => attr.size))];
        
        // Populate colors
        if (colorsContainer) {
            uniqueColors.forEach(color => {
                const colorBtn = document.createElement('button');
                colorBtn.type = 'button';
                colorBtn.className = 'btn btn-outline-secondary color-option me-2 mb-2';
                colorBtn.textContent = color;
                colorBtn.setAttribute('data-color', color);
                colorBtn.addEventListener('click', function() {
                    document.querySelectorAll('.color-option').forEach(btn => btn.classList.remove('active'));
                    this.classList.add('active');
                    
                    // Khi màu thay đổi, cập nhật attributeId
                    updateSelectedAttribute();
                });
                colorsContainer.appendChild(colorBtn);
            });
        }
        
        // Populate sizes
        if (sizesContainer) {
            uniqueSizes.forEach(size => {
                const sizeBtn = document.createElement('button');
                sizeBtn.type = 'button';
                sizeBtn.className = 'btn btn-outline-secondary size-option me-2 mb-2';
                sizeBtn.textContent = size;
                sizeBtn.setAttribute('data-size', size);
                sizeBtn.addEventListener('click', function() {
                    document.querySelectorAll('.size-option').forEach(btn => btn.classList.remove('active'));
                    this.classList.add('active');
                    
                    // Khi kích thước thay đổi, cập nhật attributeId
                    updateSelectedAttribute();
                });
                sizesContainer.appendChild(sizeBtn);
            });
        }
    }
    
    // Set product ID for add to cart button
    const quickViewAddToCartBtn = document.getElementById('quick-view-add-to-cart');
    if (quickViewAddToCartBtn) {
        quickViewAddToCartBtn.setAttribute('data-product-id', product.productID);
    }
    
    // Thêm hàm updateSelectedAttribute để cập nhật attributeId khi chọn màu và kích thước
    function updateSelectedAttribute() {
        const selectedColor = document.querySelector('.color-option.active');
        const selectedSize = document.querySelector('.size-option.active');
        
        const color = selectedColor ? selectedColor.getAttribute('data-color') : null;
        const size = selectedSize ? selectedSize.getAttribute('data-size') : null;
        
        console.log("Selected color:", color);
        console.log("Selected size:", size);
        
        // Tìm thuộc tính phù hợp
        let attribute = null;
        
        if (color && size) {
            // Tìm thuộc tính khớp cả màu và kích thước
            attribute = window.productAttributes.find(attr => 
                attr.color === color && attr.size === size
            );
        } else if (color) {
            // Chỉ tìm theo màu
            attribute = window.productAttributes.find(attr => 
                attr.color === color
            );
        } else if (size) {
            // Chỉ tìm theo kích thước
            attribute = window.productAttributes.find(attr => 
                attr.size === size
            );
        }
        
        console.log("Found attribute:", attribute);
        
        // Cập nhật attribute ID cho nút thêm vào giỏ hàng
        const addToCartButton = document.getElementById('quick-view-add-to-cart');
        if (addToCartButton) {
            if (attribute) {
                addToCartButton.setAttribute('data-attribute-id', attribute.attributeID);
                console.log("Set attribute ID:", attribute.attributeID);
            } else {
                addToCartButton.removeAttribute('data-attribute-id');
                console.log("Removed attribute ID");
            }
        }
    }
}

// Add to cart functionality
function initAddToCart() {
    // Add to cart from product list
    const addToCartButtons = document.querySelectorAll('.add-to-cart');
    
    addToCartButtons.forEach(button => {
        button.addEventListener('click', function(e) {
            e.preventDefault();
            e.stopPropagation();
            
            const productId = parseInt(this.getAttribute('data-id'), 10);
            console.log("Adding to cart from product list:", productId);
            
            // Show loading state
            this.innerHTML = '<i class="fas fa-spinner fa-spin"></i>';
            this.disabled = true;
            
            // Thử với application/x-www-form-urlencoded thay vì FormData
            const params = new URLSearchParams();
            params.append('productId', productId);
            params.append('quantity', 1);
            
            fetch('/Cart/AddToCart', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                },
                body: params
            })
            .then(response => {
                if (!response.ok) {
                    throw new Error(`HTTP error! Status: ${response.status}`);
                }
                return response.json();
            })
            .then(data => {
                console.log("Response:", data);
                if (data.success) {
                    showNotification(data.message, 'success');
                    updateCartCounter(data.cartCount);
                } else {
                    // Kiểm tra nếu cần đăng nhập
                    console.log("Checking requireLogin:", data.requireLogin);
                    if (data.requireLogin) {
                        console.log("Login required, showing notification and redirecting...");
                        showNotification(data.message, 'warning');
                        // Chuyển hướng đến trang đăng nhập sau 2 giây
                        setTimeout(() => {
                            console.log("Redirecting to login page...");
                            window.location.href = '/Account/Login?returnUrl=' + encodeURIComponent(window.location.pathname);
                        }, 2000);
                    } else {
                        console.log("Other error:", data.message);
                        showNotification(data.message, 'error');
                    }
                }
            })
            .catch(error => {
                console.error("Error:", error);
                showNotification("Có lỗi xảy ra khi thêm vào giỏ hàng", "error");
            })
            .finally(() => {
                // Reset button state
                setTimeout(() => {
                    this.innerHTML = '<i class="fas fa-shopping-cart"></i>';
                    this.disabled = false;
                }, 500);
            });
        });
    });
    
    // Add to cart from quick view modal
    const quickViewAddToCartBtn = document.getElementById('quick-view-add-to-cart');
    if (quickViewAddToCartBtn) {
        quickViewAddToCartBtn.addEventListener('click', function() {
            // Lấy productId từ data attribute
            const productId = parseInt(this.getAttribute('data-product-id'), 10);
            // Lấy attributeId từ data attribute
            const attributeId = this.hasAttribute('data-attribute-id') ? 
                                parseInt(this.getAttribute('data-attribute-id'), 10) : null;
            // Lấy số lượng
            const quantity = parseInt(document.getElementById('product-quantity')?.value || '1', 10);
            
            console.log("Quick view add to cart:", { productId, quantity, attributeId });
            
            // Kiểm tra xem đã chọn thuộc tính chưa (nếu cần)
            const hasColors = document.querySelector('.color-option') !== null;
            const hasSizes = document.querySelector('.size-option') !== null;
            
            if ((hasColors || hasSizes) && attributeId === null) {
                showNotification('Vui lòng chọn màu sắc và kích thước', 'warning');
                return;
            }
            
            // Kiểm tra giá trị
            if (isNaN(productId) || productId <= 0) {
                console.error("Invalid productId:", productId);
                showNotification('ID sản phẩm không hợp lệ', 'error');
                return;
            }
            
            // Show loading state
            this.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Đang thêm...';
            this.disabled = true;
            
            // Add to cart
            addToCart(productId, quantity, attributeId, this);
        });
    }
}

// Initialize wishlist functionality
function initWishlist() {
    const wishlistButtons = document.querySelectorAll('.add-to-wishlist');
    const checkedProductIds = new Set(); // Tránh check duplicate cho cùng productId
    
    wishlistButtons.forEach(button => {
        const productId = parseInt(button.getAttribute('data-id'), 10);
        
        // Check if product is already in wishlist (chỉ check 1 lần cho mỗi productId)
        if (!checkedProductIds.has(productId)) {
            checkWishlistStatusForAllButtons(productId);
            checkedProductIds.add(productId);
        }
        
        button.addEventListener('click', function(e) {
            e.preventDefault();
            e.stopPropagation();
            
            // Show loading state cho tất cả button cùng productId
            const allButtons = document.querySelectorAll(`[data-id="${productId}"].add-to-wishlist`);
            allButtons.forEach(btn => {
                const icon = btn.querySelector('i');
                icon.className = 'fas fa-spinner fa-spin';
                btn.disabled = true;
            });
            
            toggleWishlist(productId);
            
            // Button state sẽ được reset trong response của toggleWishlist
        });
    });
}

// Check wishlist status for all buttons of a product
function checkWishlistStatusForAllButtons(productId) {
    fetch(`/Wishlist/Check?productId=${productId}`)
    .then(response => response.json())
    .then(data => {
        // Update ALL buttons with this productId
        const allButtons = document.querySelectorAll(`[data-id="${productId}"].add-to-wishlist`);
        allButtons.forEach(button => {
            const icon = button.querySelector('i');
            if (data.inWishlist) {
                // Trong wishlist: solid heart màu đỏ
                icon.classList.remove('far', 'fa-heart-o');
                icon.classList.add('fas', 'fa-heart', 'text-danger');
            } else {
                // Không trong wishlist: outline heart không màu
                icon.classList.remove('fas', 'fa-heart', 'text-danger');
                icon.classList.add('far', 'fa-heart');
            }
        });
    })
    .catch(error => {
        console.error('Error checking wishlist status:', error);
    });
}

// Check wishlist status for a product (legacy function for compatibility)
function checkWishlistStatus(productId, button) {
    fetch(`/Wishlist/Check?productId=${productId}`)
    .then(response => response.json())
    .then(data => {
        const icon = button.querySelector('i');
        if (data.inWishlist) {
            // Trong wishlist: solid heart màu đỏ
            icon.classList.remove('far', 'fa-heart-o');
            icon.classList.add('fas', 'fa-heart', 'text-danger');
        } else {
            // Không trong wishlist: outline heart không màu
            icon.classList.remove('fas', 'fa-heart', 'text-danger');
            icon.classList.add('far', 'fa-heart');
        }
    })
    .catch(error => {
        console.error('Error checking wishlist status:', error);
    });
}

// Cập nhật hàm addToCart để chuyển đổi productId thành số
function addToCart(productId, quantity, attributeId = null, buttonElement = null) {
    console.log("Adding to cart:", { productId, quantity, attributeId });
    
    // Đảm bảo các tham số là kiểu dữ liệu đúng
    productId = parseInt(productId, 10);
    quantity = parseInt(quantity, 10);
    if (attributeId !== null && attributeId !== undefined) {
        attributeId = parseInt(attributeId, 10);
    }
    
    // Kiểm tra giá trị
    if (isNaN(productId) || productId <= 0) {
        console.error("Invalid productId:", productId);
        showNotification('ID sản phẩm không hợp lệ', 'error');
        return;
    }
    
    // Show loading state
    if (buttonElement) {
        buttonElement.innerHTML = '<i class="fas fa-spinner fa-spin"></i>';
        buttonElement.disabled = true;
    }
    
    // Tạo object request
    const requestData = {
        productId: productId,
        quantity: quantity,
        attributeId: attributeId
    };
    
    console.log("Request URL:", '/Cart/AddToCartJson');
    console.log("Request data:", requestData);
    console.log("Request JSON:", JSON.stringify(requestData));
    
    // Gửi request đến endpoint
    fetch('/Cart/AddToCartJson', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Accept': 'application/json'
        },
        body: JSON.stringify({
            productId: productId,
            quantity: quantity,
            attributeId: attributeId
        })
    })
    .then(response => {
        console.log("Response status:", response.status);
        
        // Kiểm tra response status
        if (!response.ok) {
            throw new Error(`HTTP error! Status: ${response.status}`);
        }
        
        return response.json().catch(error => {
            console.error("Error parsing JSON:", error);
            return { success: false, message: "Lỗi phân tích phản hồi từ server" };
        });
    })
    .then(data => {
        console.log("Response data:", data);
        
        if (data.success) {
            // Show success message
            showNotification(data.message, 'success');
            
            // Update cart counter
            updateCartCounter(data.cartCount);
            
            // Close modal if open
            const quickViewModal = document.getElementById('quickViewModal');
            if (quickViewModal && bootstrap.Modal.getInstance(quickViewModal)) {
                bootstrap.Modal.getInstance(quickViewModal).hide();
            }
        } else {
            // Kiểm tra nếu cần đăng nhập
            console.log("Checking requireLogin:", data.requireLogin);
            if (data.requireLogin) {
                console.log("Login required, showing notification and redirecting...");
                showNotification(data.message, 'warning');
                // Chuyển hướng đến trang đăng nhập sau 2 giây
                setTimeout(() => {
                    console.log("Redirecting to login page...");
                    window.location.href = '/Account/Login?returnUrl=' + encodeURIComponent(window.location.pathname);
                }, 2000);
            } else {
                // Use common handler for other errors
                if (typeof handleAjaxResponse === 'function') {
                    handleAjaxResponse(data);
                } else {
                    console.log("Other error:", data.message);
                    showNotification(data.message, 'error');
                }
            }
        }
    })
    .catch(error => {
        console.error('Error adding to cart:', error);
        showNotification('Có lỗi xảy ra khi thêm sản phẩm vào giỏ hàng', 'error');
    })
    .finally(() => {
        // Reset button state
        if (buttonElement) {
            setTimeout(() => {
                buttonElement.innerHTML = '<i class="fas fa-shopping-cart"></i>';
                buttonElement.disabled = false;
            }, 500);
        }
    });
}

// Update cart counter in header
function updateCartCounter(count) {
    const cartCountElement = document.querySelector('.cart-count');
    if (cartCountElement) {
        cartCountElement.textContent = count || '0';
        
        // Animation effect
        cartCountElement.classList.add('pulse');
        setTimeout(() => {
            cartCountElement.classList.remove('pulse');
        }, 1000);
    }
}

// Initialize price range slider
function initPriceRange() {
    const priceRange = document.getElementById('price-range');
    const minPriceInput = document.getElementById('min-price-input');
    const maxPriceInput = document.getElementById('max-price-input');
    const priceMinLabel = document.getElementById('price-min');
    const priceMaxLabel = document.getElementById('price-max');
    const applyPriceBtn = document.getElementById('apply-price-filter');
    
    if (priceRange && minPriceInput && maxPriceInput && priceMinLabel && priceMaxLabel) {
        // Hiển thị giá trị mặc định
        if (!minPriceInput.value) minPriceInput.value = "0";
        if (!maxPriceInput.value) maxPriceInput.value = "5000000";
        
        priceMinLabel.textContent = formatCurrency(minPriceInput.value);
        priceMaxLabel.textContent = formatCurrency(maxPriceInput.value);
        
        // Initialize noUiSlider if available, otherwise use native range input
        if (window.noUiSlider) {
            noUiSlider.create(priceRange, {
                start: [
                    parseInt(minPriceInput.value) || 0, 
                    parseInt(maxPriceInput.value) || 5000000
                ],
                connect: true,
                step: 100000,
                range: {
                    'min': 0,
                    'max': 5000000
                },
                format: {
                    to: value => Math.round(value),
                    from: value => Math.round(value)
                }
            });
            
            priceRange.noUiSlider.on('update', function(values, handle) {
                const value = values[handle];
                if (handle === 0) {
                    minPriceInput.value = value;
                    priceMinLabel.textContent = formatCurrency(value);
                } else {
                    maxPriceInput.value = value;
                    priceMaxLabel.textContent = formatCurrency(value);
                }
            });
            
            minPriceInput.addEventListener('change', function() {
                priceRange.noUiSlider.set([this.value, null]);
            });
            
            maxPriceInput.addEventListener('change', function() {
                priceRange.noUiSlider.set([null, this.value]);
            });
        } else {
            // Fallback to standard range input
            priceRange.addEventListener('input', function() {
                const value = this.value;
                maxPriceInput.value = value;
                priceMaxLabel.textContent = formatCurrency(value);
            });
            
            // Manual input event handlers
            minPriceInput.addEventListener('input', function() {
                priceMinLabel.textContent = formatCurrency(this.value);
            });
            
            maxPriceInput.addEventListener('input', function() {
                priceMaxLabel.textContent = formatCurrency(this.value);
            });
        }
        
        // Apply price filter button
        if (applyPriceBtn) {
            applyPriceBtn.addEventListener('click', function() {
                console.log("Applying price filter:", minPriceInput.value, maxPriceInput.value);
                applyFilters();
            });
        }
    }
}

// Initialize animations
function initAnimations() {
    // Product card hover effects
    const productCards = document.querySelectorAll('.product-card');
    
    productCards.forEach(card => {
        card.addEventListener('mouseenter', function() {
            this.querySelector('.product-actions')?.classList.add('show');
        });
        
        card.addEventListener('mouseleave', function() {
            this.querySelector('.product-actions')?.classList.remove('show');
        });
    });
}

// Format currency to VND
function formatCurrency(value) {
    return new Intl.NumberFormat('vi-VN').format(value) + 'đ';
}

// Toggle wishlist API call
function toggleWishlist(productId) {
    console.log('toggleWishlist called for productId:', productId);
    
    fetch('/Wishlist/Toggle', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
        },
        body: `productId=${productId}`
    })
    .then(response => response.json())
    .then(data => {
        console.log('Wishlist toggle response:', data);
        
        if (data.success) {
            // Show appropriate notification based on action
            if (data.action === 'added') {
                showNotification('Đã thêm sản phẩm vào yêu thích!', 'success');
            } else {
                showNotification('Đã xóa sản phẩm khỏi yêu thích!', 'success');
            }
            
            // Update ALL heart icons for this product (use querySelectorAll instead of querySelector)
            const heartButtons = document.querySelectorAll(`[data-id="${productId}"].add-to-wishlist`);
            console.log('Found heart buttons:', heartButtons.length);
            
            heartButtons.forEach(heartButton => {
                const icon = heartButton.querySelector('i');
                console.log('Before update - icon classes:', icon.className);
                
                // Clear loading state first
                icon.classList.remove('fa-spinner', 'fa-spin');
                heartButton.disabled = false;
                
                if (data.action === 'added') {
                    // Thêm vào yêu thích: solid heart màu đỏ
                    icon.className = 'fas fa-heart text-danger';
                } else {
                    // Xóa khỏi yêu thích: outline heart không màu
                    icon.className = 'far fa-heart';
                }
                
                console.log('After update - icon classes:', icon.className);
            });
            
            // Update wishlist count in dropdown
            updateWishlistCount();
        } else {
            // Clear loading state for all buttons on error
            const heartButtons = document.querySelectorAll(`[data-id="${productId}"].add-to-wishlist`);
            heartButtons.forEach(heartButton => {
                const icon = heartButton.querySelector('i');
                icon.classList.remove('fa-spinner', 'fa-spin');
                heartButton.disabled = false;
            });
            
            showNotification(data.message, 'error');
            
            if (data.requireLogin) {
                setTimeout(() => {
                    window.location.href = '/Account/Login';
                }, 2000);
            }
        }
    })
    .catch(error => {
        console.error('Error toggling wishlist:', error);
        
        // Clear loading state for all buttons on error
        const heartButtons = document.querySelectorAll(`[data-id="${productId}"].add-to-wishlist`);
        heartButtons.forEach(heartButton => {
            const icon = heartButton.querySelector('i');
            icon.classList.remove('fa-spinner', 'fa-spin');
            heartButton.disabled = false;
            // Restore original heart icon
            icon.classList.add('far', 'fa-heart');
        });
        
        showNotification('Có lỗi xảy ra. Vui lòng thử lại.', 'error');
    });
}

// Show notification
function showNotification(message, type = 'info') {
    // Check if Toastify exists
    if (window.Toastify) {
        Toastify({
            text: message,
            duration: 3000,
            gravity: "bottom",
            position: "right",
            className: `toast-${type}`,
            stopOnFocus: true,
            style: {
                background: type === 'success' ? '#28a745' : 
                           type === 'error' ? '#dc3545' : 
                           type === 'warning' ? '#ffc107' : '#17a2b8'
            }
        }).showToast();
    } else {
        // Fallback to standard alert if Toastify isn't available
        const notificationDiv = document.createElement('div');
        notificationDiv.classList.add('notification', `notification-${type}`);
        notificationDiv.textContent = message;
        
        document.body.appendChild(notificationDiv);
        
        setTimeout(() => {
            notificationDiv.classList.add('show');
        }, 10);
        
        setTimeout(() => {
            notificationDiv.classList.remove('show');
            setTimeout(() => {
                document.body.removeChild(notificationDiv);
            }, 300);
        }, 3000);
    }
}

// Thêm hàm mới để xử lý cuộn sidebar
function initSidebarScroll() {
    const sidebarWrapper = document.querySelector('.filter-sidebar-wrapper');
    
    if (sidebarWrapper) {
        // Xử lý sự kiện cuộn để thêm class khi cần
        sidebarWrapper.addEventListener('scroll', function() {
            if (this.scrollTop + this.clientHeight < this.scrollHeight) {
                this.classList.add('scrolled');
            } else {
                this.classList.remove('scrolled');
            }
        });
        
        // Điều chỉnh chiều cao sidebar khi kích thước cửa sổ thay đổi
        function adjustSidebarHeight() {
            const header = document.querySelector('header');
            const headerHeight = header ? header.offsetHeight : 80;
            
            sidebarWrapper.style.top = (headerHeight + 20) + 'px';
            sidebarWrapper.style.maxHeight = `calc(100vh - ${headerHeight + 40}px)`;
        }
        
        // Gọi hàm khi trang tải và khi resize cửa sổ
        adjustSidebarHeight();
        window.addEventListener('resize', adjustSidebarHeight);
        
        // Đặt lại vị trí sidebar khi tải lại trang
        window.addEventListener('beforeunload', function() {
            sidebarWrapper.scrollTop = 0;
        });

        function checkStickyState() {
            const rect = sidebarWrapper.getBoundingClientRect();
            const header = document.querySelector('header');
            const headerHeight = header ? header.offsetHeight : 80;
            
            if (rect.top <= headerHeight + 20) {
                sidebarWrapper.classList.add('sticked');
            } else {
                sidebarWrapper.classList.remove('sticked');
            }
        }
        
        // Gọi khi cuộn trang
        window.addEventListener('scroll', checkStickyState);
        // Kiểm tra ban đầu
        checkStickyState();
    }
}

// Update wishlist count in dropdown
function updateWishlistCount() {
    fetch('/Wishlist/Count')
        .then(response => response.json())
        .then(data => {
            // Update count in dropdown
            const wishlistCountElement = document.querySelector('.wishlist-count');
            if (wishlistCountElement) {
                wishlistCountElement.textContent = data.count || '0';
            }
        })
        .catch(error => {
            console.error('Error updating wishlist count:', error);
        });
}
