document.addEventListener('DOMContentLoaded', function() {
    // Initialize thumbnail gallery
    initThumbnailGallery();
    
    // Initialize quantity selector
    initQuantitySelector();
    
    // Initialize color and size selectors
    initAttributeSelectors();
    
    // Initialize add to cart functionality
    initAddToCart();
    
    // Initialize wishlist functionality
    initWishlist();
    
    // Initialize buy now functionality
    initBuyNow();
});

// Product thumbnail gallery
function initThumbnailGallery() {
    const thumbnailItems = document.querySelectorAll('.thumbnail-item');
    const mainImage = document.getElementById('main-product-image');
    
    if (thumbnailItems.length > 0 && mainImage) {
        thumbnailItems.forEach(item => {
            item.addEventListener('click', function() {
                try {
                    // Get image URL
                    const imageUrl = this.getAttribute('data-image');
                    if (imageUrl) {
                        mainImage.src = imageUrl;
                        
                        // Also update the fancybox link if exists
                        if (typeof Fancybox !== 'undefined') {
                            mainImage.setAttribute('data-src', imageUrl);
                        }
                    }
                    
                    // Update active state
                    thumbnailItems.forEach(thumb => thumb.classList.remove('active'));
                    this.classList.add('active');
                } catch (error) {
                    console.error('Error updating thumbnail image:', error);
                }
            });
        });
    }
    
    // Image zoom functionality
    const zoomBtn = document.querySelector('.zoom-btn');
    if (zoomBtn && mainImage) {
        zoomBtn.addEventListener('click', function() {
            if (typeof Fancybox !== 'undefined') {
                Fancybox.show([
                    {
                        src: mainImage.src,
                        caption: mainImage.alt
                    }
                ]);
            } else {
                // Fallback for when Fancybox is not available
                window.open(mainImage.src, '_blank');
            }
        });
    }
}

// Quantity selector
function initQuantitySelector() {
    const decreaseBtn = document.getElementById('decrease-quantity');
    const increaseBtn = document.getElementById('increase-quantity');
    const quantityInput = document.getElementById('quantity');
    
    if (decreaseBtn && increaseBtn && quantityInput) {
        const maxStock = parseInt(quantityInput.getAttribute('max')) || 999;
        
        decreaseBtn.addEventListener('click', function() {
            let currentValue = parseInt(quantityInput.value);
            if (currentValue > 1) {
                quantityInput.value = currentValue - 1;
            }
        });
        
        increaseBtn.addEventListener('click', function() {
            let currentValue = parseInt(quantityInput.value);
            if (currentValue < maxStock) {
                quantityInput.value = currentValue + 1;
            } else {
                showNotification('Số lượng đã đạt mức tối đa có sẵn', 'warning');
            }
        });
        
        // Manual input validation
        quantityInput.addEventListener('change', function() {
            let currentValue = parseInt(this.value);
            if (isNaN(currentValue) || currentValue < 1) {
                this.value = 1;
            } else if (currentValue > maxStock) {
                this.value = maxStock;
                showNotification('Số lượng đã được điều chỉnh theo số lượng có sẵn', 'warning');
            }
        });
    }
}

// Color and Size selectors
function initAttributeSelectors() {
    const colorRadios = document.querySelectorAll('.color-radio');
    const sizeRadios = document.querySelectorAll('.size-radio');
    
    if (colorRadios.length > 0) {
        colorRadios.forEach(radio => {
            radio.addEventListener('change', function() {
                document.querySelectorAll('.color-label').forEach(label => {
                    label.classList.remove('active');
                });
                
                if (this.checked) {
                    const label = document.querySelector(`label[for="${this.id}"]`);
                    if (label) {
                        label.classList.add('active');
                    }
                }
            });
        });
    }
    
    if (sizeRadios.length > 0) {
        sizeRadios.forEach(radio => {
            radio.addEventListener('change', function() {
                document.querySelectorAll('.size-label').forEach(label => {
                    label.classList.remove('active');
                });
                
                if (this.checked) {
                    const label = document.querySelector(`label[for="${this.id}"]`);
                    if (label) {
                        label.classList.add('active');
                    }
                }
            });
        });
    }
}

// Add to cart functionality
function initAddToCart() {
    const addToCartBtn = document.getElementById('add-to-cart-button');
    
    if (addToCartBtn) {
        addToCartBtn.addEventListener('click', function() {
            // Get selected attributes
            const selectedColor = document.querySelector('.color-radio:checked');
            const selectedSize = document.querySelector('.size-radio:checked');
            const quantity = document.getElementById('quantity');
            
            // Validate selections if attributes exist
            if ((document.querySelector('.color-radio') && !selectedColor) || 
                (document.querySelector('.size-radio') && !selectedSize)) {
                showNotification('Vui lòng chọn màu sắc và kích thước', 'error');
                return;
            }
            
            // Get product ID from the page
            const productId = this.closest('form').getAttribute('data-product-id');
            
            // Create cart item
            const cartItem = {
                productId: productId,
                quantity: quantity ? parseInt(quantity.value) : 1,
                color: selectedColor ? selectedColor.value : null,
                size: selectedSize ? selectedSize.value : null
            };
            
            // Add to cart animation
            animateAddToCart();
            
            // Send to server (example - replace with actual API call)
            console.log('Adding to cart:', cartItem);
            
            // For demo, show success notification
            showNotification('Sản phẩm đã được thêm vào giỏ hàng', 'success');
        });
    }
}

// Animate product to cart
function animateAddToCart() {
    const mainImage = document.getElementById('main-product-image');
    const cartIcon = document.querySelector('.cart-count');
    
    if (mainImage && cartIcon) {
        // Create flying image element
        const flyingImage = document.createElement('img');
        flyingImage.src = mainImage.src;
        flyingImage.style.position = 'fixed';
        flyingImage.style.zIndex = '9999';
        flyingImage.style.width = '100px';
        flyingImage.style.height = 'auto';
        flyingImage.style.opacity = '0.8';
        flyingImage.style.pointerEvents = 'none';
        
        // Get positions
        const imgRect = mainImage.getBoundingClientRect();
        const cartRect = cartIcon.getBoundingClientRect();
        
        // Set starting position
        flyingImage.style.top = `${imgRect.top}px`;
        flyingImage.style.left = `${imgRect.left}px`;
        
        // Add to body
        document.body.appendChild(flyingImage);
        
        // Animate
        setTimeout(() => {
            flyingImage.style.transition = 'all 1s cubic-bezier(0.25, 0.1, 0.25, 1)';
            flyingImage.style.top = `${cartRect.top}px`;
            flyingImage.style.left = `${cartRect.left}px`;
            flyingImage.style.width = '30px';
            flyingImage.style.height = 'auto';
            flyingImage.style.opacity = '0';
            
            // Remove after animation
            setTimeout(() => {
                document.body.removeChild(flyingImage);
                
                // Update cart counter
                updateCartCounter();
            }, 1000);
        }, 10);
    }
}

// Update cart counter
function updateCartCounter() {
    const cartCount = document.querySelector('.cart-count');
    if (cartCount) {
        const currentCount = parseInt(cartCount.textContent) || 0;
        cartCount.textContent = currentCount + 1;
        
        // Add pulse animation
        cartCount.classList.add('pulse');
        setTimeout(() => {
            cartCount.classList.remove('pulse');
        }, 1000);
    }
}

// Wishlist functionality
function initWishlist() {
    const wishlistBtn = document.getElementById('add-to-wishlist-button');
    
    if (wishlistBtn) {
        wishlistBtn.addEventListener('click', function() {
            const icon = this.querySelector('i');
            
            if (icon.classList.contains('far')) {
                // Add to wishlist
                icon.classList.remove('far');
                icon.classList.add('fas', 'text-danger');
                this.querySelector('span') ? 
                    this.querySelector('span').textContent = ' Đã thêm vào yêu thích' : 
                    this.innerHTML = '<i class="fas fa-heart text-danger me-2"></i> Đã thêm vào yêu thích';
                    
                showNotification('Đã thêm sản phẩm vào danh sách yêu thích', 'success');
            } else {
                // Remove from wishlist
                icon.classList.remove('fas', 'text-danger');
                icon.classList.add('far');
                this.querySelector('span') ? 
                    this.querySelector('span').textContent = ' Thêm vào danh sách yêu thích' : 
                    this.innerHTML = '<i class="far fa-heart me-2"></i> Thêm vào danh sách yêu thích';
                    
                showNotification('Đã xóa sản phẩm khỏi danh sách yêu thích', 'info');
            }
        });
    }
}

// Buy now functionality
function initBuyNow() {
    const buyNowBtn = document.getElementById('buy-now-button');
    
    if (buyNowBtn) {
        buyNowBtn.addEventListener('click', function() {
            // Get selected attributes
            const selectedColor = document.querySelector('.color-radio:checked');
            const selectedSize = document.querySelector('.size-radio:checked');
            
            // Validate selections if attributes exist
            if ((document.querySelector('.color-radio') && !selectedColor) || 
                (document.querySelector('.size-radio') && !selectedSize)) {
                showNotification('Vui lòng chọn màu sắc và kích thước', 'error');
                return;
            }
            
            // Navigate to checkout page
            window.location.href = '/Checkout';
        });
    }
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
            style: {
                background: type === 'success' ? '#28a745' : 
                           type === 'error' ? '#dc3545' : 
                           type === 'warning' ? '#ffc107' : '#17a2b8'
            }
        }).showToast();
    } else {
        // Fallback to alert if Toastify isn't available
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