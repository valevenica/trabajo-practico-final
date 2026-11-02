# Pruebas de punta a punta de la versión web

Recorren el sistema en Chrome como lo haría un usuario: ingreso, los 16 módulos del menú, desactivar y reactivar
productos e insumos, insumos especiales, órdenes, cotización, carga masiva de precios, bitácora, copias de seguridad,
usuarios y roles, permisos por rol y la vista de celular. Son 40 controles.

**Modifican datos** (crean usuarios y roles, restauran una copia, desactivan y reactivan registros). Usarlas solo con
una base de prueba, nunca con la base real.

## Requisitos

- Google Chrome instalado (se usa el canal `chrome` de Playwright, no hace falta descargar navegadores).
- Una base de prueba con al menos dos productos, un insumo y un proveedor, y el usuario administrador con la contraseña
  ya cambiada (no pendiente de cambio).

## Ejecución

1. Levantar la web sobre la base de prueba:

   ```powershell
   cd src/StockManufactura.Web
   $env:ConnectionStrings__StockManufactura = "Data Source=C:\ruta\a\prueba.db"
   dotnet run --urls http://localhost:5199
   ```

2. En otra consola:

   ```powershell
   cd tests/StockManufactura.Web.E2E
   $env:E2E_PASSWORD = "contraseña del administrador de la base de prueba"
   dotnet run
   ```

Variables opcionales: `E2E_URL` (por defecto `http://localhost:5199/`) y `E2E_USUARIO` (por defecto `Admin`).

El programa termina con código 0 si todos los controles pasan. Las capturas de las fallas y del menú de celular quedan
en `bin/Debug/net10.0/capturas`.
