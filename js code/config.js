const API_CONFIG = {
    BASE_URL: 'http://localhost:5037',
    API_URL: 'http://localhost:5037/api'
};

// Prevent redefinition
if (typeof API_URL === 'undefined') {
    const API_URL = API_CONFIG.API_URL;
}
