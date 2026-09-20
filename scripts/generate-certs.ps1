# generate-certs.ps1
# ==================
# Generate self-signed TLS certificates for local development.
# Uses OpenSSL (bundled with Git for Windows, WSL, or native).
#
# Usage:  powershell -File scripts/generate-certs.ps1
#
# For production: replace cert.pem/key.pem with Let's Encrypt certs.

$certsDir = Join-Path (Get-Location) "nginx\ssl"
$certFile = Join-Path $certsDir "cert.pem"
$keyFile  = Join-Path $certsDir "key.pem"

# Skip if certs already exist
if (Test-Path $certFile -and Test-Path $keyFile) {
    Write-Host "TLS certificates already exist at: $certsDir" -ForegroundColor Green
    exit 0
}

# Find openssl
$openssl = Get-Command "openssl" -ErrorAction SilentlyContinue
if (-not $openssl) {
    # Check common Git for Windows path
    $gitOpenSsl = "C:\Program Files\Git\usr\bin\openssl.exe"
    if (Test-Path $gitOpenSsl) {
        $openssl = $gitOpenSsl
    } else {
        Write-Host "ERROR: OpenSSL not found." -ForegroundColor Red
        Write-Host "Install Git for Windows (includes OpenSSL) or use WSL." -ForegroundColor Yellow
        Write-Host ""
        Write-Host "Alternative: generate certs manually and place in nginx/ssl/:" -ForegroundColor Yellow
        Write-Host "  nginx/ssl/cert.pem" -ForegroundColor Yellow
        Write-Host "  nginx/ssl/key.pem"  -ForegroundColor Yellow
        exit 1
    }
}

Write-Host "Generating self-signed TLS certificates..." -ForegroundColor Cyan

& $openssl req -x509 -nodes -days 365 -newkey rsa:2048 `
    -keyout $keyFile `
    -out $certFile `
    -subj "/C=US/ST=Local/L=Dev/O=Aimran/OU=Lab/CN=localhost" `
    -addext "subjectAltName=DNS:localhost,DNS:*.aimran.local,IP:127.0.0.1"

if ($?) {
    Write-Host "Certificates generated:" -ForegroundColor Green
    Write-Host "  $certFile" -ForegroundColor Green
    Write-Host "  $keyFile"  -ForegroundColor Green
    Write-Host ""
    Write-Host "For production, replace these with Let's Encrypt certs." -ForegroundColor Yellow
} else {
    Write-Host "ERROR: Certificate generation failed." -ForegroundColor Red
    exit 1
}
