# Feature: Localización multiidioma (es, en, pt-BR, zh-TW, ru)

**Objetivo**: Agregar soporte real de idiomas al app Mercader (.NET MAUI), con español como base.
**Branch**: MasterRes
**Estrategia de entrega**: trabajo inline en slices commiteables (~400 líneas por slice)
**Runner de tests**: `dotnet test Mercader.Tests/Mercader.Tests.csproj` (121 tests)
**Build de la app**: `dotnet build Mercader.csproj` (NO solo Mercader.Core — ese es solo la librería)
**Mirror Engram**: ⏳ pendiente. El servidor MCP de Engram resuelve el proyecto desde el cwd de la
sesión (`C:\Users\Lucas`, sin repo) y rechaza el write con `invalid_project` aunque se pase
`project: "mercader"`. Este archivo commiteado es la fuente de verdad hasta que se resuelva.

## Specs

### S1 — Idiomas soportados
La app debe soportar exactamente estos 5 idiomas, seleccionables en runtime:
- `es` — Español (actual, por defecto)
- `en` — English
- `pt-BR` — Português (Brasil)
- `zh-TW` — 繁體中文
- `ru` — Русский

### S2 — Persistencia de la selección
La selección de idioma debe persistir entre reinicios de la app (mismo mecanismo que el tema: `Preferences`).

### S3 — Formato de fechas y moneda según idioma
El formato de fechas y moneda debe seguir al idioma seleccionado, NO hardcodeado a `es-ES`.
Hoy hay 3 lugares con `es-ES` hardcodeado:
- `ViewModels/MainViewModel.cs` (MesLabel, MesGanancias)
- `Mercader.Core/Services/Calculators/BalanceCalculatorService.cs` (GenerarMeses)
- `Mercader.Core/Services/ExportExcelService.cs` (nombre de mes en Excel)

### S4 — Textos de la UI traducidos
Los textos visibles deben traducirse al idioma seleccionado: navegación del Shell, página de
Configuración, MainPage (encabezados de análisis, totales, exportar) y los 3 modales
(Venta/Gasto/Encargo).

### S5 — Sin dependencias externas
La localización NO debe agregar paquetes NuGet. Debe funcionar con lo ya disponible
(`System.Text.Json` 8.0.5 ya está referenciado en `Mercader.csproj`).

### S6 — Fallback seguro
Si falta una clave en el catálogo del idioma activo, la app debe mostrar el texto en español
(default), y si tampoco está, la propia clave. Nunca debe crashear ni dejar la UI vacía.

## Tasks

- T1 | S1,S5 | inline | Servicio `Loc` en Mercader.Core (catálogos, cultura, Get con fallback) ✅
- T2 | S1,S5 | inline | `TranslateExtension` (markup extension XAML) + cargador de catálogos JSON embebidos ✅
- T3 | S1,S2 | inline | 5 catálogos JSON (es, en, pt-BR, zh-TW, ru) + `Mercader.csproj` EmbeddedResource ✅
- T4 | S1,S2 | inline | Init en `App.xaml.cs` + Picker de idioma en Configuración con persistencia ✅
- T5 | S3 | inline | Reemplazar `es-ES` hardcodeado por `Loc.Culture` (3 archivos) ✅
- T6 | S4 | inline | Aplicar `{loc:Translate Key}` en Shell, Configuración, MainPage y 3 modales ✅
- T7 | S1,S2,S3,S4,S6 | inline | Build de `Mercader.csproj` + 121 tests ✅ / verificación en emulador ⏳
- T10 | S2,S6 | inline | Nombre nativo del idioma en el Picker + tests unitarios de `Loc` ✅

## Log

### L1 — Solicitud original del usuario
"Me gustaria analizar la idea de poner idiomas a la app enpezar con ingles, portuges, chino y ruso
apate del español que ya esta"

### L2 — Decisión de arquitectura
El usuario eligió "opción B: sistema centralizado" con inglés como primer piloto.
Se descartó `Maui.Localization` / `LocalizationManager`: el paquete `CommunityToolkit.Maui.Localization`
no existe en NuGet y la API `RegisterResource` no está disponible en MAUI 9.1.1.
→ Se implementa servicio propio `Loc` + markup extension, sin dependencias externas (S5).

### L3 — Incidente: MainPage.xaml.cs roto
Al agregar la localización se reconstruyó `MainPage.xaml.cs` con `write` y se perdieron 497 líneas
(handlers de export PDF/Excel, `RenderChartToPng`, handlers `InAgregar*`).
Además se compilaba `Mercader.Core.csproj` (librería) en lugar de `Mercader.csproj` (app), por lo que
el breakage no se detectaba.
→ Restaurado desde `e1fa6f5` en commit `b9071ae`. Build de la app: 0 errores, 94 tests OK.
→ Regla para esta feature: TODO build debe correr sobre `Mercader.csproj`.

### L4 — Cobertura de S4
La cobertura de S4 se limita a Shell + Configuración + MainPage + 3 modales en este slice.
Las páginas de listado (Gastos/Encargos/Venta) y las de edición quedan como tarea siguiente
(registrada abajo), porque son ~15 archivos adicionales.

### L5 — T5 y T6 completados
- `Loc.Culture` reemplaza los 3 `es-ES` hardcodeados (`MainViewModel`, `BalanceCalculatorService`,
  `ExportExcelService`). Los `ToString("C")` de los totales ahora siguen la cultura activa
  (antes seguían la del dispositivo).
- S4 aplicado con `{loc:Translate Key}` en `AppShell.xaml`, `Configuracion.xaml`, `MainPage.xaml`,
  `VentaModal.xaml`, `GastoModal.xaml`, `EncModal.xaml`, más los `DisplayAlert`/`DisplayActionSheet`
  de sus code-behinds.
- Los 5 catálogos quedaron con 120 claves idénticas (verificado por script) y las 115 claves usadas
  en el código existen en `es.json`.
- `IMarkupExtension<string>` no compila solo: `IMarkupExtension<T>` hereda de `IMarkupExtension`
  y hay que implementar `ProvideValue` explícitamente para la interfaz base.
- Las claves de catálogo son ASCII: la clave con acento (`Versión`) se renombró a `VersionApp`
  para no depender del parseo de identificadores no-ASCII del XAML.

### L6 — Tests de `Loc` (S6)
- `Mercader.Tests/LocTests.cs`: 27 tests que cubren fallback es→clave, cambio de idioma y cultura,
  evento `LanguageChanged`, `Format`/`FormatCurrency`/`FormatDate` y `RegisterCatalog`.
- `Loc` es estático y `SetLanguage` muta `CultureInfo.CurrentCulture` del hilo: la clase de tests
  restaura la cultura original en `Dispose` para no contaminar otros tests.
- Total: 121 tests, todos verdes. Build de `Mercader.csproj`: 0 errores.

### L7 — Riesgo revisado al setear la cultura global (S3)
- `Loc.SetLanguage` setea `CultureInfo.CurrentCulture` / `CurrentUICulture` globales. Se auditó el
  parseo de números: no hay `decimal.Parse`/`TryParse` ni `Convert.ToDecimal` sin cultura explícita;
  el input de usuario se parsea con `InvariantCulture` en `ValidationService`. Los `ToString("N0"/"N2")`
  de las entidades ahora siguen el idioma seleccionado, que es el efecto pedido.
- Pendiente T9: `ExportPdfService` todavía tiene encabezados de tabla en español mientras los nombres
  de mes ya salen en el idioma seleccionado → PDF mixto hasta completar T9.

## Pendiente (próximo slice)
- T8 | S4 | inline | Traducir Gastos.xaml, Encargos.xaml, Venta.xaml, EditarGastoPage.xaml,
  EditarVentaPage.xaml, EditarEncargoPage.xaml, ImageGalleryModal.xaml
- T9 | S3 | inline | `ExportPdfService.cs` — traducir encabezados de tabla por idioma
- T7 (resto) | S1,S2,S3,S4,S6 | inline | Verificación en emulador: cambiar idioma, reiniciar y
  confirmar persistencia + formatos de fecha/moneda por cultura
