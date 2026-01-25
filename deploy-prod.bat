@echo off
REM Quick Production Deployment Script
REM This script deploys the application using docker-compose.prod.yml

setlocal

echo ================================================
echo Time Series Analytics - Production Deployment
echo ================================================

REM Check if .env exists
if not exist .env (
    echo Warning: .env file not found
    echo Creating from .env.example...
    copy .env.example .env
    echo.
    echo IMPORTANT: Please edit .env file with your production settings!
    echo Press any key to continue or Ctrl+C to abort...
    pause > nul
)

echo.
echo Step 1: Stopping existing containers...
docker-compose -f docker-compose.prod.yml down

echo.
echo Step 2: Pulling/Building images...
docker-compose -f docker-compose.prod.yml build --no-cache

echo.
echo Step 3: Starting services...
docker-compose -f docker-compose.prod.yml up -d

echo.
echo Step 4: Waiting for services to be healthy...
timeout /t 10 /nobreak > nul

echo.
echo Step 5: Checking service status...
docker-compose -f docker-compose.prod.yml ps

echo.
echo ================================================
echo Deployment Complete!
echo ================================================
echo.
echo Service URLs:
echo   Frontend: http://localhost:%FRONTEND_PORT%
echo   Backend:  http://localhost:%BACKEND_PORT%/swagger
echo.
echo To view logs:
echo   docker-compose -f docker-compose.prod.yml logs -f
echo.
echo To stop services:
echo   docker-compose -f docker-compose.prod.yml down
echo.

endlocal
