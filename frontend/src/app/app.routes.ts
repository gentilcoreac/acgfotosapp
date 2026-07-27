import { Routes } from '@angular/router';
import { allowedRoutesGuard, anonGuard, authGuard } from './core/auth';
import { familiaSessionGuard } from './core/familia';

export const routes: Routes = [
  {
    path: 'login',
    canMatch: [anonGuard],
    loadComponent: () =>
      import('./features/auth/login/login.component').then((m) => m.LoginComponent),
  },
  {
    // Canje de código (ADR-02): sin registro, fuera del layout admin. Llega por el form manual
    // (/canje) o por el link del QR de la tarjeta (/canje/:codigo, precarga y dispara solo).
    path: 'canje',
    loadComponent: () =>
      import('./features/familia/canje/ui/canje.component').then((m) => m.CanjeComponent),
  },
  {
    path: 'canje/:codigo',
    loadComponent: () =>
      import('./features/familia/canje/ui/canje.component').then((m) => m.CanjeComponent),
  },
  {
    // Landing post-canje de la familia: galería mobile-first con anti-copia.
    path: 'mi-album',
    canMatch: [familiaSessionGuard],
    loadComponent: () =>
      import('./features/familia/mi-album/ui/mi-album.component').then(
        (m) => m.MiAlbumComponent,
      ),
  },
  {
    // Carrito de la sesión de familia (Fase 2, ADR-07).
    path: 'carrito',
    canMatch: [familiaSessionGuard],
    loadComponent: () =>
      import('./features/familia/carrito/ui/carrito.component').then(
        (m) => m.CarritoComponent,
      ),
  },
  {
    // Confirmación de pedido: vista propia, fuera del layout admin (ver docs/05-notas-abiertas.md).
    path: 'pedido-confirmado',
    canMatch: [familiaSessionGuard],
    loadComponent: () =>
      import('./features/familia/pedido-confirmado/ui/pedido-confirmado.component').then(
        (m) => m.PedidoConfirmadoComponent,
      ),
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
    // Solicitud de mail de recuperación de clave (link "olvidé mi contraseña" del login).
    path: 'olvide-password',
    canMatch: [anonGuard],
    loadComponent: () =>
      import('./features/auth/olvide-password/olvide-password.component').then(
        (m) => m.OlvidePasswordComponent,
      ),
  },
  {
    // Restablecimiento de clave vía link del mail. Anónima: el usuario aún no tiene sesión.
    path: 'recuperar-clave',
    canMatch: [anonGuard],
    loadComponent: () =>
      import('./features/auth/recuperar-clave/recuperar-clave.component').then(
        (m) => m.RecuperarClaveComponent,
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
        path: 'fotos/grupos',
        loadComponent: () =>
          import('./features/fotos/grupos/ui/grupos-list.component').then(
            (m) => m.GruposListComponent,
          ),
      },
      {
        // Pantalla única de fotos: subida masiva a grupo/participante + grilla de verificación
        // (thumbs con watermark, preview, descarga del original, borrado).
        path: 'fotos/galeria',
        loadComponent: () =>
          import('./features/fotos/fotos/ui/galeria.component').then((m) => m.GaleriaComponent),
      },
      {
        // Tarjetas de acceso por grupo (código + QR por participante), con impresión.
        path: 'fotos/tarjetas',
        loadComponent: () =>
          import('./features/fotos/tarjetas/ui/tarjetas.component').then(
            (m) => m.TarjetasComponent,
          ),
      },
      {
        // Admin de pedidos: listado por evento/estado, detalle y cambio de estado.
        path: 'fotos/pedidos',
        loadComponent: () =>
          import('./features/fotos/pedidos/ui/pedidos-list.component').then(
            (m) => m.PedidosListComponent,
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
