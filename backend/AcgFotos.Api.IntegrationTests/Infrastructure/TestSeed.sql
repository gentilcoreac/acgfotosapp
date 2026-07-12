-- =====================================================================
-- AcgFotos API - Seed de la base de TESTS (AcgFotos_Tests)
-- =====================================================================
-- Se ejecuta despues de cada Respawn reset (tablas vacias), por eso NO usa
-- IF NOT EXISTS ni GO: es un unico batch que se corre con un SqlCommand.
--
-- Password en claro de TODOS los usuarios sembrados: Root@AcgFotos2026!
-- (mismo PasswordHash que el seed de dev, PBKDF2-SHA256 formato Identity v3).
--
-- Modelo de datos pensado para los casos de Auth (110-auth.md):
--   - Tenant 1 = root (RootTenantId=1, activo)         -> usuarios root => isRoot=true
--   - Tenant 2 = activo                                -> usuarios no-root reales (isRoot=false)
--   - Tenant 3 = INACTIVO                              -> guarda de tenant inactivo
--   Usuarios:
--     1  root     (t1, Administrador, isRoot)          -> AUTH-01, AUTH-16, fuente de impersonacion
--     10 userb    (t2, no-root, EmailConfirmed, licencia activa) -> AUTH-02 (isRoot=false)
--     11 adminb   (t3 inactivo, Administrador, licencia activa)  -> AUTH-13 (admin ignora tenant inactivo)
--     12 userc    (t3 inactivo, no-root)               -> AUTH-12 / AUTH-29 (no-root en tenant inactivo)
-- =====================================================================

SET NOCOUNT ON;

-- Aplicacion (FK de Permisos y Parametros) --------------------------------
SET IDENTITY_INSERT [dbo].[gen_Aplicaciones] ON;
INSERT INTO [dbo].[gen_Aplicaciones] ([Id], [Codigo], [Nombre], [Activo], [Icono], [IconoUrl])
VALUES (1, N'general', N'General', 1, N'home', NULL);
SET IDENTITY_INSERT [dbo].[gen_Aplicaciones] OFF;

-- Permiso root (FK de Parametros) -----------------------------------------
SET IDENTITY_INSERT [dbo].[gen_Permisos] ON;
INSERT INTO [dbo].[gen_Permisos] ([Id], [Nombre], [CodigoPermiso], [Descripcion], [Activo], [PermisoPadreId], [AplicacionId], [EsRestringido])
VALUES (1, N'PermisoRoot', N'PermisoRoot', N'Permiso root', 1, NULL, 1, 1);
SET IDENTITY_INSERT [dbo].[gen_Permisos] OFF;

-- Tenants -----------------------------------------------------------------
SET IDENTITY_INSERT [dbo].[gen_Tenants] ON;
INSERT INTO [dbo].[gen_Tenants] ([Id], [Codigo], [Nombre], [Activo], [DarkModeByDefault], [TipoLayoutLogin], [LogoLoginLightUrl], [LogoLoginDarkUrl], [LogoHeaderLightUrl], [LogoHeaderDarkUrl], [FaviconUrl], [ErrorDescription], [HasError])
VALUES
    (1, N'root',     N'root',     1, 0, 0, N'', N'', N'', N'', N'', N'', 0),
    (2, N'tenant-b', N'Tenant B', 1, 0, 0, N'', N'', N'', N'', N'', N'', 0),
    (3, N'tenant-c', N'Tenant C', 0, 0, 0, N'', N'', N'', N'', N'', N'', 0);
SET IDENTITY_INSERT [dbo].[gen_Tenants] OFF;

-- TipoLicencia ------------------------------------------------------------
SET IDENTITY_INSERT [dbo].[gen_TipoLicencia] ON;
INSERT INTO [dbo].[gen_TipoLicencia] ([Id], [Descripcion], [CodigoTipoLicencia], [EsDefaultParaNuevoTenant])
VALUES
    (1, N'Visualizador',             N'ofDataReader', 0),
    (2, N'Admin del Tenant Cliente', N'adminCliente', 1);
SET IDENTITY_INSERT [dbo].[gen_TipoLicencia] OFF;

-- Roles -------------------------------------------------------------------
SET IDENTITY_INSERT [dbo].[gen_Roles] ON;
INSERT INTO [dbo].[gen_Roles] ([Id], [Descripcion], [EsDefaultParaNuevoTenant])
VALUES (1, N'Administrador', 0);
SET IDENTITY_INSERT [dbo].[gen_Roles] OFF;

-- Usuarios ----------------------------------------------------------------
SET IDENTITY_INSERT [dbo].[gen_Usuarios] ON;
INSERT INTO [dbo].[gen_Usuarios]
    ([Id], [TenantId], [Nombre], [Apellido], [Telefono], [Administrador], [TokenExpirationDate], [DateCreated], [FechaCambioClave], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [ProfilePicture])
VALUES
    (1,  1, N'root',    N'root', 1234567891, 1, CAST(N'2099-12-31T00:00:00' AS DateTime2), CAST(N'2001-01-01T00:00:00' AS DateTime2), CAST(N'2001-01-01T00:00:00' AS DateTime2), N'root',    N'ROOT',    N'admin@tech-bi.com',    N'ADMIN@TECH-BI.COM',    1, N'AQAAAAIAAYagAAAAEBrKnVSPJbU1wjiSQLP141pc8bQRIuttqdu0dBRmkVnYsyy5Zm3SdYs9z35wxWpvLw==', N'OEVZTCZGXK5BQTG2JM2AZOED7V3NHQTK', N'1858f640-3f6c-49f6-92e9-d0a9ffb39970', NULL, 0, 0, NULL, 0, 0, NULL),
    (10, 2, N'User',    N'B',    1122334455, 0, CAST(N'2099-12-31T00:00:00' AS DateTime2), CAST(N'2024-01-01T00:00:00' AS DateTime2), CAST(N'2024-01-01T00:00:00' AS DateTime2), N'userb',   N'USERB',   N'userb@tech-bi.com',    N'USERB@TECH-BI.COM',    1, N'AQAAAAIAAYagAAAAEBrKnVSPJbU1wjiSQLP141pc8bQRIuttqdu0dBRmkVnYsyy5Zm3SdYs9z35wxWpvLw==', N'USERB-STAMP-0000000000000000001', N'userb-concurrency-0001', NULL, 0, 0, NULL, 1, 0, NULL),
    (11, 3, N'Admin',   N'B',    1122334466, 1, CAST(N'2099-12-31T00:00:00' AS DateTime2), CAST(N'2024-01-01T00:00:00' AS DateTime2), CAST(N'2024-01-01T00:00:00' AS DateTime2), N'adminb',  N'ADMINB',  N'adminb@tech-bi.com',   N'ADMINB@TECH-BI.COM',   1, N'AQAAAAIAAYagAAAAEBrKnVSPJbU1wjiSQLP141pc8bQRIuttqdu0dBRmkVnYsyy5Zm3SdYs9z35wxWpvLw==', N'ADMINB-STAMP-000000000000000001', N'adminb-concurrency-0001', NULL, 0, 0, NULL, 1, 0, NULL),
    (12, 3, N'User',    N'C',    1122334477, 0, CAST(N'2099-12-31T00:00:00' AS DateTime2), CAST(N'2024-01-01T00:00:00' AS DateTime2), CAST(N'2024-01-01T00:00:00' AS DateTime2), N'userc',   N'USERC',   N'userc@tech-bi.com',    N'USERC@TECH-BI.COM',    1, N'AQAAAAIAAYagAAAAEBrKnVSPJbU1wjiSQLP141pc8bQRIuttqdu0dBRmkVnYsyy5Zm3SdYs9z35wxWpvLw==', N'USERC-STAMP-0000000000000000001', N'userc-concurrency-0001', NULL, 0, 0, NULL, 1, 0, NULL),
    -- pending (13): cuenta sin confirmar y SIN password -> confirmar-cuenta happy path (AUTH-51).
    (13, 2, N'Pending', N'User', 1122334488, 0, CAST(N'2099-12-31T00:00:00' AS DateTime2), CAST(N'2024-01-01T00:00:00' AS DateTime2), CAST(N'2024-01-01T00:00:00' AS DateTime2), N'pending', N'PENDING', N'pending@tech-bi.com',   N'PENDING@TECH-BI.COM',   0, NULL,                                                                                       N'PENDING-STAMP-00000000000000001', N'pending-concurrency-001', NULL, 0, 0, NULL, 1, 0, NULL),
    -- userb2 (14): segundo no-root en tenant 2 -> re-impersonar (AUTH-75) y aislamiento multi-tenant.
    (14, 2, N'User',    N'B2',   1122334499, 0, CAST(N'2099-12-31T00:00:00' AS DateTime2), CAST(N'2024-01-01T00:00:00' AS DateTime2), CAST(N'2024-01-01T00:00:00' AS DateTime2), N'userb2',  N'USERB2',  N'userb2@tech-bi.com',   N'USERB2@TECH-BI.COM',   1, N'AQAAAAIAAYagAAAAEBrKnVSPJbU1wjiSQLP141pc8bQRIuttqdu0dBRmkVnYsyy5Zm3SdYs9z35wxWpvLw==', N'USERB2-STAMP-000000000000000001', N'userb2-concurrency-0001', NULL, 0, 0, NULL, 1, 0, NULL),
    -- adminb2 (15): ADMIN del tenant 2 (Administrador=1) -> set-administrador, search excluye-admin, ops admin-only.
    (15, 2, N'Admin',   N'B2',   1122334400, 1, CAST(N'2099-12-31T00:00:00' AS DateTime2), CAST(N'2024-01-01T00:00:00' AS DateTime2), CAST(N'2024-01-01T00:00:00' AS DateTime2), N'adminb2', N'ADMINB2', N'adminb2@tech-bi.com',  N'ADMINB2@TECH-BI.COM',  1, N'AQAAAAIAAYagAAAAEBrKnVSPJbU1wjiSQLP141pc8bQRIuttqdu0dBRmkVnYsyy5Zm3SdYs9z35wxWpvLw==', N'ADMINB2-STAMP-00000000000000001', N'adminb2-concurrency-001', NULL, 0, 0, NULL, 1, 0, NULL);
SET IDENTITY_INSERT [dbo].[gen_Usuarios] OFF;

-- Cupos de licencia por tenant (TenantLicencia) ---------------------------
SET IDENTITY_INSERT [dbo].[gen_TenantLicencias] ON;
INSERT INTO [dbo].[gen_TenantLicencias] ([Id], [TenantId], [TipoLicenciaId], [Cantidad], [StartDatetime], [ExpireDatetime], [ModifiedDatetime])
VALUES
    (1, 2, 1, 5, CAST(N'2024-01-01T00:00:00' AS DateTime2), CAST(N'2099-12-31T00:00:00' AS DateTime2), CAST(N'2024-01-01T00:00:00' AS DateTime2)),
    (2, 3, 1, 5, CAST(N'2024-01-01T00:00:00' AS DateTime2), CAST(N'2099-12-31T00:00:00' AS DateTime2), CAST(N'2024-01-01T00:00:00' AS DateTime2));
SET IDENTITY_INSERT [dbo].[gen_TenantLicencias] OFF;

-- Licencia por usuario (UsuarioTipoLicencia) ------------------------------
SET IDENTITY_INSERT [dbo].[gen_UsuarioTipoLicencia] ON;
INSERT INTO [dbo].[gen_UsuarioTipoLicencia] ([Id], [UsuarioId], [TipoLicenciaId], [CreatedDatetime], [TenantId], [IsActive])
VALUES
    (1, 10, 1, CAST(N'2024-01-01T00:00:00' AS DateTime2), 2, 1),
    (2, 11, 1, CAST(N'2024-01-01T00:00:00' AS DateTime2), 3, 1),
    (3, 14, 1, CAST(N'2024-01-01T00:00:00' AS DateTime2), 2, 1),
    (4, 15, 1, CAST(N'2024-01-01T00:00:00' AS DateTime2), 2, 1);
SET IDENTITY_INSERT [dbo].[gen_UsuarioTipoLicencia] OFF;

-- Parametros de Email (Fwk.Email.*): EmailEnabled=false en dev/test (no envia SMTP real),
-- pero la fila Password debe existir (EmailSender.ObtenerParametros lanza si falta).
SET IDENTITY_INSERT [dbo].[gen_Parametros] ON;
INSERT INTO [dbo].[gen_Parametros] ([Id], [AplicacionId], [PermisoId], [TipoDato], [Nombre], [Descripcion], [Valor])
VALUES
    (7,  1, 1, 3, N'Fwk.Email.From',         N'GenParamsEmailFrom',     N'jkaiter@tech-bi.com'),
    (8,  1, 1, 3, N'Fwk.Email.SMTPServer',   N'GenParamsSMTPServer',    N'smtp.gmail.com'),
    (9,  1, 1, 1, N'Fwk.Email.Port',         N'GenParamsEmailPort',     N'587'),
    (10, 1, 1, 3, N'Fwk.Email.User',             N'GenParamsEmailUser',         N'jkaiter@tech-bi.com'),
    (11, 1, 1, 3, N'Fwk.Email.Password',         N'GenParamsEmailPassword',     N''),
    (12, 1, 1, 3, N'Fwk.Email.NombreRemitente',  N'GenParamsSenderName',        N'Test'),
    (13, 1, 1, 3, N'Fwk.Email.ForceToEmail',     N'GenParamsForceToEmail',      N'test@yopmail.com'),
    (14, 1, 1, 3, N'Fwk.Email.UseAnonymousLogin',N'GenParamsUseAnonymousLogin', N'false'),
    (15, 1, 1, 3, N'Fwk.Email.BypassCertificate',N'GenParamsBypassCertificate', N'false'),
    (16, 1, 1, 3, N'Fwk.Email.ForceTo',          N'GenParamsForceTo',           N'false'),
    (17, 1, 1, 3, N'Fwk.Email.SSL',              N'GenParamsSSL',               N'true'),
    (18, 1, 1, 3, N'Fwk.Email.EmailEnabled',     N'GenParamsEmailEnabled',      N'false');
SET IDENTITY_INSERT [dbo].[gen_Parametros] OFF;
