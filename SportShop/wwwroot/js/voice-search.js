// Voice Search Functionality for SportShop
class VoiceSearchManager {
    constructor() {
        this.recognition = null;
        this.isListening = false;
        this.listeningTimeout = null; // Thêm timeout
        this.initializeVoiceSearch();
    }

    initializeVoiceSearch() {
        // Kiểm tra browser support
        if (!('webkitSpeechRecognition' in window) && !('SpeechRecognition' in window)) {
            console.warn('Voice search không được hỗ trợ trên trình duyệt này');
            return;
        }

        // Initialize Speech Recognition
        const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
        this.recognition = new SpeechRecognition();
        
        // Configure recognition settings
        this.recognition.continuous = false;
        this.recognition.interimResults = false;
        this.recognition.lang = 'vi-VN'; // Vietnamese first
        this.recognition.maxAlternatives = 3;

        // Setup event listeners
        this.setupEventListeners();
        this.setupVoiceButtons();
    }

    setupEventListeners() {
        if (!this.recognition) return;

        // When speech recognition starts
        this.recognition.onstart = () => {
            console.log('Voice search started');
            this.onSpeechStart();
        };

        // When speech recognition ends
        this.recognition.onend = () => {
            console.log('Voice search ended');
            this.onSpeechEnd();
        };

        // When speech is recognized
        this.recognition.onresult = (event) => {
            console.log('Speech recognition result:', event);
            this.onSpeechResult(event);
        };

        // When an error occurs
        this.recognition.onerror = (event) => {
            console.log('Speech recognition error:', event.error); // Log thay vì error
            this.onSpeechError(event);
        };

        // ✅ Cải thiện onnomatch handler
        this.recognition.onnomatch = () => {
            console.log('No speech was recognized clearly');
            // Không hiển thị thông báo cho nomatch vì nó tương tự no-speech
            // this.showVoiceMessage('Không nhận diện được giọng nói rõ ràng. Vui lòng thử lại.', 'info');
        };
    }

    setupVoiceButtons() {
        // Add voice buttons to all search inputs
        const searchForms = document.querySelectorAll('.search-form');
        
        searchForms.forEach(form => {
            this.addVoiceButtonToForm(form);
        });
    }

    addVoiceButtonToForm(form) {
        const input = form.querySelector('.search-input');
        const searchBtn = form.querySelector('.btn-search');
        
        if (!input || !searchBtn) return;

        // Create voice button
        const voiceBtn = document.createElement('button');
        voiceBtn.type = 'button';
        voiceBtn.className = 'btn btn-voice';
        voiceBtn.innerHTML = '<i class="fas fa-microphone"></i>';
        voiceBtn.title = 'Tìm kiếm bằng giọng nói';
        voiceBtn.setAttribute('data-bs-toggle', 'tooltip');
        voiceBtn.setAttribute('data-bs-placement', 'top');

        // Insert voice button before search button
        searchBtn.parentNode.insertBefore(voiceBtn, searchBtn);

        // Add event listener
        voiceBtn.addEventListener('click', (e) => {
            e.preventDefault();
            this.startVoiceSearch(input, voiceBtn);
        });

        // Initialize tooltip if Bootstrap is available
        if (window.bootstrap && bootstrap.Tooltip) {
            new bootstrap.Tooltip(voiceBtn);
        }
    }

    startVoiceSearch(targetInput, voiceButton) {
        if (!this.recognition) {
            console.log('Voice search not supported on this browser');
            return;
        }

        if (this.isListening) {
            this.stopVoiceSearch();
            return;
        }

        // Store current target input
        this.currentInput = targetInput;
        this.currentButton = voiceButton;

        // Request microphone permission and start recognition
        try {
            this.recognition.start();
        } catch (error) {
            console.error('Error starting voice recognition:', error);
            // ✅ Loại bỏ notification lỗi
        }
    }

    stopVoiceSearch() {
        if (this.recognition && this.isListening) {
            try {
                this.recognition.abort(); // Sử dụng abort thay vì stop để tránh trigger error event
            } catch (error) {
                console.log('Error stopping voice recognition:', error);
            }
        }
    }

    onSpeechStart() {
        this.isListening = true;
        
        // Update UI
        if (this.currentButton) {
            this.currentButton.classList.add('listening');
            this.currentButton.innerHTML = '<i class="fas fa-microphone-slash"></i>';
            this.currentButton.title = 'Nhấn để dừng';
        }

        // Show listening modal
        this.showListeningModal();
        
        // Show placeholder text
        if (this.currentInput) {
            this.currentInput.placeholder = 'Đang nghe... Hãy nói tên sản phẩm bạn muốn tìm';
        }

        // ✅ Thêm timeout để tự động dừng sau 10 giây
        this.listeningTimeout = setTimeout(() => {
            if (this.isListening) {
                console.log('Voice search timeout - stopping automatically');
                this.stopVoiceSearch();
                // ✅ Loại bỏ notification timeout
            }
        }, 10000); // 10 giây
    }

    onSpeechEnd() {
        this.isListening = false;
        
        // ✅ Clear timeout khi kết thúc
        if (this.listeningTimeout) {
            clearTimeout(this.listeningTimeout);
            this.listeningTimeout = null;
        }
        
        // Reset UI
        if (this.currentButton) {
            this.currentButton.classList.remove('listening');
            this.currentButton.innerHTML = '<i class="fas fa-microphone"></i>';
            this.currentButton.title = 'Tìm kiếm bằng giọng nói';
        }

        // Hide listening modal
        this.hideListeningModal();
        
        // Reset placeholder
        if (this.currentInput) {
            this.currentInput.placeholder = 'Tìm kiếm sản phẩm...';
        }
    }

    onSpeechResult(event) {
        const results = event.results;
        if (results.length > 0) {
            const transcript = results[0][0].transcript;
            const confidence = results[0][0].confidence;
            
            console.log('Recognized speech:', transcript, 'Confidence:', confidence);
            
            // Process the recognized text
            this.processSpeechResult(transcript, confidence);
        }
    }

    onSpeechError(event) {
        // ✅ Chỉ reset UI nếu không phải do user cancel
        if (event.error !== 'aborted') {
            this.onSpeechEnd(); // Reset UI (sẽ clear timeout)
        } else {
            // Nếu là aborted, chỉ reset UI không hiển thị thông báo
            this.resetVoiceUI();
        }
        
        // ✅ Loại bỏ tất cả notifications - chỉ log ra console để debug
        switch (event.error) {
            case 'network':
                console.log('Voice search network error');
                break;
            case 'not-allowed':
                console.log('Microphone permission not allowed');
                break;
            case 'no-speech':
                console.log('No speech detected - this is normal if user doesn\'t speak');
                break;
            case 'audio-capture':
                console.log('Audio capture error');
                break;
            case 'service-not-allowed':
                console.log('Speech recognition service not allowed');
                break;
            case 'aborted':
                console.log('Voice search was aborted by user');
                break;
            default:
                console.log('Voice search error:', event.error);
                break;
        }
        
        // ✅ Không hiển thị thông báo nào cả
    }

    processSpeechResult(transcript, confidence) {
        // Clean up the transcript
        const cleanedText = this.cleanTranscript(transcript);
        
        // Set the search input value
        if (this.currentInput) {
            this.currentInput.value = cleanedText;
            
            // Trigger input event for suggestions
            this.currentInput.dispatchEvent(new Event('input', { bubbles: true }));
        }
        
        // ✅ Loại bỏ notification - chỉ log để debug
        const confidencePercentage = Math.round(confidence * 100);
        console.log(`Voice search result: "${cleanedText}" (${confidencePercentage}% confidence)`);
        
        // ✅ Tự động search luôn mà không cần thông báo
        if (confidencePercentage >= 70) { // Giảm threshold xuống 70%
            // Auto-search if confidence is decent
            setTimeout(() => {
                this.performSearch();
            }, 500); // Giảm delay xuống 0.5s để nhanh hơn
        }
    }

    cleanTranscript(transcript) {
        // Remove common speech artifacts and clean up text
        let cleaned = transcript.trim().toLowerCase();
        
        // Remove punctuation
        cleaned = cleaned.replace(/[.,!?;:]/g, '');
        
        // Convert common Vietnamese speech patterns
        const replacements = {
            'tìm kiếm': '',
            'tìm': '',
            'mua': '',
            'cần': '',
            'muốn': '',
            'giày đá banh': 'giày đá bóng',
            'giày bóng đá': 'giày đá bóng',
            'áo bóng đá': 'áo đá bóng',
            'quần short': 'quần sort',
            'adidass': 'adidas',
            'nai ki': 'nike',
            'pumar': 'puma'
        };
        
        Object.keys(replacements).forEach(key => {
            cleaned = cleaned.replace(new RegExp(key, 'gi'), replacements[key]);
        });
        
        return cleaned.trim();
    }

    performSearch() {
        if (this.currentInput && this.currentInput.value.trim()) {
            // Submit the form
            const form = this.currentInput.closest('form');
            if (form) {
                form.submit();
            }
        }
    }

    showListeningModal() {
        // Create or show listening modal
        let modal = document.getElementById('voiceSearchModal');
        
        if (!modal) {
            modal = this.createListeningModal();
            document.body.appendChild(modal);
        }
        
        // Show modal
        if (window.bootstrap && bootstrap.Modal) {
            const bsModal = new bootstrap.Modal(modal);
            bsModal.show();
            this.currentModal = bsModal;
            
            // ✅ Xử lý khi modal bị đóng
            modal.addEventListener('hidden.bs.modal', () => {
                this.handleModalClose();
            });
        } else {
            modal.style.display = 'block';
            
            // ✅ Xử lý cho fallback modal
            const closeBtn = modal.querySelector('[data-bs-dismiss="modal"]');
            if (closeBtn) {
                closeBtn.addEventListener('click', () => {
                    this.handleModalClose();
                    modal.style.display = 'none';
                });
            }
        }
    }

    hideListeningModal() {
        if (this.currentModal) {
            this.currentModal.hide();
        } else {
            const modal = document.getElementById('voiceSearchModal');
            if (modal) {
                modal.style.display = 'none';
            }
        }
    }

    // ✅ Thêm method để xử lý khi modal bị đóng
    handleModalClose() {
        console.log('Modal closed - stopping voice recognition');
        if (this.isListening) {
            // Dừng voice recognition khi modal đóng
            this.stopVoiceSearch();
            // Reset UI manually vì có thể onSpeechEnd không được gọi
            this.resetVoiceUI();
        }
    }

    // ✅ Thêm method để reset UI
    resetVoiceUI() {
        this.isListening = false;
        
        // Clear timeout nếu có
        if (this.listeningTimeout) {
            clearTimeout(this.listeningTimeout);
            this.listeningTimeout = null;
        }
        
        // Reset button UI
        if (this.currentButton) {
            this.currentButton.classList.remove('listening');
            this.currentButton.innerHTML = '<i class="fas fa-microphone"></i>';
            this.currentButton.title = 'Tìm kiếm bằng giọng nói';
        }
        
        // Reset placeholder
        if (this.currentInput) {
            this.currentInput.placeholder = 'Tìm kiếm sản phẩm...';
        }
    }

    createListeningModal() {
        const modal = document.createElement('div');
        modal.id = 'voiceSearchModal';
        modal.className = 'modal fade';
        modal.tabIndex = -1;
        modal.innerHTML = `
            <div class="modal-dialog modal-dialog-centered">
                <div class="modal-content">
                    <div class="modal-body text-center py-5">
                        <div class="voice-animation mb-4">
                            <div class="voice-pulse">
                                <i class="fas fa-microphone fa-3x text-primary"></i>
                            </div>
                        </div>
                        <h5 class="mb-3">🎤 Đang lắng nghe...</h5>
                        <p class="text-muted mb-3">Hãy nói rõ ràng tên sản phẩm thể thao bạn muốn tìm</p>
                        <div class="voice-examples mb-4">
                            <small class="text-muted">
                                <strong>Ví dụ:</strong> "Giày Nike Air Max", "Áo đá bóng Adidas", "Quần thể thao"
                            </small>
                        </div>
                        <div class="mb-3">
                            <small class="text-info">
                                <i class="fas fa-info-circle me-1"></i>
                                Tự động dừng sau 10 giây nếu không có giọng nói
                            </small>
                        </div>
                        <button type="button" class="btn btn-outline-secondary cancel-voice-btn" data-bs-dismiss="modal">
                            <i class="fas fa-times me-2"></i>Hủy
                        </button>
                    </div>
                </div>
            </div>
        `;
        
        // ✅ Thêm event listener cho nút hủy ngay khi tạo modal
        const cancelBtn = modal.querySelector('.cancel-voice-btn');
        if (cancelBtn) {
            cancelBtn.addEventListener('click', () => {
                console.log('Cancel button clicked');
                this.handleModalClose();
            });
        }
        
        return modal;
    }

    // ✅ Tạm thời disable notification - có thể enable lại sau
    showVoiceMessage(message, type = 'info') {
        // Chỉ log ra console thay vì hiển thị notification
        console.log(`Voice Search ${type}: ${message}`);
        
        // Comment out notification code
        /*
        // Không hiển thị duplicate messages
        const existingToasts = document.querySelectorAll('.voice-toast');
        for (let toast of existingToasts) {
            if (toast.textContent.includes(message.substring(0, 20))) {
                return; // Đã có thông báo tương tự
            }
        }

        // Use existing notification system or create toast
        if (typeof showNotification === 'function') {
            showNotification(message, type);
        } else {
            // Fallback toast
            this.showToast(message, type);
        }
        */
    }

    // ✅ Thêm method để kiểm tra modal có đang mở không
    isModalOpen() {
        const modal = document.getElementById('voiceSearchModal');
        if (!modal) return false;
        
        if (window.bootstrap && bootstrap.Modal) {
            const modalInstance = bootstrap.Modal.getInstance(modal);
            return modalInstance && modalInstance._isShown;
        } else {
            return modal.style.display === 'block';
        }
    }

    // ✅ Tạm thời disable toast notifications
    showToast(message, type) {
        // Comment out toast creation - chỉ log để debug
        console.log(`Voice Toast ${type}: ${message}`);
        
        /*
        // Create a simple toast notification
        const toast = document.createElement('div');
        toast.className = `toast voice-toast show position-fixed top-0 end-0 m-3`;
        toast.style.zIndex = '9999';
        
        let bgColor = 'bg-primary';
        let icon = 'fa-info-circle';
        
        switch (type) {
            case 'success':
                bgColor = 'bg-success';
                icon = 'fa-check-circle';
                break;
            case 'error':
                bgColor = 'bg-danger';
                icon = 'fa-exclamation-circle';
                break;
            case 'warning':
                bgColor = 'bg-warning';
                icon = 'fa-exclamation-triangle';
                break;
        }
        
        toast.innerHTML = `
            <div class="toast-header ${bgColor} text-white">
                <i class="fas ${icon} me-2"></i>
                <strong class="me-auto">Tìm kiếm giọng nói</strong>
                <button type="button" class="btn-close btn-close-white" onclick="this.closest('.toast').remove()"></button>
            </div>
            <div class="toast-body">
                ${message}
            </div>
        `;
        
        document.body.appendChild(toast);
        
        // Auto remove after 4 seconds
        setTimeout(() => {
            if (toast.parentNode) {
                toast.parentNode.removeChild(toast);
            }
        }, 4000);
        */
    }

    // Public method to check if voice search is supported
    isSupported() {
        return !!(window.SpeechRecognition || window.webkitSpeechRecognition);
    }

    // Public method to get support info
    getSupportInfo() {
        if (this.isSupported()) {
            return {
                supported: true,
                message: 'Trình duyệt hỗ trợ tìm kiếm bằng giọng nói'
            };
        } else {
            return {
                supported: false,
                message: 'Trình duyệt không hỗ trợ tìm kiếm bằng giọng nói. Vui lòng sử dụng Chrome, Edge hoặc Safari.'
            };
        }
    }
}

// Initialize voice search when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    // Create global voice search instance
    window.voiceSearchManager = new VoiceSearchManager();
    
    // Add support info to console
    const supportInfo = window.voiceSearchManager.getSupportInfo();
    console.log('🎤 Voice Search:', supportInfo.message);
});