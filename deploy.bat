@echo off
echo ====================================
echo Time Series Analytics Deployment
echo ====================================
echo.

echo Checking Docker...
docker --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: Docker is not installed or not running!
    echo Please install Docker Desktop and try again.
    pause
    exit /b 1
)

echo Docker is running!
echo.
echo Building and starting services...
echo This may take a few minutes on first run...
echo.

docker-compose up --build -d

if errorlevel 1 (
    echo.
    echo ERROR: Failed to start services!
    echo Check the logs with: docker-compose logs
    pause
    exit /b 1
)

echo.
echo ====================================
echo Services started successfully!
echo ====================================
echo.
echo Frontend: http://localhost:4200
echo Backend:  http://localhost:5000
echo ClickHouse: http://localhost:8123
echo.
echo To view logs: docker-compose logs -f
echo To stop: docker-compose down
echo.
echo Waiting for services to be ready...
timeout /t 10 /nobreak >nul

echo.
echo Opening application in browser...
start http://localhost:4200

echo.
echo Done! Check if all services are running:
docker-compose ps
echo.
pause
