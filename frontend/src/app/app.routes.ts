import { Routes } from '@angular/router';
import { allowedRoutesGuard, anonGuard, authGuard } from './core/auth';

export const routes: Routes = [
  {
    path: 'login',
    canMatch: [anonGuard],
    loadComponent: () =>
      import('./features/auth/login/login.component').then((m) => m.LoginComponent),
  },
  {
    // Activación de cuenta vía link del mail. Anónima: el usuario aún no tiene clave/sesión.
    path: 'confirmar-cuenta',
    canMatch: [anonGuard],
    loadComponent: () =>
      import('./features/auth/confirmar-cuenta/confirmar-cuenta.component').then(
        (m) => m.ConfirmarCuentaComponent,
      ),
  },
  {
    // Pantalla de reconexión: la restauración de sesión falló por causa transitoria (429/red). Sin
    // guard (el usuario no está autenticado mientras reintenta); reintenta y vuelve a `returnUrl`.
    path: 'reconnecting',
    loadComponent: () =>
      import('./features/auth/reconnecting/reconnecting.component').then(
        (m) => m.ReconnectingComponent,
      ),
  },
  {
    path: '',
    canMatch: [authGuard],
    canActivateChild: [allowedRoutesGuard],
    loadComponent: () =>
      import('./layout/main-layout/main-layout.component').then((m) => m.MainLayoutComponent),
    children: [
      {
        // Vertical Fotos (admin): el fotógrafo arma sus eventos.
        path: 'fotos/eventos',
        loadComponent: () =>
          import('./features/fotos/eventos/ui/eventos-list.component').then(
            (m) => m.EventosListComponent,
          ),
      },
      {
        path: 'fotos/cursos',
        loadComponent: () =>
          import('./features/fotos/cursos/ui/cursos-list.component').then(
            (m) => m.CursosListComponent,
          ),
      },
      {
        // Pantalla única de fotos: subida masiva a curso/álbum + grilla de verificación
        // (thumbs con watermark, preview, descarga del original, borrado).
        path: 'fotos/galeria',
        loadComponent: () =>
          import('./features/fotos/fotos/ui/galeria.component').then((m) => m.GaleriaComponent),
      },
      {
        // Tarjetas de acceso por curso (código + QR por alumno), con impresión.
        path: 'fotos/tarjetas',
        loadComponent: () =>
          import('./features/fotos/tarjetas/ui/tarjetas.component').then(
            (m) => m.TarjetasComponent,
          ),
      },
      {
        path: 'tipos-licencias',
        loadComponent: () =>
          import('./features/tipos-licencias/ui/tipos-licencias-list.component').then(
            (m) => m.TiposLicenciasListComponent,
          ),
      },
      {
        path: 'parametros',
        loadComponent: () =>
          import('./features/parametros/ui/parametros-list.component').then(
            (m) => m.ParametrosListComponent,
          ),
      },
      {
        path: 'parametros-valor-tenant',
        loadComponent: () =>
          import('./features/parametros-valor-tenant/ui/parametros-valor-tenant.component').then(
            (m) => m.ParametrosValorTenantComponent,
          ),
      },
      {
        path: 'roles',
        loadComponent: () =>
          import('./features/roles/ui/roles-list.component').then((m) => m.RolesListComponent),
      },
      {
        path: 'permisos',
        loadComponent: () =>
          import('./features/permisos/ui/permisos-list.component').then(
            (m) => m.PermisosListComponent,
          ),
      },
      {
        path: 'usuarios',
        loadComponent: () =>
          import('./features/usuarios/ui/usuarios-list.component').then(
            (m) => m.UsuariosListComponent,
          ),
      },
      {
        path: 'aplicaciones',
        loadComponent: () =>
          import('./features/aplicaciones/ui/aplicaciones-list.component').then(
            (m) => m.AplicacionesListComponent,
          ),
      },
      {
        path: 'menus',
        loadComponent: () =>
          import('./features/menu/ui/menus-list.component').then((m) => m.MenusListComponent),
      },
      {
        path: 'tenants',
        loadComponent: () =>
          import('./features/tenant/ui/tenants-list.component').then((m) => m.TenantsListComponent),
      },
      {
        path: 'grupos',
        loadComponent: () =>
          import('./features/grupos/ui/grupos-list.component').then((m) => m.GruposListComponent),
      },
      {
        path: 'endpoints',
        loadComponent: () =>
          import('./features/endpoints/ui/endpoints-list.component').then(
            (m) => m.EndpointsListComponent,
          ),
      },
      {
        path: 'logs',
        loadComponent: () =>
          import('./features/logs/ui/logs-list.component').then((m) => m.LogsListComponent),
      },
      {
        path: 'auditoria',
        loadComponent: () =>
          import('./features/auditoria/ui/auditoria-list.component').then(
            (m) => m.AuditoriaListComponent,
          ),
      },
      {
        path: 'perfil',
        loadComponent: () =>
          import('./features/profile/ui/profile.component').then((m) => m.ProfileComponent),
      },
      {
        path: 'sin-permisos',
        loadComponent: () =>
          import('./features/errors/acceso-denegado.component').then(
            (m) => m.AccesoDenegadoComponent,
          ),
      },
      {
        path: '404',
        loadComponent: () =>
          import('./features/errors/not-found.component').then((m) => m.NotFoundComponent),
      },
      {
        path: '',
        loadComponent: () => import('./features/home/home.component').then((m) => m.HomeComponent),
      },
    ],
  },
  // Ruta inexistente → 404 (el `errorInterceptor` también redirige acá ante un 404 de la API).
  { path: '**', redirectTo: '404' },
];
