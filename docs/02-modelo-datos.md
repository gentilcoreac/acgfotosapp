# Modelo de datos

Las PK son `long` autoincremental, heredando `MultiTenantEntityBase` de la plataforma (ADR-09: no pelear contra el patrón del código base). La no-adivinabilidad que este doc pedía originalmente con PKs GUID se logra donde importa: `Foto.StorageKey` (GUID único, lo único que aparece en las keys de storage — los Id internos jamás se usan en rutas de archivos) y los códigos de acceso con entropía propia. Implementado en `backend/AcgFotos.Fotos.Domain`.

**Naming genérico (ADR-10, 2026-07-14)**: el rename `Curso` → **Grupo**, `Album (alumno)` → **Participante** y `Colegio` → **LugarOrganizacion** se aplicó de punta a punta (entidades, tablas, API, front y UI) para bancar eventos no escolares. Migración `RenombrarGrupoParticipante`.

```
Evento 1──n Grupo 1──n Participante
  │            │           │
  │            └── Foto (grupal, GrupoId, ParticipanteId = null)
  │                        └── Foto (individual, ParticipanteId)
  ├── TamanoPrecio (catálogo de tamaños y precios del evento)
  └── Pedido 1──n PedidoItem ──▶ Foto + TamanoPrecio

CodigoAcceso ──▶ Participante (1 a 1, o varios códigos por participante si hace falta reemplazar uno)
```

## Entidades

### Evento
Una sesión de fotos con venta a familias: una graduación, un cumpleaños, un torneo.
- `Id`, `Nombre` ("Egresados 7ºB Colegio San José 2026"), `LugarOrganizacion` (colegio, club, salón — opcional), `Fecha`
- `FechaExpiracion` (hasta cuándo las familias pueden ver/comprar; permite limpiar storage después)
- `Estado`: Borrador | Publicado | Cerrado

### Grupo
División dentro del evento (7ºA, 7ºB, la categoría de un torneo). Existe para que las **fotos grupales** se compartan entre todos los participantes del grupo.
- `Id`, `EventoId`, `Nombre`

### Participante
Las fotos individuales de UNA persona del grupo (su "álbum"). Es la unidad de acceso de la familia.
- `Id`, `GrupoId`, `Nombre`

### Foto
- `Id`, `EventoId`, `GrupoId`, `ParticipanteId` (**null ⇒ foto grupal del grupo**, visible para todos los participantes del grupo)
- `NombreArchivoOriginal`, `Ancho`, `Alto`, `TamanoBytes`
- `EstadoProcesamiento`: Pendiente | Lista | Error (derivados generados o no)
- Claves de storage derivables por convención: `originals/{EventoId}/{Id}.jpg`, etc.

### CodigoAcceso
- `Id`, `ParticipanteId`, `Codigo` (corto, legible, con entropía suficiente; ej. `K7F3-9QMD`), `Activo`
- Regla: un código da acceso a las fotos individuales de su participante **más** las grupales del grupo del participante.

### TamanoPrecio
Catálogo por evento (los precios cambian entre eventos; los pedidos ya hechos guardan copia del precio).
- `Id`, `EventoId`, `Nombre` ("10x15", "13x18", "20x30"), `PrecioUnitario`, `Orden`, `Activo`

### Pedido
- `Id`, `ParticipanteId`, `NombreContacto`, `TelefonoContacto`
- `Estado`: **Pendiente → Impreso → Entregado** (+ `Cancelado`; fase 2 agrega `Pagado` entre Pendiente e Impreso)
- `Total` (snapshot al confirmar), `CreadoEn`
- Fase 2: `MedioPago` (MercadoPago | Efectivo), `MercadoPagoPreferenciaId`, `PagadoEn`

### PedidoItem
- `Id`, `PedidoId`, `FotoId`, `TamanoPrecioId`, `Cantidad`
- `PrecioUnitarioSnapshot` (el precio vigente al confirmar; el catálogo puede cambiar después)

### UsuarioAdmin
Cubierto por la plataforma heredada (`gen_Usuarios` + roles/permisos, ASP.NET Identity): no existe como entidad del vertical.

## Ejemplo con datos: usuarios, códigos y QR (2026-07-15)

**Usuarios** (plataforma, `gen_*`): las familias NO tienen usuario (ADR-02).

| Usuario | Tenant | Rol | Ve |
|---|---|---|---|
| `root` | 1 (root) | Administrador (PermisoRoot) | Administración + Plataforma; asiste al fotógrafo impersonándolo |
| `fotografo` | 2 (su negocio) | Fotógrafo (PermisoFotos) | Solo Fotos → Eventos / Grupos / Galería / Tarjetas |

**Vertical** (`fot_*`, tenant 2) — colación con 2 grupos:

```
Evento 1 "Colación 2026"  (LugarOrganizacion: "Colegio San José")
├── Grupo 10 "7ºA"
│   ├── Participante 100 "Ana Pérez"   ── CodigoAcceso "K7F3-9QMD" (Activo)
│   ├── Participante 101 "José López"  ── CodigoAcceso "B2WM-4XKT" (Activo)
│   └── Foto 5001..5020  (GrupoId 10, ParticipanteId NULL → grupales: banderas, formación)
│       Foto 5021..5025  (ParticipanteId 100 → individuales de Ana)
│       Foto 5026..5031  (ParticipanteId 101 → individuales de José)
└── Grupo 11 "7ºB" ...
```

- El código nace CON el participante (al guardarlo en el ABM de Grupos) y es único y revocable (se desactiva y se genera otro).
- La **tarjeta** (pantalla Tarjetas) imprime por participante: nombre + código + QR. El QR codifica `UrlCanjeTemplate` con el código: `https://<dominio>/canje/B2WM-4XKT`.
- La familia de José escanea su QR (o tipea el código) → **ve fotos 5001..5020 (grupales) ∪ 5026..5031 (las de José)**. Nada más: ni las individuales de Ana ni otros grupos. Un solo QR por familia: las grupales entran solas.
- En Fase 2 el canje emite un token de sesión anónimo acotado a ese conjunto (sin registro); el diseño prevé sumar OTRO código a la misma sesión (hermanos / persona en dos grupos — ver docs/05).

## Reglas de negocio clave

1. **Visibilidad**: código → participante → `fotos individuales del participante ∪ fotos grupales de su grupo`. Ninguna consulta pública puede salirse de ese conjunto.
2. **Snapshot de precios**: `PedidoItem` congela el precio; editar `TamanoPrecio` no altera pedidos existentes.
3. **Pedido confirmado es inmutable** para la familia; para modificar, contacta al fotógrafo (que puede cancelar y la familia rehace). Simplifica muchísimo el MVP.
4. **Lista de impresión**: por evento, el admin exporta la agregación `foto original + tamaño + suma de cantidades` de pedidos en estado Pendiente/Pagado, agrupada por participante, para llevar al laboratorio.
