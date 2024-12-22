// API Base URL
const API_URL = 'http://localhost:5037/api';

// Function to handle API errors
function handleError(error) {
    console.error('Error:', error);
    alert('An error occurred. Please try again.');
}

// Products API calls
async function getProducts() {
    try {
        const response = await fetch(`${API_URL}/products`);
        const products = await response.json();
        return products;
    } catch (error) {
        handleError(error);
        return [];
    }
}

async function getProduct(id) {
    try {
        const response = await fetch(`${API_URL}/products/${id}`);
        const product = await response.json();
        return product;
    } catch (error) {
        handleError(error);
        return null;
    }
}

// Categories API calls
async function getCategories() {
    try {
        const response = await fetch(`${API_URL}/categories`);
        const categories = await response.json();
        return categories;
    } catch (error) {
        handleError(error);
        return [];
    }
}

// Users API calls
async function login(email, password) {
    try {
        const response = await fetch(`${API_URL}/users`);
        const users = await response.json();
        const user = users.find(u => u.email === email && u.password === password);
        if (user) {
            localStorage.setItem('currentUser', JSON.stringify(user));
            return true;
        }
        return false;
    } catch (error) {
        handleError(error);
        return false;
    }
}

async function register(userData) {
    try {
        const response = await fetch(`${API_URL}/users`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(userData)
        });
        const newUser = await response.json();
        return newUser;
    } catch (error) {
        handleError(error);
        return null;
    }
}

// Orders API calls
async function createOrder(orderData) {
    try {
        const response = await fetch(`${API_URL}/orders`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(orderData)
        });
        const newOrder = await response.json();
        return newOrder;
    } catch (error) {
        handleError(error);
        return null;
    }
}

async function createOrderDetails(orderDetailsData) {
    try {
        const response = await fetch(`${API_URL}/orderdetails`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(orderDetailsData)
        });
        const newOrderDetails = await response.json();
        return newOrderDetails;
    } catch (error) {
        handleError(error);
        return null;
    }
}

// Cart functionality
let cart = JSON.parse(localStorage.getItem('cart')) || [];

function removeFromCart(productId) {
    cart = cart.filter(item => item.productId !== productId);
    localStorage.setItem('cart', JSON.stringify(cart));
    updateCartDisplay();
}

function updateCartDisplay() {
    const cartCount = cart.reduce((total, item) => total + item.quantity, 0);
    const cartCountElement = document.getElementById('cart-count');
    if (cartCountElement) {
        cartCountElement.textContent = cartCount;
    }
}

// Load and display products on the home page
async function loadFeaturedProducts() {
    const products = await getProducts();
    const container = document.querySelector('#product1 .pro-container');
    if (container) {
        container.innerHTML = products.map(product => `
            <div class="pro" onclick="window.location.href='sproduct.html?id=${product.productId}'">
                <img src="${product.imagePath || `img/product/f${product.productId}.jpg`}" alt="${product.productName}">
                <div class="des">
                    <span>${product.categoryName || 'Category'}</span>
                    <h5>${product.productName}</h5>
                    <div class="star">
                        <i class="fas fa-star"></i>
                        <i class="fas fa-star"></i>
                        <i class="fas fa-star"></i>
                        <i class="fas fa-star"></i>
                        <i class="fas fa-star"></i>
                    </div>
                    <h4>$${product.price}</h4>
                </div>
                <a href="sproduct.html?id=${product.productId}">
                    <i class="fal fa-shopping-cart cart"></i>
                </a>
            </div>
        `).join('');
    }
}

// Initialize cart display, load products and check login state
document.addEventListener('DOMContentLoaded', () => {
    updateCartDisplay();
    if (window.location.pathname.includes('shop.html')) {
        loadProducts();
    } else {
        loadFeaturedProducts();
    }
    checkLoginState();
});

// Load all products for the shop page
async function loadProducts() {
    try {
        const products = await getProducts();
        const container = document.querySelector('.pro-container');
        if (!container) return;
        
        container.innerHTML = ''; // Clear existing products

        products.forEach(product => {
            const productElement = document.createElement('div');
            productElement.classList.add('pro');
            productElement.innerHTML = `
                <img src="${product.imagePath || `img/product/f${product.productId}.jpg`}" alt="${product.productName}">
                <div class="des">
                    <span>${product.categoryName || 'Category'}</span>
                    <h5>${product.productName}</h5>
                    <div class="star">
                        <i class="fas fa-star"></i>
                        <i class="fas fa-star"></i>
                        <i class="fas fa-star"></i>
                        <i class="fas fa-star"></i>
                        <i class="fas fa-star"></i>
                    </div>
                    <h4>$${product.price}</h4>
                </div>
                <a href="sproduct.html?id=${product.productId}">
                    <i class="fal fa-shopping-cart cart"></i>
                </a>
            `;
            container.appendChild(productElement);
        });
    } catch (error) {
        console.error('Error loading products:', error);
    }
}

// Function to check login state and update UI
function checkLoginState() {
    const currentUser = JSON.parse(localStorage.getItem('currentUser'));
    const loginNav = document.querySelector('.login-nav a');
    const popup = document.querySelector('.create-account-popup');
    
    if (currentUser && loginNav) {
        loginNav.textContent = currentUser.userName;
        loginNav.href = '#';
        loginNav.classList.remove('active');
        
        if (popup) {
            popup.innerHTML = `
                <a href="#" onclick="logout()">Logout</a>
            `;
        }
    }
}

// Logout function
function logout() {
    localStorage.removeItem('currentUser');
    localStorage.removeItem('userToken');
    const loginNav = document.querySelector('.login-nav a');
    if (loginNav) {
        loginNav.textContent = 'Login';
        loginNav.href = 'login.html';
        loginNav.classList.add('active');
        
        const popup = document.querySelector('.create-account-popup');
        if (popup) {
            popup.innerHTML = `
                <a href="register.html">Create Account</a>
            `;
        }
    }
    window.location.href = 'login.html';
}

// Mobile menu functionality
const bar = document.getElementById('bar');
const nav = document.getElementById('navbar');
const close = document.getElementById('close');

if (bar) {
    bar.addEventListener('click', () => {
        nav.classList.add('active');
    });
}

if (close) {
    close.addEventListener('click', () => {
        nav.classList.remove('active');
    });
}
