# =============================================
# Dockerfile — Library Management System API
# Stack: .NET 8 + Dapper + SQL Server
#
# Build context = thư mục GỐC repo (cần copy Backend/ và Frontend/):
#   docker build -f Backend/LibraryManagement.API/Dockerfile .
# =============================================

# ---------- STAGE 1: BUILD ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy toàn bộ Backend để restore + build (cache tốt hơn)
COPY Backend/ ./Backend/
RUN dotnet restore "Backend/LibraryManagement.API/LibraryManagement.API.csproj"

# Publish Release
RUN dotnet publish "Backend/LibraryManagement.API/LibraryManagement.API.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore

# ---------- STAGE 2: RUNTIME ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# curl phục vụ HEALTHCHECK (image aspnet không có sẵn)
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# Bản build Release
COPY --from=build /app/publish/ .

# Frontend tĩnh — Program.cs serve từ "../../Frontend" so với
# thư mục làm việc /app, nên đặt tại /Frontend
COPY Frontend/ /Frontend/

# Môi trường runtime
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

# Healthcheck (endpoint /health do Task 4 cung cấp)
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl -f http://localhost/health || exit 1

ENTRYPOINT ["dotnet", "LibraryManagement.API.dll"]