// recommendation.js - JavaScript for frontend integration
// Add to wwwroot/js/ in your ASP.NET Core project

class RecommendationService {
    static apiBase = '/api/recommendation';
    
    /**
     * Get personalized recommendations for a user
     */
    static async getRecommendations(userId, count = 10) {
        try {
            const response = await fetch(`${this.apiBase}/${userId}?count=${count}`);
            if (!response.ok) throw new Error(`HTTP ${response.status}`);
            return await response.json();
        } catch (error) {
            console.error('Error fetching recommendations:', error);
            return { recommendations: [], count: 0, error: error.message };
        }
    }
    
    /**
     * Get popular recommendations for anonymous users
     */
    static async getPopularRecommendations(count = 10) {
        try {
            const response = await fetch(`${this.apiBase}/popular?count=${count}`);
            if (!response.ok) throw new Error(`HTTP ${response.status}`);
            return await response.json();
        } catch (error) {
            console.error('Error fetching popular recommendations:', error);
            return { recommendations: [], count: 0, error: error.message };
        }
    }
    
    /**
     * Get similar products based on a specific product
     */
    static async getSimilarProducts(productId, count = 6) {
        try {
            const response = await fetch(`${this.apiBase}/similar/${productId}?count=${count}`);
            if (!response.ok) throw new Error(`HTTP ${response.status}`);
            return await response.json();
        } catch (error) {
            console.error('Error fetching similar products:', error);
            return { recommendations: [], count: 0, error: error.message };
        }
    }
    
    /**
     * Log user interaction for future model training
     */
    static async logInteraction(userId, productId, eventType = 'VIEW_PRODUCT', eventValue = 1.0) {
        try {
            const response = await fetch(`${this.apiBase}/interaction`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    userId: userId,
                    productId: productId,
                    eventType: eventType,
                    eventValue: eventValue
                })
            });
            
            if (!response.ok) throw new Error(`HTTP ${response.status}`);
            return await response.json();
        } catch (error) {
            console.error('Error logging interaction:', error);
            return { error: error.message };
        }
    }
    
    /**
     * Get recommendation system statistics
     */
    static async getStats() {
        try {
            const response = await fetch(`${this.apiBase}/stats`);
            if (!response.ok) throw new Error(`HTTP ${response.status}`);
            return await response.json();
        } catch (error) {
            console.error('Error fetching stats:', error);
            return { error: error.message };
        }
    }
    
    /**
     * Render recommendations in HTML
     */
    static renderRecommendations(recommendations, containerId, options = {}) {
        const container = document.getElementById(containerId);
        if (!container) {
            console.error(`Container ${containerId} not found`);
            return;
        }
        
        // Default options
        const defaultOptions = {
            showScore: false,
            showCategory: true,
            showBrand: true,
            imageClass: 'ai-product-image',
            cardClass: 'ai-recommendation-item',
            showAddToCart: true
        };
        
        const config = { ...defaultOptions, ...options };
        
        if (!recommendations || recommendations.length === 0) {
            container.innerHTML = `
                <div class="ai-no-recommendations col-12">
                    <p>Không có gợi ý sản phẩm nào</p>
                </div>
            `;
            return;
        }

        container.innerHTML = recommendations.map(rec => {
            // Fix image path - handle both relative and absolute URLs
            let imageUrl = rec.imageURL || rec.ImageURL || '/image/product-placeholder.png';
            
            // If it's a relative path, prepend with /upload/product/
            if (imageUrl && !imageUrl.startsWith('http') && !imageUrl.startsWith('/upload/') && !imageUrl.startsWith('/image/')) {
                imageUrl = `/upload/product/${imageUrl}`;
            }
            
            // Determine badge based on price or other criteria
            let badge = '';
            if (rec.price > 3000000) {
                badge = '<span class="badge bg-danger position-absolute top-0 start-0 m-2">Cao cấp</span>';
            } else if (rec.score && rec.score > 4.5) {
                badge = '<span class="badge bg-success position-absolute top-0 start-0 m-2">Gợi ý tốt</span>';
            }
            
            // Generate star rating based on actual rating or default to 0
            const rating = rec.averageRating || rec.AverageRating || 0;
            let stars = '';
            for (let i = 1; i <= 5; i++) {
                stars += `<i class="fas fa-star ${i <= rating ? 'text-warning' : 'text-muted'}"></i>`;
            }
            
            // Format price
            const formattedPrice = rec.price ? new Intl.NumberFormat('vi-VN').format(rec.price) + 'đ' : 'Liên hệ';
            
            return `
                <div class="col" data-product-id="${rec.productID}">
                    <div class="card h-100 product-card ai-recommendation-item">
                        <div class="card-img-top-container position-relative overflow-hidden">
                            <img src="${imageUrl}" 
                                 class="card-img-top" 
                                 alt="${rec.productName}" 
                                 loading="lazy"
                                 onerror="this.src='/image/product-placeholder.png'">
                            ${badge}
                        </div>
                        
                        <div class="card-body">
                            <div class="product-info">
                                <h5 class="card-title product-title">
                                    <a href="/Product/Details/${rec.productID}" class="text-decoration-none text-dark">
                                        ${rec.productName}
                                    </a>
                                </h5>
                                
                                <!-- Product Description (hidden in grid, shown in list) -->
                                <p class="card-text product-description">
                                    Sản phẩm được gợi ý dựa trên sở thích của bạn
                                </p>
                                
                                <div class="product-meta mb-2">
                                    ${config.showBrand && rec.brandName ? 
                                        `<span class="badge bg-light text-dark me-1">${rec.brandName}</span>` : ''}
                                    ${config.showCategory && rec.categoryName ? 
                                        `<span class="badge bg-secondary">${rec.categoryName}</span>` : ''}
                                </div>
                                
                                <!-- Rating -->
                                <div class="rating mb-2">
                                    <div class="stars">
                                        ${stars}
                                    </div>
                                    <span class="text-muted small ms-1">
                                        ${rating > 0 ? 
                                            `${rating.toFixed(1)} (${rec.reviewCount || rec.ReviewCount || 0})` : 
                                            'Chưa có đánh giá'
                                        }
                                    </span>
                                </div>
                            </div>
                            
                            <!-- Price and Actions Section -->
                            <div class="price-section">
                                <div class="product-price">
                                    <span class="price fw-bold fs-5 text-primary">${formattedPrice}</span>
                                    <div class="stock-info small">
                                        <i class="fas fa-check-circle text-success"></i> Có sẵn
                                    </div>
                                </div>
                            </div>
                            
                            <!-- Action Buttons (positioned same as product pages) -->
                            ${config.showAddToCart ? `
                                <div class="product-actions mt-auto">
                                    <button class="btn btn-primary add-to-cart" 
                                            onclick="handleAddToCart(${rec.productID})" 
                                            title="Thêm vào giỏ hàng">
                                        <i class="fas fa-shopping-cart"></i>
                                        <span class="d-none d-lg-inline ms-2 grid-view-text">Thêm vào giỏ</span>
                                    </button>
                                    <button class="btn btn-outline-primary quick-view" 
                                            onclick="handleViewProduct(${rec.productID})" 
                                            title="Xem nhanh">
                                        <i class="fas fa-eye"></i>
                                    </button>
                                    <button class="btn btn-outline-danger add-to-wishlist" 
                                            onclick="handleAddToWishlist(${rec.productID})"
                                            title="Thêm vào yêu thích">
                                        <i class="far fa-heart"></i>
                                    </button>
                                </div>
                            ` : ''}
                        </div>
                    </div>
                </div>
            `;
        }).join('');
        
        // No automatic VIEW_PRODUCT logging - only log when user explicitly interacts
        // Removed auto-logging to reduce noise in interaction data
    }
}

// Global functions for UI interactions
window.handleAddToCart = function(productId) {
    // No frontend tracking - backend CartController will handle interaction logging
    // This prevents duplicate ADD_TO_CART entries in InteractionEvent table
    
    // Your existing add to cart logic
    if (typeof addToCart === 'function') {
        addToCart(productId, 1);
    } else {
        console.log(`Adding product ${productId} to cart`);
        // Default implementation - you can customize this
        fetch('/Cart/AddToCartJson', {
            method: 'POST',
            headers: { 
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
            },
            body: JSON.stringify({ productId: productId, quantity: 1 })
        }).then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.json();
        }).then(data => {
            if (data.success) {
                showNotification(data.message || 'Đã thêm sản phẩm vào giỏ hàng thành công!', 'success');
                if (typeof updateCartCounter === 'function') {
                    updateCartCounter(data.cartCount);
                }
            } else {
                showNotification(data.message || 'Có lỗi xảy ra khi thêm sản phẩm vào giỏ hàng', 'error');
            }
        }).catch(error => {
            console.error('Error:', error);
            showNotification('Có lỗi xảy ra khi thêm sản phẩm vào giỏ hàng', 'error');
        });
    }
};

window.handleAddToWishlist = function(productId) {
    if (typeof toggleWishlist === 'function') {
        toggleWishlist(productId);
    } else {
        // Default implementation
        fetch('/Wishlist/Toggle', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json'
            },
            body: JSON.stringify({ productId: parseInt(productId) })
        })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                showNotification(data.message, 'success');
                
                // Update all wishlist buttons for this product
                const wishlistButtons = document.querySelectorAll(`[onclick*="${productId}"]`);
                wishlistButtons.forEach(button => {
                    if (button.onclick && button.onclick.toString().includes('handleAddToWishlist')) {
                        const icon = button.querySelector('i');
                        if (icon) {
                            if (data.isInWishlist) {
                                icon.classList.remove('far');
                                icon.classList.add('fas');
                                button.classList.remove('btn-outline-danger');
                                button.classList.add('btn-danger');
                            } else {
                                icon.classList.remove('fas');
                                icon.classList.add('far');
                                button.classList.remove('btn-danger');
                                button.classList.add('btn-outline-danger');
                            }
                        }
                    }
                });
            } else {
                showNotification(data.message || 'Có lỗi xảy ra khi cập nhật danh sách yêu thích', 'error');
            }
        })
        .catch(error => {
            console.error('Error:', error);
            showNotification('Có lỗi xảy ra khi cập nhật danh sách yêu thích', 'error');
        });
    }
};

window.handleViewProduct = function(productId) {
    // No VIEW_PRODUCT logging - removed to reduce noise in interaction data
    // Navigate to product detail page (ASP.NET MVC routing)
    window.location.href = `/Product/Details/${productId}`;
};

// Global function to load recommendations
window.loadRecommendations = async function() {
    console.log('Loading recommendations for user:', window.currentUserId);
    
    const userRecsContainer = document.getElementById('user-recommendations');
    if (!userRecsContainer) {
        console.log('No recommendations container found');
        return;
    }
    
    // Show loading state
    userRecsContainer.innerHTML = '<div class="ai-loading col-12">Đang tải gợi ý...</div>';
    
    try {
        if (window.currentUserId) {
            console.log(`Loading personalized recommendations for user ${window.currentUserId}`);
            
            const userRecs = await RecommendationService.getRecommendations(window.currentUserId, 5);
            if (userRecs.recommendations && userRecs.recommendations.length > 0) {
                console.log(`Found ${userRecs.recommendations.length} personalized recommendations`);
                RecommendationService.renderRecommendations(
                    userRecs.recommendations, 
                    'user-recommendations',
                    { showScore: true }
                );
            } else {
                console.log('No personalized recommendations found, loading popular ones');
                const popularRecs = await RecommendationService.getPopularRecommendations(5);
                RecommendationService.renderRecommendations(
                    popularRecs.recommendations, 
                    'user-recommendations'
                );
            }
        } else {
            console.log('Loading popular recommendations for anonymous user');
            
            const popularRecs = await RecommendationService.getPopularRecommendations(5);
            RecommendationService.renderRecommendations(
                popularRecs.recommendations, 
                'user-recommendations'
            );
        }
    } catch (error) {
        console.error('Error loading recommendations:', error);
        userRecsContainer.innerHTML = `<div class="ai-no-recommendations col-12">Lỗi tải gợi ý: ${error.message}</div>`;
    }
};

// Function to update user and reload recommendations
window.updateUserRecommendations = function(newUserId) {
    console.log('Updating user ID and reloading recommendations:', newUserId);
    window.currentUserId = newUserId;
    window.loadRecommendations();
};

// Auto-load recommendations when page loads
document.addEventListener('DOMContentLoaded', async function() {
    console.log('Page loaded, initializing recommendations...');
    
    // Initial load
    await window.loadRecommendations();
    
    // Monitor for user session changes more frequently after login
    let lastUserId = window.currentUserId;
    let checkCount = 0;
    
    const sessionChecker = setInterval(() => {
        checkCount++;
        
        // Check if user ID changed (after login/logout)
        if (window.currentUserId !== lastUserId) {
            console.log(`🔄 User ID changed from ${lastUserId} to ${window.currentUserId}, reloading recommendations`);
            lastUserId = window.currentUserId;
            window.loadRecommendations();
        }
        
        // Check more frequently in first 30 seconds (for login detection)
        if (checkCount > 30) {
            clearInterval(sessionChecker);
            
            // Then check less frequently
            setInterval(() => {
                if (window.currentUserId !== lastUserId) {
                    console.log(`🔄 User ID changed from ${lastUserId} to ${window.currentUserId}, reloading recommendations`);
                    lastUserId = window.currentUserId;
                    window.loadRecommendations();
                }
            }, 5000); // Check every 5 seconds after initial period
        }
    }, 1000); // Check every second for first 30 seconds
    
    // Also check for session changes via storage events
    window.addEventListener('storage', function(e) {
        if (e.key === 'userLoggedIn' || e.key === 'userLoggedOut') {
            console.log('🔄 Storage event detected, reloading recommendations');
            setTimeout(() => {
                window.loadRecommendations();
            }, 1000);
        }
    });
    
    // Load similar products on product detail page
    if (window.currentProductId) {
        const similarContainer = document.getElementById('similar-products');
        if (similarContainer) {
            similarContainer.innerHTML = '<div class="loading">Đang tải sản phẩm tương tự...</div>';
            
            const similarRecs = await RecommendationService.getSimilarProducts(window.currentProductId, 6);
            RecommendationService.renderRecommendations(
                similarRecs.recommendations, 
                'similar-products'
            );
        }
    }
});

// Utility function for showing notifications
function showNotification(message, type = 'info') {
    // Create notification element
    const notification = document.createElement('div');
    notification.className = `ai-notification ai-notification-${type}`;
    notification.innerHTML = `
        <span>${message}</span>
        <button onclick="this.parentElement.remove()">×</button>
    `;
    
    // Add to page
    const container = document.getElementById('notification-container') || document.body;
    container.appendChild(notification);
    
    // Auto remove after 3 seconds
    setTimeout(() => {
        if (notification.parentElement) {
            notification.remove();
        }
    }, 3000);
}