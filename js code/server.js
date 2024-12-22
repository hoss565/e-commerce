const express = require('express');
const path = require('path');
const app = express();

// Serve static files
app.use(express.static(__dirname));

// Handle all routes by serving index.html
app.get('*', (req, res) => {
    res.sendFile(path.join(__dirname, req.path));
});

const PORT = 5037;
app.listen(PORT, () => {
    console.log(`Server running at http://localhost:${PORT}`);
});
