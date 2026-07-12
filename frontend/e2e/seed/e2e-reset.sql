-- =====================================================================
-- E2E - Reset entre corridas (AcgFotos_TestE2E)
-- =====================================================================
-- Devuelve la base al estado base conocido borrando SOLO los datos transitorios que dejan los
-- tests que mutan (CRUD de usuarios): los usuarios creados con prefijo `e2e` (userName `e2e{ts}`,
-- NormalizedUserName `E2E…`). Los tests los borran en su propio flujo; esto limpia los que queden
-- por una corrida interrumpida, para que la suite sea determinista corrida a corrida.
--
-- NO toca el catálogo ni los usuarios sintéticos del seed (root/userb/usersinlic/userc/pageuser*).
-- Idempotente. NO incluye USE (se corre con sqlcmd -d AcgFotos_TestE2E). El `global-setup` lo aplica
-- antes de la corrida (y luego re-aplica e2e-extras.sql, idempotente, por si faltara algo).
-- =====================================================================
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;  -- gen_Usuarios tiene índices filtrados → lo exige para DELETE
SET ANSI_NULLS ON;

DECLARE @ids TABLE ([Id] BIGINT PRIMARY KEY);
INSERT INTO @ids ([Id])
SELECT [Id] FROM [dbo].[gen_Usuarios] WHERE [NormalizedUserName] LIKE N'E2E%';

DECLARE @n INT = (SELECT COUNT(*) FROM @ids);

-- Hijos directos del usuario (no-op si el alta del test solo creó la fila General).
DELETE FROM [dbo].[gen_UsuarioRoles]        WHERE [UsuarioId] IN (SELECT [Id] FROM @ids);
DELETE FROM [dbo].[gen_UsuarioTipoLicencia] WHERE [UsuarioId] IN (SELECT [Id] FROM @ids);
DELETE FROM [dbo].[gen_UsuarioAplicaciones] WHERE [UsuarioId] IN (SELECT [Id] FROM @ids);
DELETE FROM [dbo].[gen_Usuarios]            WHERE [Id]        IN (SELECT [Id] FROM @ids);

PRINT CONCAT('E2E reset OK (', @n, ' usuario(s) e2e* purgado(s)).');
