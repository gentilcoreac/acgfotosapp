## Context

Ver `proposal.md` § Why para la motivación, y **ADR-15** (`docs/04-decisiones.md:281`) para las
decisiones de producto ya cerradas con el fotógrafo el 2026-07-26/27. Este documento traduce ese ADR
a decisiones de implementación sobre el código actual; no lo reabre.

Estado del código sobre el que se apoya:

- `ImageSharpImageProcessor` (`AcgFotos.Fotos.Infrastructure/Imaging/`) genera preview + thumb en
  WebP: resize `ResizeMode.Max` sin upscale, `LimpiarMetadatos` (EXIF/IPTC/XMP a null) y
  `AplicarWatermark`, que dibuja texto rotado ~-26,5° en grilla ladrillo con `SixLabors.Fonts`.
- `FotoProcesadorAppService` arma `OpcionesDerivados` leyendo `OpcionesFotos` (sección `Fotos` del
  appsettings) e invoca el processor; lo dispara `FotoProcesamientoWorker`, un `BackgroundService`
  con canal en memoria (ADR-04) que re-encola lo que quedó `Pendiente` al arrancar.
- `FotoStorage` mete todo bajo `private/` del `IStorageProvider` de la plataforma; las keys las arma
  `FotoStorageKeys` (`fotos/originals/…` vs `fotos/derived/…`, separación estructural de ADR-06).
- Los AppServices admin del vertical llaman `FamiliaSessionGuard.EnsureNoFamiliaSession` al inicio de
  cada método público — defensa que NO depende de `AuthorizationEnabled` (que sigue en `false`).
- `Evento` es `MultiTenantEntityBase` con `TamanosPrecios` como colección hija: el patrón a copiar
  para las capas.

## Goals / Non-Goals

**Goals:**

- Una sola implementación del dibujo de la marca (el navegador), verificable a ojo por el fotógrafo
  antes de comprometerla.
- Que el pipeline sin perfiles cargados produzca exactamente lo de hoy — el deploy no cambia ninguna
  foto por sí solo.
- Que el reparto front/API quede verificado por un test antes de construir la UI encima.

**Non-Goals:**

- Texto dinámico por foto ("Familia {nombre}" horneado): exigiría un derivado por foto × participante
  (ADR-15 § Consecuencias). El overlay dinámico del front sigue como está.
- La pantalla `/fotos/presentar` (modo fotógrafo, `docs/05-notas-abiertas.md`): independiente, se
  puede hacer antes o después.
- La política de retención de originales: diseñada aparte, no entra acá.
- Editor de texto enriquecido, biblioteca de plantillas o marcas por participante.

## Decisions

### D1 — El contrato entre front y API es un PNG + colocación, no una descripción de dibujo

La API recibe el asset ya rasterizado y sólo lo coloca/escala/rota/funde. Los píxeles que el
fotógrafo vio son literalmente los que se componen.

*Alternativa descartada*: mandar la descripción (texto, fuente, tamaño, color) y que la API dibuje —
es el esquema de doble motor que ADR-15 rechaza explícitamente: dos implementaciones del mismo
algoritmo que hay que mantener idénticas para siempre, y el día que divergen el fotógrafo calibra
contra una imagen que no existe.

*Consecuencia técnica*: `SixLabors.Fonts` y `ResolverFuente` se eliminan del backend.

### D2 — `OpcionesDerivados` pasa a llevar capas ya resueltas; el processor no conoce entidades

`IImageProcessor` sigue siendo un puerto de Application y sigue recibiendo un record plano, pero en
vez de `TextoWatermark`/`Opacidad` lleva `IReadOnlyList<CapaComposicion>` (bytes del PNG +
colocación) y los parámetros de resolución/calidad. Quien resuelve la cascada y lee los assets del
storage es `FotoProcesadorAppService`; el processor no toca la base ni el storage.

*Alternativa descartada*: pasarle el `PerfilMarcaAgua` al processor — acopla Infrastructure de imagen
al dominio y vuelve intestable la composición pura.

### D3 — La cascada se resuelve en un servicio propio, no en el processor ni inline

`ResolverConfiguracionPublicacion(eventoId)` → `(PerfilMarcaAgua?, OpcionesPublicacion?)` con el
orden evento → default del tenant → `OpcionesFotos`. Un solo lugar que responde "qué se aplica a esta
foto", usado tanto por el procesamiento normal como por la regeneración y por la vista previa del
editor.

*Por qué importa*: el fallback a `OpcionesFotos` es lo que garantiza que sin perfiles cargados el
comportamiento sea idéntico al actual (requisito de la spec `marca-agua`).

### D4 — Los assets de capa viven en el storage privado, con su propia familia de keys

`fotos/watermarks/{perfilId}/{capaId}.png` bajo el mismo prefijo `private/`. No se sirven
públicamente; el editor los lee por endpoint autenticado. Se guardan en PNG sin recodificar: el
derivado ya se comprime con pérdida una vez, comprimir también el asset degradaría dos veces la misma
marca (ADR-15 § 7).

`FotoStorageKeys` gana los métodos correspondientes — sigue siendo la única fuente de keys.

### D5 — Validación del upload en dos pasos: identificar antes de decodificar

`Image.Identify` lee sólo la cabecera y devuelve dimensiones sin materializar el bitmap: sirve para
rechazar una bomba de descompresión (un PNG de 200 KB que declara 40.000 × 40.000) **antes** de que
`Image.Load` intente reservar gigabytes. Recién pasado ese techo se decodifica de verdad, y esa
decodificación es también la que determina el formato real (no la extensión ni el content-type).

Techo de dimensiones y de peso configurables, con default en la sección `Fotos`.

### D6 — El asset se rasteriza al máximo tamaño de uso; la composición sólo escala hacia abajo

Reducir un bitmap da nitidez, agrandarlo no tiene arreglo — y ocurre antes de que el encoder toque
nada. Para la capa de texto es gratis (la dibuja el front al tamaño que haga falta). Para el logo
subido no depende de nosotros: se valida al subirlo y se avisa con el número concreto ("tu logo tiene
300 px de ancho; a esta escala se va a ver borroso — subí uno de al menos 1200 px o bajá la escala").

Con el tope actual (lado mayor 1600, escala hasta 70%) el piso es ~1200 px de lado mayor.

### D7 — Los modos de fusión se apoyan en la especificación W3C, no en código nuestro

`GraphicsOptions.ColorBlendingMode` de ImageSharp y el `globalCompositeOperation` de canvas
implementan la misma especificación. Por eso se verifica **una vez**, con un test que compone la
misma muestra por ambos caminos y compara: nadie edita nunca una fórmula de fusión, así que no
reintroduce el problema de la lógica duplicada.

**Ese test va primero** (tarea 2.1), antes de construir la pantalla: todo el reparto front/API se
apoya en que las dos fórmulas coincidan. Si no coinciden, hay que saberlo antes de tener una UI
encima.

### D8 — La vista previa del editor comprime a WebP antes de mostrar

WebP con pérdida ataca primero las transiciones suaves y el bajo contraste — exactamente una marca
sutil, una sombra difusa o el trazo fino de un logo. Previsualizar sin comprimir muestra algo que
nunca va a existir. El editor pasa su canvas por `canvas.toBlob('image/webp', calidad)` y muestra el
resultado decodificado.

De ahí sale la guía de diseño que el editor le sugiere al fotógrafo: color sólido y opacidad plana
sobreviven; la sombra difusa es la primera víctima.

### D9 — La regeneración reusa el worker existente: pone las fotos en `Pendiente` y encola

No hay infraestructura nueva. El endpoint cuenta las fotos del evento, las marca `Pendiente` y las
encola en la `FotoProcesamientoQueue`; el `FotoProcesamientoWorker` hace el resto con el pipeline
normal, así la regeneración y el alta comparten camino (y fallos, y visibilidad en la galería).

El conteo se devuelve **antes** de encolar para el diálogo de confirmación.

*Alternativa descartada*: regenerar sincrónicamente en el request — 400 fotos bloquean el request y
el usuario no ve progreso.

### D10 — Perfil y opciones de publicación son entidades separadas

Son ejes independientes: se puede querer marca sutil con resolución baja o al revés (ADR-15 § 5). Una
entidad "perfil" que además fijara la resolución haría dos cosas. `MarcarThumb` sí va en el perfil de
marca: es una decisión sobre la marca, no sobre la resolución.

Ambas cuelgan del tenant con un flag `EsDefault`, y `Evento` gana dos FKs nullable — null significa
"usar la del estudio", que es el default del ABM.

### D11 — Seed de un perfil "Estándar" equivalente a la marca actual

La migración siembra un perfil que reproduce lo de hoy (texto blanco al 50%, mosaico diagonal), para
que **el primer render post-deploy se vea igual que el último pre-deploy**. El asset PNG de ese
perfil se genera una vez y se versiona con la migración.

*Nota*: no se marca como default del tenant. Sin default, la cascada cae en `OpcionesFotos` y el
resultado es el mismo; el perfil sembrado existe para que el fotógrafo lo vea, lo edite y entienda el
mecanismo, no para cambiar el comportamiento.

### D12 — Las guardas dicen el número concreto (principio transversal de ADR-15)

Ninguna validación de este vertical avisa en genérico ni actúa en silencio: "tu logo tiene 300 px de
ancho" en vez de "imagen inválida"; "se van a regenerar 412 fotos, tarda unos minutos" antes de
encolar; "las familias van a ver estas fotos sin ninguna protección" al guardar un perfil sin marca;
el aviso de que un logo en una esquina se recorta fácil, ahí donde se elige la posición.

Esto es requisito de las specs, no cosmética: una guarda que el fotógrafo no entiende es una guarda
que va a tratar de saltear.

## Risks / Trade-offs

- **La fusión de canvas y la de ImageSharp no coinciden pixel a pixel** → el test de D7 va primero,
  antes de construir la UI. Si aparece una diferencia, se decide con el resultado en la mano
  (tolerancia por canal, o recortar los modos de fusión a los que sí coincidan) en vez de descubrirlo
  con la pantalla terminada.
- **Un logo subido con dimensiones enormes agota memoria del servidor** → `Image.Identify` antes de
  decodificar (D5), más techo de peso.
- **El fotógrafo calibra contra una sola foto y arruina el resto del evento** → el editor verifica
  sobre tres muestras simultáneas (clara/oscura/mixta) más fotos propias; es requisito de la pantalla,
  no una opción.
- **Regenerar un evento grande satura el worker mientras las familias miran fotos** → el worker es
  secuencial y las fotos ya subidas conservan sus derivados actuales hasta que se rehacen; el costo se
  informa antes (D9/D12). Si en uso real molesta, la palanca es priorizar la cola, no cambiar este
  diseño.
- **`OpcionesDerivados` es un contrato público del vertical y cambia (BREAKING)** → el blast radius
  real es chico: `FotoProcesadorAppService` es su único consumidor de producción, más los tests de
  imagen. Se aprovecha para eliminar `TextoWatermark` del contrato en vez de dejarlo muerto.
- **`SixLabors.Fonts` se elimina** → si alguna vez vuelve a hacer falta dibujar texto en el servidor
  (texto dinámico por foto, hoy Non-Goal), hay que reintroducirlo. Se acepta: eliminarlo es lo que
  hace que el servidor deje de depender de fuentes instaladas.

## Migration Plan

1. Migración EF con las 3 tablas nuevas (`fot_PerfilesMarcaAgua`, `fot_CapasMarcaAgua`,
   `fot_OpcionesPublicacion`) y las 2 FKs nullable en `fot_Eventos`. Todo nullable / con default:
   no requiere backfill.
2. Seed del perfil "Estándar" (D11), sin marcarlo default.
3. Deploy del backend. **Sin perfiles ni opciones cargadas la cascada cae en `OpcionesFotos` y el
   pipeline se comporta como antes** — ninguna foto existente cambia.
4. Deploy del frontend con las pantallas nuevas y los menús sembrados.
5. El fotógrafo diseña su perfil, lo asigna a un evento y regenera explícitamente cuando quiere.

**Rollback**: revertir el deploy. Las tablas nuevas quedan huérfanas pero inertes (las FKs son
nullable); el pipeline viejo lee `OpcionesFotos` y sigue funcionando. Si ya se regeneró un evento con
la marca nueva, volver atrás exige regenerarlo otra vez con la versión vieja — el original nunca se
tocó, así que es siempre recuperable.
