# -------- build --------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Cache restore layer
COPY McpWeatherServer.csproj ./McpWeatherServer/
RUN dotnet restore ./McpWeatherServer/McpWeatherServer.csproj

# Copy source
COPY . ./McpWeatherServer/

# Publish for Linux with an apphost executable
RUN dotnet publish ./McpWeatherServer/McpWeatherServer.csproj \
    -c Release \
    -r linux-x64 \
    --self-contained false \
    -o /out

# -------- runtime --------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /out ./

ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_ENVIRONMENT=Development

EXPOSE 8080

ENTRYPOINT ["./McpWeatherServer"]
