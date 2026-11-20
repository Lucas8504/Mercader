# Feature: Localización multiidioma (es, en, pt-BR, zh-TW, ru)

**Objetivo**: Agregar soporte real de idiomas al app Mercader (.NET MAUI), con español como base.
**Branch**: MasterRes
**Estrategia de entrega**: trabajo inline en slices commiteables (~400 líneas por slice)
**Runner de tests**: `dotnet test Mercader.Tests/Mercader.Tests.csproj` (137 tests)
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
- T7 | S1,S2,S3,S4,S6 | inline | Build de `Mercader.csproj` + 137 tests ✅ / verificación en emulador ✅
- T10 | S2,S6 | inline | Nombre nativo del idioma en el Picker + tests unitarios de `Loc` ✅
- T8 | S4 | delegated | Traducir páginas de listado y edición + ImageGalleryModal + 169 claves nuevas en los 5 catálogos ✅
- T9 | S3,S4 | delegated | Traducir `ExportPdfService` y `ExportExcelService` (cabeceras, hojas, métricas, fechas y moneda por cultura) ✅
- T11 | S3,S4 | inline | Bugs de localización: lógica de período desacoplada del idioma, pickers estáticos, moneda del chart y textos residuales ✅

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

### L8 — T8/T9 delegadas a un writer: rate limit a mitad de camino
- T8/T9 se delegaron a `gentle-ai-worker` (razón de writer: unidad grande, 21 archivos, catálogos
  sincronizados entre sí y con el código). El worker agregó las 169 claves de UI/export a
  `es.json` y tradujo 8 XAML + 7 code-behinds, pero un `429 Too Many Requests` del proveedor
  cortó la ejecución antes de traducirlas a los otros 4 idiomas y antes de tocar los exports.
- No es un defecto de Gentle AI: es un rate limit del proveedor de modelos. Se retomó la misma
  sesión del worker (conserva el contexto de las 169 claves) y completó el trabajo.
- Trampa evitada: el chequeo de columna de moneda en `ExportExcelService` comparaba el TEXTO de la
  cabecera (`Contains("PRECIO"/"MONTO"/"TOTAL")`). Con cabeceras traducidas se rompía. Se
  desacopló con metadata explícita: `bool[] columnasMoneda` por hoja.

### L9 — Bugs de localización encontrados en el barrido final (S3/S4)
Un barrido de textos literales en XAML y de `switch` sobre strings en Core encontró 5 defectos
reales que la suite existente no cubría:
1. **Lógica de período atada al idioma (grave).** `BalanceCalculatorService` filtraba y agrupaba
   con `switch` sobre `"Días"/"Semanas"/"Meses"/"Años"` en español, pero la UI ya entregaba la
   etiqueta traducida ("Months", "Месяцы") → el filtro caía al caso por defecto y el balance se
   calculaba sobre el rango equivocado. Afectaba a 4 de 5 idiomas.
2. **Picker estático pisando al binding.** `MainPage.xaml` declaraba `ItemsSource` (traducido) Y
   `<Picker.Items>` hardcodeado ("Días/Semanas/Meses"); el estático gana y el selector quedaba en
   español, además de perder la opción "Años".
3. **Picker de recordatorio en español.** `Configuracion.xaml` traía las 5 opciones de recordatorio
   como `<x:String>` literales.
4. **Moneda del chart hardcodeada.** `ChartService.FormatearValorEntero` usaba `"$"` fijo, así que
   el eje del gráfico mostraba "$20K" mientras las tarjetas mostraban "25.700,00 €".
5. **Textos residuales.** `AppShell.xaml` tenía `Title="Home"` y el diálogo de confirmación de
   cancelación se titulaba "Cancelar" (faltaba la clave `Confirmar`).

### L10 — T11: correcciones aplicadas y verificadas
- **Claves canónicas de período (nuevo `Mercader.Core/Services/Calculators/Periodos.cs`)**:
  `Periodos.Normalizar` traduce cualquier etiqueta (clave canónica, legacy con tilde o traducción
  del idioma activo) a una clave estable de lógica. `BalanceCalculatorService` normaliza antes de
  filtrar y de agrupar. La UI puede mostrar lo que quiera; el cálculo no cambia con el idioma.
- Se quitó el `<Picker.Items>` estático de `MainPage.xaml` y las opciones del recordatorio ahora se
  inyectan desde `Loc.Get` en `Configuracion.xaml.cs` (5 claves nuevas × 5 catálogos).
- `ChartService.FormatearValorEntero` usa el símbolo y el separador decimal de la cultura activa,
  con el símbolo antes o después según `CurrencyPositivePattern`.
- Se agregó la clave `Confirmar` a los 5 catálogos para el diálogo de cancelación.
- **Test-first**: `Mercader.Tests/BalanceCalculatorPeriodoTests.cs` (8 tests). RED observado:
  4 fallaban (claves canónicas `Dias`/`Anios` y etiquetas traducidas `Days`/`Years` caían al
  default: 130 en vez de 133/10) y 4 pasaban fijando el comportamiento previo (`Días`, `Años`,
  `Semanas`, `Meses`). GREEN: 137/137 tests.
- `LocTests` y el test nuevo comparten la colección xunit `"Localization"` porque ambas clases mutan
  el estado estático de `Loc`.
- **Verificación en emulador** (`emulator-5554`, 1080x2340): en español "0,00 €", "25.700,00 €",
  "noviembre 2026", flyout 100% traducido; tras cambiar a inglés la app completa queda en inglés
  ("Home/Sale/Expense/Order", "$0.00", "November 2026") y el selector de período ofrece
  "Days/Weeks/Months/Years". Al elegir "Days" el eje del gráfico muestra fechas diarias
  (`14/11`, `15/11`…) y no nombres de mes: confirma que la lógica ya no depende del idioma.
- Catálogos: **299 claves idénticas** en los 5 idiomas (verificado por script y por
  `LocalizationXamlTests`).
- **Risk: item 3** — se cambia un contrato compartido (`BalanceCalculatorService` /
  `Periodos.Normalizar`) del que dependen `MainViewModel` y las exportaciones. La corrección mantiene
  la compatibilidad hacia atrás (las etiquetas legacy en español siguen funcionando).

## Pendiente
- **Mirror Engram**: sigue bloqueado por `invalid_project` (el servidor MCP resuelve el proyecto
  desde el cwd de la sesión, `C:\Users\Lucas`, sin repo). Este archivo commiteado es la fuente de
  verdad. Reintentar cuando Engram resuelva el repo correcto.
- **Error preexistente de PDFsharp**: `ExportPdfService` lanza "An XGraphics object already exists
  for this page". No es de esta feature (existe en la base) y no fue tocado.
- La preferencia "moneda" de Configuración sigue sin aplicarse al formato de los montos (hoy mandan
  la cultura y el símbolo de la locale). Es una inconsistencia preexistente de la app, fuera del
  alcance de esta feature.
