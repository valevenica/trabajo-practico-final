# StockManufactura

Estructura inicial del proyecto StockManufactura usando .NET 9 y Clean Architecture.

Requisitos incluidos:
- Solution y proyectos base
- EF Core configured for SQL Server (placeholders)
- JWT Authentication placeholders
- Swagger
- Serilog
- AutoMapper
- FluentValidation
- Dockerfile + docker-compose con SQL Server
- appsettings.json / appsettings.Development.json
- Estructura de carpetas: Controllers, Entities, DTOs, Repositories, Services, Middleware, Migrations

Cómo ejecutar (local):
- Reemplaza la cadena de conexión en src/StockManufactura.Api/appsettings.json si no usas Docker.
- dotnet restore
- dotnet build
- dotnet run --project src/StockManufactura.Api

Cómo ejecutar con Docker:
- docker compose up --build

Notas:
- No hay lógica de negocio implementada todavía. Esto es un bootstrap para comenzar el desarrollo.
