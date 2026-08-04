# Ejemplos de pruebas funcionales (Desktop)

Esta guia te permite validar funcionalidades clave con datos de ejemplo.

## 1) Login y seguridad

### Caso A: login correcto
- Usuario: `Admin`
- Correo: `admin@test.com`
- Password: `Admin123` (o la que tengas configurada)
- Resultado esperado:
- Accede al dashboard.
- Se registra auditoria de login exitoso.

### Caso B: login fallido y bloqueo
- Intentar 5 veces con password incorrecta.
- Resultado esperado:
- Mensajes de error de credenciales.
- Usuario bloqueado temporalmente.

## 2) Proveedores

### Alta de proveedor
- Nombre: `Metalurgica Sur`
- Razon social: `Metalurgica Sur S.A.`
- CUIT: `30-71234567-9`
- Ciudad: `Rosario`
- Resultado esperado:
- Se ve en listado.
- Queda activo por defecto.

### Activar/Desactivar
- Seleccionar proveedor creado y ejecutar toggle.
- Resultado esperado:
- Cambia estado Activo.
- Se refleja al recargar.

## 3) Productos

### Alta de producto
- Codigo: `PRD-001`
- Nombre: `Gear Assembly A`
- Costo fabricacion: `25000`
- Margen: `0.25`
- Precio sugerido: `31250`
- Resultado esperado:
- Producto visible en listado.
- No permite duplicar codigo.

## 4) Receta BOM

### Alta de item de receta
- Producto: `PRD-001`
- Recurso: `Acero SAE 1045`
- Cantidad: `1.5`
- Costo parcial manual: `12500`
- Resultado esperado:
- Item agregado a receta.
- Costo total receta actualizado.

### Deteccion de duplicados
- Intentar agregar mismo recurso al mismo producto.
- Resultado esperado:
- Mensaje de validacion por duplicado.

## 5) Ordenes de produccion (persistencia en base)

### Alta y flujo de estados
- Producto: `PRD-001`
- Cantidad planeada: `50`
- Observaciones: `Lote piloto`
- Acciones: Crear -> Planificar -> Iniciar -> Registrar (50) -> Finalizar
- Resultado esperado:
- Estado final: `Finalizada`.
- Fechas de inicio/fin registradas.

### Verificacion de persistencia (clave)
- Crear una orden en borrador.
- Cerrar la app.
- Abrir la app y volver al modulo.
- Resultado esperado:
- La orden sigue existiendo en el listado.

## 6) Backup y estado del sistema

### Backup manual
- Ejecutar backup desde modulo de backups.
- Resultado esperado:
- Archivo zip creado en carpeta de backups.
- Estado del sistema muestra ultimo backup.

### Estado general
- Ir a Dashboard y verificar tarjetas de estado.
- Resultado esperado:
- Conteos de productos/recursos/usuarios coherentes.
- Sin errores de carga de estado.

## 7) Smoke test de arranque

- Abrir la app.
- Verificar splash con logo.
- Iniciar sesion.
- Navegar por: Productos, BOM, Proveedores, Ordenes.
- Resultado esperado:
- Sin crashes ni excepciones visibles.

## Comandos rapidos utiles

- Ejecutar unit tests:
  - `dotnet test tests/StockManufactura.UnitTests/StockManufactura.UnitTests.csproj`
- Build desktop:
  - `dotnet build src/StockManufactura.Desktop/StockManufactura.Desktop.csproj`
- Ejecutable publish (ya generado):
  - `src/StockManufactura.Desktop/bin/Release/net10.0-windows/win-x64/publish/StockManufactura.Desktop.exe`
