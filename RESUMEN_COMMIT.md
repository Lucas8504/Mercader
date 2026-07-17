feat(encargos): rediseño de lista con búsqueda, filtros y estados

## ✨ Nueva funcionalidad
- ✅ Barra de búsqueda por nombre de cliente o descripción del producto
- ✅ Filtros horizontales: TODOS, PENDIENTES, ENTREGADOS
- ✅ Badge de estado: PENDIENTE (naranja) / ENTREGADO (verde)
- ✅ Botón ✓ marca como ENTREGADO sin eliminar el encargo de la lista
- ✅ Campo `Estado` agregado a la entity Encargo (default: "PENDIENTE")

## 🎨 Diseño
- ✅ Tarjetas rediseñadas: badge, cliente, teléfono, descripción, fecha, desglose, total
- ✅ Iconos de acción inline (✓ ✏️ 🗑️) en lugar de SwipeView
- ✅ Colores armonizados: botones Volver/Cancelar en azul-gris (#6B7B8D / #8E99A4)
- ✅ EstadoColorConverter para badge dinámico

## 🔧 Técnico
- ✅ EncargosViewModel: TextoBusqueda, FiltroEstado, AplicarFiltros(), MarcarEntregadoCommand
- ✅ Búsqueda filtra por Nombre o Descripcion (case-insensitive)
- ✅ Filtros por estado con auto-aplicación al cambiar
- ✅ Datos existentes sin estado se muestran como PENDIENTE (?? "PENDIENTE")
- ✅ 0 errores de build
