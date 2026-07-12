# Modelo de datos

Las PK son `long` autoincremental, heredando `MultiTenantEntityBase` de la plataforma (ADR-09: no pelear contra el patrón del código base). La no-adivinabilidad que este doc pedía originalmente con PKs GUID se logra donde importa: `Foto.StorageKey` (GUID único, lo único que aparece en las keys de storage — los Id internos jamás se usan en rutas de archivos) y los códigos de acceso con entropía propia. Implementado en `backend/AcgFotos.Fotos.Domain`.

```
Evento 1──n Curso 1──n Album (alumno)
  │            │           │
  │            └── Foto (grupal, CursoId, AlbumId = null)
  │                        └── Foto (individual, AlbumId)
  ├── TamanoPrecio (catálogo de tamaños y precios del evento)
  └── Pedido 1──n PedidoItem ──▶ Foto + TamanoPrecio

CodigoAcceso ──▶ Album (1 a 1, o varios códigos por álbum si hace falta reemplazar uno)
```

## Entidades

### Evento
Una graduación en un colegio.
- `Id`, `Nombre` ("Egresados 7ºB Colegio San José 2026"), `Colegio`, `Fecha`
- `FechaExpiracion` (hasta cuándo las familias pueden ver/comprar; permite limpiar storage después)
- `Estado`: Borrador | Publicado | Cerrado

### Curso
División dentro del evento (7ºA, 7ºB...). Existe para que las **fotos grupales** se compartan entre todos los alumnos del curso.
- `Id`, `EventoId`, `Nombre`

### Album
Las fotos individuales de UN alumno. Es la unidad de acceso de la familia.
- `Id`, `CursoId`, `NombreAlumno`

### Foto
- `Id`, `EventoId`, `CursoId`, `AlbumId` (**null ⇒ foto grupal del curso**, visible para todos los álbumes del curso)
- `NombreArchivoOriginal`, `Ancho`, `Alto`, `TamanoBytes`
- `EstadoProcesamiento`: Pendiente | Lista | Error (derivados generados o no)
- Claves de storage derivables por convención: `originals/{EventoId}/{Id}.jpg`, etc.

### CodigoAcceso
- `Id`, `AlbumId`, `Codigo` (corto, legible, con entropía suficiente; ej. `K7F3-9QMD`), `Activo`
- Regla: un código da acceso a las fotos individuales de su álbum **más** las grupales del curso del álbum.

### TamanoPrecio
Catálogo por evento (los precios cambian entre eventos; los pedidos ya hechos guardan copia del precio).
- `Id`, `EventoId`, `Nombre` ("10x15", "13x18", "20x30"), `PrecioUnitario`, `Orden`, `Activo`

### Pedido
- `Id`, `AlbumId`, `NombreContacto`, `TelefonoContacto`
- `Estado`: **Pendiente → Impreso → Entregado** (+ `Cancelado`; fase 2 agrega `Pagado` entre Pendiente e Impreso)
- `Total` (snapshot al confirmar), `CreadoEn`
- Fase 2: `MedioPago` (MercadoPago | Efectivo), `MercadoPagoPreferenciaId`, `PagadoEn`

### PedidoItem
- `Id`, `PedidoId`, `FotoId`, `TamanoPrecioId`, `Cantidad`
- `PrecioUnitarioSnapshot` (el precio vigente al confirmar; el catálogo puede cambiar después)

### UsuarioAdmin
Cubierto por la plataforma heredada (`gen_Usuarios` + roles/permisos, ASP.NET Identity): no existe como entidad del vertical.

## Reglas de negocio clave

1. **Visibilidad**: código → álbum → `fotos individuales del álbum ∪ fotos grupales del curso`. Ninguna consulta pública puede salirse de ese conjunto.
2. **Snapshot de precios**: `PedidoItem` congela el precio; editar `TamanoPrecio` no altera pedidos existentes.
3. **Pedido confirmado es inmutable** para la familia; para modificar, contacta al fotógrafo (que puede cancelar y la familia rehace). Simplifica muchísimo el MVP.
4. **Lista de impresión**: por evento, el admin exporta la agregación `foto original + tamaño + suma de cantidades` de pedidos en estado Pendiente/Pagado, agrupada por álbum, para llevar al laboratorio.
