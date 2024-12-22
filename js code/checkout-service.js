class CheckoutService {
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
        return `EGP ${amount.toFixed(2)}`;
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

    async fetchShippingAddresses() {
        try {
            const user = JSON.parse(localStorage.getItem('currentUser'));
            if (!user?.user?.userId) {
                throw new Error('User not found');
            }

            return await this.retryOperation(async () => {
                const response = await this.fetchWithTimeout(
                    `${this.apiUrl}/ShippingAddresses/user/${user.user.userId}`,
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
                    throw new Error(error || 'Failed to fetch shipping addresses');
                }

                return await response.json();
            });
        } catch (error) {
            console.error('Error fetching shipping addresses:', error);
            if (error.message.includes('Failed to fetch')) {
                throw new Error('Network error. Please check your connection and try again.');
            }
            return [];
        }
    }

    validateForm() {
        const requiredFields = {
            'phone': 'Phone Number',
            'streetAddress': 'Street Address',
            'city': 'City',
            'state': 'State'
        };

        const errors = [];
        Object.entries(requiredFields).forEach(([field, label]) => {
            const input = document.getElementById(field);
            if (!input || !input.value.trim()) {
                errors.push(`${label} is required`);
            }
        });

        // Validate phone format
        const phone = document.getElementById('phone');
        if (phone?.value && !this.isValidPhone(phone.value)) {
            errors.push('Please enter a valid Egyptian phone number (e.g., 01234567890)');
        }

        // Validate address length
        const streetAddress = document.getElementById('streetAddress');
        if (streetAddress?.value && (streetAddress.value.length < 5 || streetAddress.value.length > 200)) {
            errors.push('Street address must be between 5 and 200 characters');
        }

        return errors;
    }

    async saveShippingAddress() {
        const user = JSON.parse(localStorage.getItem('currentUser'));
        if (!user?.user?.userId) {
            throw new Error('User not found');
        }

        const phoneNumber = document.getElementById('phone').value.replace(/\s/g, '').trim();
        if (!this.isValidPhone(phoneNumber)) {
            throw new Error('Please enter a valid Egyptian phone number (e.g., 01234567890)');
        }

        const addressData = {
            userId: parseInt(user.user.userId),
            streetAddress: document.getElementById('streetAddress').value.trim(),
            city: document.getElementById('city').value.trim(),
            state: document.getElementById('state').value.trim(),
            country: 'Egypt',
            phone: phoneNumber,
            createdAt: new Date().toISOString(),
            updatedAt: new Date().toISOString()
        };

        return await this.retryOperation(async () => {
            const response = await this.fetchWithTimeout(
                `${this.apiUrl}/ShippingAddresses`,
                {
                    method: 'POST',
                    headers: this.getAuthHeaders(),
                    body: JSON.stringify(addressData)
                }
            );

            if (response.status === 401) {
                window.location.href = 'login.html';
                return;
            }

            if (!response.ok) {
                const error = await response.text();
                throw new Error(error || 'Failed to save shipping address');
            }

            return await response.json();
        });
    }

    isValidPhone(phone) {
        return /^01[0125][0-9]{8}$/.test(phone.replace(/\s/g, ''));
    }

    async placeOrder() {
        try {
            const token = localStorage.getItem('userToken');
            if (!token) {
                window.location.href = 'login.html';
                return;
            }

            const errors = this.validateForm();
            if (errors.length > 0) {
                throw new Error(errors.join('\n'));
            }

            const user = JSON.parse(localStorage.getItem('currentUser'));
            if (!user?.user?.userId) {
                window.location.href = 'login.html';
                return;
            }

            // Show loading state
            const button = document.getElementById('placeOrderBtn');
            button.disabled = true;
            button.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Processing...';

            try {
                const shippingAddress = await this.saveShippingAddress();
                console.log('Shipping address saved:', shippingAddress);

                const checkoutData = {
                    shippingAddressId: shippingAddress.shippingAddressId,
                    paymentMethod: 'CashOnDelivery'
                };

                const checkoutResponse = await this.retryOperation(async () => {
                    const response = await this.fetchWithTimeout(
                        `${this.apiUrl}/Cart/${user.user.userId}/checkout`,
                        {
                            method: 'POST',
                            headers: this.getAuthHeaders(),
                            body: JSON.stringify(checkoutData)
                        }
                    );

                    if (response.status === 401) {
                        window.location.href = 'login.html';
                        return;
                    }

                    if (!response.ok) {
                        const error = await response.text();
                        throw new Error(error || 'Failed to process checkout');
                    }

                    return await response.json();
                });

                console.log('Checkout successful:', checkoutResponse);
                this.showSuccessMessage({
                    orderId: checkoutResponse.orderId,
                    total: checkoutResponse.total
                });

                return checkoutResponse;
            } catch (error) {
                console.error('Error in transaction:', error);
                button.disabled = false;
                button.innerHTML = '<i class="far fa-check-circle"></i> Place Order';
                
                if (error.message.includes('log in')) {
                    window.location.href = 'login.html';
                    return;
                }
                throw new Error(error.message || 'Failed to complete order. Please try again.');
            }
        } catch (error) {
            console.error('Error placing order:', error);
            throw error;
        }
    }

    showSuccessMessage(order) {
        const checkoutContainer = document.querySelector('.checkout-container');
        checkoutContainer.innerHTML = `
            <div class="checkout-header">
                <h1>Order Confirmation</h1>
                <div class="checkout-steps">
                    <div class="step completed">
                        <div class="step-icon">
                            <i class="far fa-shopping-cart"></i>
                        </div>
                        <div class="step-label">Cart</div>
                    </div>
                    <div class="step completed">
                        <div class="step-icon">
                            <i class="far fa-map-marker-alt"></i>
                        </div>
                        <div class="step-label">Shipping</div>
                    </div>
                    <div class="step completed">
                        <div class="step-icon">
                            <i class="far fa-credit-card"></i>
                        </div>
                        <div class="step-label">Payment</div>
                    </div>
                    <div class="step completed">
                        <div class="step-icon">
                            <i class="far fa-check-circle"></i>
                        </div>
                        <div class="step-label">Confirmation</div>
                    </div>
                </div>
            </div>
            <div style="max-width: 600px; margin: 40px auto; background: white; padding: 40px; border-radius: 15px; box-shadow: 0 2px 15px rgba(0,0,0,0.05);">
                <div style="text-align: center;">
                    <i class="far fa-check-circle" style="font-size: 64px; color: #4CAF50; margin-bottom: 20px;"></i>
                    <h2 style="color: #1a1a1a; margin-bottom: 15px; font-size: 28px;">Order Placed Successfully!</h2>
                    <p style="color: #666; margin-bottom: 10px; font-size: 16px;">Thank you for your purchase</p>
                    <p style="color: #666; margin-bottom: 30px; font-size: 16px;">Order ID: #${order.orderId}</p>
                </div>
                
                <div style="background: #f8f9fa; padding: 20px; border-radius: 10px; margin-bottom: 30px;">
                    <h3 style="color: #1a1a1a; margin-bottom: 20px; font-size: 20px;">Order Details</h3>
                    <div style="display: flex; justify-content: space-between; padding-top: 15px; border-top: 2px solid #e8e8e8; margin-top: 15px;">
                        <span style="color: #1a1a1a; font-weight: 600; font-size: 20px;">Total</span>
                        <span style="color: #1a1a1a; font-weight: 600; font-size: 20px;">${this.formatCurrency(order.total)}</span>
                    </div>
                </div>

                <div style="background: #f0f9f9; padding: 20px; border-radius: 10px; margin-bottom: 30px; border: 2px solid #088178;">
                    <div style="display: flex; align-items: center; gap: 15px;">
                        <i class="far fa-money-bill-alt" style="color: #088178; font-size: 24px;"></i>
                        <div>
                            <h4 style="color: #1a1a1a; margin: 0 0 5px 0; font-size: 16px;">Payment Method</h4>
                            <p style="color: #666; margin: 0; font-size: 14px;">Cash on Delivery - Pay when you receive your order</p>
                        </div>
                    </div>
                </div>

                <div style="text-align: center;">
                    <a href="orders.html" class="normal" style="background: #088178; color: white; padding: 15px 30px; border-radius: 30px; text-decoration: none; display: inline-flex; align-items: center; gap: 8px; font-weight: 600; margin-right: 10px;">
                        <i class="far fa-list-alt"></i>
                        View Orders
                    </a>
                    <a href="index.html" class="normal" style="background: #088178; color: white; padding: 15px 30px; border-radius: 30px; text-decoration: none; display: inline-flex; align-items: center; gap: 8px; font-weight: 600;">
                        <i class="far fa-shopping-bag"></i>
                        Continue Shopping
                    </a>
                </div>
            </div>
        `;
    }
}

// Create a global instance
const checkoutService = new CheckoutService();
