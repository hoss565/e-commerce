class ProductService {
    constructor() {
        this.apiUrl = 'http://localhost:5037/api/Products';
    }

    async getProducts() {
        try {
            const response = await fetch(this.apiUrl);
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            const data = await response.json();
            return data;
        } catch (error) {
            console.error('Error getting products:', error);
            throw error;
        }
    }

    async getProduct(id) {
        try {
            const response = await fetch(`${this.apiUrl}/${id}`);
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            const result = await response.json();
            console.log('Fetched product:', result); // Debug log
            
            // Add base URL to image paths
            if (result.product) {
                if (result.product.imagePath && !result.product.imagePath.startsWith('http')) {
                    result.product.imagePath = `http://localhost:5037/${result.product.imagePath}`;
                }
                if (result.product.imagePaths) {
                    result.product.imagePaths = result.product.imagePaths.map(path => {
                        if (path && !path.startsWith('http')) {
                            return `http://localhost:5037/${path}`;
                        }
                        return path;
                    });
                }
            }
            
            return result.product;
        } catch (error) {
            console.error('Error getting product:', error);
            throw error;
        }
    }

    async getProductsByCategory(categoryId) {
        try {
            const response = await fetch(`${this.apiUrl}/category/${categoryId}`);
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            const result = await response.json();
            
            // Add base URL to image paths
            if (result && Array.isArray(result)) {
                result.forEach(product => {
                    if (product.imagePath && !product.imagePath.startsWith('http')) {
                        product.imagePath = `http://localhost:5037/${product.imagePath}`;
                    }
                });
            }
            
            return result;
        } catch (error) {
            console.error('Error getting products by category:', error);
            throw error;
        }
    }

    // Function to load products based on current page
    async loadPageProducts() {
        const currentPage = window.location.pathname.split('/').pop();
        let categoryId;
        
        switch(currentPage) {
            case 'men.html':
                categoryId = 5; // Men's category
                break;
            case 'women.html':
                categoryId = 7; // Women's category
                break;
            case 'kids.html':
                categoryId = 8; // Kids' category
                break;
            default:
                return;
        }

        try {
            const products = await this.getProductsByCategory(categoryId);
            this.displayProducts(products);
        } catch (error) {
            console.error('Error loading products:', error);
        }
    }

    displayProducts(products) {
        const container = document.querySelector('.pro-container');
        if (!container) return;

        if (products && products.length > 0) {
            container.innerHTML = products.map(product => `
                <div class="pro" onclick="window.location.href='product-detail.html?id=${product.productId}'">
                    <img src="${product.imagePath || (product.imagePaths && product.imagePaths[0]) || 'img/product/f1.jpg'}" 
                         alt="${product.productName}">
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
            container.innerHTML = '<p>No products found.</p>';
        }
    }
}

// Create a global instance
const productService = new ProductService();

// Load products when page loads
document.addEventListener('DOMContentLoaded', function() {
    productService.loadPageProducts();
});
