# Imagen de la versión web para publicar la demo en un hosting con contenedores (por ejemplo Render, plan gratuito).
# La base de demo viaja dentro de la imagen: cada vez que el servicio se reinicia, los datos vuelven a su estado inicial.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/ src/
RUN dotnet publish src/StockManufactura.Web/StockManufactura.Web.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out .
RUN mkdir -p /app/data
COPY demo/StockManufactura-demo.db /app/data/StockManufactura.db
# SQLite necesita escribir junto al archivo (diarios -wal y -shm), por eso la carpeta es del usuario sin privilegios.
RUN chown -R $APP_UID /app/data
USER $APP_UID

ENV ASPNETCORE_HTTP_PORTS=10000 \
    ASPNETCORE_ENVIRONMENT=Production \
    TZ=America/Argentina/Buenos_Aires \
    Demo__Habilitada=true \
    ConnectionStrings__StockManufactura="Data Source=/app/data/StockManufactura.db"
EXPOSE 10000
ENTRYPOINT ["dotnet", "StockManufactura.Web.dll"]
