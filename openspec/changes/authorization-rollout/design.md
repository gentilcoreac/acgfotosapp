## Context

Ver `proposal.md` § Why. Acá el relevamiento que define el trabajo.

**La maquinaria ya está construida y probada.** `EndpointAuthoritation` (Core) resuelve los permisos
efectivos del usuario contra el catálogo de endpoints, cachea por versión de autorización, devuelve
403 con la forma estándar y audita el deny. Hay un host de tests con el flag en `true`
(`AuthzWebApplicationFactory`) y siete archivos de test que lo ejercitan. **Este change no construye
mecanismo: puebla datos y prende un flag.**

**Lo que hoy está vacío**:

| Tabla | Estado actual |
|---|---|
| `gen_Permisos` | Una fila: `PermisoRoot` |
| `gen_Endpoints` | Vacío — se puebla llamando `GET /api/general/discover` |
| permiso→endpoint | Sin filas |
| rol→permiso | Solo lo mínimo de root |

**Superficie a cubrir**: ~165 endpoints — 75 acciones declaradas explícitamente (51 en
`AcgFotos.Base.Controllers`, 21 en `AcgFotos.Fotos.Controllers`, 3 en `AcgFotos.Api`) más 18
controllers que heredan las 5 acciones del CRUD genérico (`update`, `delete`, `get by id`, `get all`,
`csv`).

**Quién se ve afectado al prender el flag** — `AppContext.CheckPemissions()` exime a root salvo
impersonando:

- root → sin cambios, bypassa.
- sesiones de familia → ya resuelto, tienen rama propia (`AllowFamiliaSession`) y tests.
- el fotógrafo (no-root) → bloqueado hasta que su rol tenga permisos.
- la suite → los tests que usan root siguen pasando; los que usan `TestData.UserB` (~151 call sites) y
  `AdminB` (~61) fallan hasta mapear.

## Goals / Non-Goals

**Goals**

- Que la matriz de permisos se evalúe en la aplicación real, no solo en el host de tests.
- Que una base nueva nazca con la autorización operativa, sin pasos manuales.
- Que la taxonomía de permisos sea legible: que alguien pueda mirar un rol y entender qué puede hacer.

**Non-Goals**

- Construir o modificar el mecanismo de autorización. Existe, funciona y está testeado.
- Rediseñar los roles existentes o el modelo de licencias.
- Una UI de administración de permisos más allá de la que ya existe.
- La puerta pública de registro autogestionado. Este change habilita al fotógrafo a administrarse
  solo; el formulario de "registrate" es otro trabajo.

## Decisions

### D1 — Taxonomía por área funcional, no por operación

Un permiso por área funcional coherente, no un permiso por operación CRUD. La granularidad fina
(`Evento.Leer`, `Evento.Escribir`, `Evento.Borrar`) multiplica por tres o cuatro el catálogo para
distinguir casos que hoy nadie necesita distinguir: no hay ningún rol previsto que deba leer eventos
pero no crearlos.

Se arranca con permisos de área y se parte uno en dos el día que aparezca un rol real que necesite la
distinción. Partir después es barato —una fila nueva y re-mapear los endpoints de esa área—; nacer con
un catálogo de 500 filas que nadie usa no lo es.

Áreas previstas: administración de plataforma (root), operación del vertical Fotos, y las áreas
heredadas de la plataforma —usuarios, roles y permisos, tenants, licencias, auditoría, parámetros,
menús, archivos—.

### D2 — El catálogo de endpoints se puebla por `discover`, no a mano

`GET /api/general/discover` enumera todos los endpoints por reflexión sobre los controllers
registrados y escribe `gen_Endpoints`. Es la fuente correcta: una lista escrita a mano se desactualiza
en el primer endpoint nuevo y nadie se entera hasta que alguien recibe un 403 raro.

El mapeo permiso→endpoint sí es una decisión humana, pero se hace **sobre el catálogo descubierto**,
no sobre una lista paralela.

**Consecuencia operativa a documentar**: agregar un endpoint nuevo implica volver a correr `discover`
y mapearlo. Va al checklist de endpoint nuevo de `backend/CONTRIBUTING.md`, que ya existe.

### D3 — Los seeds llevan la matriz completa

`TestSeed.sql` y `dev-alta-fotografo.sql` incluyen permisos, endpoints y asignaciones. Una base nueva
tiene que nacer operativa: si el sembrado deja la autorización a medias, el primer arranque es un
403 inexplicable y la conclusión natural de quien lo sufre es apagar el flag.

Esto conecta con el ítem ya anotado en `docs/03-fases.md` (Fase 0b) sobre el seed de instalación
versionado que AcgFotos perdió del código base. Este change lo empuja pero no lo cierra: acá se
siembra lo de autorización, no el seed de instalación completo.

### D4 — La suite es el instrumento de descubrimiento del mapeo

Mismo método que en `aislamiento-tenant-anonimo` y por el mismo motivo: **el modo de falla es
ruidoso**. Un endpoint sin mapear da 403 inmediato con el usuario y la ruta en la auditoría.

El orden es: prender el flag, correr la suite, y mapear lo que los 403 expongan. La auditoría del deny
—que el filtro ya escribe— es literalmente el listado de lo que falta.

No se mapea "por las dudas": cada asignación tiene que estar justificada por un fallo concreto o por
la operación real de un rol.

### D5 — `FamiliaSessionGuard` no se retira

La defensa puntual que se agregó en Fase 2 se conserva aunque el flag quede prendido. No es
redundancia inútil: protege contra que alguien apague el flag más adelante —por una urgencia, por un
ambiente mal configurado— y reabra el agujero original sin darse cuenta.

Los cuatro tests de regresión que corren a propósito con authz off se mantienen tal cual.

### D6 — La administración del propio tenant entra acá, no en un change aparte

El pendiente del 2026-07-26 —que el fotógrafo administre su tenant, sus usuarios y sus grupos sin ser
root— se resuelve dentro de este change y no después.

**Por qué juntos**: son el mismo trabajo mirado dos veces. Este change diseña la taxonomía de
permisos de toda la aplicación; dejar afuera la administración del propio tenant significaría diseñar
la taxonomía, y a las pocas semanas volver a abrirla para acomodar permisos nuevos sobre las mismas
áreas. Una sola pasada sale más barata y deja un catálogo coherente.

**Y hay una razón de producto**: sin esto, el onboarding autogestionado de fotógrafos es imposible.
Un fotógrafo que se registra solo pero necesita pedirle a la plataforma que le dé de alta un
ayudante, le desbloquee un usuario o le cambie el logo convierte al dueño de la plataforma en mesa de
ayuda de cada operación. Con un fotógrafo eso no molesta; con cincuenta, no escala.

**Por qué es más barato de lo que parece**: el aislamiento ya lo hace el filtro global multi-tenant.
Un usuario no-root que consulta usuarios **solo ve los de su tenant** sin que haya que escribir nada
—y con `aislamiento-tenant-anonimo` aplicado, esa garantía es todavía más firme—. El trabajo real es
definir el permiso, mapear los endpoints y hacer visibles los menús; no construir el alcance.

**Lo que sí hay que separar con cuidado**: las operaciones que son genuinamente cross-tenant
—listar todos los tenants, impersonar, administrar el catálogo de la plataforma— tienen que quedar
root-only aunque toquen las mismas entidades. La distinción no es "qué entidad" sino "de quién son
los datos".

### D7 — El rol del fotógrafo se valida recorriendo su operación real

No alcanza con que la suite quede verde: los tests no ejercitan la operación del fotógrafo de punta a
punta como la vive él. La validación es un recorrido manual completo en dev con el usuario
`fotografo` —crear evento, cargar grupos y participantes, subir fotos, generar tarjetas, ver pedidos,
ajustar marca de agua— buscando 403 que la suite no cubra.

## Risks / Trade-offs

- **Es el change que más superficie toca de la tanda** → Va solo, con su propio commit y su propia
  corrida de suite, entre `aislamiento-tenant-anonimo` y `pagos-mercado-pago`.
- **Un endpoint sin mapear llega a producción y bloquea a un usuario real** → Mitigado por D4 (la
  suite descubre) y D7 (el recorrido manual cubre lo que la suite no). Y el diagnóstico es directo:
  la auditoría del deny dice exactamente qué usuario y qué endpoint.
- **La granularidad de D1 puede quedar corta** → Aceptado a propósito: partir un permiso en dos
  después es barato; el catálogo inflado desde el día uno no se poda nunca.
- **El caché de permisos por versión de autorización puede confundir durante el desarrollo** → El
  mecanismo ya existe y tiene tests (`AuthzCacheVersionTests`); si un permiso recién otorgado no se ve,
  el primer sospechoso es la versión de autorización, no el mapeo.
- **Los ~165 endpoints incluyen superficie heredada que hoy nadie usa** → Es el costo elegido del
  alcance "catálogo completo": deja la casa ordenada para roles futuros a cambio de mapear cosas que
  hoy solo toca root.

## Migration Plan

1. Poblar `gen_Endpoints` con `discover` sobre la base de dev y sobre la de tests.
2. Definir y sembrar la taxonomía de permisos.
3. Mapear permiso→endpoint sobre el catálogo descubierto, área por área.
4. Asignar permisos a los roles existentes.
5. Prender `AuthorizationEnabled=true`.
6. Correr la suite; mapear lo que los 403 expongan; repetir hasta verde.
7. Recorrido manual del fotógrafo en dev (D7).
8. **Rollback**: `AuthorizationEnabled=false` restituye el comportamiento anterior de inmediato, sin
   tocar datos. Las filas sembradas quedan y no molestan. Es el rollback más barato de los tres
   changes de la tanda — otra razón para hacerlo ahora y no cerca del deploy.
