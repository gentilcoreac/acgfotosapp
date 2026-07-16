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
-- Correr con:  sqlcmd -S localhost -d AcgFotos -I -i backend\scripts\dev-alta-fotografo.sql
-- (Para el alta REAL con el tenant/usuario definitivos, usar los ABMs del front.)
-- =====================================================================
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;

-- Apps por tenant: root resuelve su aplicacionId de aca (aplicaciones-tenant).
IF NOT EXISTS (SELECT 1 FROM [dbo].[gen_TenantAplicaciones] WHERE [TenantId] = 1 AND [AplicacionId] = 1)
    INSERT INTO [dbo].[gen_TenantAplicaciones] ([AplicacionId], [TenantId]) VALUES (1, 1);
IF NOT EXISTS (SELECT 1 FROM [dbo].[gen_TenantAplicaciones] WHERE [TenantId] = 2 AND [AplicacionId] = 1)
    INSERT INTO [dbo].[gen_TenantAplicaciones] ([AplicacionId], [TenantId]) VALUES (1, 2);

-- Rol Administrador (Id 1): habilitado para la licencia Visualizador y con el permiso de los
-- menus del seed (PermisoRoot=1; ver comentario del seed de menus en TestSeed.sql).
IF NOT EXISTS (SELECT 1 FROM [dbo].[gen_TipoLicenciaRoles] WHERE [RolId] = 1 AND [TipoLicenciaId] = 1)
    INSERT INTO [dbo].[gen_TipoLicenciaRoles] ([RolId], [TipoLicenciaId]) VALUES (1, 1);
IF NOT EXISTS (SELECT 1 FROM [dbo].[gen_RolPermisos] WHERE [RolId] = 1 AND [PermisoId] = 1)
    INSERT INTO [dbo].[gen_RolPermisos] ([RolId], [PermisoId]) VALUES (1, 1);

-- ---------------------------------------------------------------------
-- Menu curado del fotografo (2026-07-15): un permiso propio del vertical y un rol Fotografo
-- que SOLO lo tiene. Los menus de Fotos se re-apuntan de PermisoRoot a PermisoFotos; el rol
-- Administrador (root) tambien recibe PermisoFotos para que root siga viendo el vertical.
-- Resultado: el fotografo ve solo la seccion Fotos; root ve todo. (Blueprint del alta real.)
-- ---------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [dbo].[gen_Permisos] WHERE [CodigoPermiso] = N'PermisoFotos')
    INSERT INTO [dbo].[gen_Permisos] ([Nombre], [CodigoPermiso], [Descripcion], [Activo], [PermisoPadreId], [AplicacionId], [EsRestringido])
    VALUES (N'PermisoFotos', N'PermisoFotos', N'Vertical Fotos (menus del fotografo)', 1, NULL, 1, 0);

DECLARE @permisoFotos BIGINT = (SELECT [Id] FROM [dbo].[gen_Permisos] WHERE [CodigoPermiso] = N'PermisoFotos');

UPDATE [dbo].[gen_Menus]
SET [PermisoId] = @permisoFotos
WHERE [Codigo] IN (N'Fotos', N'FotosEventos', N'FotosGrupos', N'FotosGaleria', N'FotosTarjetas');

-- Rol default para nuevo tenant (patron seed.sql de CodigoBase: rol "Administrador Cliente"
-- EsDefaultParaNuevoTenant=1). Sin un rol con este flag, el alta de tenant desde el ABM crea el
-- usuario admin SIN rol y el tenant queda "sin administradores" (GetAdministradoresAsync busca
-- usuarios con un rol default). El UPDATE repara bases donde el rol ya existia con el flag en 0.
IF NOT EXISTS (SELECT 1 FROM [dbo].[gen_Roles] WHERE [Descripcion] = N'Fotógrafo')
    INSERT INTO [dbo].[gen_Roles] ([Descripcion], [EsDefaultParaNuevoTenant]) VALUES (N'Fotógrafo', 1);

UPDATE [dbo].[gen_Roles] SET [EsDefaultParaNuevoTenant] = 1
WHERE [Descripcion] = N'Fotógrafo' AND [EsDefaultParaNuevoTenant] = 0;

DECLARE @rolFotografo BIGINT = (SELECT [Id] FROM [dbo].[gen_Roles] WHERE [Descripcion] = N'Fotógrafo');

-- Habilitado para ambas licencias: Visualizador (1, la del usuario fotografo dev) y
-- Admin del Tenant Cliente (2, la default que recibe el admin de un tenant nuevo).
IF NOT EXISTS (SELECT 1 FROM [dbo].[gen_TipoLicenciaRoles] WHERE [RolId] = @rolFotografo AND [TipoLicenciaId] = 1)
    INSERT INTO [dbo].[gen_TipoLicenciaRoles] ([RolId], [TipoLicenciaId]) VALUES (@rolFotografo, 1);
IF NOT EXISTS (SELECT 1 FROM [dbo].[gen_TipoLicenciaRoles] WHERE [RolId] = @rolFotografo AND [TipoLicenciaId] = 2)
    INSERT INTO [dbo].[gen_TipoLicenciaRoles] ([RolId], [TipoLicenciaId]) VALUES (@rolFotografo, 2);
IF NOT EXISTS (SELECT 1 FROM [dbo].[gen_RolPermisos] WHERE [RolId] = @rolFotografo AND [PermisoId] = @permisoFotos)
    INSERT INTO [dbo].[gen_RolPermisos] ([RolId], [PermisoId]) VALUES (@rolFotografo, @permisoFotos);
IF NOT EXISTS (SELECT 1 FROM [dbo].[gen_RolPermisos] WHERE [RolId] = 1 AND [PermisoId] = @permisoFotos)
    INSERT INTO [dbo].[gen_RolPermisos] ([RolId], [PermisoId]) VALUES (1, @permisoFotos);

-- Reparacion: usuarios admin de tenants cliente creados ANTES de que existiera el rol default
-- (quedaron sin ningun rol → el tenant listaba "sin administradores" y el usuario no veia menus).
-- Les asigna el rol Fotografo; idempotente por el NOT EXISTS.
INSERT INTO [dbo].[gen_UsuarioRoles] ([UsuarioId], [RolId], [TenantId])
SELECT u.[Id], @rolFotografo, u.[TenantId]
FROM [dbo].[gen_Usuarios] u
WHERE u.[TenantId] <> 1
  AND u.[Administrador] = 1
  AND NOT EXISTS (SELECT 1 FROM [dbo].[gen_UsuarioRoles] ur WHERE ur.[UsuarioId] = u.[Id] AND ur.[RolId] = @rolFotografo);

-- Usuario fotografo (tenant 2, admin del tenant, email confirmado, mismo hash del seed).
IF NOT EXISTS (SELECT 1 FROM [dbo].[gen_Usuarios] WHERE [NormalizedUserName] = N'FOTOGRAFO')
BEGIN
    INSERT INTO [dbo].[gen_Usuarios]
        ([TenantId], [Nombre], [Apellido], [Telefono], [Administrador], [TokenExpirationDate], [DateCreated],
         [FechaCambioClave], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed],
         [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumberConfirmed], [TwoFactorEnabled],
         [LockoutEnabled], [AccessFailedCount])
    VALUES
        (2, N'Fotógrafo', N'Dev', 1100000000, 1, CAST(N'2099-12-31T00:00:00' AS DATETIME2), SYSUTCDATETIME(),
         SYSUTCDATETIME(), N'fotografo', N'FOTOGRAFO', N'fotografo@tech-bi.com', N'FOTOGRAFO@TECH-BI.COM', 1,
         N'AQAAAAIAAYagAAAAEBrKnVSPJbU1wjiSQLP141pc8bQRIuttqdu0dBRmkVnYsyy5Zm3SdYs9z35wxWpvLw==',
         N'FOTOGRAFO-STAMP-0000000000000001', N'fotografo-concurrency-0001', 0, 0, 1, 0);
END

DECLARE @uid BIGINT = (SELECT [Id] FROM [dbo].[gen_Usuarios] WHERE [NormalizedUserName] = N'FOTOGRAFO');

-- Licencia activa (cupo del tenant 2 ya sembrado), rol Fotografo (menu curado) y aplicacion default.
IF NOT EXISTS (SELECT 1 FROM [dbo].[gen_UsuarioTipoLicencia] WHERE [UsuarioId] = @uid)
    INSERT INTO [dbo].[gen_UsuarioTipoLicencia] ([UsuarioId], [TipoLicenciaId], [CreatedDatetime], [TenantId], [IsActive])
    VALUES (@uid, 1, SYSUTCDATETIME(), 2, 1);
DELETE FROM [dbo].[gen_UsuarioRoles] WHERE [UsuarioId] = @uid AND [RolId] = 1; -- antes tenia el rol Administrador (veia toda la plataforma)
IF NOT EXISTS (SELECT 1 FROM [dbo].[gen_UsuarioRoles] WHERE [UsuarioId] = @uid AND [RolId] = @rolFotografo)
    INSERT INTO [dbo].[gen_UsuarioRoles] ([UsuarioId], [RolId], [TenantId]) VALUES (@uid, @rolFotografo, 2);
IF NOT EXISTS (SELECT 1 FROM [dbo].[gen_UsuarioAplicaciones] WHERE [UsuarioId] = @uid AND [AplicacionId] = 1)
    INSERT INTO [dbo].[gen_UsuarioAplicaciones] ([UsuarioId], [AplicacionId], [Default], [TenantId]) VALUES (@uid, 1, 1, 2);

PRINT CONCAT('Alta dev del fotografo OK (usuario fotografo, Id ', @uid, ', tenant 2).');
