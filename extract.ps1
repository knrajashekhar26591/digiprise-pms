Add-Type -AssemblyName System.IO.Compression.FileSystem
function Read-DocxText {
    param($path)
    $zip = [System.IO.Compression.ZipFile]::OpenRead($path)
    $docXml = $zip.Entries | Where-Object { $_.FullName -eq 'word/document.xml' }
    $stream = $docXml.Open()
    $reader = New-Object System.IO.StreamReader($stream)
    $xmlString = $reader.ReadToEnd()
    $reader.Close()
    $stream.Close()
    $zip.Dispose()
    $text = $xmlString -replace '<[^>]+>', '
' -replace '
+', '
'
    return $text.Trim()
}

$files = @(
    'Digiprise_PMS_Implementation_Document.docx',
    'Digiprise_PMS_LLD_Architecture_v1.0.docx',
    'Digiprise_PMS_LLD_v2.docx'
)

foreach ($f in $files) {
    if (Test-Path $f) {
        $out = $f + '.txt'
        Read-DocxText $f | Out-File -FilePath $out -Encoding utf8
        Write-Output "Extracted $f to $out"
    }
}
