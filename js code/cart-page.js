class CartPage {
    constructor() {
        this.loadCart();
    }

    async loadCart() {
        try {
            const cart = await cartService.getCart();
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
            cartContainer.innerHTML = '<tr><td colspan="7" style="text-align: center;">Your cart is empty</td></tr>';
            this.updateTotals(0);
            return;
        }

        cartContainer.innerHTML = cart.cartItems.map(item => {
            return `
                <tr>
                    <td><a href="#" onclick="cartPage.removeItem(${item.cartItemId}); return false;">
                        <i class="far fa-times-circle"></i>
                    </a></td>
                    <td><img src="${item.imagePath || 'http://localhost:5037/images/default-product.jpg'}" alt="${item.productName}" style="width: 50px;"></td>
                    <td>${item.productName}</td>
                    <td>Size: ${item.size}</td>
                    <td>${cartService.formatCurrency(item.price)}</td>
                    <td>
                        <input type="number" value="${item.quantity}" min="1"
                            onchange="cartPage.updateQuantity(${item.cartItemId}, this.value)">
                    </td>
                    <td>${cartService.formatCurrency(item.price * item.quantity)}</td>
                </tr>
            `;
        }).join('');

        this.updateTotals(subtotal);
    }

    updateTotals(subtotal) {
        document.getElementById('cart-subtotal').textContent = cartService.formatCurrency(subtotal);
        document.getElementById('cart-total').textContent = cartService.formatCurrency(subtotal);
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

            const response = await fetch(`${cartService.apiUrl}/${user.user.userId}/update`, {
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
            await cartService.updateCartCount();
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

            const response = await fetch(`${cartService.apiUrl}/${user.user.userId}/items/${cartItemId}`, {
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
            await cartService.updateCartCount();
        } catch (error) {
            console.error('Error removing item:', error);
            alert(error.message || 'Error removing item. Please try again.');
        }
    }
}

// Initialize cart page when document loads
let cartPage;
document.addEventListener('DOMContentLoaded', () => {
    if (!cartService.isUserLoggedIn()) {
        window.location.href = 'login.html';
        return;
    }
    cartPage = new CartPage();
});

function proceedToCheckout() {
    window.location.href = 'checkout-updated.html';
}