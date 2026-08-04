## 1. Modelo y migración

- [x] 1.1 Entidades `PerfilMarcaAgua` (tenant, nombre, `EsDefault`, `MarcarThumb`) y `CapaMarcaAgua`
      (colección hija con orden, modo de colocación, posición, escala %, margen, ángulo, opacidad,
      modo de fusión y key del asset) en `AcgFotos.Fotos.Domain/Entities`, siguiendo el patrón
      `Evento` → `TamanoPrecio`
- [x] 1.2 Entidad `OpcionesPublicacion` (tenant, nombre, `EsDefault`, lado mayor preview, lado mayor
      thumb, calidad)
- [x] 1.3 FKs nullable `PerfilMarcaAguaId` y `OpcionesPublicacionId` en `Evento` (null = "usar la del
      estudio"; `Restrict`, no `Cascade`)
- [x] 1.4 Configuraciones EF (`fot_PerfilesMarcaAgua`, `fot_CapasMarcaAgua`,
      `fot_OpcionesPublicacion`) con índice por `TenantId` y cascada capa→perfil
- [x] 1.5 Migración EF `MarcaAguaConfigurable`, generada limpia sobre `AjustaTimestampsSinZonaHoraria`
      (drift preexistente aislado aparte) y verificada contra `AcgFotos_Tests` (508/508 tests). **Sin
      aplicar a la base dev real** (`AcgFotos`) — pendiente de que se revise/pruebe la migración de
      timestamps con cuidado antes de tocar esos datos

## 2. Verificación del reparto front/API (va antes de construir la UI)

- [x] 2.1 Test que compone la misma muestra por canvas y por la librería de composición con cada modo
      de fusión (`Normal`, `Superponer`, `Diferencia`) y compara las salidas — si no coinciden, frenar
      y decidir con el resultado en la mano antes de seguir (D7). **Resultado**: ImageSharp 3.1.8 no
      tiene `Difference` — no era una diferencia de fórmula, el modo no existe en la librería. Resuelto
      con SkiaSharp para la composición de capas (ADR-16, docs/04-decisiones.md); `BlendModeParityTests`
      3/3 verde contra fixtures reales de Chromium/Playwright, tolerancia ±2 por canal
- [x] 2.2 Tolerancia de comparación fijada en ±2 por canal (redondeo del pipeline de 8 bits, no
      discrepancia de fórmula); anotado en `design.md` § D7 y § Risks/Trade-offs

## 3. Composición en el pipeline

- [x] 3.1 Cambiar el contrato: `OpcionesDerivados` deja de tener `TextoWatermark`/`Opacidad` y pasa a
      llevar `IReadOnlyList<CapaComposicion>` (bytes del PNG + colocación) más resolución y calidad
      (**BREAKING**, D2). `CapaComposicion` reutiliza los enums de Domain (`ModoColocacionMarcaAgua`,
      `PosicionMarcaAgua`, `ModoFusionMarcaAgua`) en vez de duplicarlos — son valores sin estado, el
      mismo criterio que ya usa `EstadoProcesamientoFoto` cruzando capas
- [x] 3.2 `ImageSharpImageProcessor`: reemplazar `AplicarWatermark` por la composición de capas
      (colocar, escalar sólo hacia abajo, rotar, fundir) usando **SkiaSharp** (`SKBlendMode`, ADR-16) —
      decodificar/resize/EXIF/encode siguen en ImageSharp, la composición puntual cede los píxeles a
      SkiaSharp (`SKBitmap.InstallPixels` sobre el buffer de `Image<Rgba32>`, copia de vuelta con
      `ProcessPixelRows`) y retoma ImageSharp para el WebP final
- [x] 3.3 Eliminar `ResolverFuente` y la dependencia de `SixLabors.Fonts`/`SixLabors.ImageSharp.Drawing`
      del proyecto Infrastructure (queda sólo `SixLabors.ImageSharp` + `SkiaSharp`)
- [x] 3.4 Implementar colocación repetida en mosaico (mismo pitch 1,25×/2,2× y patrón ladrillo que el
      código pre-ADR-15, ahora hardcodeado en el compositor — no es un campo configurable por capa
      todavía) y las 9 posiciones fijas con margen
- [x] 3.5 Respetar `MarcarThumb` del perfil
- [x] 3.6 Servicio de resolución en cascada evento → default del tenant → `OpcionesFotos` (D3):
      `IConfiguracionMarcaAguaResolver`, devuelve `(PerfilMarcaAgua?, OpcionesPublicacion?)` — la
      conversión a `OpcionesDerivados` (incluida la lectura de assets del storage) queda en el AppService
- [x] 3.7 `FotoProcesadorAppService`: usa la cascada en vez de leer `OpcionesFotos` directo. **Hallazgo
      real al implementarla**: ADR-15 §4 prometía que sin perfiles cargados el pipeline se comporta
      "exactamente como hoy", pero D1 prohíbe que la API dibuje texto — contradicción con task 3.3, que
      borra el código de texto. Resuelto sin romper ninguna de las dos: se generó (mientras el código
      viejo todavía existía) un PNG que reproduce el watermark real de producción como UN SOLO tile,
      embebido en el binario (`Imaging/Assets/marca-agua-default.png`, leído vía
      `IFotoStorage.LeerCapaMarcaAguaDefaultAsync`); sin perfil, el resolver arma una `CapaComposicion`
      sintética con ese asset y los parámetros que reproducen el aspecto de hoy (ángulo -26,565°, escala
      94,75%, opacidad 0,5, Normal) — la API sigue sin dibujar texto NUNCA, ni siquiera en el fallback.
      `OpcionesFotos` pierde `TextoWatermark`/`OpacidadWatermark` (quedaban muertos); `appsettings.json`
      actualizado. **Adelantado de 4.1**: el lado de LECTURA de `IFotoStorage`
      (`LeerCapaMarcaAguaAsync`/`LeerCapaMarcaAguaDefaultAsync` + `FotoStorageKeys.CapaMarcaAgua`), sin
      el cual 3.7 no tenía con qué leer los assets — el lado de escritura/validación de subida sigue en
      4.1. Se agregaron `IPerfilMarcaAguaRepository`/`IOpcionesPublicacionRepository` (con lectura de
      capas y de default), necesarios para la cascada y reusables por el CRUD del grupo 5
- [x] 3.8 Tests de composición (`ImageProcessorTests.cs`, reescrito): mosaico cubre las 4 esquinas,
      posición fija respeta el margen, varias capas se componen en orden, no se agranda el asset más
      allá de su tamaño natural. Comparaciones con tolerancia por canal (WebP es lossy incluso sobre
      color plano) en vez de igualdad exacta — la primera versión con `Assert.Equal` exacto rompía por
      esto, no por un bug de composición. "Sin perfiles equivale a hoy" NO tiene test de integración
      todavía (necesitaría comparar contra una captura congelada del pipeline viejo); la fidelidad la
      sostiene que 3.7 generó el asset con el código viejo real antes de borrarlo, no un test — anotado
      como hueco de cobertura, no bloqueante

## 4. Storage y validación de assets

- [x] 4.1 `FotoStorageKeys.CapaMarcaAgua` + `IFotoStorage`: lectura adelantada en 3.7; escritura
      (`GuardarCapaMarcaAguaAsync`) agregada acá — guarda el PNG tal cual, sin recodificar (D4)
- [x] 4.2 Validación del upload: `IValidadorAssetMarcaAgua.ValidarAsync` — peso primero (más barato),
      después `Image.IdentifyAsync` (sólo cabecera) para el techo de dimensiones ANTES de decodificar
      completo, recién ahí decodifica y exige PNG real (no por extensión/content-type) vía
      `Metadata.DecodedImageFormat`. Ambos techos configurables en `OpcionesFotos` (D5)
- [x] 4.3 `ResultadoValidacionAsset.TieneCanalAlfa` (vía `PngMetadata.ColorType`, no escaneando
      píxeles) — la fraseo del aviso al fotógrafo queda para el AppService del grupo 5, este método
      sólo expone el hecho
- [x] 4.4 `IValidadorAssetMarcaAgua.EvaluarEscala(anchoAssetPx, escalaPorcentaje)`: contra una foto de
      referencia de 1600px (el tope que cita ADR-15 §8, no depende del evento — un perfil default del
      tenant puede terminar en cualquier evento). Devuelve el ancho necesario + si alcanza; la fraseo
      del aviso también queda para el grupo 5 (D6/D12)
- [x] 4.5 Tests de las validaciones (`ValidadorAssetMarcaAguaTests`, 9 casos): acepta/detecta alfa,
      rechaza no-imagen, rechaza no-PNG, rechaza peso excedido, rechaza/acepta dimensiones contra el
      techo configurado, `EvaluarEscala` alcanza/no alcanza. El rechazo por dimensiones se prueba por
      su resultado observable, no interceptando que `Image.LoadAsync` no se haya llamado — la garantía
      de "antes de decodificar" la sostiene la lectura del código (D5), no una aserción de test

## 5. API de administración

- [x] 5.1 AppService + controller de perfiles de marca de agua (CRUD, con `FamiliaSessionGuard` al
      inicio de cada método público). **Decisión tomada al implementarla (D14 en design.md)**: el alta
      de una capa nueva (y del perfil que la contiene, si todavía no existe) va por `SubirCapaAsync`,
      no por el `Update` genérico — resuelve que la key de storage necesita un `PerfilMarcaAguaId` real
      y un perfil no puede persistir con 0 capas al mismo tiempo. El CRUD estándar
      (`ExtendedEntityAppServiceBase`, mismo patrón que `Evento`/`TamanoPrecio`) sólo edita metadata y
      la colocación de capas ya existentes; `PerfilMarcaAguaInputDto.Capas` rechaza filas con Id 0.
- [x] 5.2 AppService + controller de opciones de publicación (CRUD, misma guarda) — `EntityAppServiceBase`
      de un solo DTO (mismo patrón que `ParametroAppService`), sin necesidad de asset ni de Input/Output separados
- [x] 5.3 Endpoint de subida del PNG de capa, con las validaciones de la sección 4 — `POST
      api/fotos/marca-agua/perfiles/capas/upload` (multipart), ver D14
- [x] 5.4 Endpoint de lectura del asset de una capa (autenticado, para el editor) — `GET
      api/fotos/marca-agua/perfiles/{perfilId}/capas/{storageKey}`
- [x] 5.5 Validadores FluentValidation: 1–3 capas, rangos de escala/opacidad/ángulo/margen, rangos de
      resolución y calidad. "Un solo default por tenant" NO es una regla de FluentValidation (el
      validador se instancia sin dependencias — `CheckInputValidations` usa `Activator.CreateInstance`
      sin argumentos, no puede inyectar un repo): se sostiene en el AppService (`LimpiarDefaultAnteriorAsync`,
      mismo criterio que `PerfilMarcaAguaConfig`/`OpcionesPublicacionConfig` ya documentaban en el grupo 1)
- [x] 5.6 Aviso al guardar un perfil sin protección efectiva ("las familias van a ver estas fotos sin
      ninguna protección") — todas las capas con opacidad 0, calculado en `ToOutput`
- [x] 5.7 DTOs + mappers Mapperly
- [x] 5.8 Tests de integración: CRUD, aislamiento entre tenants, 403 con sesión de familia, un solo
      default — `PerfilMarcaAguaCrudTests` (13 casos), `OpcionesPublicacionCrudTests` (5 casos),
      + FAMGUARD-06 en `FamiliaSessionAdminGuardTests`. Bugs de la primera corrida, corregidos: (a)
      `OpcionesPublicacionAppService` no overrideaba `GetByIdAsync` — el `ToOutput` heredado llamaba al
      mapper Mapperly con la entidad en null (404 esperado) y tiraba 500; (b) test con dato inválido
      propio (`PosicionFija` sin `Posicion`, el validador de 5.5 ya lo exige)
- [x] 5.9 Seed del perfil "Estándar" equivalente a la marca actual (D11: **sin** marcarlo default),
      con su asset PNG subido a través del endpoint real de 5.3 (no SQL crudo + copia manual de
      archivo — movida desde el grupo 1: no hay dónde apoyar un asset real hasta que este endpoint
      exista) — decisión tomada al implementar 1.6, ver `design.md` § Decisions D11.
      **Mecanismo, decidido con Alberto al implementarla**: el repo no tenía NINGÚN seed en C# (todo
      es SQL manual, `TestSeed.sql`/`dev-alta-fotografo.sql`), y además "Estándar" es un dato por
      TENANT, no de instalación — así que en vez de un `IHostedService` que recorra todos los tenants
      (hubiera exigido que el vertical Fotos referenciara `AcgFotos.Base.*` para listar tenants, un
      límite arquitectónico que hoy no cruza en ningún lado), se siembra perezosamente la primera vez
      que el tenant abre el listado (`PerfilMarcaAguaAppService.EnsurePerfilEstandarSembradoAsync`,
      llamado desde `SearchAsync`/`GetAllAsync`): reusa `SubirCapaAsync` + `UpdateAsync` (los mismos
      dos métodos reales de la API, no un camino paralelo), con los parámetros de colocación que
      reproducen el aspecto de hoy extraídos a `MarcaAguaLegadoConstantes` (compartida con el fallback
      de `FotoProcesadorAppService`, antes duplicados). Idempotente (no re-siembra si ya existe un
      perfil "Estándar") y best-effort (un fallo al sembrar no rompe el listado — el pipeline real
      sigue funcionando por el fallback embebido). Test: PMA-14 (siembra en el primer listado, no
      default, idempotente); PMA-11 se ajustó (el listado de un tenant nuevo ya no está vacío).

## 6. Regeneración por evento

- [x] 6.1 Endpoint que devuelve el conteo de fotos a regenerar de un evento (para el diálogo de
      confirmación, antes de encolar) — `GET api/fotos/fotos/regenerar/conteo?eventoId=`. "A
      regenerar" = fotos `Lista` o `Error` (ya procesaron al menos una vez); una `Pendiente` ya va a
      pasar por el pipeline normal, re-encolarla sería doble trabajo
- [x] 6.2 Endpoint que marca esas fotos `Pendiente` y las encola en la `FotoProcesamientoQueue` (D9) —
      `POST api/fotos/fotos/regenerar?eventoId=`, mismo criterio de orden que `SubirAsync` (encolar
      DESPUÉS del commit, para que el worker encuentre el estado ya persistido)
- [x] 6.3 Tests de integración: encola el conteo informado, evento sin fotos no encola nada, 403 con
      sesión de familia, rechazo de evento de otro tenant, el original no se toca —
      `FotoRegeneracionTests` (4 casos) + FAMGUARD-07 en `FamiliaSessionAdminGuardTests`

## 7. Front — pantalla `/fotos/marca-agua`

- [x] 7.1 Feature `features/fotos/marca-agua/{domain,data,ui}` + ruta en `app.routes.ts`
- [x] 7.2 Listado de perfiles con la marca real renderizada por fila — `tbi-perfil-marca-agua-canvas`
      (compositor de canvas, réplica en TS de `ImageSharpImageProcessor.ComponerCapa`: mosaico,
      posición fija, escala sin upscale, modos de fusión) sobre una foto de muestra sintética
- [x] 7.3 Editor de capas (1–3): grilla de 9 posiciones, colocación repetida, escala, margen, ángulo,
      opacidad y modo de fusión — el alta de una capa es **subir un PNG con transparencia** (D14: el
      backend no distingue de dónde salió el PNG). "Diseñar texto" en el editor (rasterizar texto a
      PNG en el navegador, con color/negrita/contorno) se completó en 7.4. Nuevo control reusable
      `shared/ui/tbi-slider` (`FormValueControl<number>` sobre `input[type=range]`, con tests)
- [x] 7.4 Rasterizado de la capa a PNG con transparencia al tamaño máximo de uso (D6) y subida —
      `marca-agua-texto.util.ts` (`rasterizarTextoComoPng`): mide el texto con canvas 2D, escala la
      fuente para que el ancho natural llegue a `ANCHO_REFERENCIA_FOTO_PX` (1600px, el mismo default
      que usa `IValidadorAssetMarcaAgua.EvaluarEscala` en el backend), y lo rasteriza a un PNG con
      transparencia — así el asset nunca queda corto sea cual sea la escala (1-100%) que se elija
      después en el editor de colocación, sin necesidad de re-subirlo. Botón "Diseñar texto" en
      `PerfilMarcaAguaEditComponent`, con vista previa en vivo (mismo rasterizador a un ancho menor,
      así lo que se ve es exactamente lo que se sube — D1) y confirmación que sube el PNG generado por
      el mismo `subirCapa` que un archivo elegido a mano (D14, mismo mecanismo). **Sin tests de la
      función de rasterizado en sí** (necesita canvas 2D real — `measureText`/`toBlob` —, que jsdom no
      provee sin el paquete nativo `canvas`, no instalado; mismo criterio ya documentado en 7.8/8.3):
      se extrajo la parte pura y testeable (`calcularTamanoFuente`, el cálculo de escala de fuente) a
      una función aparte, con 3 casos en `marca-agua-texto.util.spec.ts`; el dibujo/rasterizado en sí
      queda como hueco de cobertura anotado, no bloqueante
- [~] 7.5 Verificación sobre tres muestras simultáneas (clara/oscura/mixta) más fotos propias —
      **parcial**: las tres variantes sintéticas existen (`marca-agua-muestra.util.ts`, puerto del
      generador del prototipo) y se pueden elegir una a la vez en el editor, pero **no simultáneas**
      (una sola vista a la vez, no las tres lado a lado) y **sin "probar con una foto mía"** todavía
- [x] 7.6 Vista previa **pasada por el encoder WebP** antes de mostrarse (D8) — toggle "Ver
      comprimido" en el editor (`canvas.toBlob('image/webp', calidad)` real, no simulado)
- [x] 7.7 Aviso de que un logo en una esquina se recorta fácil, en el punto donde se elige la posición
- [x] 7.8 Unit tests del editor y del rasterizado — `marca-agua-canvas.util.spec.ts` (6 casos: posición
      fija, never-upscale, opacidad/fusión, mosaico, orden de capas, asset sin dimensiones) +
      `tbi-slider.component.spec.ts` (3 casos). **Sin tests de componente** para el diálogo del editor
      ni el listado (upload/guardar/eliminar) — hueco de cobertura anotado, no bloqueante

## 8. Front — pantalla `/fotos/publicacion`

- [x] 8.1 Feature `features/fotos/publicacion/{domain,data,ui}` + ruta `/fotos/publicacion` — mismo
      patrón que `eventos` (CRUD paginado con `tbi-table`; acá no hay nada por renderizar por fila,
      sólo números, así que no hace falta el canvas por fila que sí necesitaba marca de agua)
- [x] 8.2 ABM de opciones de publicación con Signal Forms — `OpcionesPublicacionEditComponent`,
      mismos rangos que `OpcionesPublicacionDtoValidator` (100–4000 / 50–2000 / 1–100), reusa
      `tbi-slider` (grupo 7) para los 3 campos numéricos
- [x] 8.3 Comparador de tamaños (300/600/900/1200/1600) sobre una foto real, con dpi equivalente al
      imprimir en 10×15 y peso medido — util puro `comparador-tamanos.util.ts` (dpi, dimensiones sin
      upscale más allá del original —mismo criterio que ADR-15 §8—, formato de peso) +
      `ComparadorTamanosComponent`: el fotógrafo sube una foto propia (`createImageBitmap`), se
      redimensiona y codifica a WebP en canvas para cada lado mayor (D8: se muestra el resultado real
      ya comprimido, no una estimación), con un slider de calidad propio del comparador (independiente
      de las filas del ABM — sirve para decidir los números ANTES de guardar una fila). Dpi = lado
      mayor resultante / (15cm de un 10×15 en pulgadas); aviso "se va a ver borroso al imprimir" bajo
      150dpi (guarda informativa, no bloqueante — a diferencia de las validaciones de asset del grupo
      4, acá no hay nada que rechazar, es sólo información para decidir). **Sin test de componente**
      (mismo criterio que `PerfilMarcaAguaCanvasComponent` del grupo 7: `createImageBitmap`/
      `canvas.toBlob` no son testeables sin un canvas real de jsdom) — cubierto por los tests puros
      del util, hueco de cobertura anotado, no bloqueante
- [x] 8.4 Unit tests — `comparador-tamanos.util.spec.ts` (8 casos: dpi, escalado sin upscale, foto
      vertical, formato de peso), `opciones-publicacion-edit.component.spec.ts` (4 casos: guarda,
      nombre vacío, rango inválido, edición) y `publicacion.component.spec.ts` (3 casos, mismo patrón
      que `eventos-list.component.spec.ts`). Suite completa verde (104 archivos / 524 tests) + lint +
      build sin errores

## 9. Front — asignación y regeneración

- [x] 9.0 Backend: exponer `PerfilMarcaAguaId`/`OpcionesPublicacionId` en `EventoDto`,
      `EventoInputDto`, `EventoMapper` y `EventoInputDtoValidator` (detectado al compilar 1.3: los
      warnings RMG020 de Mapperly muestran que hoy nada mapea las FKs nuevas del lado de Evento). Los
      dos ids escalares se agregaron a `EventoHeaderDto` (heredado por `EventoDto`, mismo lugar que
      `Estado`/`Fecha`) — mapean por convención de nombre; sólo hizo falta `[MapperIgnoreSource]` en
      `PerfilMarcaAgua`/`OpcionesPublicacion` (las navegaciones, sin contraparte en el DTO: el combo
      del ABM sólo necesita el id). **`EventoInputDtoValidator` no ganó ninguna regla nueva**: "el
      perfil/las opciones existen y son del tenant" no es una validación de forma (mismo criterio que
      "un solo default por tenant" del grupo 5) — es un guard de seguridad multi-tenant, agregado en
      `EventoAppService.UpdateAsync` (mismo patrón que `GrupoAppService` con `EventoId`: la FK sola
      aceptaría un perfil/opciones de OTRO tenant porque el filtro global de EF sólo scopea lecturas,
      no valida FKs — `GetByIdAsync` tenant-scoped devuelve null tanto si no existe como si es de otro
      tenant, y ambos casos cortan con 400 antes del commit). Tests: EVT-11/EVT-12 en
      `EventoCrudTests.cs`
- [x] 9.1 Campos "Marca de agua" y "Opciones de publicación" en el ABM de Eventos, default "Usar la
      del estudio" — dos `tbi-select` en `EventoEditComponent` con `{ value: null, label: 'Usar la del
      estudio' }` a la cabeza de cada combo (mismo patrón que `menu-edit.component.ts` con "(Ninguno)"/
      "(Sin padre)"), poblados vía `lookupResource` sobre `MarcaAguaService`/`OpcionesPublicacionService`
      (import cruzado entre features del vertical Fotos, mismo criterio ya usado por `tarjetas` con
      `eventos`/`grupos`)
- [x] 9.2 Acción de regenerar desde la galería del evento, con el conteo en el diálogo de confirmación
      antes de encolar (D12) — botón "Regenerar fotos del evento" en `GaleriaComponent` (visible con
      un evento elegido, no depende del grupo): pide el conteo primero (`GET regenerar/conteo`), si es
      0 avisa y no molesta con un diálogo vacío, si no arma el mensaje de confirmación CON el número
      real ("se van a regenerar N foto(s)…", principio "las guardas se explican solas") y recién ahí
      encola (`POST regenerar`)
- [x] 9.3 Unit tests de ambos — `evento-edit.component.spec.ts` (+3 casos: guarda los ids elegidos,
      carga los de la entidad en edición, arma "Usar la del estudio" + las opciones del tenant) y
      `galeria.component.spec.ts` (+3 casos: pide conteo → confirma con el número real → encola; sin
      fotos para regenerar avisa sin pedir confirmación; cancelar la confirmación no encola). Suite
      completa verde (104 archivos / 528 tests) + lint + build del frontend; back: EVT-11/EVT-12 verdes
      + build sin warnings RMG020 nuevos

## 10. Menús y cierre

- [x] 10.1 Menús nuevos en `TestSeed.sql` y `scripts/dev-alta-fotografo.sql` (igual que `FotosPedidos`)
      — `FotosMarcaAgua` (`/fotos/marca-agua`, ícono `branding_watermark`) y `FotosPublicacion`
      (`/fotos/publicacion`, ícono `high_quality`), Ids 106/107 (100-109 es el bloque reservado del
      vertical Fotos en el seed, ver Administración empieza en 110). `dev-alta-fotografo.sql`: se
      agregaron ambos códigos al `UPDATE ... WHERE Codigo IN (...)` que re-apunta el permiso del
      fotógrafo — igual que los demás ítems del vertical, este script sólo re-apunta PermisoId de
      menús que YA existen; no los crea en una base real (los de Eventos/Grupos/etc. tampoco los creó
      nunca este script, se cargaron a mano en su momento). **Confirmado al ejecutar 10.4**: en la
      base de dev real (`AcgFotos`) los dos menús nuevos se dieron de alta a mano por SQL (mismos Ids
      106/107, mismo PermisoId=2 que ya tenían los demás ítems de Fotos ahí) después de aplicar la
      migración — el script no los creó solo, como se esperaba
- [x] 10.2 Suite de integración del backend verde — 551/551 (incluye EVT-11/EVT-12 del grupo 9),
      re-verificada después del cambio de seed de 10.1 (agregar filas a `TestSeed.sql` no rompió
      ningún test existente — no hay ningún test que cuente filas de `gen_Menus`)
- [x] 10.3 Unit tests + lint + build del frontend verdes — 528/528 (104 archivos), lint limpio, build
      sin errores (mismos 2 warnings de budget preexistentes del grupo 7/8, no nuevos)
- [x] 10.4 Verificación manual end-to-end: diseñar un perfil, asignarlo a un evento, regenerar y
      comprobar el resultado en la galería de familias. **Migración aplicada** (2026-08-02, con
      autorización explícita de Alberto): backup previo (`pg_dump -F c`) de `AcgFotos`, `dotnet ef
      database update` corrió las dos migraciones pendientes (`AjustaTimestampsSinZonaHoraria` +
      `MarcaAguaConfigurable`) sin errores; conteos de filas (usuarios/tenants/eventos/fotos/menús)
      verificados antes y después, sin pérdida de datos. Altas de los menús `FotosMarcaAgua`/
      `FotosPublicacion` por SQL (ver 10.1). **Flujo real probado con Playwright standalone** (no la
      suite `e2e/` del repo — esa reseedea `AcgFotos_TestE2E`; esto corrió a mano, una vez, contra la
      API+front reales apuntando a `AcgFotos`): login como `fotografo` (tenant 2, sandbox propio — no
      se tocó el evento piloto real "Rep Dom" de tenant 4, cuyas credenciales no tengo) → creó evento
      + grupo + subió una foto real → **screenshot 1**: la foto ya sale con la marca de agua default
      (fallback embebido, sin perfil asignado) → creó un perfil nuevo (capa PNG con transparencia
      generada en canvas, roja y opaca, bien distinguible del fallback) → lo asignó al evento desde el
      ABM → "Regenerar fotos del evento" con el diálogo de confirmación mostrando el conteo real ("Se
      van a regenerar 1 foto(s)…") → esperó el reproceso → **screenshot 2**: la MISMA foto ahora
      muestra la marca nueva (mosaico rojo "MARCA E2E"), confirmando visual e inequívocamente que
      perfil → asignación → regeneración → derivado servido es una cadena real y funcional, no sólo
      unit tests pasando. Datos y sesiones de prueba borrados al terminar (por la UI, para que el
      storage de foto/capa se limpie también, no sólo las filas); conteos de la base verificados
      idénticos a los previos al cierre
- [x] 10.5 Tildar los 8 ítems en `docs/03-fases.md` y cerrar el pendiente del ABM en
      `docs/05-notas-abiertas.md`
