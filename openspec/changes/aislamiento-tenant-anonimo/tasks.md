## 1. Cerrar el default del filtro

- [ ] 1.1 Quitar `|| _appContext.IsAnonymous` de `SetupMultitenantFilter` en `AcgFotos.Base.Infrastructure/Data/AcgFotosDbContext.cs`
- [ ] 1.2 Quitar la misma cláusula de `AcgFotos.Core/Infrastructure/CustomDbContext.cs`
- [ ] 1.3 Dejar en el código el comentario que explica que el cruce de tenants se declara por consulta, para que quien lea el filtro sepa dónde está la excepción

## 2. Declarar los cruces de los flujos anónimos de autenticación

- [ ] 2.1 `UsuarioRepository.GetByUserNameAsync`: declarar `IgnoreQueryFilters()` con el comentario que justifica el cruce (el login resuelve la identidad antes de conocer su tenant)
- [ ] 2.2 `UsuarioRepository.GetByUserNameWithAplicacionesAsync`: mismo tratamiento
- [ ] 2.3 Correr la suite completa de integración y anotar cada fallo con el flujo anónimo que lo produce
- [ ] 2.4 Declarar el cruce en cada consulta que la suite exponga (roles, licencias, aplicaciones y lo que aparezca durante el login), una por una y con su comentario
- [ ] 2.5 Repetir hasta que la suite quede en verde, sin declarar cruces "por las dudas": solo los que un fallo concreto justifique

## 3. Proteger la regla

- [ ] 3.1 Test de invariante en `MultiTenantInvariantTests`: un contexto anónimo no obtiene filas de una entidad multi-tenant por el camino de consulta normal
- [ ] 3.2 Test de que un contexto de tenant A no obtiene filas del tenant B por el camino normal
- [ ] 3.3 Verificar que el test nuevo falla si se reintroduce la cláusula (comprobarlo a mano antes de dar el trabajo por cerrado)

## 4. Verificación

- [ ] 4.1 Suite de integración completa en verde
- [ ] 4.2 Login manual en dev con el usuario `fotografo` (no-root): entra, ve sus menús y opera su vertical
- [ ] 4.3 Canje manual de un código de acceso en dev: la familia entra y ve su álbum
- [ ] 4.4 Recuperación de contraseña y confirmación de cuenta ejercitadas en dev

## 5. Documentación

- [ ] 5.1 ADR nuevo en `docs/04-decisiones.md`: el aislamiento de tenant es el default y el cruce se declara por consulta; incluir el relevamiento de qué dependía de la cláusula
- [ ] 5.2 Anotar en `docs/05-notas-abiertas.md`, dentro del ítem de sincronización con CodigoBase, que este fix debe **portarse a CodigoBase** en la próxima vuelta: es una mejora de seguridad de plataforma que beneficia a todo fork futuro. Incluir el diff ya verificado acá (los `IgnoreQueryFilters()` que hubo que declarar) para que el backport no se haga a ciegas, y advertir que hasta portarlo es una divergencia deliberada que la sincronización no debe deshacer
- [ ] 5.3 Registrar en `backend/CONTRIBUTING.md` la regla: una consulta que cruza tenants lo declara con `IgnoreQueryFilters()` y un comentario que explique por qué
