# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["TimeSeriesAnalytics.Api/TimeSeriesAnalytics.Api.csproj", "TimeSeriesAnalytics.Api/"]
RUN dotnet restore "TimeSeriesAnalytics.Api/TimeSeriesAnalytics.Api.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/TimeSeriesAnalytics.Api"
RUN dotnet build "TimeSeriesAnalytics.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "TimeSeriesAnalytics.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TimeSeriesAnalytics.Api.dll"]
