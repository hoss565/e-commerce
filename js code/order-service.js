class OrderService {
    constructor() {
        this.baseUrl = 'http://localhost:5037';
        this.apiUrl = `${this.baseUrl}/api`;
        this.requestTimeout = 30000; // 30 seconds timeout
        this.maxRetries = 3;
    }

    getAuthHeaders() {
        const token = localStorage.getItem('userToken');
        if (!token) {
            throw new Error('Please log in to continue');
        }
        return {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json'
        };
    }

    formatCurrency(amount) {
        return `$ ${Number(amount || 0).toFixed(2)}`;
    }

    formatDate(dateString) {
        const date = new Date(dateString);
        return date.toLocaleDateString('en-US', {
            year: 'numeric',
            month: 'long',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    }

    getStatusColor(status) {
        const colors = {
            'Pending': '#f59e0b',
            'Processing': '#3b82f6',
            'Shipped': '#10b981',
            'Delivered': '#059669',
            'Cancelled': '#ef4444'
        };
        return colors[status] || '#6b7280';
    }

    getPaymentStatusColor(status) {
        const colors = {
            'Pending': '#f59e0b',
            'Paid': '#10b981',
            'Failed': '#ef4444',
            'Refunded': '#6366f1'
        };
        return colors[status] || '#6b7280';
    }

    getImagePath(imagePath) {
        if (!imagePath) {
            return `${this.baseUrl}/images/default-product.jpg`;
        }
        // Remove any leading slashes to avoid double slashes in URL
        const cleanPath = imagePath.replace(/^\/+/, '');
        return imagePath.startsWith('http') ? imagePath : `${this.baseUrl}/${cleanPath}`;
    }

    async fetchWithTimeout(url, options) {
        const controller = new AbortController();
        const timeout = setTimeout(() => controller.abort(), this.requestTimeout);
        
        try {
            const response = await fetch(url, {
                ...options,
                signal: controller.signal
            });
            clearTimeout(timeout);
            return response;
        } catch (error) {
            clearTimeout(timeout);
            if (error.name === 'AbortError') {
                throw new Error('Request timed out. Please try again.');
            }
            throw error;
        }
    }

    async retryOperation(operation, retries = this.maxRetries) {
        for (let i = 0; i < retries; i++) {
            try {
                return await operation();
            } catch (error) {
                if (i === retries - 1) throw error;
                await new Promise(resolve => setTimeout(resolve, 1000 * Math.pow(2, i)));
            }
        }
    }

    async createOrder(orderData) {
        try {
            return await this.retryOperation(async () => {
                const response = await this.fetchWithTimeout(
                    `${this.apiUrl}/Orders`,
                    {
                        method: 'POST',
                        headers: this.getAuthHeaders(),
                        body: JSON.stringify(orderData)
                    }
                );

                if (response.status === 401) {
                    window.location.href = 'login.html';
                    return;
                }

                if (!response.ok) {
                    const error = await response.text();
                    throw new Error(error || 'Failed to create order');
                }

                return await response.json();
            });
        } catch (error) {
            console.error('Error creating order:', error);
            throw error;
        }
    }

    async getUserOrders(userId) {
        try {
            return await this.retryOperation(async () => {
                const response = await this.fetchWithTimeout(
                    `${this.apiUrl}/Orders/user/${userId}`,
                    {
                        method: 'GET',
                        headers: this.getAuthHeaders()
                    }
                );

                if (response.status === 401) {
                    window.location.href = 'login.html';
                    return;
                }

                if (!response.ok) {
                    const error = await response.text();
                    throw new Error(error || 'Failed to fetch orders');
                }

                return await response.json();
            });
        } catch (error) {
            console.error('Error fetching orders:', error);
            throw error;
        }
    }
}

// Create a global instance
const orderService = new OrderService();
