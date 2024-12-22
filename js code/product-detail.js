document.addEventListener('DOMContentLoaded', async function() {
    const urlParams = new URLSearchParams(window.location.search);
    const productId = urlParams.get('id');

    if (!productId) {
        alert('No product ID specified');
        return;
    }

    try {
        const product = await productService.getProduct(productId);
        console.log('Fetched product:', product); // Debug log
        if (product) {
            displayProduct(product);
            updateSizeOptions(product.sizes);
            loadSuggestedProducts(product.categoryId);
        } else {
            throw new Error('Product not found');
        }
    } catch (error) {
        console.error('Error fetching product:', error);
        alert('Error loading product details');
    }
});

function displayProduct(product) {
    // Update product details
    document.getElementById('productName').textContent = product.productName;
    document.getElementById('productPrice').textContent = `$${product.price.toFixed(2)}`;
    document.getElementById('productDescription').textContent = product.description;
    document.getElementById('productCategory').textContent = product.categoryName;
    document.title = `${product.productName} - Product Details`;

    // Handle images
    const mainImg = document.getElementById('MainImg');
    const imageGallery = document.getElementById('imageGallery');

    // Get all available images and ensure they have absolute URLs
    let allImages = [];
    if (product.imagePath) {
        let imagePath = product.imagePath;
        if (!imagePath.startsWith('http')) {
            imagePath = `http://localhost:5037/${imagePath}`;
        }
        allImages.push(imagePath);
    }
    if (product.imagePaths && product.imagePaths.length > 0) {
        // Add any images from imagePaths that aren't already included
        product.imagePaths.forEach(path => {
            let imagePath = path;
            if (!imagePath.startsWith('http')) {
                imagePath = `http://localhost:5037/${imagePath}`;
            }
            if (!allImages.includes(imagePath)) {
                allImages.push(imagePath);
            }
        });
    }

    console.log('Product images:', allImages); // Debug log

    if (allImages.length > 0) {
        // Set main image
        mainImg.src = allImages[0];
        console.log('Set main image:', allImages[0]); // Debug log

        // Create thumbnails
        imageGallery.innerHTML = allImages.map((path, index) => `
            <div class="small-img-col">
                <img src="${path}" 
                     width="100%" 
                     class="small-img ${index === 0 ? 'active' : ''}" 
                     onclick="changeMainImage(this)"
                     alt="${product.productName} image ${index + 1}">
            </div>
        `).join('');
        console.log('Created thumbnails for images'); // Debug log
    } else {
        // Use default image if no images are available
        console.log('No images found, using default'); // Debug log
        const defaultImage = 'img/product/f1.jpg';
        mainImg.src = defaultImage;
        imageGallery.innerHTML = `
            <div class="small-img-col">
                <img src="${defaultImage}" 
                     width="100%" 
                     class="small-img active" 
                     onclick="changeMainImage(this)"
                     alt="Default product image">
            </div>`;
    }
}

function updateSizeOptions(sizes) {
    const sizeSelect = document.getElementById('size');
    sizeSelect.innerHTML = '<option value="">Select Size</option>';
    
    if (sizes && sizes.length > 0) {
        sizes.forEach(sizeObj => {
            if (sizeObj.quantity > 0) {
                const option = document.createElement('option');
                option.value = sizeObj.size;
                option.textContent = `${sizeObj.size} (${sizeObj.quantity} available)`;
                sizeSelect.appendChild(option);
            }
        });
    }
}

function changeMainImage(clickedImg) {
    console.log('Changing main image to:', clickedImg.src); // Debug log
    
    // Update main image
    const mainImg = document.getElementById('MainImg');
    mainImg.src = clickedImg.src;
    
    // Update active state of thumbnails
    document.querySelectorAll('.small-img').forEach(img => {
        img.classList.remove('active');
    });
    clickedImg.classList.add('active');
}

function updateQuantity(change) {
    const quantityInput = document.getElementById('quantity');
    let currentValue = parseInt(quantityInput.value);
    let newValue = currentValue + change;
    
    // Ensure value stays within min and max bounds
    newValue = Math.max(1, Math.min(10, newValue));
    
    quantityInput.value = newValue;
}

async function addToCart() {
    const urlParams = new URLSearchParams(window.location.search);
    const productId = urlParams.get('id');
    const quantity = parseInt(document.getElementById('quantity').value);
    const size = document.getElementById('size').value;

    if (!size) {
        alert('Please select a size');
        return;
    }

    try {
        await cartService.addToCart(productId, quantity, size);
        window.location.href = 'cart.html';
    } catch (error) {
        if (error.message === 'Please login first') {
            window.location.href = 'login.html';
        } else {
            alert('Error adding item to cart: ' + error.message);
        }
    }
}

async function loadSuggestedProducts(categoryId) {
    try {
        // Fetch products from the same category
        const products = await productService.getProductsByCategory(categoryId);
        
        // Filter out the current product and limit to 4 suggestions
        const currentProductId = new URLSearchParams(window.location.search).get('id');
        const suggestedProducts = products
            .filter(p => p.productId.toString() !== currentProductId)
            .slice(0, 4);

        // Display the suggested products
        const container = document.getElementById('suggestedProductsContainer');
        container.innerHTML = suggestedProducts.map(product => `
            <div class="pro" onclick="window.location.href='product-detail.html?id=${product.productId}'">
                <img src="${product.imagePath ? 
                    (product.imagePath.startsWith('http') ? 
                        product.imagePath : 
                        'http://localhost:5037/' + product.imagePath) : 
                    'img/product/f1.jpg'}" 
                    alt="${product.productName}">
                <div class="des">
                    <span>${product.categoryName}</span>
                    <h5>${product.productName}</h5>
                    <h4>$${product.price.toFixed(2)}</h4>
                </div>
                <div class="cart">
                    <i class="far fa-shopping-bag"></i>
                </div>
            </div>
        `).join('');
    } catch (error) {
        console.error('Error loading suggested products:', error);
        document.getElementById('suggestedProductsContainer').innerHTML = 
            '<p>Unable to load suggested products</p>';
    }
}
