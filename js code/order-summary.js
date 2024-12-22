class OrderSummary {
    constructor() {
        this.SHIPPING_COST = 10.00;
    }

    async updateSummary() {
        try {
            const cartData = await cartService.getCartItems();
            if (!cartData || !cartData.items) {
                return;
            }

            // Calculate subtotal
            const subtotal = cartData.items.reduce((sum, item) => {
                const itemSubtotal = parseFloat(item.subTotal) || 0;
                return sum + itemSubtotal;
            }, 0);

            // Calculate total
            const shipping = this.SHIPPING_COST;
            const total = subtotal + shipping;

            // Update cart items display
            const cartItemsContainer = document.getElementById('cartItems');
            if (cartItemsContainer) {
                cartItemsContainer.innerHTML = cartData.items.map(item => `
                    <div class="cart-item">
                        <img src="${item.imagePath ? `http://localhost:5037/${item.imagePath}` : 'http://localhost:5037/images/default-product.jpg'}" 
                             alt="${item.productName}" 
                             class="item-image">
                        <div class="item-details">
                            <div class="item-name">${item.productName}</div>
                            <div class="item-meta">
                                Size: ${item.size} | Quantity: ${item.quantity}
                            </div>
                        </div>
                        <div class="item-price">
                            EGP ${item.subTotal.toFixed(2)}
                        </div>
                    </div>
                `).join('');
            }

            // Update summary totals
            const subtotalElement = document.getElementById('cart-subtotal');
            const shippingElement = document.getElementById('shipping');
            const totalElement = document.getElementById('cart-total');

            if (subtotalElement) subtotalElement.textContent = `EGP ${subtotal.toFixed(2)}`;
            if (shippingElement) shippingElement.textContent = `EGP ${shipping.toFixed(2)}`;
            if (totalElement) totalElement.innerHTML = `<strong>EGP ${total.toFixed(2)}</strong>`;

            // Update cart count
            const cartCount = document.getElementById('cart-count');
            if (cartCount) {
                cartCount.textContent = cartData.items.length;
                cartCount.style.display = cartData.items.length > 0 ? 'block' : 'none';
            }
        } catch (error) {
            console.error('Error updating summary:', error);
            throw error;
        }
    }
}

// Create global instance
const orderSummary = new OrderSummary();
