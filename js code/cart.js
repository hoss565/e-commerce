import { getCartItems, updateCartItemQuantity, removeFromCart } from './cart-service.js';

document.addEventListener('DOMContentLoaded', async () => {
    try {
        await loadCartItems();
    } catch (error) {
        console.error('Error loading cart:', error);
        // Show error message to user
        alert('Error loading cart items. Please try again later.');
    }
});

async function loadCartItems() {
    try {
        const cartItems = await getCartItems();
        displayCartItems(cartItems);
        updateCartTotals(cartItems);
    } catch (error) {
        throw error;
    }
}

function displayCartItems(items) {
    const cartItemsContainer = document.getElementById('cart-items');
    cartItemsContainer.innerHTML = '';

    if (items.length === 0) {
        cartItemsContainer.innerHTML = `
            <tr>
                <td colspan="6" style="text-align: center;">Your cart is empty</td>
            </tr>
        `;
        return;
    }

    items.forEach(item => {
        const row = document.createElement('tr');
        row.innerHTML = `
            <td><a href="#" class="remove-item" data-id="${item.id}"><i class="far fa-times-circle"></i></a></td>
            <td><img src="${item.product.imagePath}" alt="${item.product.name}"></td>
            <td>${item.product.name}</td>
            <td>$ ${item.product.price.toFixed(2)}</td>
            <td>
                <input type="number" value="${item.quantity}" min="1" 
                    class="quantity-input" data-id="${item.id}">
            </td>
            <td>$ ${(item.quantity * item.product.price).toFixed(2)}</td>
        `;
        cartItemsContainer.appendChild(row);
    });

    // Add event listeners for quantity changes and remove buttons
    addCartEventListeners();
}

function updateCartTotals(items) {
    const subtotal = items.reduce((sum, item) => sum + (item.quantity * item.product.price), 0);
    document.getElementById('cart-subtotal').textContent = `$ ${subtotal.toFixed(2)}`;
    document.getElementById('cart-total').textContent = `$ ${subtotal.toFixed(2)}`;
}

function addCartEventListeners() {
    // Handle quantity changes
    document.querySelectorAll('.quantity-input').forEach(input => {
        input.addEventListener('change', async (e) => {
            const cartItemId = e.target.dataset.id;
            const newQuantity = parseInt(e.target.value);
            
            if (newQuantity < 1) {
                e.target.value = 1;
                return;
            }

            try {
                await updateCartItemQuantity(cartItemId, newQuantity);
                await loadCartItems(); // Reload cart to update totals
            } catch (error) {
                console.error('Error updating quantity:', error);
                alert('Error updating quantity. Please try again.');
                await loadCartItems(); // Reload to reset to previous state
            }
        });
    });

    // Handle remove item clicks
    document.querySelectorAll('.remove-item').forEach(button => {
        button.addEventListener('click', async (e) => {
            e.preventDefault();
            const cartItemId = e.target.closest('.remove-item').dataset.id;
            
            try {
                await removeFromCart(cartItemId);
                await loadCartItems(); // Reload cart after removing item
            } catch (error) {
                console.error('Error removing item:', error);
                alert('Error removing item. Please try again.');
            }
        });
    });
}
