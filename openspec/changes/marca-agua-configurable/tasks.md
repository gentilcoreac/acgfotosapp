## 1. Modelo y migración

- [ ] 1.1 Entidades `PerfilMarcaAgua` (tenant, nombre, `EsDefault`, `MarcarThumb`) y `CapaMarcaAgua`
      (colección hija con orden, modo de colocación, posición, escala %, margen, ángulo, opacidad,
      modo de fusión y key del asset) en `AcgFotos.Fotos.Domain/Entities`, siguiendo el patrón
      `Evento` → `TamanoPrecio`
- [ ] 1.2 Entidad `OpcionesPublicacion` (tenant, nombre, `EsDefault`, lado mayor preview, lado mayor
      thumb, calidad)
- [ ] 1.3 FKs nullable `PerfilMarcaAguaId` y `OpcionesPublicacionId` en `Evento` (null = "usar la del
      estudio")
- [ ] 1.4 Configuraciones EF (`fot_PerfilesMarcaAgua`, `fot_CapasMarcaAgua`,
      `fot_OpcionesPublicacion`) con índice por `TenantId` y cascada capa→perfil
- [ ] 1.5 Migración EF `MarcaAguaConfigurable` + verificar que aplica sobre la base dev

## 2. Verificación del reparto front/API (va antes de construir la UI)

- [ ] 2.1 Test que compone la misma muestra por canvas y por ImageSharp con cada modo de fusión
      (`Normal`, `Superponer`, `Diferencia`) y compara las salidas — si no coinciden, frenar y decidir
      con el resultado en la mano antes de seguir (D7)
- [ ] 2.2 Fijar la tolerancia de comparación (o recortar los modos soportados) según lo que arroje 2.1,
      y dejarlo anotado en `design.md` § Decisions

## 3. Composición en el pipeline

- [ ] 3.1 Cambiar el contrato: `OpcionesDerivados` deja de tener `TextoWatermark`/`Opacidad` y pasa a
      llevar `IReadOnlyList<CapaComposicion>` (bytes del PNG + colocación) más resolución y calidad
      (**BREAKING**, D2)
- [ ] 3.2 `ImageSharpImageProcessor`: reemplazar `AplicarWatermark` por la composición de capas
      (colocar, escalar sólo hacia abajo, rotar, fundir con `GraphicsOptions.ColorBlendingMode`)
- [ ] 3.3 Eliminar `ResolverFuente` y la dependencia de `SixLabors.Fonts`
- [ ] 3.4 Implementar colocación repetida en mosaico y las 9 posiciones fijas con margen
- [ ] 3.5 Respetar `MarcarThumb` del perfil
- [ ] 3.6 Servicio de resolución en cascada evento → default del tenant → `OpcionesFotos` (D3)
- [ ] 3.7 `FotoProcesadorAppService`: usar la cascada en vez de leer `OpcionesFotos` directo, y leer
      los assets de capa del storage
- [ ] 3.8 Tests de composición: mosaico cubre las 4 esquinas, posición fija respeta el margen, varias
      capas se componen en orden, sin perfiles el resultado equivale al de hoy, y no se agranda el
      asset

## 4. Storage y validación de assets

- [ ] 4.1 `FotoStorageKeys` + `IFotoStorage`/`FotoStorage`: keys de asset de capa
      (`fotos/watermarks/{perfilId}/{capaId}.png`) bajo el prefijo `private/`, guardadas como PNG sin
      recodificar (D4)
- [ ] 4.2 Validación del upload: `Image.Identify` para atajar bombas de descompresión ANTES de
      decodificar, techo de dimensiones y de peso configurables, formato real por decodificación (D5)
- [ ] 4.3 Aviso (no bloqueo) si el logo viene sin canal alfa
- [ ] 4.4 Aviso (no bloqueo) si el ancho del logo es menor al que pide la escala elegida, con el ancho
      real, el necesario y la consecuencia (D6/D12)
- [ ] 4.5 Tests de las validaciones, incluyendo el rechazo de la bomba de descompresión sin
      decodificarla

## 5. API de administración

- [ ] 5.1 AppService + controller de perfiles de marca de agua (CRUD, con `FamiliaSessionGuard` al
      inicio de cada método público)
- [ ] 5.2 AppService + controller de opciones de publicación (CRUD, misma guarda)
- [ ] 5.3 Endpoint de subida del PNG de capa, con las validaciones de la sección 4
- [ ] 5.4 Endpoint de lectura del asset de una capa (autenticado, para el editor)
- [ ] 5.5 Validadores FluentValidation: 1–3 capas, rangos de escala/opacidad/ángulo/margen, rangos de
      resolución y calidad, un solo default por tenant
- [ ] 5.6 Aviso al guardar un perfil sin protección efectiva ("las familias van a ver estas fotos sin
      ninguna protección")
- [ ] 5.7 DTOs + mappers Mapperly
- [ ] 5.8 Tests de integración: CRUD, aislamiento entre tenants, 403 con sesión de familia, un solo
      default
- [ ] 5.9 Seed del perfil "Estándar" equivalente a la marca actual (D11: **sin** marcarlo default),
      con su asset PNG subido a través del endpoint real de 5.3 (no SQL crudo + copia manual de
      archivo — movida desde el grupo 1: no hay dónde apoyar un asset real hasta que este endpoint
      exista) — decisión tomada al implementar 1.6, ver `design.md` § Decisions D11

## 6. Regeneración por evento

- [ ] 6.1 Endpoint que devuelve el conteo de fotos a regenerar de un evento (para el diálogo de
      confirmación, antes de encolar)
- [ ] 6.2 Endpoint que marca esas fotos `Pendiente` y las encola en la `FotoProcesamientoQueue` (D9)
- [ ] 6.3 Tests de integración: encola el conteo informado, evento sin fotos no encola nada, 403 con
      sesión de familia, rechazo de evento de otro tenant, el original no se toca

## 7. Front — pantalla `/fotos/marca-agua`

- [ ] 7.1 Feature `features/fotos/marca-agua/{domain,data,ui}` + ruta en `app.routes.ts`
- [ ] 7.2 Listado de perfiles con la marca real renderizada por fila
- [ ] 7.3 Editor de capas (1–3): subir logo o diseñar texto, grilla de 9 posiciones, colocación
      repetida, escala, margen, ángulo, opacidad y modo de fusión
- [ ] 7.4 Rasterizado de la capa a PNG con transparencia al tamaño máximo de uso (D6) y subida
- [ ] 7.5 Verificación sobre tres muestras simultáneas (clara/oscura/mixta) más fotos propias
- [ ] 7.6 Vista previa **pasada por el encoder WebP** antes de mostrarse (D8), con la guía de diseño
      (color sólido y opacidad plana sobreviven; la sombra difusa no)
- [ ] 7.7 Aviso de que un logo en una esquina se recorta fácil, en el punto donde se elige la posición
- [ ] 7.8 Unit tests del editor y del rasterizado

## 8. Front — pantalla `/fotos/publicacion`

- [ ] 8.1 Feature `features/fotos/publicacion/{domain,data,ui}` + ruta
- [ ] 8.2 ABM de opciones de publicación con Signal Forms
- [ ] 8.3 Comparador de tamaños (300/600/900/1200/1600) sobre una foto real, con dpi equivalente al
      imprimir en 10×15 y peso medido
- [ ] 8.4 Unit tests

## 9. Front — asignación y regeneración

- [ ] 9.0 Backend: exponer `PerfilMarcaAguaId`/`OpcionesPublicacionId` en `EventoDto`,
      `EventoInputDto`, `EventoMapper` y `EventoInputDtoValidator` (detectado al compilar 1.3: los
      warnings RMG020 de Mapperly muestran que hoy nada mapea las FKs nuevas del lado de Evento)
- [ ] 9.1 Campos "Marca de agua" y "Opciones de publicación" en el ABM de Eventos, default "Usar la
      del estudio"
- [ ] 9.2 Acción de regenerar desde la galería del evento, con el conteo en el diálogo de confirmación
      antes de encolar (D12)
- [ ] 9.3 Unit tests de ambos

## 10. Menús y cierre

- [ ] 10.1 Menús nuevos en `TestSeed.sql` y `scripts/dev-alta-fotografo.sql` (igual que `FotosPedidos`)
- [ ] 10.2 Suite de integración del backend verde
- [ ] 10.3 Unit tests + lint + build del frontend verdes
- [ ] 10.4 Verificación manual end-to-end: diseñar un perfil, asignarlo a un evento, regenerar y
      comprobar el resultado en la galería de familias
- [ ] 10.5 Tildar los 8 ítems en `docs/03-fases.md` y cerrar el pendiente del ABM en
      `docs/05-notas-abiertas.md`
