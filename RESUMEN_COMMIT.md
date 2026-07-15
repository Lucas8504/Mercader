feat(ui): SfDatePicker con dialog mode reemplaza DatePicker nativo

## ✨ Nueva funcionalidad
- ✅ SfDatePicker en modo Dialog reemplaza al DatePicker nativo en EditarEncargoPage y EncModal
- ✅ Label tappable muestra la fecha y abre el picker al tocarla
- ✅ Fecha se actualiza al cerrar el diálogo (evento Closed)
- ✅ Ya no se rompe visualmente en dark mode en Android

## 🎨 Diseño
- ✅ Theming completo con AppThemeBinding y colores del tema de la app
- ✅ Header, columnas, selección, y textos adaptados a light/dark mode
- ✅ Sin hardcode de colores — todo referenciado a StaticResource (Primary, Secondary, Gray100, etc.)

## ♻️ Refactor
- ✅ Eliminado styles.xml con hack de DatePickerDialog para Android
- ✅ Eliminado color datePickerDarkBackground de colors.xml
- ✅ MainActivity vuelve a Maui.SplashTheme (revertido styles.xml temporal)

## 📦 Dependencias
- ✅ Syncfusion.Maui.Toolkit 1.0.6 agregada (compatible con net8.0)
- ✅ ConfigureSyncfusionToolkit() en MauiProgram.cs

## 🔧 Técnico
- ✅ SfDatePicker oculto con HeightRequest="0" — no ocupa espacio visual
- ✅ Formato con PickerDateFormat.dd_MM_yyyy (enum, no string)
- ✅ v1.0.6 usa PickerTextStyle en vez de TextColor directo en las vistas
- ✅ v1.0.6 no tiene StrokeThickness en PickerSelectionView (usamos Stroke)
- ✅ Background del dialog body con AppThemeBinding (CardBackground / CardBackgroundDark)
- ✅ ColumnDividerColor con DividerColor / DividerColorDark
- ✅ FooterView agregado con OK/Cancel y AppThemeBinding para botones
- ✅ 0 errores de build, 0 warnings
