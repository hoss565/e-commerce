// Function to load products by category
async function loadProductsByCategory(categoryId) {
    try {
        const response = await fetch(`http://localhost:5037/api/Products/category/${categoryId}`);
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        const products = await response.json();
        
        const container = document.querySelector('.pro-container');
        if (!container) return;

        if (products && products.length > 0) {
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
        } else {
            container.innerHTML = '<p>No products found in this category.</p>';
        }
    } catch (error) {
        console.error('Error loading products:', error);
        const container = document.querySelector('.pro-container');
        if (container) {
            container.innerHTML = '<p>Error loading products. Please try again later.</p>';
        }
    }
}

// Function to determine which category to load based on current page
function loadCurrentPageProducts() {
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
    loadCurrentPageProducts();
    
    // Check authentication if auth.js is loaded
    if (typeof checkAuth === 'function') {
        checkAuth();
    }
});
