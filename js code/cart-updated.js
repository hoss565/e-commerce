// Cart functionality
class CartManager {
    constructor() {
        this.apiUrl = 'http://localhost:5037/api/Cart';
    }

    async loadCart() {
        try {
            const user = JSON.parse(localStorage.getItem('currentUser'));
            if (!user || !user.user || !user.user.userId) {
                window.location.href = 'login.html';
                return;
            }

            const response = await fetch(`${this.apiUrl}/${user.user.userId}`, {
                headers: {
                    'Authorization': `Bearer ${localStorage.getItem('userToken')}`,
                    'Content-Type': 'application/json'
                }
            });

            if (response.status === 401) {
                window.location.href = 'login.html';
                return;
            }

            if (!response.ok) {
                throw new Error('Failed to fetch cart');
            }

            const cart = await response.json();
            this.displayCart(cart);
        } catch (error) {
            console.error('Error loading cart:', error);
            alert('Error loading cart. Please try again later.');
        }
    }

    displayCart(cart) {
        const cartContainer = document.getElementById('cart-items');
        let subtotal = cart.total || 0;

        if (!cart || !cart.cartItems || cart.cartItems.length === 0) {
            cartContainer.innerHTML = `
                <tr>
                    <td colspan="7" style="text-align: center; padding: 40px;">
                        <i class="far fa-shopping-cart" style="font-size: 48px; color: #ddd; margin-bottom: 20px;"></i>
                        <p style="font-size: 18px; color: #666;">Your cart is empty</p>
                        <a href="index.html" class="normal" style="display: inline-block; margin-top: 20px;">Continue Shopping</a>
                    </td>
                </tr>`;
            this.updateTotals(0);
            this.updateCartCount(0);
            return;
        }

        cartContainer.innerHTML = cart.cartItems.map(item => {
            return `
                <tr>
                    <td>
                        <a href="#" class="remove-btn" onclick="cartManager.removeItem(${item.cartItemId}); return false;">
                            <i class="far fa-times-circle"></i>
                        </a>
                    </td>
                    <td>
                        <img src="${item.imagePath || 'http://localhost:5037/images/default-product.jpg'}" 
                             alt="${item.productName}">
                    </td>
                    <td>
                        <strong>${item.productName}</strong>
                        <span class="stock-info">In Stock</span>
                    </td>
                    <td>Size: ${item.size}</td>
                    <td>$${item.price.toFixed(2)}</td>
                    <td>
                        <div class="quantity-controls">
                            <input type="number" value="${item.quantity}" min="1"
                                onchange="cartManager.updateQuantity(${item.cartItemId}, this.value)"
                                onkeypress="return event.charCode >= 48 && event.charCode <= 57">
                        </div>
                    </td>
                    <td><strong>$${item.subTotal.toFixed(2)}</strong></td>
                </tr>
            `;
        }).join('');

        this.updateTotals(subtotal);
        this.updateCartCount(cart.cartItems.length);
    }

    updateTotals(subtotal) {
        const shipping = 10; // $10 shipping
        const total = subtotal + shipping;
        document.getElementById('cart-subtotal').textContent = `$ ${subtotal.toFixed(2)}`;
        document.getElementById('cart-total').textContent = `$ ${total.toFixed(2)}`;
    }

    updateCartCount(count) {
        const cartCount = document.getElementById('cart-count');
        if (cartCount) {
            cartCount.textContent = count;
            cartCount.style.display = count > 0 ? 'block' : 'none';
        }
    }

    async updateQuantity(cartItemId, newQuantity) {
        try {
            if (newQuantity < 1) {
                alert('Quantity must be at least 1');
                await this.loadCart();
                return;
            }

            const user = JSON.parse(localStorage.getItem('currentUser'));
            if (!user || !user.user || !user.user.userId) {
                window.location.href = 'login.html';
                return;
            }

            const response = await fetch(`${this.apiUrl}/${user.user.userId}/update`, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${localStorage.getItem('userToken')}`
                },
                body: JSON.stringify({
                    cartItemId: cartItemId,
                    quantity: parseInt(newQuantity)
                })
            });

            if (response.status === 401) {
                window.location.href = 'login.html';
                return;
            }

            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(errorData.message || 'Failed to update quantity');
            }

            await this.loadCart();
        } catch (error) {
            console.error('Error updating quantity:', error);
            alert(error.message || 'Error updating quantity. Please try again.');
        }
    }

    async removeItem(cartItemId) {
        try {
            const user = JSON.parse(localStorage.getItem('currentUser'));
            if (!user || !user.user || !user.user.userId) {
                window.location.href = 'login.html';
                return;
            }

            const response = await fetch(`${this.apiUrl}/${user.user.userId}/items/${cartItemId}`, {
                method: 'DELETE',
                headers: {
                    'Authorization': `Bearer ${localStorage.getItem('userToken')}`,
                    'Content-Type': 'application/json'
                }
            });

            if (response.status === 401) {
                window.location.href = 'login.html';
                return;
            }

            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(errorData.message || 'Failed to remove item');
            }

            await this.loadCart();
        } catch (error) {
            console.error('Error removing item:', error);
            alert(error.message || 'Error removing item. Please try again.');
        }
    }

    async checkout() {
        try {
            const user = JSON.parse(localStorage.getItem('currentUser'));
            if (!user || !user.user || !user.user.userId) {
                alert('Please login to proceed with checkout');
                window.location.href = 'login.html';
                return;
            }

            // Redirect to checkout page
            window.location.href = 'checkout.html';
        } catch (error) {
            console.error('Error during checkout:', error);
            alert(error.message || 'Error during checkout. Please try again.');
        }
    }
}

// Create a single instance of CartManager
const cartManager = new CartManager();

// Initialize cart when page loads
document.addEventListener('DOMContentLoaded', async () => {
    await cartManager.loadCart();
});

// Add function to handle checkout button click
function proceedToCheckout() {
    cartManager.checkout();
}
