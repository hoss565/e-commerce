const API_URL = 'http://localhost:5037/api';

// Register function
async function register(userData) {
    try {
        const response = await fetch(`${API_URL}/auth/register`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(userData)
        });

        if (!response.ok) {
            const errorText = await response.text();
            alert(errorText || 'Registration failed. Please try again.');
            return null;
        }

        return await response.json();
    } catch (error) {
        console.error('Registration error:', error);
        alert('An error occurred during registration. Please try again.');
        return null;
    }
}

// Login function
async function login(loginData) {
    try {
        const response = await fetch(`${API_URL}/auth/login`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(loginData)
        });

        if (!response.ok) {
            const errorText = await response.text();
            alert(errorText || 'Login failed. Please check your credentials.');
            return null;
        }

        const userData = await response.json();
        localStorage.setItem('currentUser', JSON.stringify(userData));
        localStorage.setItem('userToken', userData.token);
        window.location.href = 'index.html'; // Redirect to home page after login
        return userData;
    } catch (error) {
        console.error('Login error:', error);
        alert('An error occurred during login. Please try again.');
        return null;
    }
}

// Logout function
function logout() {
    localStorage.removeItem('currentUser');
    localStorage.removeItem('userToken');
    window.location.href = 'login.html';
}

// Check if user is logged in and update UI
function checkAuth() {
    const user = localStorage.getItem('currentUser');
    const token = localStorage.getItem('userToken');
    
    if (user && token) {
        const userData = JSON.parse(user);
        const loginNav = document.querySelector('.login-nav a');
        if (loginNav) {
            loginNav.textContent = userData.user.userName; // Update to use the correct path to userName
            loginNav.href = '#';
            loginNav.classList.remove('active');
            
            const popup = document.querySelector('.create-account-popup');
            if (popup) {
                popup.innerHTML = '<a href="#" onclick="logout()">Logout</a>';
            }
        }
        return userData;
    }
    return null;
}

// Call checkAuth when the page loads
document.addEventListener('DOMContentLoaded', checkAuth);
