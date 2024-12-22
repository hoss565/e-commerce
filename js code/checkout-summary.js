class CheckoutSummary {
    constructor() {
        this.SHIPPING_COST = 10.00; // Fixed shipping cost
    }

    async updateSummary() {
        try {
            // Get cart data
            const cartData = await cartService.getCartItems();
            const orderSummary = await cartService.getCartSummary();

            if (!cartData || !cartData.items) {
                throw new Error('No items in cart');
            }

            // Get DOM elements
            const subtotalElement = document.getElementById('cart-subtotal');
            const shippingElement = document.getElementById('shipping');
            const totalElement = document.getElementById('cart-total');

            if (orderSummary) {
                // Use server-provided summary
                subtotalElement.textContent = `$ ${orderSummary.subTotal.toFixed(2)}`;
                shippingElement.textContent = `$ ${orderSummary.shipping.toFixed(2)}`;
                totalElement.textContent = `$ ${orderSummary.total.toFixed(2)}`;
            } else {
                // Calculate locally if server summary not available
                const subtotal = cartData.items.reduce((sum, item) => sum + item.subTotal, 0);
                const shipping = this.SHIPPING_COST;
                const total = subtotal + shipping;

                // Update DOM
                subtotalElement.textContent = `$ ${subtotal.toFixed(2)}`;
                shippingElement.textContent = `$ ${shipping.toFixed(2)}`;
                totalElement.textContent = `$ ${total.toFixed(2)}`;
            }
        } catch (error) {
            console.error('Error updating summary:', error);
            throw error;
        }
    }
}

// Create global instance
const checkoutSummary = new CheckoutSummary();
