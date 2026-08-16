## 1. Catálogo de endpoints

- [ ] 1.1 Correr `GET /api/general/discover` contra la base de dev y verificar que `gen_Endpoints` queda poblado con los ~165 endpoints
- [ ] 1.2 Exportar el catálogo descubierto a un listado revisable, agrupado por controller, para usarlo como base del mapeo
- [ ] 1.3 Confirmar que los endpoints anónimos y los marcados para sesión de familia quedan identificables en el catálogo

## 2. Taxonomía de permisos

- [ ] 2.1 Definir los permisos por área funcional (D1) y documentar qué cubre cada uno
- [ ] 2.2 Sembrar los permisos nuevos en `gen_Permisos`, conservando `PermisoRoot` y `PermisoFotos` existentes
- [ ] 2.3 Revisar la taxonomía contra los roles que existen hoy (Fotógrafo, Administrador Cliente) y detectar áreas sin dueño

## 3. Mapeo permiso → endpoint

- [ ] 3.1 Mapear el vertical Fotos: eventos, grupos y participantes, fotos, tarjetas, pedidos, marca de agua, opciones de publicación
- [ ] 3.2 Mapear las áreas heredadas de plataforma: usuarios, roles y permisos, tenants, licencias, auditoría, parámetros, menús, archivos
- [ ] 3.3 Mapear las 5 acciones heredadas del CRUD genérico en cada uno de los 18 controllers que lo extienden
- [ ] 3.4 Asignar los permisos a los roles existentes
- [ ] 3.5 Revisar que ningún endpoint quede sin permiso que lo cubra, salvo los anónimos y los de sesión de familia

## 3b. Administración del propio tenant (D6)

- [ ] 3b.1 Definir el permiso de administración del propio tenant y qué cubre: edición del tenant (incluidos logos, colores y hoja de estilos), usuarios y grupos de usuarios
- [ ] 3b.2 Mapear ese permiso a los endpoints correspondientes de tenant, usuarios y grupos
- [ ] 3b.3 Identificar las operaciones genuinamente cross-tenant sobre esas mismas entidades —listar todos los tenants, impersonar, catálogo de plataforma— y verificar que siguen exigiendo `PermisoRoot`
- [ ] 3b.4 Asignar el permiso al rol por defecto de tenant nuevo, para que un tenant recién creado nazca autoadministrable
- [ ] 3b.5 Hacer visibles en el menú del fotógrafo las secciones de tenant, usuarios y grupos
- [ ] 3b.6 Verificar por test que un usuario no-root con el permiso solo alcanza datos de su tenant, y que sin el permiso recibe 403
- [ ] 3b.7 Verificar el caso que originó el pendiente: el fotógrafo puede reenviar la confirmación de cuenta y desbloquear a un usuario suyo sin intervención de root

## 4. Seeds

- [ ] 4.1 Incorporar permisos, endpoints y asignaciones a `TestSeed.sql`
- [ ] 4.2 Incorporar lo mismo a `backend/scripts/dev-alta-fotografo.sql`
- [ ] 4.3 Verificar que una base sembrada de cero queda operativa sin pasos manuales (D3)

## 5. Prender el flag y estabilizar

- [ ] 5.1 `AuthorizationEnabled=true` en `appsettings.json`
- [ ] 5.2 Correr la suite completa y registrar cada 403 con su usuario y endpoint
- [ ] 5.3 Mapear lo que los 403 expongan, justificando cada asignación con el fallo concreto que la motiva (D4)
- [ ] 5.4 Repetir hasta que la suite quede verde, sin asignar permisos "por las dudas"
- [ ] 5.5 Verificar que los tests que corren a propósito con authz off (`FamiliaSessionAdminGuardTests`) siguen pasando sin cambios (D5)

## 6. Verificación manual

- [ ] 6.1 Recorrido completo del fotógrafo en dev: crear evento, cargar grupos y participantes, subir fotos, generar tarjetas, ver y operar pedidos, ajustar marca de agua y opciones de publicación (D7)
- [ ] 6.2 Verificar que el fotógrafo recibe 403 en endpoints de administración de plataforma
- [ ] 6.2b Recorrido de autoadministración: el fotógrafo cambia su logo y colores, da de alta un usuario suyo, lo desbloquea y le reenvía la confirmación — todo sin root (D6)
- [ ] 6.3 Recorrido de una sesión de familia: canje, álbum, carrito, confirmación de pedido
- [ ] 6.4 Verificar en el front que un 403 por falta de permiso se muestra con un mensaje claro, no como error genérico
- [ ] 6.5 Verificar que el deny queda en la auditoría con usuario y endpoint

## 7. Documentación

- [ ] 7.1 ADR nuevo en `docs/04-decisiones.md`: autorización por endpoint activa, taxonomía por área funcional y por qué se revierte la decisión de posponerla
- [ ] 7.2 Agregar al checklist de endpoint nuevo de `backend/CONTRIBUTING.md` el paso de correr `discover` y mapear el permiso (D2)
- [ ] 7.3 Actualizar el ítem de Deploy en `docs/03-fases.md` que daba este rollout por pendiente
- [ ] 7.4 Actualizar en `docs/05-notas-abiertas.md` el hallazgo de `AuthorizationEnabled` de 2026-07-18, dejando constancia de que la defensa puntual se conserva
- [ ] 7.5 Cerrar en `docs/05-notas-abiertas.md` el pendiente del 2026-07-26 sobre administración del propio tenant, y dejar anotado que la puerta pública de registro autogestionado sigue pendiente y que este change era su prerrequisito
