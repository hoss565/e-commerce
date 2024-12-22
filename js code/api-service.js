class ApiService {
    constructor() {
        this.baseUrl = 'http://localhost:5037/api';
    }

    // Generic method to handle API errors
    handleError(error) {
        console.error('API Error:', error);
        if (error.response) {
            throw new Error(error.response.data.message || 'حدث خطأ في الخادم');
        }
        throw new Error('حدث خطأ في الاتصال بالخادم');
    }

    // Generic GET request
    async get(endpoint) {
        try {
            const response = await fetch(`${this.baseUrl}${endpoint}`, {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json',
                    // Add auth header if user is logged in
                    ...(authService.isAuthenticated() && {
                        'Authorization': `Bearer ${authService.getCurrentUser().token}`
                    })
                }
            });

            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.message || 'حدث خطأ في الخادم');
            }

            return await response.json();
        } catch (error) {
            this.handleError(error);
        }
    }

    // Generic POST request
    async post(endpoint, data) {
        try {
            const response = await fetch(`${this.baseUrl}${endpoint}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    // Add auth header if user is logged in
                    ...(authService.isAuthenticated() && {
                        'Authorization': `Bearer ${authService.getCurrentUser().token}`
                    })
                },
                body: JSON.stringify(data)
            });

            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.message || 'حدث خطأ في الخادم');
            }

            return await response.json();
        } catch (error) {
            this.handleError(error);
        }
    }

    // Generic PUT request
    async put(endpoint, data) {
        try {
            const response = await fetch(`${this.baseUrl}${endpoint}`, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json',
                    // Add auth header if user is logged in
                    ...(authService.isAuthenticated() && {
                        'Authorization': `Bearer ${authService.getCurrentUser().token}`
                    })
                },
                body: JSON.stringify(data)
            });

            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.message || 'حدث خطأ في الخادم');
            }

            return await response.json();
        } catch (error) {
            this.handleError(error);
        }
    }

    // Generic DELETE request
    async delete(endpoint) {
        try {
            const response = await fetch(`${this.baseUrl}${endpoint}`, {
                method: 'DELETE',
                headers: {
                    'Content-Type': 'application/json',
                    // Add auth header if user is logged in
                    ...(authService.isAuthenticated() && {
                        'Authorization': `Bearer ${authService.getCurrentUser().token}`
                    })
                }
            });

            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.message || 'حدث خطأ في الخادم');
            }

            return await response.json();
        } catch (error) {
            this.handleError(error);
        }
    }

    // Auth endpoints
    async login(credentials) {
        return this.post('/Auth/login', credentials);
    }

    async register(userData) {
        return this.post('/Auth/register', userData);
    }

    // Users endpoints
    async getCurrentUser() {
        return this.get('/Users/current');
    }

    async updateUser(userId, userData) {
        return this.put(`/Users/${userId}`, userData);
    }

    async deleteUser(userId) {
        return this.delete(`/Users/${userId}`);
    }
}

// Create a single instance to be used across the application
const apiService = new ApiService();
