@echo off
REM Production Docker Image Build and Push Script for Windows
REM Usage: build-images.bat <version> <registry>
REM Example: build-images.bat v1.0.0 myregistry.azurecr.io

setlocal

set VERSION=%1
set REGISTRY=%2

if "%VERSION%"=="" set VERSION=latest

if "%REGISTRY%"=="" (
    set BACKEND_IMAGE=ts-analytics-backend:%VERSION%
    set FRONTEND_IMAGE=ts-analytics-frontend:%VERSION%
) else (
    set BACKEND_IMAGE=%REGISTRY%/ts-analytics-backend:%VERSION%
    set FRONTEND_IMAGE=%REGISTRY%/ts-analytics-frontend:%VERSION%
)

echo ================================================
echo Building Time Series Analytics Docker Images
echo Version: %VERSION%
if "%REGISTRY%"=="" (
    echo Registry: local
) else (
    echo Registry: %REGISTRY%
)
echo ================================================

REM Build Backend
echo.
echo Building Backend...
docker build -t "%BACKEND_IMAGE%" ./backend
if errorlevel 1 goto :error
echo Backend built: %BACKEND_IMAGE%

REM Build Frontend
echo.
echo Building Frontend...
docker build -t "%FRONTEND_IMAGE%" ./frontend
if errorlevel 1 goto :error
echo Frontend built: %FRONTEND_IMAGE%

REM Tag as latest
if NOT "%VERSION%"=="latest" (
    if NOT "%REGISTRY%"=="" (
        docker tag "%BACKEND_IMAGE%" "%REGISTRY%/ts-analytics-backend:latest"
        docker tag "%FRONTEND_IMAGE%" "%REGISTRY%/ts-analytics-frontend:latest"
        echo Tagged as latest
    ) else (
        docker tag "%BACKEND_IMAGE%" "ts-analytics-backend:latest"
        docker tag "%FRONTEND_IMAGE%" "ts-analytics-frontend:latest"
        echo Tagged as latest
    )
)

REM List images
echo.
echo Built Images:
docker images | findstr "ts-analytics"

REM Push to registry if specified
if NOT "%REGISTRY%"=="" (
    echo.
    echo Pushing to registry...
    
    docker push "%BACKEND_IMAGE%"
    if errorlevel 1 goto :error
    
    docker push "%FRONTEND_IMAGE%"
    if errorlevel 1 goto :error
    
    if NOT "%VERSION%"=="latest" (
        docker push "%REGISTRY%/ts-analytics-backend:latest"
        docker push "%REGISTRY%/ts-analytics-frontend:latest"
    )
    
    echo Images pushed to %REGISTRY%
    echo.
    echo Pull commands:
    echo    docker pull %BACKEND_IMAGE%
    echo    docker pull %FRONTEND_IMAGE%
) else (
    echo.
    echo Images built locally only (no registry specified)
    echo To push to registry, run: build-images.bat %VERSION% ^<registry^>
)

echo.
echo Done!
goto :end

:error
echo.
echo Error occurred during build/push
exit /b 1

:end
endlocal
