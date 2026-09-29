param(
    [string]$PostgresPassword = "buildingfex"
)

$ErrorActionPreference = "Stop"
$apiDir = Join-Path $PSScriptRoot "BuildingFex.Api"
$localConfig = Join-Path $apiDir "appsettings.Local.json"

if (-not $PostgresPassword) {
    Write-Host "Configuracion local de BuildingFex (PostgreSQL + Mercado Pago)" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "PostgreSQL esta corriendo en tu PC (docker compose up -d)."
    Write-Host "Ingresa la contrasena del usuario postgres."
    $secure = Read-Host "Postgres password" -AsSecureString
    $PostgresPassword = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
        [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure))
}

function ConvertTo-NpgsqlValue([string]$value) {
    if ([string]::IsNullOrEmpty($value)) { return "''" }
    if ($value.Contains("'")) { return "'" + $value.Replace("'", "''") + "'" }
    if ($value -match '[;=\s"]') { return "'" + $value + "'" }
    return $value
}

$connection = "Host=localhost;Port=5432;Database=buildingfex;Username=postgres;Password=" +
    (ConvertTo-NpgsqlValue $PostgresPassword)

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
Write-Host "  docker compose up -d"
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
