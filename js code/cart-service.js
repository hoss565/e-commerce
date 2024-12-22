class CartService {
    constructor() {
        this.apiUrl = 'http://localhost:5037/api/Cart';
    }

    async addToCart(productId, quantity = 1, size) {
        try {
            const userData = JSON.parse(localStorage.getItem('currentUser'));
            if (!userData || !userData.user) {
                throw new Error('Please login first');
            }

            const response = await fetch(`${this.apiUrl}/${userData.user.userId}/add`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    productId: parseInt(productId),
                    quantity: quantity,
                    size: size
                })
            });

            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.message || 'Error adding item to cart');
            }

            const result = await response.json();
            console.log('Add to cart response:', result);
            
            await this.updateCartCount();
            return result;
        } catch (error) {
            console.error('Error adding to cart:', error);
            throw error;
        }
    }

    async getCartSummary() {
        try {
            const userData = JSON.parse(localStorage.getItem('currentUser'));
            if (!userData || !userData.user) {
                throw new Error('Please login first');
            }

            const response = await fetch(`${this.apiUrl}/${userData.user.userId}/summary`);
            if (!response.ok) {
                throw new Error('Failed to fetch cart summary');
            }

            return await response.json();
        } catch (error) {
            console.error('Error getting cart summary:', error);
            throw error;
        }
    }

    async getCartItems() {
        try {
            const userData = JSON.parse(localStorage.getItem('currentUser'));
            console.log('Getting cart items for user:', userData);
            
            if (!userData || !userData.user) {
                console.log('No user data found, redirecting to login');
                window.location.href = 'login.html';
                return null;
            }

            const url = `${this.apiUrl}/${userData.user.userId}`;
            console.log('Fetching cart items from URL:', url);
            
            const response = await fetch(url);
            console.log('Raw response status:', response.status);
            console.log('Raw response headers:', [...response.headers.entries()]);
            
            if (!response.ok) {
                const errorText = await response.text();
                console.error('Error response:', errorText);
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const responseText = await response.text();
            console.log('Raw response text:', responseText);

            const result = JSON.parse(responseText);
            console.log('Parsed cart response:', result);

            if (!result) {
                console.error('No data returned from API');
                return null;
            }

            // Detailed logging of cart structure
            console.log('Cart structure:', {
                hasItems: !!result.items,
                itemsLength: result.items ? result.items.length : 0,
                firstItem: result.items && result.items.length > 0 ? result.items[0] : null,
                keys: Object.keys(result)
            });

            // Add base URL to image paths and validate data structure
            if (result.items && Array.isArray(result.items)) {
                result.items = result.items.map(item => {
                    if (!item) {
                        console.warn('Null or undefined item in cart');
                        return null;
                    }

                    console.log('Processing cart item:', item);

                    if (item.productImageUrl && !item.productImageUrl.startsWith('http')) {
                        item.productImageUrl = `http://localhost:5037/${item.productImageUrl}`;
                    }
                    return item;
                }).filter(item => item !== null);
            } else {
                console.warn('Cart items is not an array:', result.items);
                result.items = [];
            }
            
            return result;
        } catch (error) {
            console.error('Error getting cart items:', error);
            throw error;
        }
    }

    async updateCartCount() {
        try {
            const userData = JSON.parse(localStorage.getItem('currentUser'));
            if (!userData || !userData.user) {
                return;
            }

            const cart = await this.getCartItems();
            const cartCountElement = document.getElementById('cart-count');
            if (cartCountElement && cart) {
                cartCountElement.textContent = cart.totalItems || 0;
            }
        } catch (error) {
            console.error('Error updating cart count:', error);
        }
    }

    async removeFromCart(cartItemId) {
        try {
            const userData = JSON.parse(localStorage.getItem('currentUser'));
            if (!userData || !userData.user) {
                throw new Error('Please login first');
            }

            const response = await fetch(`${this.apiUrl}/${userData.user.userId}/items/${cartItemId}`, {
                method: 'DELETE'
            });

            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.message || 'Error removing item from cart');
            }

            await this.updateCartCount();
            return true;
        } catch (error) {
            console.error('Error removing item from cart:', error);
            throw error;
        }
    }

    async updateCartItemQuantity(cartItemId, quantity) {
        try {
            const userData = JSON.parse(localStorage.getItem('currentUser'));
            if (!userData || !userData.user) {
                throw new Error('Please login first');
            }

            const response = await fetch(`${this.apiUrl}/${userData.user.userId}/update`, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    cartItemId: cartItemId,
                    quantity: quantity
                })
            });

            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.message || 'Error updating cart item');
            }

            await this.updateCartCount();
            return await response.json();
        } catch (error) {
            console.error('Error updating cart item quantity:', error);
            throw error;
        }
    }
}

// Create a global instance
const cartService = new CartService();

// Global function to add to cart
async function addToCart(productId, quantity = 1, size = null) {
    try {
        if (!size) {
            const sizeSelect = document.getElementById('size');
            if (sizeSelect) {
                size = sizeSelect.value;
                if (!size) {
                    alert('Please select a size');
                    return;
                }
            }
        }

        const quantityInput = document.getElementById('quantity');
        if (quantityInput) {
            quantity = parseInt(quantityInput.value);
        }

        await cartService.addToCart(productId, quantity, size);
        alert('Product added to cart successfully');
    } catch (error) {
        if (error.message === 'Please login first') {
            window.location.href = 'login.html';
        } else {
            alert('Error adding item to cart: ' + error.message);
        }
    }
}

// Update cart count when page loads
document.addEventListener('DOMContentLoaded', function() {
    if (localStorage.getItem('currentUser')) {
        cartService.updateCartCount();
    }
});
