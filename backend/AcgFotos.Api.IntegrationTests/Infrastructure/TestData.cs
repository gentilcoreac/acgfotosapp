namespace AcgFotos.Api.IntegrationTests.Infrastructure
{
    /// <summary>
    /// Datos del seed canonico de tests (<c>TestSeed.sql</c>). Centralizados para no hardcodear
    /// strings sueltos en los tests.
    /// </summary>
    public static class TestData
    {
        /// <summary>Password en claro de todos los usuarios sembrados.</summary>
        public const string Password = "Root@AcgFotos2026!";

        /// <summary>RootTenantId (appsettings.Development) = tenant 1.</summary>
        public const long RootTenantId = 1;
        public const long ActiveTenantId = 2;   // tenant-b
        public const long InactiveTenantId = 3;  // tenant-c

        // Usuarios sembrados (ver cabecera de TestSeed.sql).
        public const string Root = "root";        // t1, Administrador, isRoot
        public const string UserB = "userb";      // t2, no-root, licencia activa
        public const string AdminB = "adminb";    // t3 inactivo, Administrador, licencia activa
        public const string UserC = "userc";      // t3 inactivo, no-root
        public const string Pending = "pending";  // t2, sin confirmar y sin password
        public const string UserB2 = "userb2";    // t2, segundo no-root (re-impersonar, multi-tenant)
        public const string AdminB2 = "adminb2";  // t2, ADMIN del tenant (set-administrador, ops admin)

        public const long RootId = 1;
        public const long UserBId = 10;
        public const long AdminBId = 11;
        public const long UserCId = 12;
        public const long PendingId = 13;
        public const long UserB2Id = 14;
        public const long AdminB2Id = 15;

        // Módulo Reportes — seed ids
        public const long CarpetaT2Id = 1;   // pbi_Carpetas, tenant 2
        public const long CarpetaT3Id = 2;   // pbi_Carpetas, tenant 3
        public const long ReporteConRlsId = 1;    // pbi_Reportes, tenant 2, RequiereRol=1
        public const long ReporteSinRlsId = 2;    // pbi_Reportes, tenant 2, RequiereRol=0
        public const long ReporteOtroTenantId = 3; // pbi_Reportes, tenant 3
        public const long RolPBIUserBId = 1;  // pbi_Roles: userb/Vendedores en ReporteConRls
    }
}
