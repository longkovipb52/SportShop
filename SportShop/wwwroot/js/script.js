// Custom JavaScript for Sporty AI Store

document.addEventListener("DOMContentLoaded", () => {
  // Initialize all interactive features
  initScrollToTop()
  initSmoothScrolling()
  initCartFunctionality()
  initProductInteractions()
  initAnimations()
  initNavbarEffects()
  initBootstrapComponents()
  initLazyLoading()
  initMobileMenu()
  initProductSearch()
  initHeroEffects() // Added line
  initProductMarquee();
  initChatbot(); // Initialize chatbot
})

// Scroll to Top Button
function initScrollToTop() {
  const scrollToTopBtn = document.getElementById("scrollToTop")

  window.addEventListener("scroll", () => {
    if (window.scrollY > 300) {
      scrollToTopBtn.style.opacity = "1"
      scrollToTopBtn.style.visibility = "visible"
    } else {
      scrollToTopBtn.style.opacity = "0"
      scrollToTopBtn.style.visibility = "hidden"
    }
  })

  scrollToTopBtn.addEventListener("click", () => {
    window.scrollTo({
      top: 0,
      behavior: "smooth"
    })
  })
}

// Smooth Scrolling for Navigation Links
function initSmoothScrolling() {
  const navLinks = document.querySelectorAll('a[href^="#"]')

  navLinks.forEach((link) => {
    link.addEventListener("click", function (e) {
      if (this.getAttribute("href") !== "#") {
        e.preventDefault()
        const target = document.querySelector(this.getAttribute("href"))
        if (target) {
          target.scrollIntoView({
            behavior: "smooth"
          })
        }
      }
    })
  })
}

// Cart Functionality
function initCartFunctionality() {
  let cartCount = 3 // Initial cart count
  const cartCountElement = document.querySelector(".cart-count")
  const addToCartButtons = document.querySelectorAll('button.btn-primary')

  // Add to cart functionality
  document.addEventListener("click", (e) => {
    if (e.target.textContent.includes("Add to Cart")) {
      e.preventDefault()

      // Animate button
      const button = e.target
      const originalText = button.textContent

      button.innerHTML = '<span class="loading"></span> Adding...'
      button.disabled = true

      setTimeout(() => {
        cartCount++
        if (cartCountElement) {
          cartCountElement.textContent = cartCount
        }

        button.textContent = "Added!"
        button.classList.remove("btn-primary")
        button.classList.add("btn-success")

        setTimeout(() => {
          button.textContent = originalText
          button.classList.remove("btn-success")
          button.classList.add("btn-primary")
          button.disabled = false
        }, 1500)

        // Show success notification
        showNotification("Product added to cart!", "success")
      }, 1000)
    }
  })
}

// Product Interactions
function initProductInteractions() {
  // Product card hover effects
  const productCards = document.querySelectorAll(".product-card, .category-card")

  productCards.forEach((card) => {
    card.addEventListener("mouseenter", function () {
      this.style.transform = "translateY(-10px)"
    })

    card.addEventListener("mouseleave", function () {
      this.style.transform = "translateY(0)"
    })
  })

  // View Details button functionality
  document.addEventListener("click", (e) => {
    if (e.target.textContent.includes("View Details")) {
      e.preventDefault()
      showNotification("Product details coming soon!", "info")
    }
  })
}

// Animations on Scroll
function initAnimations() {
  const observerOptions = {
    threshold: 0.1,
    rootMargin: "0px 0px -50px 0px",
  }

  const observer = new IntersectionObserver((entries) => {
    entries.forEach((entry) => {
      if (entry.isIntersecting) {
        entry.target.classList.add("animate-fade-in")
        observer.unobserve(entry.target)
      }
    })
  }, observerOptions)

  // Observe elements for animation
  const animateElements = document.querySelectorAll(".card, .ai-content, .hero-content")
  animateElements.forEach((el) => observer.observe(el))
}

// Navbar Effects
function initNavbarEffects() {
  const navbar = document.querySelector(".navbar")

  window.addEventListener("scroll", () => {
    if (window.scrollY > 50) {
      navbar.classList.add("shadow-lg")
      navbar.style.backgroundColor = "rgba(255, 255, 255, 0.95)"
      navbar.style.backdropFilter = "blur(10px)"
    } else {
      navbar.classList.remove("shadow-lg")
      navbar.style.backgroundColor = "white"
      navbar.style.backdropFilter = "none"
    }
  })
}

// Notification System
function showNotification(message, type = "success") {
  // Create notification element
  const notification = document.createElement("div")
  notification.classList.add("toast", "position-fixed", "top-0", "end-0", "m-3")
  notification.style.zIndex = "9999"
  
  // Set color based on type
  let bgColor = "bg-success"
  if (type === "warning") bgColor = "bg-warning"
  if (type === "error") bgColor = "bg-danger"
  if (type === "info") bgColor = "bg-info"
  
  notification.innerHTML = `
    <div class="toast-header ${bgColor} text-white">
      <strong class="me-auto">Notification</strong>
      <button type="button" class="btn-close btn-close-white" data-bs-dismiss="toast"></button>
    </div>
    <div class="toast-body">
      ${message}
    </div>
  `
  
  document.body.appendChild(notification)
  
  // Initialize Bootstrap toast
  const toast = new bootstrap.Toast(notification, { delay: 3000 })
  toast.show()
  
  // Remove element after it's hidden
  notification.addEventListener('hidden.bs.toast', function () {
    notification.remove()
  })
}

// Newsletter Subscription
document.addEventListener("click", (e) => {
  if (e.target.textContent.includes("Subscribe")) {
    e.preventDefault()
    const emailInput = e.target.previousElementSibling
    const email = emailInput.value

    if (email && isValidEmail(email)) {
      showNotification("Thank you for subscribing to our newsletter!", "success")
      emailInput.value = ""
    } else {
      showNotification("Please enter a valid email address.", "warning")
    }
  }
})

// Email validation helper
function isValidEmail(email) {
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
  return emailRegex.test(email)
}

// Product Search Functionality với Live Suggestions
function initProductSearch() {
  const searchInputs = document.querySelectorAll('.search-input');
  let searchTimeout = null; // Debounce timer
  
  searchInputs.forEach(searchInput => {
    if (searchInput) {
      // Hiệu ứng nhấp nháy placeholder
      const searchTerms = ['Giày thể thao', 'Áo bóng đá', 'Quần thể thao', 'Phụ kiện tập gym'];
      let currentIndex = 0;
      
      // Hiệu ứng đổi placeholder
      setInterval(() => {
        if (searchInput.value === '' && document.activeElement !== searchInput) {
          searchInput.setAttribute('placeholder', `Tìm kiếm ${searchTerms[currentIndex]}...`);
          currentIndex = (currentIndex + 1) % searchTerms.length;
        }
      }, 3000);
      
      // Xử lý sự kiện nhập với debounce
      searchInput.addEventListener('input', function() {
        const searchTerm = this.value.trim();
        const suggestionsContainer = this.parentElement.querySelector('.search-suggestions');
        
        // Clear timeout trước đó
        if (searchTimeout) {
          clearTimeout(searchTimeout);
        }
        
        if (searchTerm.length >= 2) {
          // Hiển thị loading state
          showLoadingState(suggestionsContainer);
          
          // Debounce API call (chờ 300ms sau khi user ngừng gõ)
          searchTimeout = setTimeout(() => {
            fetchSearchSuggestions(searchTerm, this);
          }, 300);
        } else {
          // Ẩn suggestions nếu từ khóa quá ngắn
          hideSuggestions(suggestionsContainer);
        }
      });
      
      // Hiển thị suggestions khi focus vào input có giá trị
      searchInput.addEventListener('focus', function() {
        const searchTerm = this.value.trim();
        if (searchTerm.length >= 2) {
          const suggestionsContainer = this.parentElement.querySelector('.search-suggestions');
          if (suggestionsContainer && !suggestionsContainer.classList.contains('d-none')) {
            suggestionsContainer.classList.remove('d-none');
          }
        }
      });
      
      // Ẩn suggestions khi blur (với delay dài hơn để cho phép click vào suggestion)
      searchInput.addEventListener('blur', function() {
        const inputElement = this;
        
        // Clear any existing timeout
        if (inputElement.blurTimeout) {
          clearTimeout(inputElement.blurTimeout);
        }
        
        inputElement.blurTimeout = setTimeout(() => {
          // Kiểm tra xem có đang hover trên suggestions không
          const suggestionsContainer = inputElement.parentElement.querySelector('.search-suggestions');
          if (suggestionsContainer) {
            const isHovering = suggestionsContainer.matches(':hover');
            console.log('Blur timeout triggered, isHovering:', isHovering);
            
            if (!isHovering) {
              hideSuggestions(suggestionsContainer);
            } else {
              console.log('Not hiding suggestions because user is hovering');
            }
          }
        }, 500); // Tăng từ 200ms lên 500ms
      });
      
      // Xử lý phím mũi tên và Enter cho navigation trong suggestions
      searchInput.addEventListener('keydown', function(e) {
        const suggestionsContainer = this.parentElement.querySelector('.search-suggestions');
        const suggestionItems = suggestionsContainer?.querySelectorAll('.suggestion-item');
        
        if (!suggestionItems || suggestionItems.length === 0) return;
        
        let currentSelected = suggestionsContainer.querySelector('.suggestion-item.selected');
        let currentIndex = currentSelected ? Array.from(suggestionItems).indexOf(currentSelected) : -1;
        
        switch(e.key) {
          case 'ArrowDown':
            e.preventDefault();
            if (currentSelected) currentSelected.classList.remove('selected');
            currentIndex = (currentIndex + 1) % suggestionItems.length;
            suggestionItems[currentIndex].classList.add('selected');
            suggestionItems[currentIndex].scrollIntoView({ block: 'nearest' });
            break;
            
          case 'ArrowUp':
            e.preventDefault();
            if (currentSelected) currentSelected.classList.remove('selected');
            currentIndex = currentIndex <= 0 ? suggestionItems.length - 1 : currentIndex - 1;
            suggestionItems[currentIndex].classList.add('selected');
            suggestionItems[currentIndex].scrollIntoView({ block: 'nearest' });
            break;
            
          case 'Enter':
            if (currentSelected) {
              e.preventDefault();
              currentSelected.click();
            }
            break;
            
          case 'Escape':
            hideSuggestions(suggestionsContainer);
            this.blur();
            break;
        }
      });
      
      // Đảm bảo nút tìm kiếm không bị lỗi khi click
      const searchButton = searchInput.parentElement.querySelector('.btn-search');
      if (searchButton) {
        searchButton.addEventListener('mousedown', function(e) {
          e.preventDefault(); // Ngăn input mất focus khi click vào nút
        });
      }
    }
  });
  
  // Đóng suggestions khi click ra ngoài
  document.addEventListener('click', (e) => {
    if (!e.target.closest('.search-form')) {
      const suggestions = document.querySelectorAll('.search-suggestions');
      suggestions.forEach(el => hideSuggestions(el));
    }
  });
  
  // Check voice search support and show info
  document.addEventListener('DOMContentLoaded', function() {
    if (window.voiceSearchManager) {
      const supportInfo = window.voiceSearchManager.getSupportInfo();
      
      if (!supportInfo.supported) {
        // Show browser support info in search areas
        const searchForms = document.querySelectorAll('.search-form');
        searchForms.forEach(form => {
          const infoDiv = document.createElement('div');
          infoDiv.className = 'voice-support-info show';
          infoDiv.innerHTML = `
            <i class="fas fa-info-circle me-2"></i>
            ${supportInfo.message}
          `;
          form.appendChild(infoDiv);
        });
      }
    }
  });
}

// Fetch suggestions từ API
async function fetchSearchSuggestions(term, inputElement) {
  try {
    console.log('Fetching suggestions for:', term);
    const response = await fetch(`/Product/SearchSuggestions?term=${encodeURIComponent(term)}&limit=6`);
    const data = await response.json();
    
    console.log('API Response:', data);
    
    if (data.success) {
      displaySuggestions(data.suggestions, inputElement);
    } else {
      showEmptyState(inputElement.parentElement.querySelector('.search-suggestions'));
    }
  } catch (error) {
    console.error('Error fetching search suggestions:', error);
    showEmptyState(inputElement.parentElement.querySelector('.search-suggestions'));
  }
}

// Hiển thị suggestions
function displaySuggestions(suggestions, inputElement) {
  const suggestionsContainer = inputElement.parentElement.querySelector('.search-suggestions');
  const suggestionsList = suggestionsContainer.querySelector('.suggestions-list');
  const loadingState = suggestionsContainer.querySelector('.suggestions-loading');
  const emptyState = suggestionsContainer.querySelector('.suggestions-empty');
  
  // Ẩn loading và empty states
  loadingState.classList.add('d-none');
  emptyState.classList.add('d-none');
  
  if (suggestions.length > 0) {
    // Build HTML for suggestions
    const html = suggestions.map(product => `
      <div class="suggestion-item" data-id="${product.id}" role="button" tabindex="0">
        <img src="${product.imageUrl}" alt="${product.name}" class="suggestion-img" loading="lazy" 
             onerror="this.src='/image/loading-placeholder.png';">
        <div class="suggestion-content">
          <div class="suggestion-name">${escapeHtml(product.name)}</div>
          <div class="suggestion-meta">
            <span class="suggestion-price">${formatPrice(product.price)}</span>
            <span class="suggestion-category">${escapeHtml(product.categoryName)}</span>
          </div>
        </div>
      </div>
    `).join('');
    
    suggestionsList.innerHTML = html;
    suggestionsContainer.classList.remove('d-none');
    
    // Attach event listeners to suggestion items
    attachSuggestionListeners(suggestionsList, suggestionsContainer);
  } else {
    showEmptyState(suggestionsContainer);
  }
}

// Attach event listeners to suggestion items
function attachSuggestionListeners(suggestionsList, suggestionsContainer) {
  const suggestionItems = suggestionsList.querySelectorAll('.suggestion-item');
  
  suggestionItems.forEach(item => {
    // Click event
    item.addEventListener('click', function(e) {
      e.preventDefault();
      e.stopPropagation();
      
      const productId = this.getAttribute('data-id');
      if (productId) {
        suggestionsContainer.classList.add('d-none');
        goToProduct(parseInt(productId));
      }
    });
    
    // Keyboard navigation for accessibility
    item.addEventListener('keydown', function(e) {
      if (e.key === 'Enter' || e.key === ' ') {
        e.preventDefault();
        this.click();
      }
    });
    
    // Prevent blur hiding when hovering over suggestions
    item.addEventListener('mouseenter', function() {
      const searchInput = suggestionsContainer.parentElement.querySelector('.search-input');
      if (searchInput && searchInput.blurTimeout) {
        clearTimeout(searchInput.blurTimeout);
        searchInput.blurTimeout = null;
      }
    });
    
    // Touch support for mobile
    item.addEventListener('touchend', function(e) {
      e.preventDefault();
      const productId = this.getAttribute('data-id');
      if (productId) {
        suggestionsContainer.classList.add('d-none');
        goToProduct(parseInt(productId));
      }
    });
  });
}

// Hiển thị loading state
function showLoadingState(suggestionsContainer) {
  if (!suggestionsContainer) return;
  
  const suggestionsList = suggestionsContainer.querySelector('.suggestions-list');
  const loadingState = suggestionsContainer.querySelector('.suggestions-loading');
  const emptyState = suggestionsContainer.querySelector('.suggestions-empty');
  
  suggestionsList.innerHTML = '';
  loadingState.classList.remove('d-none');
  emptyState.classList.add('d-none');
  suggestionsContainer.classList.remove('d-none');
}

// Hiển thị empty state
function showEmptyState(suggestionsContainer) {
  if (!suggestionsContainer) return;
  
  const suggestionsList = suggestionsContainer.querySelector('.suggestions-list');
  const loadingState = suggestionsContainer.querySelector('.suggestions-loading');
  const emptyState = suggestionsContainer.querySelector('.suggestions-empty');
  
  suggestionsList.innerHTML = '';
  loadingState.classList.add('d-none');
  emptyState.classList.remove('d-none');
  suggestionsContainer.classList.remove('d-none');
}

// Ẩn suggestions
function hideSuggestions(suggestionsContainer) {
  if (suggestionsContainer) {
    suggestionsContainer.classList.add('d-none');
    // Remove selected states
    const selectedItems = suggestionsContainer.querySelectorAll('.suggestion-item.selected');
    selectedItems.forEach(item => item.classList.remove('selected'));
  }
}

// Utility functions
function escapeHtml(text) {
  const div = document.createElement('div');
  div.textContent = text;
  return div.innerHTML;
}

function formatPrice(price) {
  return new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND'
  }).format(price);
}

// Function để chuyển đến trang sản phẩm hoặc mở modal
function goToProduct(productId) {
  // Ẩn suggestions
  const suggestions = document.querySelectorAll('.search-suggestions');
  suggestions.forEach(el => hideSuggestions(el));
  
  // Kiểm tra xem có modal quick view không
  const modal = document.getElementById('quickViewModal');
  
  if (modal) {
    openQuickViewModal(productId);
  } else {
    // Nếu không có modal, chuyển đến trang product index với tham số để mở modal
    window.location.href = `/Product/Index?openModal=${productId}`;
  }
}

// Function để mở modal quick view
function openQuickViewModal(productId) {
  const modal = document.getElementById('quickViewModal');
  if (modal) {
    // Show loading state
    try {
      if (typeof showQuickViewLoading === 'function') {
        showQuickViewLoading();
      } else {
        // Simple loading state
        const imageEl = document.getElementById('quick-view-image');
        const titleEl = document.getElementById('quick-view-title');
        const priceEl = document.getElementById('quick-view-price');
        
        if (imageEl) imageEl.src = '/image/loading-placeholder.png';
        if (titleEl) titleEl.textContent = 'Đang tải...';
        if (priceEl) priceEl.textContent = 'Đang tải giá...';
      }
    } catch (error) {
      console.error('Error in loading state:', error);
    }
    
    // Show modal
    try {
      const bsModal = new bootstrap.Modal(modal);
      bsModal.show();
    } catch (error) {
      console.error('Error showing modal:', error);
      return;
    }
    
    // Fetch product data
    fetch(`/Product/GetProductJson?id=${productId}`)
      .then(response => {
        if (!response.ok) {
          throw new Error(`HTTP error! status: ${response.status}`);
        }
        return response.json();
      })
      .then(data => {
        // Kiểm tra nếu có error property (từ catch exception)
        if (data.error) {
          console.error('API returned error:', data.error);
          showProductError();
          return;
        }
        
        // Kiểm tra nếu có productID (data hợp lệ)
        if (data.productID) {
          if (typeof populateQuickView === 'function') {
            populateQuickView(data);
          } else {
            simplePopulateQuickView(data);
          }
        } else {
          console.error('Invalid product data received:', data);
          showProductError();
        }
      })
      .catch(error => {
        console.error('Error loading product:', error);
        showProductError();
      });
  } else {
    console.error('Quick view modal not found');
  }
}

// Simple fallback function để populate modal
function simplePopulateQuickView(product) {
  try {
    const imageEl = document.getElementById('quick-view-image');
    const titleEl = document.getElementById('quick-view-title');
    const priceEl = document.getElementById('quick-view-price');
    const descEl = document.getElementById('quick-view-description');
    const categoryEl = document.getElementById('quick-view-category');
    const brandEl = document.getElementById('quick-view-brand');
    
    // Product image với đường dẫn đúng
    if (imageEl) {
      const imagePath = product.imageURL ? 
        (product.imageURL.startsWith('/') ? product.imageURL : `/upload/product/${product.imageURL}`) :
        '/image/loading-placeholder.png';
      imageEl.src = imagePath;
      imageEl.alt = product.name || 'Sản phẩm';
    }
    
    if (titleEl) {
      titleEl.textContent = product.name || 'Sản phẩm';
    }
    
    if (priceEl) {
      priceEl.textContent = formatPrice(product.price || 0);
    }
    
    if (descEl) {
      descEl.textContent = product.description || 'Đang cập nhật thông tin sản phẩm...';
    }
    
    if (categoryEl) {
      categoryEl.textContent = product.categoryName || 'Chưa phân loại';
    }
    
    if (brandEl) {
      brandEl.textContent = product.brandName || 'Không có thương hiệu';
    }
  } catch (error) {
    console.error('Error in simple populate:', error);
  }
}

// Function để hiển thị lỗi trong modal
function showProductError() {
  try {
    const imageEl = document.getElementById('quick-view-image');
    const titleEl = document.getElementById('quick-view-title');
    const priceEl = document.getElementById('quick-view-price');
    const descEl = document.getElementById('quick-view-description');
    
    if (imageEl) imageEl.src = '/image/loading-placeholder.png';
    if (titleEl) titleEl.textContent = 'Lỗi tải sản phẩm';
    if (priceEl) priceEl.textContent = '';
    if (descEl) descEl.textContent = 'Không thể tải thông tin sản phẩm. Vui lòng thử lại sau.';
  } catch (error) {
    console.error('Error showing product error:', error);
  }
}

// Mobile Menu Enhancement
function initMobileMenu() {
  const navbarToggler = document.querySelector(".navbar-toggler")
  const navbarCollapse = document.querySelector(".navbar-collapse")

  if (navbarToggler && navbarCollapse) {
    navbarToggler.addEventListener("click", () => {
      setTimeout(() => {
        if (navbarCollapse.classList.contains("show")) {
          document.body.style.overflow = "hidden"
        } else {
          document.body.style.overflow = "auto"
        }
      }, 300)
    })
  }
}

// Performance optimization: Lazy loading for images
function initLazyLoading() {
  if ('loading' in HTMLImageElement.prototype) {
    const lazyImages = document.querySelectorAll('img[loading="lazy"]')
    lazyImages.forEach(img => {
      img.src = img.dataset.src
    })
  } else {
    // Fallback for browsers that don't support lazy loading
    const lazyImageObserver = new IntersectionObserver((entries) => {
      entries.forEach((entry) => {
        if (entry.isIntersecting) {
          const lazyImage = entry.target
          lazyImage.src = lazyImage.dataset.src
          lazyImageObserver.unobserve(lazyImage)
        }
      })
    })
    
    const lazyImages = document.querySelectorAll('img[data-src]')
    lazyImages.forEach(image => {
      lazyImageObserver.observe(image)
    })
  }
}

// Initialize tooltips and popovers
function initBootstrapComponents() {
  // Initialize tooltips
  const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'))
  tooltipTriggerList.map(function (tooltipTriggerEl) {
    return new bootstrap.Tooltip(tooltipTriggerEl)
  })
  
  // Initialize popovers
  const popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'))
  popoverTriggerList.map(function (popoverTriggerEl) {
    return new bootstrap.Popover(popoverTriggerEl)
  })
}

// Enhanced Hero Image Effects
function initHeroEffects() {
  const heroImg = document.querySelector('.hero-img');
  const heroSection = document.querySelector('.hero-section');
  
  if (heroImg && heroSection) {
    // Parallax effect on scroll
    window.addEventListener('scroll', () => {
      const scrollPosition = window.scrollY;
      if (scrollPosition < window.innerHeight) {
        heroImg.style.transform = `translateY(${scrollPosition * 0.15}px)`;
        heroSection.style.backgroundPosition = `center ${scrollPosition * 0.1}px`;
      }
    });
    
    // Mouse movement effect
    heroSection.addEventListener('mousemove', (e) => {
      const x = e.clientX / window.innerWidth - 0.5;
      const y = e.clientY / window.innerHeight - 0.5;
      
      heroImg.style.transform = `translateX(${x * 20}px) translateY(${y * 20}px)`;
    });
    
    // Reset transform on mouse leave
    heroSection.addEventListener('mouseleave', () => {
      heroImg.style.transform = 'translateX(0) translateY(0)';
    });
  }
}

// Product Marquee Initialization
function initProductMarquee() {
  const productCarousel = document.getElementById('productCarousel');
  
  // Nếu có sản phẩm carousel
  if (productCarousel) {
    // Khởi tạo Bootstrap carousel với các tùy chọn
    const carousel = new bootstrap.Carousel(productCarousel, {
      interval: 5000, // Thời gian chuyển đổi (5 giây)
      pause: 'hover', // Tạm dừng khi hover
      wrap: true,     // Lặp lại từ đầu khi đến slide cuối
      ride: 'carousel', // Tự động chạy
      touch: true     // Cho phép chạm để điều khiển trên thiết bị di động
    });
    
    // Thêm hiệu ứng fade khi chuyển slide (sử dụng CSS transition thay vì GSAP)
    productCarousel.addEventListener('slide.bs.carousel', function (e) {
      const activeItem = this.querySelector('.carousel-item.active');
      const nextItem = e.relatedTarget;
      
      // Áp dụng hiệu ứng fade với CSS
      if (activeItem) {
        activeItem.style.transition = 'opacity 0.5s ease';
        activeItem.style.opacity = '0.5';
      }
      
      if (nextItem) {
        nextItem.style.transition = 'opacity 0.5s ease';
        nextItem.style.opacity = '1';
      }
    });

    // Tự động cuộn carousel sau mỗi khoảng thời gian
    setInterval(function() {
      carousel.next();
    }, 5000);
  }
}

// Chatbot Functionality
function initChatbot() {
  const chatbotBtn = document.getElementById('chatbotBtn');
  const chatbotWidget = document.getElementById('chatbotWidget');
  const closeChatbot = document.getElementById('closeChatbot');
  const minimizeChatbot = document.getElementById('minimizeChatbot');
  const chatbotInput = document.getElementById('chatbotInput');
  const sendButton = document.getElementById('sendMessage');
  const chatbotMessages = document.getElementById('chatbotMessages');
  const chatbotTyping = document.getElementById('chatbotTyping');
  const quickActionBtns = document.querySelectorAll('.quick-action-btn');

  // Toggle chatbot widget
  chatbotBtn.addEventListener('click', () => {
    const isVisible = chatbotWidget.style.display === 'flex';
    if (isVisible) {
      chatbotWidget.style.display = 'none';
    } else {
      chatbotWidget.style.display = 'flex';
      chatbotWidget.classList.remove('minimized');
      chatbotInput.focus();
    }
  });

  // Close chatbot
  closeChatbot.addEventListener('click', () => {
    chatbotWidget.style.display = 'none';
  });

  // Minimize/maximize chatbot
  minimizeChatbot.addEventListener('click', () => {
    chatbotWidget.classList.toggle('minimized');
    if (chatbotWidget.classList.contains('minimized')) {
      minimizeChatbot.innerHTML = '<i class="fas fa-window-maximize"></i>';
    } else {
      minimizeChatbot.innerHTML = '<i class="fas fa-minus"></i>';
      chatbotInput.focus();
    }
  });

  // Send message function
  function sendMessage() {
    const message = chatbotInput.value.trim();
    if (!message) return;

    // Add user message
    addMessage(message, 'user');
    chatbotInput.value = '';

    // Show typing indicator
    showTypingIndicator();

    // Simulate bot response after delay
    setTimeout(() => {
      hideTypingIndicator();
      const botResponse = generateBotResponse(message);
      addMessage(botResponse, 'bot');
    }, 1000 + Math.random() * 2000); // Random delay between 1-3 seconds
  }

  // Send message on button click
  sendButton.addEventListener('click', sendMessage);

  // Send message on Enter key
  chatbotInput.addEventListener('keypress', (e) => {
    if (e.key === 'Enter') {
      sendMessage();
    }
  });

  // Quick action buttons
  quickActionBtns.forEach(btn => {
    btn.addEventListener('click', () => {
      const action = btn.dataset.action;
      const actionText = btn.textContent.trim();
      
      // Add user message
      addMessage(actionText, 'user');
      
      // Show typing indicator
      showTypingIndicator();
      
      // Generate response based on action
      setTimeout(() => {
        hideTypingIndicator();
        const response = generateQuickActionResponse(action);
        addMessage(response, 'bot');
      }, 1500);
    });
  });

  // Add message to chat
  function addMessage(text, sender) {
    const messageDiv = document.createElement('div');
    messageDiv.className = `message ${sender}-message`;
    
    const currentTime = new Date().toLocaleTimeString('vi-VN', { 
      hour: '2-digit', 
      minute: '2-digit' 
    });

    if (sender === 'bot') {
      messageDiv.innerHTML = `
        <div class="message-avatar">
          <i class="fas fa-robot"></i>
        </div>
        <div class="message-content">
          <div class="message-bubble">${text}</div>
          <div class="message-time">${currentTime}</div>
        </div>
      `;
    } else {
      messageDiv.innerHTML = `
        <div class="message-avatar">
          <i class="fas fa-user"></i>
        </div>
        <div class="message-content">
          <div class="message-bubble">${text}</div>
          <div class="message-time">${currentTime}</div>
        </div>
      `;
    }

    chatbotMessages.appendChild(messageDiv);
    chatbotMessages.scrollTop = chatbotMessages.scrollHeight;
  }

  // Show typing indicator
  function showTypingIndicator() {
    chatbotTyping.style.display = 'flex';
    sendButton.disabled = true;
  }

  // Hide typing indicator
  function hideTypingIndicator() {
    chatbotTyping.style.display = 'none';
    sendButton.disabled = false;
  }

  // Generate bot response based on user input
  function generateBotResponse(userMessage) {
    const message = userMessage.toLowerCase();
    
    // Greetings
    if (message.includes('xin chào') || message.includes('hello') || message.includes('hi')) {
      return 'Xin chào! 😊 Tôi là trợ lý ảo của LoLo Sport. Tôi có thể giúp bạn tìm kiếm sản phẩm, tư vấn kích cỡ, hoặc hỗ trợ đặt hàng. Bạn cần hỗ trợ gì ạ?';
    }
    
    // Product search
    if (message.includes('sản phẩm') || message.includes('tìm') || message.includes('mua')) {
      return 'Tôi có thể giúp bạn tìm sản phẩm! 🛍️ Bạn đang quan tâm đến loại sản phẩm nào? Ví dụ: giày thể thao, áo tập gym, dụng cụ tập luyện, v.v. Hoặc bạn có thể sử dụng thanh tìm kiếm ở trên để tìm kiếm trực tiếp.';
    }
    
    // Size guide
    if (message.includes('size') || message.includes('kích cỡ') || message.includes('số') || message.includes('đo')) {
      return `Hướng dẫn chọn size: 📏
      
• **Giày thể thao**: Đo chiều dài bàn chân và cộng thêm 0.5-1cm
• **Áo quần**: Tham khảo bảng size chi tiết trong mỗi sản phẩm  
• **Phụ kiện**: Thông tin size được ghi rõ trong mô tả

Bạn cần tư vấn size cho sản phẩm nào cụ thể không ạ?`;
    }
    
    // Order support
    if (message.includes('đặt hàng') || message.includes('mua hàng') || message.includes('thanh toán')) {
      return `Quy trình đặt hàng dễ dàng: ✨
      
1. Thêm sản phẩm vào giỏ hàng
2. Xem lại giỏ hàng và chọn "Thanh toán"
3. Nhập thông tin giao hàng
4. Chọn phương thức thanh toán
5. Xác nhận đơn hàng

Bạn cần hỗ trợ thêm về quy trình nào không ạ?`;
    }
    
    // Shipping
    if (message.includes('giao hàng') || message.includes('ship') || message.includes('vận chuyển')) {
      return `Thông tin giao hàng: 🚚
      
• **Miễn phí ship** đơn hàng từ 500.000đ
• **Giao hàng nhanh** trong nội thành 1-2 ngày
• **Giao hàng toàn quốc** 2-5 ngày làm việc
• **Thanh toán khi nhận hàng (COD)** được hỗ trợ

Bạn có câu hỏi gì khác về giao hàng không ạ?`;
    }
    
    // Return policy
    if (message.includes('đổi trả') || message.includes('bảo hành') || message.includes('chính sách')) {
      return `Chính sách đổi trả: 🔄
      
• **Đổi trả miễn phí** trong 30 ngày
• Sản phẩm chưa qua sử dụng, còn nguyên tag
• **Bảo hành** 6-12 tháng tùy theo sản phẩm
• Hỗ trợ đổi size miễn phí lần đầu

Bạn cần hỗ trợ đổi trả sản phẩm nào không ạ?`;
    }
    
    // Contact
    if (message.includes('liên hệ') || message.includes('hotline') || message.includes('số điện thoại')) {
      return `Thông tin liên hệ: 📞
      
• **Hotline**: (028) 3835 4266
• **Email**: support@lolosport.com  
• **Địa chỉ**: 227 Nguyễn Văn Cừ, Quận 5, TP.HCM
• **Giờ làm việc**: 8:00 - 22:00 hằng ngày

Chúng tôi luôn sẵn sàng hỗ trợ bạn! 😊`;
    }
    
    // Thanks
    if (message.includes('cảm ơn') || message.includes('thanks') || message.includes('tks')) {
      return 'Không có gì ạ! 😊 Rất vui được hỗ trợ bạn. Nếu bạn có thêm câu hỏi gì khác, đừng ngần ngại hỏi tôi nhé!';
    }
    
    // Default response
    const defaultResponses = [
      'Tôi hiểu bạn đang cần hỗ trợ. Bạn có thể nói rõ hơn để tôi giúp bạn tốt nhất có thể được không ạ? 😊',
      'Xin lỗi, tôi chưa hiểu rõ câu hỏi của bạn. Bạn có thể sử dụng các nút tắt bên dưới hoặc liên hệ trực tiếp với nhân viên tư vấn qua hotline (028) 3835 4266.',
      'Tôi đang học hỏi thêm để hỗ trợ bạn tốt hơn. Hiện tại bạn có thể hỏi tôi về sản phẩm, size, đặt hàng, hoặc chính sách của cửa hàng nhé! 💪'
    ];
    
    return defaultResponses[Math.floor(Math.random() * defaultResponses.length)];
  }

  // Generate quick action responses
  function generateQuickActionResponse(action) {
    switch (action) {
      case 'products':
        return `Sản phẩm HOT nhất hiện tại: 🔥
        
• **Giày Nike Air Max** - Giảm 30%
• **Áo tập Under Armour** - Mẫu mới 2024
• **Găng tay tập gym Adidas** - Best seller
• **Bình nước thể thao** - Miễn phí ship

<a href="/Product" class="text-primary">👉 Xem tất cả sản phẩm</a>`;
        
      case 'size-guide':
        return `Bảng hướng dẫn size chi tiết: 📐
        
**Giày thể thao (cm):**
• Size 39: 24.5cm | Size 40: 25.5cm
• Size 41: 26.0cm | Size 42: 27.0cm
• Size 43: 27.5cm | Size 44: 28.5cm

**Quần áo:**
• **S**: Ngực 86-91cm, Eo 71-76cm
• **M**: Ngực 91-96cm, Eo 76-81cm  
• **L**: Ngực 96-101cm, Eo 81-86cm

💡 *Tip: Đo vào buổi chiều để có kết quả chính xác nhất!*`;
        
      case 'support':
        return `Đội ngũ hỗ trợ LoLo Sport: 👥
        
🔹 **Chat trực tuyến**: 24/7 (như bây giờ)
🔹 **Hotline**: (028) 3835 4266  
🔹 **Email**: support@lolosport.com
🔹 **Facebook**: LoLoSport Official

**Thời gian phản hồi:**
• Chat/Call: Ngay lập tức
• Email: Trong 2 giờ làm việc

Bạn cần hỗ trợ gì cụ thể không ạ? 😊`;
        
      default:
        return 'Tôi đang xử lý yêu cầu của bạn... 🤔';
    }
  }
}

console.log("Sporty AI Store - All systems loaded! 🏃‍♂️⚡")