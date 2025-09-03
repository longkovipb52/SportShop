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

// AI Chat Button Functionality
document.addEventListener("click", (e) => {
  if (e.target.textContent.includes("Chat with AI")) {
    e.preventDefault()
    showNotification("AI Assistant is coming soon! Stay tuned for personalized recommendations.", "info")
  }
})

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

// Product Search Functionality
function initProductSearch() {
  const searchInputs = document.querySelectorAll('.search-input');
  
  searchInputs.forEach(searchInput => {
    if (searchInput) {
      // Hiệu ứng nhấp nháy placeholder
      const searchTerms = ['Giày thể thao', 'Áo bóng đá', 'Quần thể thao', 'Phụ kiện tập gym'];
      let currentIndex = 0;
      
      // Hiệu ứng đổi placeholder
      setInterval(() => {
        searchInput.setAttribute('placeholder', searchTerms[currentIndex]);
        currentIndex = (currentIndex + 1) % searchTerms.length;
      }, 3000);
      
      // Xử lý sự kiện nhập
      searchInput.addEventListener('input', function() {
        const searchTerm = this.value.toLowerCase();
        if (searchTerm.length > 2) {
          showSearchSuggestions(searchTerm, this);
        } else {
          // Ẩn suggestions nếu từ khóa quá ngắn
          const suggestionsContainer = this.parentElement.querySelector('.search-suggestions');
          if (suggestionsContainer) {
            suggestionsContainer.style.display = 'none';
          }
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
    if (!e.target.closest('.search-container')) {
      const suggestions = document.querySelectorAll('.search-suggestions');
      suggestions.forEach(el => el.style.display = 'none');
    }
  });
}

// Hiển thị gợi ý tìm kiếm
function showSearchSuggestions(term, inputElement) {
  // Kiểm tra xem đã có suggestions chưa
  let suggestionsContainer = inputElement.parentElement.querySelector('.search-suggestions');
  
  // Nếu chưa có, tạo mới
  if (!suggestionsContainer) {
    suggestionsContainer = document.createElement('div');
    suggestionsContainer.className = 'search-suggestions';
    inputElement.parentElement.appendChild(suggestionsContainer);
  }
  
  // Giả lập API tìm kiếm với một số kết quả mẫu
  const mockProducts = [
    { id: 1, name: 'Giày Nike Air Max 270', price: '2.890.000đ', image: '/img/products/shoe1.jpg' },
    { id: 2, name: 'Áo đấu Manchester United', price: '1.250.000đ', image: '/img/products/shirt1.jpg' },
    { id: 3, name: 'Quần thể thao Adidas', price: '850.000đ', image: '/img/products/pants1.jpg' },
    { id: 4, name: 'Balo thể thao Nike', price: '1.350.000đ', image: '/img/products/bag1.jpg' }
  ];
  
  // Lọc sản phẩm theo từ khóa
  const filteredProducts = mockProducts.filter(product => 
    product.name.toLowerCase().includes(term)
  );
  
  // Hiển thị kết quả
  if (filteredProducts.length > 0) {
    let html = '';
    
    filteredProducts.forEach(product => {
      html += `
        <div class="suggestion-item" data-id="${product.id}">
          <div class="suggestion-img-container">
            <div style="width: 40px; height: 40px; background-color: #f0f0f0; border-radius: 5px;"></div>
          </div>
          <div class="ms-2">
            <div class="suggestion-name">${product.name}</div>
            <div class="suggestion-price">${product.price}</div>
          </div>
        </div>
      `;
    });
    
    suggestionsContainer.innerHTML = html;
    suggestionsContainer.style.display = 'block';
    
    // Thêm sự kiện click cho các suggestion
    const suggestionItems = suggestionsContainer.querySelectorAll('.suggestion-item');
    suggestionItems.forEach(item => {
      item.addEventListener('click', () => {
        const productId = item.getAttribute('data-id');
        console.log(`Navigating to product ${productId}`);
        // Trong thực tế: window.location.href = `/product/${productId}`;
        
        // Hiển thị thông báo
        showNotification(`Đang chuyển đến trang sản phẩm: ${item.querySelector('.suggestion-name').textContent}`, 'success');
        
        // Đóng suggestions
        suggestionsContainer.style.display = 'none';
      });
    });
  } else {
    suggestionsContainer.innerHTML = '<div class="p-2 text-muted">Không tìm thấy sản phẩm phù hợp</div>';
    suggestionsContainer.style.display = 'block';
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
    
    // Thêm hiệu ứng fade khi chuyển slide
    productCarousel.addEventListener('slide.bs.carousel', function (e) {
      const activeItem = this.querySelector('.carousel-item.active');
      const nextItem = e.relatedTarget;
      
      // Áp dụng hiệu ứng fade out cho item hiện tại
      gsap.to(activeItem, { opacity: 0, duration: 0.5 });
      
      // Áp dụng hiệu ứng fade in cho item tiếp theo
      gsap.fromTo(nextItem, 
        { opacity: 0 }, 
        { opacity: 1, duration: 0.5, delay: 0.3 }
      );
    });

    // Tự động cuộn carousel sau mỗi khoảng thời gian
    setInterval(function() {
      carousel.next();
    }, 5000);
  }
}

console.log("Sporty AI Store - All systems loaded! 🏃‍♂️⚡")