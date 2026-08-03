FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# copy csproj and restore as distinct layers
COPY . ./
RUN dotnet restore "src/StockManufactura.Api/StockManufactura.Api.csproj"

# build
RUN dotnet publish "src/StockManufactura.Api/StockManufactura.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "StockManufactura.Api.dll"]
