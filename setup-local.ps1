param(
    [string]$MySqlPassword = ""
)

$ErrorActionPreference = "Stop"
$apiDir = Join-Path $PSScriptRoot "BuildingFex.Api"
$localConfig = Join-Path $apiDir "appsettings.Local.json"

if (-not $MySqlPassword) {
    Write-Host "Configuracion local de BuildingFex (MySQL + Mercado Pago)" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "MySQL 8 esta instalado en tu PC. Ingresa la contraseña del usuario root."
    Write-Host "(La que definiste al instalar MySQL; dejala vacia y Enter si no tiene contraseña)"
    $secure = Read-Host "MySQL root password" -AsSecureString
    $MySqlPassword = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
        [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure))
}

$escaped = $MySqlPassword.Replace("'", "''")
$connection = "server=localhost;port=3306;user=root;password=$escaped;database=buildingfex"

$mpAccessToken = $env:MP_ACCESS_TOKEN
$mpPublicKey = $env:MP_PUBLIC_KEY
$mpWebhookSecret = $env:MP_WEBHOOK_SECRET

$config = @{
    ConnectionStrings = @{
        DefaultConnection = $connection
    }
}

if ($mpAccessToken -and $mpPublicKey -and $mpWebhookSecret) {
    $config.MercadoPago = @{
        AccessToken      = $mpAccessToken
        PublicKey        = $mpPublicKey
        WebhookSecret    = $mpWebhookSecret
        FrontendBaseUrl  = "http://localhost:5173"
        NotificationUrl  = "http://localhost:5001/api/v1/payments/webhook"
    }
}
else {
    Write-Host ""
    Write-Host "Sin MP_ACCESS_TOKEN, MP_PUBLIC_KEY ni MP_WEBHOOK_SECRET en el entorno:" -ForegroundColor Yellow
    Write-Host "la seccion MercadoPago NO se escribio en appsettings.Local.json." -ForegroundColor Yellow
    Write-Host "Exportalas antes de correr este script si necesitas pagos en local." -ForegroundColor Yellow
}

$config = $config | ConvertTo-Json -Depth 4

Set-Content -Path $localConfig -Value $config -Encoding UTF8
Write-Host ""
Write-Host "Listo: $localConfig" -ForegroundColor Green
Write-Host ""
Write-Host "Siguiente paso:"
Write-Host "  cd BuildingFex.Api"
Write-Host "  dotnet run"
Write-Host ""
Write-Host "Frontend (otra terminal):"
Write-Host "  cd ..\Fronted"
Write-Host "  npm run dev"
Write-Host ""
Write-Host "Credenciales de prueba:"
Write-Host "  Admin:     admin@buildingfex.test / admin123"
Write-Host "  Residente: residente@buildingfex.test / residente123"
