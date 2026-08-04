# StockManufactura

Sistema para PyME manufacturera con arquitectura limpia, MVVM en Desktop y persistencia SQLite.

## Pruebas funcionales guiadas

- Ver ejemplos por modulo en `docs/ejemplos-pruebas-funcionales.md`.

## Diseno actual de recursos y cotizacion monetaria

El sistema ya no sincroniza precios de recursos desde proveedores externos.

- Los precios de recursos se cargan manualmente por el usuario.
- La unica actualizacion automatica es la cotizacion del dolar.
- Todos los costos de fabricacion se calculan en ARS.

## Proveedores

Proveedor representa una entidad comercial y no participa en sincronizacion automatica.

Campos principales:

- Nombre
- Razon social
- CUIT
- Direccion
- Ciudad
- Provincia
- Pais
- Telefono
- Email
- Persona de contacto
- Observaciones
- Activo
- Fecha de alta

## Recursos

Recurso incluye:

- Codigo
- Nombre
- Descripcion
- Categoria
- Unidad de medida
- Stock actual
- Stock minimo
- Precio
- Moneda (ARS, USD)
- Proveedor habitual
- Fecha ultima actualizacion
- Observaciones
- Activo

Reglas:

- Si moneda = ARS, costo = precio ingresado.
- Si moneda = USD, costo = precio USD x cotizacion vigente.

## Configuracion Monetaria

Modulo dedicado para administrar cotizacion del dolar:

- Ver cotizacion actual
- Actualizacion manual
- Actualizacion automatica
- Seleccion de fuente
- Historial de cotizaciones

Entidad principal: ExchangeRate

- Valor
- Fecha
- Fuente
- Usuario
- Automatica

### Open/Closed

Se utiliza la interfaz IExchangeRateProvider para soportar nuevas fuentes sin modificar el servicio central.

Implementacion inicial:

- DolarHoyProvider (placeholder en esta fase)

Futuras fuentes posibles:

- Banco Nacion
- BCRA
- Bluelytics
- APIs privadas

## Historial y trazabilidad

Cada cambio de precio de recurso genera ResourcePriceHistory con:

- Fecha
- Usuario
- Precio anterior
- Precio nuevo
- Moneda
- Cotizacion utilizada
- Observaciones

Cada calculo de costo genera ResourceCostCalculation con:

- Recurso
- Fecha de calculo
- Cotizacion utilizada
- Costo en pesos

## Backups

La informacion de cotizaciones e historiales forma parte del backup SQLite porque se almacena en las mismas tablas de la base principal.

## Arquitectura

Se mantiene:

- Clean Architecture
- MVVM
- Repository Pattern
- Unit Of Work
- SOLID

Toda la logica de negocio se implementa en Application. Las Views no contienen logica de negocio.
