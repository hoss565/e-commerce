// Common function to load products by category
async function loadProductsByCategory(categoryId) {
    try {
        const response = await fetch(`http://localhost:5037/api/Products/category/${categoryId}`);
        const products = await response.json();
        
        if (products.length > 0) {
            const container = document.querySelector('.pro-container');
            container.innerHTML = products.map(product => `
                <div class="pro" onclick="window.location.href='product-detail.html?id=${product.productId}'">
                    <img src="${product.imagePath || 'img/product/f1.jpg'}" alt="${product.productName}">
                    <div class="des">
                        <h5>${product.productName}</h5>
                        <div class="star">
                            <i class="fas fa-star"></i>
                            <i class="fas fa-star"></i>
                            <i class="fas fa-star"></i>
                            <i class="fas fa-star"></i>
                            <i class="fas fa-star"></i>
                        </div>
                        <h4>$${product.price.toFixed(2)}</h4>
                    </div>
                    <button class="normal" onclick="addToCart(${product.productId}); event.stopPropagation();">Add to Cart</button>
                </div>
            `).join('');
        }
    } catch (error) {
        console.error('Error loading products:', error);
        // Keep the default products if API fails
    }
}

// Function to determine which category to load based on current page
function loadPageProducts() {
    const currentPage = window.location.pathname.split('/').pop();
    
    switch(currentPage) {
        case 'men.html':
            loadProductsByCategory(5); // Men's category
            break;
        case 'women.html':
            loadProductsByCategory(7); // Women's category
            break;
        case 'kids.html':
            loadProductsByCategory(8); // Kids' category
            break;
    }
}

// Load products when page loads
document.addEventListener('DOMContentLoaded', function() {
    loadPageProducts();
    if (typeof checkAuth === 'function') {
        checkAuth(); // Check authentication status if auth.js is loaded
    }
});

// Mobile navbar functionality
const bar = document.getElementById('bar');
const close = document.getElementById('close');
const nav = document.getElementById('navbar');

if (bar) {
    bar.addEventListener('click', () => {
        nav.classList.add('active');
    })
}

if (close) {
    close.addEventListener('click', () => {
        nav.classList.remove('active');
    })
}
