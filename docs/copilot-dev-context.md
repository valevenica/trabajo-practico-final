# Contexto de desarrollo — StockManufactura Desktop

Sos un desarrollador especializado en aplicaciones de escritorio WPF con .NET 10.  
Este documento captura las decisiones de arquitectura, patrones establecidos y errores conocidos del proyecto.

---

## Arquitectura general

**Stack:**
- WPF (.NET 10.0-windows), patrón MVVM con CommunityToolkit.Mvvm
- Entity Framework Core 8 + SQLite (Desktop) / SQL Server (API)
- Inyección de dependencias con Microsoft.Extensions.DependencyInjection
- Serilog para logging
- Multi-proyecto: Domain → Application → Infrastructure → Desktop

**Carpeta del exe publicado:**
```
src/StockManufactura.Desktop/bin/Release/net10.0-windows/win-x64/publish/StockManufactura.Desktop.exe
```

**Comando de publicación:**
```powershell
Set-Location "c:\Users\vveni\trabajo-practico-final"
& "C:\Program Files\dotnet\dotnet.exe" publish src/StockManufactura.Desktop/StockManufactura.Desktop.csproj -c Release -r win-x64 --output "src/StockManufactura.Desktop/bin/Release/net10.0-windows/win-x64/publish" --self-contained=false
```
> Nota: usar `& "C:\Program Files\dotnet\dotnet.exe"` — el alias `dotnet` no siempre está disponible en PowerShell.

---

## Regla crítica: DbContext en Desktop (Transient DI)

**El problema más importante de este proyecto:**  
En Desktop, TODO es `AddTransient`. Cada vez que DI resuelve `StockManufacturaDbContext`, crea una instancia nueva. Si un `UnitOfWork` y sus repositorios individuales se inyectan por separado, cada uno tiene su **propio DbContext distinto**. Entonces `SaveChangesAsync()` en el UoW guarda un contexto vacío.

**La solución aplicada:**  
`UnitOfWork` crea todos sus repositorios internamente pasando su propio `_context`:
```csharp
public UnitOfWork(StockManufacturaDbContext context)
{
    _context = context;
    Recursos = new RecursoRepository(context);      // mismo context
    ExchangeRates = new ExchangeRateRepository(context); // mismo context
    // ... todos los repos usan el mismo context
}
```

**Regla:** Cuando un servicio de Application necesite guardar datos, debe usar **solo** `_unitOfWork.*` para todos los accesos a repos, **nunca** repos inyectados directamente junto con UoW.

**Servicios ya corregidos:**
- `ResourcePricingService` → solo usa `_unitOfWork`
- `MonetaryConfigurationService` → solo usa `_unitOfWork`
- `AuditLogService` → sigue usando repo directo + UoW separado (pendiente, no crítico porque rara vez falla visible)

---

## Regla: nunca asignar navigation properties a entidades de otro DbContext

Cuando EF Core ve `history.Product = product` y `product` fue cargado de otro DbContext (o con AsNoTracking de otro alcance), intenta insertarlo en la DB causando `UNIQUE constraint failed`.

**Regla:** Al crear entidades para `AddAsync`, usar **solo FK**:
```csharp
// CORRECTO
var history = new ProductCostHistory { ProductId = product.Id, ... };

// INCORRECTO — EF intenta insertar `product` de nuevo
var history = new ProductCostHistory { Product = product, ProductId = product.Id, ... };
```

---

## Regla: EF Core SQLite y `Guid[]` con Contains()

EF Core SQLite falla con `Guid[]` en `.Contains()` porque internamente intenta usar `ReadOnlySpan<Guid>` que viola restricciones genéricas.

**Siempre usar `List<Guid>`:**
```csharp
// CORRECTO
var ids = productIds.ToList(); // List<Guid>
query.Where(x => ids.Contains(x.ProductoId))

// INCORRECTO
var ids = productIds.ToArray(); // Guid[]
query.Where(x => ids.Contains(x.ProductoId)) // explota en runtime
```

---

## Regla: OrderBy sobre navigation properties en EF Core SQLite

EF Core SQLite no puede traducir `ThenBy(x => x.Proveedor.Nombre)` cuando `Proveedor` viene de un `Include`. **Hacer el sort en memoria:**
```csharp
var items = await _context.RecursoProveedores
    .AsNoTracking()
    .Include(x => x.Proveedor)
    .Where(x => x.RecursoId == recursoId)
    .ToListAsync(); // primero traer todo

return items
    .OrderByDescending(x => x.EsPrioritario)
    .ThenBy(x => x.Proveedor?.Nombre ?? string.Empty) // luego ordenar en memoria
    .ToList();
```

---

## Regla: Threading en ViewModels WPF

**Nunca** usar `Task.Run(async () => await asyncMethod())` para cargar datos. El DbContext creado en el UI thread no puede usarse en un thread de background (EF Core violation). Además, las `ObservableCollection` solo pueden modificarse desde el UI thread.

```csharp
// CORRECTO
var products = (await _unitOfWork.Productos.ListAsync()).OrderBy(x => x.Nombre).ToArray();

// INCORRECTO — causa crashes silenciosos y UI vacía
var products = await Task.Run(async () => await _unitOfWork.Productos.ListAsync());
```

---

## Regla: StaticResource en XAML

Si un `StaticResource` no existe en `App.xaml`, WPF lanza `XamlParseException` y la vista queda en blanco. El global exception handler la traga silenciosamente.

**Recursos disponibles en App.xaml:**
- `TextMutedBrush` (NO existe `TextSecondaryBrush`)
- `PrimaryButtonStyle`, `SecondaryButtonStyle`
- `CardBorderStyle`, `StatusMessageStyle`
- `BackgroundSoftLightBrush`, `BackgroundDarkMetalBrush`
- `PrimaryAccentBrush`, `SecondaryAccentBrush`
- `BoolToVisibilityConverter`

---

## Regla: Bindings en XAML

Un espacio antes del binding lo rompe — WPF lo trata como texto literal:
```xml
<!-- CORRECTO -->
<TextBlock Text="{Binding CurrentRate}" />

<!-- INCORRECTO — muestra " {Binding CurrentRate}" como texto -->
<TextBlock Text=" {Binding CurrentRate}" />
```

---

## Regla: CollectionViewSource para filtros en ComboBox

Si se filtra una `ObservableCollection` haciendo `.Clear()` y re-agregando items, el `ComboBox` pierde su `SelectedItem`. Usar `CollectionViewSource` para filtrar sin tocar la colección:

```csharp
private readonly CollectionViewSource _viewSource = new();
public ICollectionView View => _viewSource.View;

// En constructor:
_viewSource.Source = MiColeccion;
_viewSource.Filter += (s, e) => {
    if (e.Item is MiTipo item)
        e.Accepted = item.Nombre.Contains(SearchText);
};

// Al cambiar el texto de búsqueda:
View.Refresh();
```

---

## Patrones MVVM establecidos

### Dirty state (botón Guardar habilitado solo si hay cambios)
```csharp
private bool _loading;

[ObservableProperty] private bool _isDirty;

partial void OnNombreChanged(string value) { if (!_loading) IsDirty = true; }

partial void OnSelectedResourceChanged(Recurso? value)
{
    _loading = true;
    try { /* cargar datos */ }
    finally { _loading = false; IsDirty = false; }
}
```

### Fire-and-forget en constructor
```csharp
public MiViewModel() { _ = LoadAsync(); }
```

### Propiedad computada para binding negado
```csharp
public bool ShowPassword { get => _show; set => SetProperty(ref _show, value); }
public bool HidePassword => !ShowPassword; // para binding sin ConverterParameter
```

---

## Sistema de cotización del dólar

**Proveedor activo:** `DolarHoyProvider` → `https://dolarapi.com/v1/dolares/blue` → usa `venta`.  
**Proveedor alternativo:** `BluelyticsExchangeRateProvider` → `https://api.bluelytics.com.ar/v2/latest`.

**Flujo de prioridad:**
1. `ExchangeRate.EsPrioritaria = true` marca cuál cotización se usa para calcular
2. `GetLatestAsync()` devuelve la prioritaria primero, cae a la más reciente si no hay ninguna
3. Desde Finanzas → Config. Monetaria: seleccionar una fila → "★ Marcar prioritaria"
4. "🔄 Actualizar blue" → obtiene el precio actual de dolarapi.com y lo guarda como nueva entrada
5. "Recalcular todos los USD" (en Insumos) → recalcula todos los insumos USD con la cotización prioritaria y propaga a productos

---

## Migraciones EF Core

**Generar nueva migración:**
```powershell
Set-Location "c:\Users\vveni\trabajo-practico-final\src\StockManufactura.Infrastructure"
& "C:\Program Files\dotnet\dotnet.exe" ef migrations add NombreMigracion --startup-project "../StockManufactura.Desktop"
```
> El output puede ser silencioso en PowerShell. Verificar con `dir Migrations`.  
> El comando SIEMPRE genera dos archivos: `xxxx_Nombre.cs` y `xxxx_Nombre.Designer.cs`.  
> No crear manualmente archivos `.cs` de migración cuando `dotnet ef` ya generó uno — resultará en clase `partial` con métodos duplicados.

---

## Menú lateral — comandos de navegación

Los comandos del menú están en `MainWindowViewModel`. Todos llaman `EnsureDashboardAvailable()` que requiere que `AuthSession.Current?.Usuario` no sea null (el usuario debe estar logueado).

**Para agregar una nueva sección al menú:**
1. Agregar botón en `MainWindow.xaml` con `Command="{Binding NavigateXCommand}"`
2. Agregar `ICommand NavigateXCommand` en `MainWindowViewModel`
3. Agregar `bool IsXSelected => _activeMenuKey == "X"` para el highlight
4. Implementar `NavigateX()` siguiendo el mismo patrón que los existentes
5. Agregar `DataTemplate` en el `ContentControl.Resources` del MainWindow

---

## Estructura multi-proveedor de insumos

Cada `Recurso` puede tener múltiples `RecursoProveedor` asociados. El que tiene `EsPrioritario = true` define el precio del insumo.

**Al agregar un proveedor:**
- Si es el primero → se marca como prioritario y actualiza el precio del insumo
- Si no es el primero → se agrega sin ser prioritario
- Botón "★ Prioritario" permite cambiar cuál proveedor es el prioritario

---

## Logs de la aplicación

```
C:\Users\vveni\Documents\StockManufactura\Logs\StockManufactura[YYYYMMDD].log
```
Los errores capturados por `try-catch` en ViewModels NO se loguean automáticamente.  
Para diagnosticar: agregar `Log.Error(ex, "contexto")` en el catch antes de `StatusMessage`.

---

## Git workflow

- Rama nueva por feature/fix: `git checkout -b fix/nombre-descriptivo`
- Commits pequeños y temáticos: `fix(insumos):`, `feat(monetary):`, `chore:`
- Merge a main con `--no-ff` cuando la rama está completa
- No commitear binarios del publish (son grandes y cambian con cada build)

---

## Regla: mantener actualizada la documentación del Proyecto Final (tesis)

El documento `C:\Users\vveni\Desktop\Proyecto Final V5.docx` es la entrega académica (UTN FRR, cátedra Proyecto) y describe el mismo sistema que se desarrolla en este repositorio. **Cada vez que se hace un cambio funcional en el programa** (nueva pantalla, nuevo campo del dominio, cambio de arquitectura, nueva entidad, etc.), hay que revisar si ese cambio afecta algo de lo documentado — especialmente:

- Requerimientos Funcionales / No Funcionales
- Análisis Detallado de las funciones (tabla)
- Arquitectura del producto de software
- Diagrama de clases / Modelo de Datos (si se agrega o modifica una entidad)
- Estándares de Desarrollo (si cambia un patrón usado en todo el proyecto)

Si corresponde, actualizar el `.docx` y guardar una nueva versión. **No sobrescribir la versión anterior**: cada actualización incrementa el número de versión menor.

**Convención de versionado:** `V5.n`, donde `n` es un contador que arranca en `0` (el estado actual, `Proyecto Final V5.docx`, es `V5.0`). La próxima actualización se guarda como `Proyecto Final V5.1.docx`, la siguiente `V5.2.docx`, etc. Un cambio de versión mayor (`V6`) solo aplica ante una revisión estructural grande (ej. una nueva entrega de la cátedra), no para actualizaciones incrementales de contenido.

**Requisitos formales de la entrega:** están definidos en `docs/Proyecto - Apunte General 2017.pdf` (Cátedra Proyecto, cuando se subió como documento en el chat). Ahí están los puntos obligatorios del índice (Tema, Objetivos, Marco, Justificación, Ventaja Competitiva, Factibilidad —incluye Económica—, Entorno de Desarrollo, Planificación/Metodología, Presupuesto, Documentación de Análisis y Diseño con Arquitectura/Diagrama de clases/Modelo de Datos/Estándares de Desarrollo/Conclusión Personal). Usar ese apunte como checklist antes de dar por completa una actualización.

### Registro de cambios pendientes de reflejar en el .docx

Cada entrada acá es un cambio funcional ya hecho en el código que todavía no se volcó a `Proyecto Final V5.docx`. Al hacer la próxima pasada de actualización del documento, tomar estas entradas, aplicarlas, y guardar como `Proyecto Final V5.1.docx` (la siguiente ronda sería V5.2, etc.), tachando o quitando las entradas ya reflejadas.

- ~~Restablecer base de datos (reseteo total a estado inicial).~~ Aplicado el 2026-08-12 en `Proyecto Final V5.1.docx`: nuevo ítem en Requerimientos Funcionales, y bullet en Estándares de Desarrollo sobre el patrón de confirmación para acciones destructivas.

### Historial de versiones del .docx

- `Proyecto Final V5.docx` — versión base (V5.0).
- `Proyecto Final V5.1.docx` (2026-08-12) — agrega código de proveedor por insumo (`RecursoProveedor.CodigoProveedor`), la pantalla de carga masiva de precios desde Excel, el restablecimiento total de base de datos desde Backups, y limpia la mención a entidades ya eliminadas del código (`ResourceCostCalculation`, `Stock`, `CostoIndirecto`) en la sección "Diagrama de clases". Es la versión más actualizada; `V5.docx` no fue modificado.

### Cómo editar el .docx (Word COM vía PowerShell)

No hay una herramienta directa para leer/escribir `.docx`; se edita programáticamente con Word COM (`New-Object -ComObject Word.Application`) desde PowerShell. Reglas aprendidas a los golpes en la sesión donde se armó V5 (evitan corromper o perder contenido del documento):

- **Trabajar siempre sobre una copia** (`Copy-Item` a un nombre nuevo) antes de tocar el archivo real. Nunca abrir el original con `ReadOnly=$false` para editar directo.
- **Nunca usar `Paragraph.Previous()` ni `Paragraph.Next()` inmediatamente después de una asignación masiva a `Range.Text`** (insertar varios párrafos de una vez). Esa navegación devuelve resultados incorrectos o `$null` justo después de una edición masiva — en cambio, quedarse con la colección `Range.Paragraphs` que devuelve la propia asignación e indexar directo ahí (`@($rng.Paragraphs)[0]`, etc.).
- **Asignar `.Text` a un `Range` que ya incluye la marca de párrafo final (p. ej. `paragraph.Range`) sin una `` `r `` final en el string de reemplazo "absorbe" esa marca y fusiona el párrafo con el siguiente.** Si el texto de reemplazo no tiene saltos de línea propios, el resultado se une silenciosamente al párrafo de al lado. Solución: si el reemplazo es de una sola línea dentro de un párrafo ya existente (renombrar un título, por ejemplo), recortar el rango un carácter antes del final (`Range.End - 1`) para no tocar la marca. Si es contenido nuevo (varios párrafos), terminar siempre el string unido con una `` `r `` extra al final para forzar una marca nueva y no reutilizar la original.
- **`Paragraph.Next()` no funciona de forma confiable en el último párrafo del documento**, incluso después de `InsertParagraphAfter()`. Para insertar al final del documento, re-obtener el último párrafo con `$doc.Paragraphs.Item($doc.Paragraphs.Count)` en vez de navegar con `.Next()`.
- **Insertar imágenes** (diagramas) funciona bien con SVG directo (`doc.InlineShapes.AddPicture(rutaSvg, $false, $true, range)`) en Word 2016+/365 — no hace falta convertir a PNG.
- Guardar (`doc.Save()`), cerrar (`doc.Close($false)`) y `word.Quit()` **siempre**, incluso en el `catch`, para no dejar procesos `WINWORD.EXE` huérfanos con el archivo bloqueado (si un script falla a mitad de camino, matar el proceso con `Get-Process -Name WINWORD | Stop-Process -Force` antes de reintentar).
- Los archivos `.ps1` que contengan texto en español con tildes deben guardarse con BOM UTF-8 (`[System.IO.File]::WriteAllText($path, $content, (New-Object System.Text.UTF8Encoding($true)))`), si no PowerShell 5.1 los interpreta mal y tira errores de sintaxis en las líneas con acentos.
- Verificar cada paso releyendo los títulos (`Título 1`/`Título 2`) del documento completo antes de seguir al siguiente paso — los bugs de fusión de párrafos no siempre tiran una excepción, a veces el script termina "OK" pero el contenido quedó mal.
