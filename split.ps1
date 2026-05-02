$content = Get-Content -Raw -Path "Digiprise.PMS.API\wwwroot\index.html"
if ($content -match '(?s)<style>(.*?)</style>') {
    $css = $matches[1].Trim()
    $content = $content -replace '(?s)<style>.*?</style>', '<link rel="stylesheet" href="style.css">'
    Set-Content -Path "Digiprise.PMS.API\wwwroot\style.css" -Value $css -Encoding UTF8
}

if ($content -match '(?s)<script>(.*?)</script>') {
    $js = $matches[1].Trim()
    $content = $content -replace '(?s)<script>.*?</script>', '<script src="app.js"></script>'
    Set-Content -Path "Digiprise.PMS.API\wwwroot\app.js" -Value $js -Encoding UTF8
}

Set-Content -Path "Digiprise.PMS.API\wwwroot\index.html" -Value $content -Encoding UTF8
Write-Output "Successfully split index.html into style.css and app.js"
