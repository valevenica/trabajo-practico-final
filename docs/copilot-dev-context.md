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
