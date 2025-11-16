// AI-Powered Chatbot for LoLoSport
class AIChatbot {
    constructor() {
        this.initializeElements();
        this.setupEventListeners();
        this.conversationHistory = [];
    }

    initializeElements() {
        this.chatbotBtn = document.getElementById('chatbotBtn');
        this.chatbotWidget = document.getElementById('chatbotWidget');
        this.closeChatbot = document.getElementById('closeChatbot');
        this.minimizeChatbot = document.getElementById('minimizeChatbot');
        this.chatbotInput = document.getElementById('chatbotInput');
        this.sendButton = document.getElementById('sendMessage');
        this.chatbotMessages = document.getElementById('chatbotMessages');
        this.chatbotTyping = document.getElementById('chatbotTyping');
    }

    setupEventListeners() {
        if (this.chatbotBtn) {
            this.chatbotBtn.addEventListener('click', () => this.toggleChatbot());
        }
        
        if (this.closeChatbot) {
            this.closeChatbot.addEventListener('click', () => this.hideChatbot());
        }
        
        if (this.minimizeChatbot) {
            this.minimizeChatbot.addEventListener('click', () => this.minimizeWidget());
        }
        
        if (this.sendButton) {
            this.sendButton.addEventListener('click', () => this.sendMessage());
        }
        
        if (this.chatbotInput) {
            this.chatbotInput.addEventListener('keypress', (e) => {
                if (e.key === 'Enter') this.sendMessage();
            });
        }

        // Quick action buttons
        document.querySelectorAll('.quick-action-btn').forEach(btn => {
            btn.addEventListener('click', () => {
                const action = btn.dataset.action;
                this.handleQuickAction(action);
            });
        });
    }

    toggleChatbot() {
        const isVisible = this.chatbotWidget.style.display === 'flex';
        if (isVisible) {
            this.hideChatbot();
        } else {
            this.showChatbot();
        }
    }

    showChatbot() {
        this.chatbotWidget.style.display = 'flex';
        this.chatbotWidget.classList.remove('minimized');
        if (this.chatbotInput) {
            this.chatbotInput.focus();
        }
    }

    hideChatbot() {
        this.chatbotWidget.style.display = 'none';
    }

    minimizeWidget() {
        this.chatbotWidget.classList.toggle('minimized');
        if (this.chatbotWidget.classList.contains('minimized')) {
            this.minimizeChatbot.innerHTML = '<i class="fas fa-window-maximize"></i>';
        } else {
            this.minimizeChatbot.innerHTML = '<i class="fas fa-minus"></i>';
            if (this.chatbotInput) {
                this.chatbotInput.focus();
            }
        }
    }

    async sendMessage() {
        const message = this.chatbotInput.value.trim();
        if (!message) return;

        // Add user message to UI
        this.addMessage(message, 'user');
        this.chatbotInput.value = '';

        // Show typing indicator
        this.showTypingIndicator();

        try {
            // Call API
            const response = await fetch('/api/Chatbot/chat', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ message: message })
            });

            if (!response.ok) {
                throw new Error('Network response was not ok');
            }

            const data = await response.json();
            
            // Hide typing indicator
            this.hideTypingIndicator();

            // Add bot response to UI
            this.addMessage(data.message, 'bot');

            // Save to conversation history
            this.conversationHistory.push({
                user: message,
                bot: data.message,
                timestamp: new Date()
            });

        } catch (error) {
            console.error('Chatbot error:', error);
            this.hideTypingIndicator();
            this.addMessage(
                'Xin lỗi, tôi đang gặp chút vấn đề. Vui lòng thử lại sau hoặc liên hệ hotline (028) 3835 4266. 😊',
                'bot'
            );
        }
    }

    addMessage(text, sender) {
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
                    <div class="message-bubble">${this.formatMessage(text)}</div>
                    <div class="message-time">${currentTime}</div>
                </div>
            `;
        } else {
            messageDiv.innerHTML = `
                <div class="message-content">
                    <div class="message-bubble">${text}</div>
                    <div class="message-time">${currentTime}</div>
                </div>
                <div class="message-avatar">
                    <i class="fas fa-user"></i>
                </div>
            `;
        }

        this.chatbotMessages.appendChild(messageDiv);
        this.chatbotMessages.scrollTop = this.chatbotMessages.scrollHeight;
    }

    formatMessage(text) {
        // Convert markdown-like formatting
        text = text.replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>'); // Bold
        text = text.replace(/\*(.*?)\*/g, '<em>$1</em>'); // Italic
        text = text.replace(/\n/g, '<br>'); // Line breaks
        
        // Convert custom image format [IMAGE:url] to HTML
        text = text.replace(/\[IMAGE:([^\]]+)\]/g, '<img src="$1" alt="Hình ảnh sản phẩm" class="chatbot-image img-fluid rounded" style="max-width: 200px; max-height: 200px;">');
        
        // Convert custom image format <IMAGE>url</IMAGE> to HTML
        text = text.replace(/<IMAGE>([^<]+)<\/IMAGE>/g, '<img src="$1" alt="Hình ảnh sản phẩm" class="chatbot-image img-fluid rounded" style="max-width: 200px; max-height: 200px;">');
        
        // Convert markdown images ![alt](url) to HTML (fallback)
        text = text.replace(/!\[([^\]]*)\]\(([^)]+)\)/g, '<img src="$2" alt="$1" class="chatbot-image img-fluid rounded" style="max-width: 200px; max-height: 200px;">');
        
        // Convert markdown links [text](url) to HTML
        text = text.replace(/\[([^\]]+)\]\(([^)]+)\)/g, '<a href="$2" class="product-link">$1 <i class="fas fa-external-link-alt"></i></a>');
        
        return text;
    }

    showTypingIndicator() {
        if (this.chatbotTyping) {
            this.chatbotTyping.style.display = 'flex';
            this.chatbotMessages.scrollTop = this.chatbotMessages.scrollHeight;
        }
    }

    hideTypingIndicator() {
        if (this.chatbotTyping) {
            this.chatbotTyping.style.display = 'none';
        }
    }

    async handleQuickAction(action) {
        const actionMessages = {
            'products': 'Cho tôi xem sản phẩm HOT nhất hiện tại',
            'size-guide': 'Hướng dẫn tôi chọn size phù hợp',
            'support': 'Tôi cần hỗ trợ'
        };

        const message = actionMessages[action];
        if (message) {
            this.chatbotInput.value = message;
            await this.sendMessage();
        }
    }
}

// Initialize chatbot when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    const chatbot = new AIChatbot();
    console.log('AI Chatbot initialized! 🤖');
});
