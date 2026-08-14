FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY AgroUnion.sln ./
COPY src/AgroUnion.Domain/AgroUnion.Domain.csproj src/AgroUnion.Domain/
COPY src/AgroUnion.Application/AgroUnion.Application.csproj src/AgroUnion.Application/
COPY src/AgroUnion.Infrastructure/AgroUnion.Infrastructure.csproj src/AgroUnion.Infrastructure/
COPY src/AgroUnion.Web/AgroUnion.Web.csproj src/AgroUnion.Web/
RUN dotnet restore src/AgroUnion.Web/AgroUnion.Web.csproj
COPY src/ ./src/
RUN dotnet publish src/AgroUnion.Web/AgroUnion.Web.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
RUN addgroup --system agro && adduser --system --ingroup agro agro
COPY --from=build /app/publish .
RUN mkdir -p /app/logs /app/App_Data/releases /app/.aspnet/DataProtection-Keys \
    && chown -R agro:agro /app
USER agro
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV HOME=/app
HEALTHCHECK --interval=15s --timeout=5s --start-period=30s --retries=5 \
    CMD curl --fail --silent --show-error http://127.0.0.1:8080/health || exit 1
ENTRYPOINT ["dotnet", "AgroUnion.Web.dll"]
