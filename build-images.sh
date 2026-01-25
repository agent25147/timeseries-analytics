#!/bin/bash

# Production Docker Image Build and Push Script
# Usage: ./build-images.sh <version> <registry>
# Example: ./build-images.sh v1.0.0 myregistry.azurecr.io

set -e  # Exit on error

VERSION=${1:-latest}
REGISTRY=${2:-""}

if [ -n "$REGISTRY" ]; then
  BACKEND_IMAGE="$REGISTRY/ts-analytics-backend:$VERSION"
  FRONTEND_IMAGE="$REGISTRY/ts-analytics-frontend:$VERSION"
else
  BACKEND_IMAGE="ts-analytics-backend:$VERSION"
  FRONTEND_IMAGE="ts-analytics-frontend:$VERSION"
fi

echo "================================================"
echo "Building Time Series Analytics Docker Images"
echo "Version: $VERSION"
echo "Registry: ${REGISTRY:-local}"
echo "================================================"

# Build Backend
echo ""
echo "📦 Building Backend..."
docker build -t "$BACKEND_IMAGE" ./backend
echo "✅ Backend built: $BACKEND_IMAGE"

# Build Frontend
echo ""
echo "📦 Building Frontend..."
docker build -t "$FRONTEND_IMAGE" ./frontend
echo "✅ Frontend built: $FRONTEND_IMAGE"

# Tag as latest
if [ "$VERSION" != "latest" ]; then
  if [ -n "$REGISTRY" ]; then
    docker tag "$BACKEND_IMAGE" "$REGISTRY/ts-analytics-backend:latest"
    docker tag "$FRONTEND_IMAGE" "$REGISTRY/ts-analytics-frontend:latest"
    echo "✅ Tagged as latest"
  else
    docker tag "$BACKEND_IMAGE" "ts-analytics-backend:latest"
    docker tag "$FRONTEND_IMAGE" "ts-analytics-frontend:latest"
    echo "✅ Tagged as latest"
  fi
fi

# List images
echo ""
echo "📋 Built Images:"
docker images | grep "ts-analytics"

# Push to registry if specified
if [ -n "$REGISTRY" ]; then
  echo ""
  echo "🚀 Pushing to registry..."
  
  # Push versioned tags
  docker push "$BACKEND_IMAGE"
  docker push "$FRONTEND_IMAGE"
  
  # Push latest tags
  if [ "$VERSION" != "latest" ]; then
    docker push "$REGISTRY/ts-analytics-backend:latest"
    docker push "$REGISTRY/ts-analytics-frontend:latest"
  fi
  
  echo "✅ Images pushed to $REGISTRY"
  echo ""
  echo "📝 Pull commands:"
  echo "   docker pull $BACKEND_IMAGE"
  echo "   docker pull $FRONTEND_IMAGE"
else
  echo ""
  echo "ℹ️  Images built locally only (no registry specified)"
  echo "   To push to registry, run: ./build-images.sh $VERSION <registry>"
fi

echo ""
echo "✨ Done!"
