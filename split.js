const fs = require('fs');
const path = require('path');

const indexFile = path.join(__dirname, 'Digiprise.PMS.API', 'wwwroot', 'index.html');
let content = fs.readFileSync(indexFile, 'utf8');

// Extract CSS
const styleRegex = /<style>([\s\S]*?)<\/style>/;
const styleMatch = content.match(styleRegex);
if (styleMatch) {
    fs.writeFileSync(path.join(__dirname, 'Digiprise.PMS.API', 'wwwroot', 'style.css'), styleMatch[1].trim());
    content = content.replace(styleRegex, '<link rel="stylesheet" href="style.css">');
}

// Extract JS
const scriptRegex = /<script>([\s\S]*?)<\/script>/;
const scriptMatch = content.match(scriptRegex);
if (scriptMatch) {
    fs.writeFileSync(path.join(__dirname, 'Digiprise.PMS.API', 'wwwroot', 'app.js'), scriptMatch[1].trim());
    content = content.replace(scriptRegex, '<script src="app.js"></script>');
}

fs.writeFileSync(indexFile, content);
console.log('Successfully split index.html into style.css and app.js');
