-- =====================================================================
-- E2E - Extras sintéticos sobre el catálogo (AcgFotos_TestE2E)
-- =====================================================================
-- Se corre DESPUÉS del catálogo (Api/Docs/postman/seed.sql, con el USE quitado).
-- Agrega los datos transaccionales que el catálogo NO trae y que necesitan los
-- casos por rol (AUTH-02/03 + AUTHZ): un tenant cliente activo + un no-root real
-- (tenant ≠ raíz) con licencia y rol acotado + un no-root SIN licencia.
--
-- Password en claro de root/userb/usersinlic/userc: Root@AcgFotos2026!
-- ⚡ Los hashes son PBKDF2 a **1000 iteraciones** (no 100k): Identity lee el nº de iteraciones del
--   propio hash al verificar, así que el login es ~instantáneo y la suite deja de flakear por
--   latencia CPU-bound. Solo para la base E2E (NUNCA prod). Combinar con la API levantada con
--   Security__PasswordHasherIterations=1000 (los usuarios nuevos del CRUD también se hashean rápido).
--   Regenerar con: Api/Docs/postman/generate-password-hash.ps1 -Password '...' -IterCount 1000.
-- Idempotente (IF NOT EXISTS por Id). NO incluye USE (se corre con sqlcmd -d AcgFotos_TestE2E).
-- =====================================================================
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

-- root viene del catálogo (seed.sql) con hash a 100k → bajarlo a 1000 iter en la base E2E para que
-- el login de root (el actor de la mayoría de los specs) también sea rápido.
UPDATE [dbo].[gen_Usuarios]
SET [PasswordHash] = N'AQAAAAIAAYagAAAAEBrKnVSPJbU1wjiSQLP141pc8bQRIuttqdu0dBRmkVnYsyy5Zm3SdYs9z35wxWpvLw=='
WHERE [NormalizedUserName] = N'ROOT';

-- Tenant 2 = cliente ACTIVO (tenant ≠ raíz → sus usuarios son no-root reales) -----------
IF NOT EXISTS (SELECT 1 FROM [dbo].[gen_Tenants] WHERE [Id] = 2)
BEGIN
    SET IDENTITY_INSERT [dbo].[gen_Tenants] ON;
    INSERT INTO [dbo].[gen_Tenants] ([Id], [Codigo], [Nombre], [Activo], [DarkModeByDefault], [TipoLayoutLogin], [LogoLoginLightUrl], [LogoLoginDarkUrl], [LogoHeaderLightUrl], [LogoHeaderDarkUrl], [FaviconUrl], [ErrorDescription], [HasError])
    VALUES (2, N'tenant-b', N'Tenant B', 1, 0, 0, N'', N'', N'', N'', N'', N'', 0);
    SET IDENTITY_INSERT [dbo].[gen_Tenants] OFF;
END

-- Branding del tenant 2 (para THEME-01/02/03): colores + logos + favicon + hostName + título.
-- URLs distintivas (no necesitan existir; los tests afirman el atributo src, no que cargue).
-- UPDATE no-guardado: idempotente y arregla la fila aunque ya existiera sin branding.
UPDATE [dbo].[gen_Tenants] SET
    [ColorPrimarioLight]      = N'#7B1FA2',
    [ColorPrimarioDark]       = N'#CE93D8',
    [LogoHeaderLightUrl]      = N'https://e2e.test/tenant-b-header.png',
    [LogoHeaderDarkUrl]       = N'https://e2e.test/tenant-b-header-dark.png',
    [LogoLoginLightUrl]       = N'https://e2e.test/tenant-b-login.png',
    [LogoLoginDarkUrl]        = N'https://e2e.test/tenant-b-login-dark.png',
    [ImagenFondoLoginLightUrl]= N'https://e2e.test/tenant-b-bg.png',
    [FaviconUrl]              = N'https://e2e.test/tenant-b-fav.png',
    [TituloWeb]               = N'Tenant B',
    [HostName]                = N'tenant-b.e2e.test'
WHERE [Id] = 2;

-- Cupos de licencia del tenant 2: adminCliente (2) y Visualizador (1) -------------------
IF NOT EXISTS (SELECT 1 FROM [dbo].[gen_TenantLicencias] WHERE [Id] = 10)
BEGIN
    SET IDENTITY_INSERT [dbo].[gen_TenantLicencias] ON;
    INSERT INTO [dbo].[gen_TenantLicencias] ([Id], [TenantId], [TipoLicenciaId], [Cantidad], [StartDatetime], [ExpireDatetime], [ModifiedDatetime])
    VALUES
        (10, 2, 2, 5, CAST(N'2024-01-01' AS DateTime2), CAST(N'2099-12-31' AS DateTime2), CAST(N'2024-01-01' AS DateTime2)),
        (11, 2, 1, 5, CAST(N'2024-01-01' AS DateTime2), CAST(N'2099-12-31' AS DateTime2), CAST(N'2024-01-01' AS DateTime2));
    SET IDENTITY_INSERT [dbo].[gen_TenantLicencias] OFF;
END

-- userb (10): no-root del tenant 2, EmailConfirmed, con licencia adminCliente + Rol 2 ----
IF NOT EXISTS (SELECT 1 FROM [dbo].[gen_Usuarios] WHERE [Id] = 10)
BEGIN
    SET IDENTITY_INSERT [dbo].[gen_Usuarios] ON;
    INSERT INTO [dbo].[gen_Usuarios]
        ([Id], [TenantId], [Nombre], [Apellido], [Telefono], [Administrador], [TokenExpirationDate], [DateCreated], [FechaCambioClave], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [ProfilePicture])
    VALUES
        (10, 2, N'User', N'B', 1122334455, 0, CAST(N'2099-12-31' AS DateTime2), CAST(N'2024-01-01' AS DateTime2), CAST(N'2024-01-01' AS DateTime2), N'userb', N'USERB', N'userb@acgfotos.com', N'USERB@TECH-BI.COM', 1, N'AQAAAAIAAYagAAAAEBrKnVSPJbU1wjiSQLP141pc8bQRIuttqdu0dBRmkVnYsyy5Zm3SdYs9z35wxWpvLw==', N'USERB-STAMP-0000000000000000001', N'userb-concurrency-0001', NULL, 0, 0, NULL, 1, 0, NULL);
    SET IDENTITY_INSERT [dbo].[gen_Usuarios] OFF;
END

-- usersinlic (11): no-root del tenant 2, SIN licencia (login se bloquea) -----------------
IF NOT EXISTS (SELECT 1 FROM [dbo].[gen_Usuarios] WHERE [Id] = 11)
BEGIN
    SET IDENTITY_INSERT [dbo].[gen_Usuarios] ON;
    INSERT INTO [dbo].[gen_Usuarios]
        ([Id], [TenantId], [Nombre], [Apellido], [Telefono], [Administrador], [TokenExpirationDate], [DateCreated], [FechaCambioClave], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [ProfilePicture])
    VALUES
        (11, 2, N'User', N'SinLic', 1122334466, 0, CAST(N'2099-12-31' AS DateTime2), CAST(N'2024-01-01' AS DateTime2), CAST(N'2024-01-01' AS DateTime2), N'usersinlic', N'USERSINLIC', N'usersinlic@acgfotos.com', N'USERSINLIC@TECH-BI.COM', 1, N'AQAAAAIAAYagAAAAEBrKnVSPJbU1wjiSQLP141pc8bQRIuttqdu0dBRmkVnYsyy5Zm3SdYs9z35wxWpvLw==', N'USERSINLIC-STAMP-000000000000001', N'usersinlic-concurrency-001', NULL, 0, 0, NULL, 1, 0, NULL);
    SET IDENTITY_INSERT [dbo].[gen_Usuarios] OFF;
END

-- userb: Rol 2 (Administrador Cliente) en tenant 2 → menú acotado (Administración Cliente)
IF NOT EXISTS (SELECT 1 FROM [dbo].[gen_UsuarioRoles] WHERE [Id] = 10)
BEGIN
    SET IDENTITY_INSERT [dbo].[gen_UsuarioRoles] ON;
    INSERT INTO [dbo].[gen_UsuarioRoles] ([Id], [UsuarioId], [RolId], [TenantId]) VALUES (10, 10, 2, 2);
    SET IDENTITY_INSERT [dbo].[gen_UsuarioRoles] OFF;
END

-- userb: licencia adminCliente activa (habilita el Rol 2 por licencia) -------------------
IF NOT EXISTS (SELECT 1 FROM [dbo].[gen_UsuarioTipoLicencia] WHERE [Id] = 10)
BEGIN
    SET IDENTITY_INSERT [dbo].[gen_UsuarioTipoLicencia] ON;
    INSERT INTO [dbo].[gen_UsuarioTipoLicencia] ([Id], [UsuarioId], [TipoLicenciaId], [CreatedDatetime], [TenantId], [IsActive])
    VALUES (10, 10, 2, CAST(N'2024-01-01' AS DateTime2), 2, 1);
    SET IDENTITY_INSERT [dbo].[gen_UsuarioTipoLicencia] OFF;
END

-- userb: aplicación general (fuente del aplicacionId del header para no-root) ------------
IF NOT EXISTS (SELECT 1 FROM [dbo].[gen_UsuarioAplicaciones] WHERE [Id] = 10)
BEGIN
    SET IDENTITY_INSERT [dbo].[gen_UsuarioAplicaciones] ON;
    INSERT INTO [dbo].[gen_UsuarioAplicaciones] ([Id], [UsuarioId], [AplicacionId], [Default], [TenantId]) VALUES (10, 10, 1, 1, 2);
    SET IDENTITY_INSERT [dbo].[gen_UsuarioAplicaciones] OFF;
END

-- =====================================================================
-- Multi-app (grupo C / APP): 2ª aplicación + userb multi-app + userc single-app.
-- =====================================================================

-- Segunda aplicación (activa) para que userb sea multi-app y aparezca el selector ---------
IF NOT EXISTS (SELECT 1 FROM [dbo].[gen_Aplicaciones] WHERE [Id] = 2)
BEGIN
    SET IDENTITY_INSERT [dbo].[gen_Aplicaciones] ON;
    -- Sin acento a propósito: evita mojibake del seed por codepage de sqlcmd y selectores frágiles.
    INSERT INTO [dbo].[gen_Aplicaciones] ([Id], [Codigo], [Nombre], [Activo], [Icono], [IconoUrl]) VALUES (2, N'app-b', N'Aplicacion B', 1, N'apps', NULL);
    SET IDENTITY_INSERT [dbo].[gen_Aplicaciones] OFF;
END

-- userb: 2ª app (queda con app 1 General + app 2 → isMultiApp → selector visible) ---------
IF NOT EXISTS (SELECT 1 FROM [dbo].[gen_UsuarioAplicaciones] WHERE [Id] = 11)
BEGIN
    SET IDENTITY_INSERT [dbo].[gen_UsuarioAplicaciones] ON;
    INSERT INTO [dbo].[gen_UsuarioAplicaciones] ([Id], [UsuarioId], [AplicacionId], [Default], [TenantId]) VALUES (11, 10, 2, 0, 2);
    SET IDENTITY_INSERT [dbo].[gen_UsuarioAplicaciones] OFF;
END

-- userc (12): no-root del tenant 2, con licencia (Visualizador) y UNA sola app → NO ve selector
IF NOT EXISTS (SELECT 1 FROM [dbo].[gen_Usuarios] WHERE [Id] = 12)
BEGIN
    SET IDENTITY_INSERT [dbo].[gen_Usuarios] ON;
    INSERT INTO [dbo].[gen_Usuarios]
        ([Id], [TenantId], [Nombre], [Apellido], [Telefono], [Administrador], [TokenExpirationDate], [DateCreated], [FechaCambioClave], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [ProfilePicture])
    VALUES
        (12, 2, N'User', N'C', 1122334477, 0, CAST(N'2099-12-31' AS DateTime2), CAST(N'2024-01-01' AS DateTime2), CAST(N'2024-01-01' AS DateTime2), N'userc', N'USERC', N'userc@acgfotos.com', N'USERC@TECH-BI.COM', 1, N'AQAAAAIAAYagAAAAEBrKnVSPJbU1wjiSQLP141pc8bQRIuttqdu0dBRmkVnYsyy5Zm3SdYs9z35wxWpvLw==', N'USERC-STAMP-0000000000000000001', N'userc-concurrency-0001', NULL, 0, 0, NULL, 1, 0, NULL);
    SET IDENTITY_INSERT [dbo].[gen_Usuarios] OFF;
END

-- userc: licencia Visualizador activa (login OK) ----------------------------------------
IF NOT EXISTS (SELECT 1 FROM [dbo].[gen_UsuarioTipoLicencia] WHERE [Id] = 12)
BEGIN
    SET IDENTITY_INSERT [dbo].[gen_UsuarioTipoLicencia] ON;
    INSERT INTO [dbo].[gen_UsuarioTipoLicencia] ([Id], [UsuarioId], [TipoLicenciaId], [CreatedDatetime], [TenantId], [IsActive])
    VALUES (12, 12, 1, CAST(N'2024-01-01' AS DateTime2), 2, 1);
    SET IDENTITY_INSERT [dbo].[gen_UsuarioTipoLicencia] OFF;
END

-- userc: UNA sola aplicación (General) → isMultiApp false → selector oculto ---------------
IF NOT EXISTS (SELECT 1 FROM [dbo].[gen_UsuarioAplicaciones] WHERE [Id] = 12)
BEGIN
    SET IDENTITY_INSERT [dbo].[gen_UsuarioAplicaciones] ON;
    INSERT INTO [dbo].[gen_UsuarioAplicaciones] ([Id], [UsuarioId], [AplicacionId], [Default], [TenantId]) VALUES (12, 12, 1, 1, 2);
    SET IDENTITY_INSERT [dbo].[gen_UsuarioAplicaciones] OFF;
END

-- userc: Rol 3 (Planificador Editor) → permisos gral_cli(4)+gral_pla(5) → menú acotado
IF NOT EXISTS (SELECT 1 FROM [dbo].[gen_UsuarioRoles] WHERE [Id] = 12)
BEGIN
    SET IDENTITY_INSERT [dbo].[gen_UsuarioRoles] ON;
    INSERT INTO [dbo].[gen_UsuarioRoles] ([Id], [UsuarioId], [RolId], [TenantId]) VALUES (12, 12, 3, 2);
    SET IDENTITY_INSERT [dbo].[gen_UsuarioRoles] OFF;
END

-- =====================================================================
-- Paginación (CRUD-03/07): 15 usuarios "de relleno" en el tenant 1 (root) para que el listado
-- tenga >1 página (pageSize 10 → 2 páginas). Solo se listan (no logean): mínimos campos.
-- =====================================================================
DECLARE @i INT = 1;
WHILE @i <= 15
BEGIN
    DECLARE @id INT = 100 + @i;
    DECLARE @u NVARCHAR(50) = CONCAT(N'pageuser', RIGHT(CONCAT(N'0', @i), 2));
    IF NOT EXISTS (SELECT 1 FROM [dbo].[gen_Usuarios] WHERE [Id] = @id)
    BEGIN
        SET IDENTITY_INSERT [dbo].[gen_Usuarios] ON;
        INSERT INTO [dbo].[gen_Usuarios]
            ([Id], [TenantId], [Nombre], [Apellido], [Telefono], [Administrador], [TokenExpirationDate], [DateCreated], [FechaCambioClave], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [ProfilePicture])
        VALUES
            (@id, 1, CONCAT(N'Page', @i), N'User', 1122330000, 0, CAST(N'2099-12-31' AS DateTime2), CAST(N'2024-01-01' AS DateTime2), CAST(N'2024-01-01' AS DateTime2), @u, UPPER(@u), CONCAT(@u, N'@e2e.test'), UPPER(CONCAT(@u, N'@e2e.test')), 1, NULL, CONCAT(N'PAGEUSER-STAMP-', @id), CONCAT(N'pageuser-conc-', @id), NULL, 0, 0, NULL, 1, 0, NULL);
        SET IDENTITY_INSERT [dbo].[gen_Usuarios] OFF;
    END
    SET @i = @i + 1;
END

-- =====================================================================
-- Logs de aplicación (gen_LogInfos): vacíos en la base de tests (Serilog solo escribe errores en
-- runtime). Sembramos 2 filas en tenants distintos para el listado/detalle de la pantalla Logs
-- (root → AllTenants, cross-tenant) y para los tests de aislamiento por tenant.
-- =====================================================================
IF NOT EXISTS (SELECT 1 FROM [dbo].[gen_LogInfos] WHERE [Id] = 9001)
BEGIN
    SET IDENTITY_INSERT [dbo].[gen_LogInfos] ON;
    INSERT INTO [dbo].[gen_LogInfos] ([Id], [Message], [MessageTemplate], [Level], [TimeStamp], [Exception], [Properties], [TenantId])
    VALUES (9001, N'Error de prueba E2E (tenant 1)', N'Error de prueba E2E ({Tenant})', N'Error', SYSUTCDATETIME(), N'System.Exception: e2e tenant 1', N'{"Tenant":1}', 1);
    SET IDENTITY_INSERT [dbo].[gen_LogInfos] OFF;
END
IF NOT EXISTS (SELECT 1 FROM [dbo].[gen_LogInfos] WHERE [Id] = 9002)
BEGIN
    SET IDENTITY_INSERT [dbo].[gen_LogInfos] ON;
    INSERT INTO [dbo].[gen_LogInfos] ([Id], [Message], [MessageTemplate], [Level], [TimeStamp], [Exception], [Properties], [TenantId])
    VALUES (9002, N'Aviso de prueba E2E (tenant 2)', N'Aviso de prueba E2E ({Tenant})', N'Warning', SYSUTCDATETIME(), NULL, N'{"Tenant":2}', 2);
    SET IDENTITY_INSERT [dbo].[gen_LogInfos] OFF;
END

PRINT 'E2E extras OK (tenant 2 + userb + userc + 15 pageusers + 2 logs).';
