-- =====================================================================
-- AcgFotos — Alta DEV del negocio del fotógrafo (idempotente)
-- =====================================================================
-- Ensayo del "alta real del negocio" (docs/05): root administra la plataforma pero NO puede
-- guardar datos de negocio en su propio tenant (guard multi-tenant). Este script deja un usuario
-- NO-root operable en dev:
--
--   usuario:  fotografo   (tenant 2, admin de su tenant)
--   password: Root@AcgFotos2026!   (el hash compartido del seed de dev)
--
-- Con él se puede entrar directo en :4200 (o impersonarlo desde root) y usar los ABMs de Fotos:
-- ve los menús del seed (rol Administrador → PermisoRoot) y persiste eventos/cursos en tenant 2.
--
-- Correr con:  psql -U postgres -h localhost -d AcgFotos -f backend/scripts/dev-alta-fotografo.sql
-- (Para el alta REAL con el tenant/usuario definitivos, usar los ABMs del front.)
--
-- Portado a PL/pgSQL (DO block): Postgres no tiene IF/DECLARE a nivel de script como T-SQL, así
-- que todo el script pasa a ser un único bloque anónimo. Identificadores sin comillas y en el case
-- que sea: Postgres los pliega a minúscula (UseLowerCaseNamingConvention, ver DatabaseFactory)
-- y matchean igual contra las tablas/columnas reales. Excepción: "default" es palabra reservada,
-- necesita comillas siempre (columna gen_UsuarioAplicaciones.Default).
-- =====================================================================

DO $$
DECLARE
    permiso_fotos BIGINT;
    rol_fotografo BIGINT;
    uid BIGINT;
BEGIN
    -- Apps por tenant: root resuelve su aplicacionId de aca (aplicaciones-tenant).
    IF NOT EXISTS (SELECT 1 FROM gen_TenantAplicaciones WHERE TenantId = 1 AND AplicacionId = 1) THEN
        INSERT INTO gen_TenantAplicaciones (AplicacionId, TenantId) VALUES (1, 1);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM gen_TenantAplicaciones WHERE TenantId = 2 AND AplicacionId = 1) THEN
        INSERT INTO gen_TenantAplicaciones (AplicacionId, TenantId) VALUES (1, 2);
    END IF;

    -- Rol Administrador (Id 1): habilitado para la licencia Visualizador y con el permiso de los
    -- menus del seed (PermisoRoot=1; ver comentario del seed de menus en TestSeed.sql).
    IF NOT EXISTS (SELECT 1 FROM gen_TipoLicenciaRoles WHERE RolId = 1 AND TipoLicenciaId = 1) THEN
        INSERT INTO gen_TipoLicenciaRoles (RolId, TipoLicenciaId) VALUES (1, 1);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM gen_RolPermisos WHERE RolId = 1 AND PermisoId = 1) THEN
        INSERT INTO gen_RolPermisos (RolId, PermisoId) VALUES (1, 1);
    END IF;

    -- ---------------------------------------------------------------------
    -- Menu curado del fotografo (2026-07-15): un permiso propio del vertical y un rol Fotografo
    -- que SOLO lo tiene. Los menus de Fotos se re-apuntan de PermisoRoot a PermisoFotos; el rol
    -- Administrador (root) tambien recibe PermisoFotos para que root siga viendo el vertical.
    -- Resultado: el fotografo ve solo la seccion Fotos; root ve todo. (Blueprint del alta real.)
    -- ---------------------------------------------------------------------
    IF NOT EXISTS (SELECT 1 FROM gen_Permisos WHERE CodigoPermiso = 'PermisoFotos') THEN
        INSERT INTO gen_Permisos (Nombre, CodigoPermiso, Descripcion, Activo, PermisoPadreId, AplicacionId, EsRestringido)
        VALUES ('PermisoFotos', 'PermisoFotos', 'Vertical Fotos (menus del fotografo)', true, NULL, 1, false);
    END IF;

    permiso_fotos := (SELECT Id FROM gen_Permisos WHERE CodigoPermiso = 'PermisoFotos');

    UPDATE gen_Menus
    SET PermisoId = permiso_fotos
    WHERE Codigo IN ('Fotos', 'FotosEventos', 'FotosGrupos', 'FotosGaleria', 'FotosTarjetas', 'FotosPedidos');

    -- Rol default para nuevo tenant (patron seed.sql de CodigoBase: rol "Administrador Cliente"
    -- EsDefaultParaNuevoTenant=true). Sin un rol con este flag, el alta de tenant desde el ABM crea el
    -- usuario admin SIN rol y el tenant queda "sin administradores" (GetAdministradoresAsync busca
    -- usuarios con un rol default). El UPDATE repara bases donde el rol ya existia con el flag en false.
    IF NOT EXISTS (SELECT 1 FROM gen_Roles WHERE Descripcion = 'Fotógrafo') THEN
        INSERT INTO gen_Roles (Descripcion, EsDefaultParaNuevoTenant) VALUES ('Fotógrafo', true);
    END IF;

    UPDATE gen_Roles SET EsDefaultParaNuevoTenant = true
    WHERE Descripcion = 'Fotógrafo' AND EsDefaultParaNuevoTenant = false;

    rol_fotografo := (SELECT Id FROM gen_Roles WHERE Descripcion = 'Fotógrafo');

    -- Habilitado para ambas licencias: Visualizador (1, la del usuario fotografo dev) y
    -- Admin del Tenant Cliente (2, la default que recibe el admin de un tenant nuevo).
    IF NOT EXISTS (SELECT 1 FROM gen_TipoLicenciaRoles WHERE RolId = rol_fotografo AND TipoLicenciaId = 1) THEN
        INSERT INTO gen_TipoLicenciaRoles (RolId, TipoLicenciaId) VALUES (rol_fotografo, 1);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM gen_TipoLicenciaRoles WHERE RolId = rol_fotografo AND TipoLicenciaId = 2) THEN
        INSERT INTO gen_TipoLicenciaRoles (RolId, TipoLicenciaId) VALUES (rol_fotografo, 2);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM gen_RolPermisos WHERE RolId = rol_fotografo AND PermisoId = permiso_fotos) THEN
        INSERT INTO gen_RolPermisos (RolId, PermisoId) VALUES (rol_fotografo, permiso_fotos);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM gen_RolPermisos WHERE RolId = 1 AND PermisoId = permiso_fotos) THEN
        INSERT INTO gen_RolPermisos (RolId, PermisoId) VALUES (1, permiso_fotos);
    END IF;

    -- Reparacion: usuarios admin de tenants cliente creados ANTES de que existiera el rol default
    -- (quedaron sin ningun rol → el tenant listaba "sin administradores" y el usuario no veia menus).
    -- Les asigna el rol Fotografo; idempotente por el NOT EXISTS.
    INSERT INTO gen_UsuarioRoles (UsuarioId, RolId, TenantId)
    SELECT u.Id, rol_fotografo, u.TenantId
    FROM gen_Usuarios u
    WHERE u.TenantId <> 1
      AND u.Administrador = true
      AND NOT EXISTS (SELECT 1 FROM gen_UsuarioRoles ur WHERE ur.UsuarioId = u.Id AND ur.RolId = rol_fotografo);

    -- Reparacion (mismo criterio, hallazgo 2026-07-24): admin de un tenant cliente ACTIVO sin
    -- ninguna fila en gen_UsuarioAplicaciones no puede resolver AplicacionId (viene de un header que
    -- el front arma a partir de esta tabla) → MenuAppService.ArmarMenuUsuarioSegunPermisosAsync
    -- devuelve [] SIEMPRE para ese usuario, aunque tenga el rol y el permiso correctos (se vio
    -- impersonando adminb2: mismo rol que fotografo, pero sin fila acá quedaba sin menú). Se le da
    -- la app general (1) como default; solo tenants activos (el tenant inactivo de prueba, Id 3,
    -- queda a propósito sin esto — no es un caso real a navegar). Idempotente por el NOT EXISTS.
    INSERT INTO gen_UsuarioAplicaciones (UsuarioId, AplicacionId, "default", TenantId)
    SELECT u.Id, 1, true, u.TenantId
    FROM gen_Usuarios u
    INNER JOIN gen_Tenants t ON t.Id = u.TenantId AND t.Activo = true
    WHERE u.TenantId <> 1
      AND u.Administrador = true
      AND NOT EXISTS (SELECT 1 FROM gen_UsuarioAplicaciones ua WHERE ua.UsuarioId = u.Id AND ua.AplicacionId = 1);

    -- Usuario fotografo (tenant 2, admin del tenant, email confirmado, mismo hash del seed).
    IF NOT EXISTS (SELECT 1 FROM gen_Usuarios WHERE NormalizedUserName = 'FOTOGRAFO') THEN
        INSERT INTO gen_Usuarios
            (TenantId, Nombre, Apellido, Telefono, Administrador, TokenExpirationDate, DateCreated,
             FechaCambioClave, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed,
             PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumberConfirmed, TwoFactorEnabled,
             LockoutEnabled, AccessFailedCount)
        VALUES
            (2, 'Fotógrafo', 'Dev', 1100000000, true, '2099-12-31T00:00:00', now(),
             now(), 'fotografo', 'FOTOGRAFO', 'fotografo@tech-bi.com', 'FOTOGRAFO@TECH-BI.COM', true,
             'AQAAAAIAAYagAAAAEBrKnVSPJbU1wjiSQLP141pc8bQRIuttqdu0dBRmkVnYsyy5Zm3SdYs9z35wxWpvLw==',
             'FOTOGRAFO-STAMP-0000000000000001', 'fotografo-concurrency-0001', false, false, true, 0);
    END IF;

    uid := (SELECT Id FROM gen_Usuarios WHERE NormalizedUserName = 'FOTOGRAFO');

    -- Licencia activa (cupo del tenant 2 ya sembrado), rol Fotografo (menu curado) y aplicacion default.
    IF NOT EXISTS (SELECT 1 FROM gen_UsuarioTipoLicencia WHERE UsuarioId = uid) THEN
        INSERT INTO gen_UsuarioTipoLicencia (UsuarioId, TipoLicenciaId, CreatedDatetime, TenantId, IsActive)
        VALUES (uid, 1, now(), 2, true);
    END IF;

    DELETE FROM gen_UsuarioRoles WHERE UsuarioId = uid AND RolId = 1; -- antes tenia el rol Administrador (veia toda la plataforma)

    IF NOT EXISTS (SELECT 1 FROM gen_UsuarioRoles WHERE UsuarioId = uid AND RolId = rol_fotografo) THEN
        INSERT INTO gen_UsuarioRoles (UsuarioId, RolId, TenantId) VALUES (uid, rol_fotografo, 2);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM gen_UsuarioAplicaciones WHERE UsuarioId = uid AND AplicacionId = 1) THEN
        INSERT INTO gen_UsuarioAplicaciones (UsuarioId, AplicacionId, "default", TenantId) VALUES (uid, 1, true, 2);
    END IF;

    RAISE NOTICE 'Alta dev del fotografo OK (usuario fotografo, Id %, tenant 2).', uid;
END $$;
