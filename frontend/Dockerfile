# Build stage
FROM node:22-alpine AS build
WORKDIR /app

# Copy package files
COPY timeseries-analytics-ui/package*.json ./

# Install dependencies
RUN npm ci

# Copy source files
COPY timeseries-analytics-ui/ ./

# Build the Angular app for production
RUN npm run build -- --configuration production

# Production stage with Nginx
FROM nginx:alpine
COPY --from=build /app/dist/timeseries-analytics-ui/browser /usr/share/nginx/html

# Copy custom nginx config
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
