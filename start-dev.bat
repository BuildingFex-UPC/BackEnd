@echo off
setlocal
cd /d "%~dp0"

if not exist "BuildingFex.Api\appsettings.Local.json" (
  echo Ejecuta primero: powershell -ExecutionPolicy Bypass -File setup-local.ps1
  exit /b 1
)

echo [1/3] MySQL: usa tu instalacion local ^(MySQL80^) o Docker ^(docker compose up -d^)
echo [2/3] Compilando API...
cd BuildingFex.Api
dotnet build -v q
if errorlevel 1 exit /b 1

echo [3/3] API en http://localhost:5001
echo   Admin:     admin@buildingfex.test / admin123
echo   Residente: residente@buildingfex.test / residente123
echo.
dotnet run --urls "http://localhost:5001"
