import { BreakpointObserver } from '@angular/cdk/layout';
import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { map } from 'rxjs';
import { AplicacionContextStore } from '../../core/aplicacion-context';
import { AuthService, AuthStore, ImpersonationService } from '../../core/auth';
import { AppConfigService } from '../../core/config';
import { TenantStyleStore } from '../../core/tenant-style';
import { ThemeStore } from '../../core/theme';
import { ImpersonationDialogComponent } from '../impersonation/impersonation-dialog.component';
import { MENU_ICON_BY_CODE, MENU_LABELS, MenuItem, MenuNode, MenuSection } from '../menu/menu-item.model';

/** Ítem "Inicio" siempre presente (encabeza el sidenav y es el fallback mínimo si el menú falla). */
const HOME_ITEM: MenuItem = { label: 'Inicio', icon: 'home', route: '/' };
import { MenuService } from '../menu/menu.service';

/**
 * Shell autenticado: toolbar + sidenav toggleable + outlet de rutas hijas.
 *
 * El sidenav es toggleable (abrir/cerrar). El modo comprimido (solo iconos) /
 * expandido (icono + texto) queda para un segundo momento.
 *
 * El menú se construye desde el backend (`menus/principal`, por permisos del usuario y aplicación
 * activa). Se aplana a las hojas navegables (rutas que existen en el front); si falla, deja solo
 * "Inicio" (nunca el menú completo, que filtraría opciones a un usuario sin permiso).
 */
@Component({
  selector: 'tbi-main-layout',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    MatToolbarModule,
    MatSidenavModule,
    MatListModule,
    MatIconModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
  ],
  templateUrl: './main-layout.component.html',
  styleUrl: './main-layout.component.scss',
})
export class MainLayoutComponent {
  private readonly auth = inject(AuthService);
  private readonly menuService = inject(MenuService);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly impersonation = inject(ImpersonationService);
  protected readonly authStore = inject(AuthStore);
  protected readonly aplicacionStore = inject(AplicacionContextStore);
  protected readonly theme = inject(ThemeStore);
  protected readonly tenantStyle = inject(TenantStyleStore);

  /**
   * Breakpoint mobile (< 768px). En mobile el sidenav pasa a `over` (overlay con backdrop, cerrado
   * por defecto); en desktop es `side` (siempre visible, con rail). `BreakpointObserver` mantiene el
   * signal en sync ante resize/rotación.
   */
  private readonly breakpoints = inject(BreakpointObserver);
  protected readonly isMobile = toSignal(
    this.breakpoints.observe('(max-width: 767.98px)').pipe(map((s) => s.matches)),
    { initialValue: this.breakpoints.isMatched('(max-width: 767.98px)') },
  );

  /** Título de la app (fallback cuando el tenant no tiene logo de header). */
  protected readonly appTitle = inject(AppConfigService).config().appTitle;

  /** Inicial del usuario efectivo para el avatar del sidenav (primera letra del `sub`). */
  protected readonly userInitial = computed(() => {
    const name = this.authStore.currentUserName();
    return name ? name.charAt(0).toUpperCase() : '?';
  });

  /** Secciones del sidenav, agrupadas desde el menú del backend (por permisos). */
  protected readonly menuSections = signal<MenuSection[]>([]);
  /** Sidenav en modo rail (sólo íconos, 84px) vs expandido (288px). En desktop, el hamburger lo alterna. */
  protected readonly collapsed = signal(false);
  /** Abierto/cerrado del sidenav en modo `over` (mobile). En desktop el sidenav está siempre abierto. */
  protected readonly opened = signal(false);
  /** Spinner del botón "Salir" del banner de impersonalización. */
  protected readonly stopping = signal(false);

  constructor() {
    // Recarga el menú cuando el contexto de aplicación quedó resuelto (`version`, que bumpea al
    // final de cada `load()` de login/impersonar/parar/F5) o cuando el usuario cambia de app a mano
    // (`selectedId`). NO watcheamos `currentUserName` directo: cambia antes (sync, con el token) que
    // la app (async), y dispararía una primera carga de menú con la aplicación vieja. `version`
    // garantiza una única recarga, ya con (usuario + app) consistentes.
    effect(() => {
      this.aplicacionStore.version();
      this.aplicacionStore.selectedId();
      this.loadMenu();
    });
  }

  private loadMenu(): void {
    this.menuService.getPrincipal().subscribe({
      next: (tree) => this.menuSections.set(this.toSections(tree)),
      // Si el menú del backend falla (ej. 429, blip), NO caer al menú estático completo: mostraría
      // TODAS las opciones a un usuario final. Tampoco conservar el anterior (tras impersonar sería
      // el del usuario previo). Dejamos solo "Inicio" hasta el próximo load exitoso.
      error: () => this.menuSections.set([{ label: null, items: [HOME_ITEM] }]),
    });
  }

  /**
   * Hamburger. En mobile (`over`) abre/cierra el overlay; en desktop (`side`) alterna el rail.
   */
  protected toggle(): void {
    if (this.isMobile()) {
      this.opened.update((value) => !value);
    } else {
      this.collapsed.update((value) => !value);
    }
  }

  /** En mobile, navegar cierra el overlay (en desktop el sidenav queda fijo). */
  protected onNavigate(): void {
    if (this.isMobile()) {
      this.opened.set(false);
    }
  }

  protected onSelectAplicacion(id: number): void {
    this.aplicacionStore.select(id);
  }

  protected logout(): void {
    this.auth.logout();
  }

  /** Abre el selector de impersonalización (tenant + usuario). Visible solo para root. */
  protected openImpersonate(): void {
    this.dialog.open(ImpersonationDialogComponent, { width: '420px', autoFocus: false });
  }

  /** Vuelve al contexto de root. */
  protected stopImpersonation(): void {
    this.stopping.set(true);
    this.impersonation.stop().subscribe({
      next: () => this.stopping.set(false),
      error: () => this.stopping.set(false),
    });
  }

  /**
   * Agrupa el árbol del backend en las secciones del sidenav. La agrupación es **data-driven**: cada
   * menú padre de primer nivel es una sección y sus hojas navegables (rutas con `routePath` que
   * **exista** como ruta registrada en el front) son sus ítems. Antepone "Inicio" en una sección sin
   * encabezado; etiqueta secciones e ítems vía `MENU_LABELS` (puente hasta i18n) e íconos vía
   * `MENU_ICON_BY_CODE`; ordena secciones e ítems por el `orden` del backend. Una sección sin hojas
   * navegables (todas sus features sin migrar) no se renderiza.
   */
  private toSections(tree: MenuNode[]): MenuSection[] {
    const registered = this.registeredRoutes();

    // Hojas navegables dentro del subárbol de un nodo (incluido el propio nodo si tiene ruta).
    const collectLeaves = (node: MenuNode): MenuItem[] => {
      const leaves: { item: MenuItem; orden: number }[] = [];
      const walk = (n: MenuNode): void => {
        const route = n.routePath ?? '';
        if (route && registered.has(route.replace(/^\//, ''))) {
          leaves.push({
            item: {
              label: MENU_LABELS[n.codigo] ?? n.nombre,
              icon: MENU_ICON_BY_CODE[n.codigo] ?? n.imagenWeb ?? 'chevron_right',
              // Normalizado a slash inicial: el `routerLink` resuelve igual (la ruta es absoluta
              // desde la raíz autenticada) y el `data-testid` queda estable (`nav-/usuarios`),
              // alineado con el `href` que renderiza el `<a>`.
              route: route.startsWith('/') ? route : `/${route}`,
            },
            orden: n.orden,
          });
        }
        n.menuHijos?.forEach(walk);
      };
      walk(node);
      return leaves.sort((a, b) => a.orden - b.orden).map((x) => x.item);
    };

    // "Inicio" + cualquier menú navegable que cuelgue directo de la raíz (sin sección) van en una
    // sección sin encabezado.
    const rootless: MenuItem[] = [HOME_ITEM];
    const sections: MenuSection[] = [];

    for (const node of [...tree].sort((a, b) => a.orden - b.orden)) {
      const leaves = collectLeaves(node);
      if (!leaves.length) {
        continue;
      }
      const route = node.routePath ?? '';
      const isNavigableLeaf = !!route && registered.has(route.replace(/^\//, '')) && !node.menuHijos?.length;
      if (isNavigableLeaf) {
        rootless.push(leaves[0]);
      } else {
        sections.push({ label: MENU_LABELS[node.codigo] ?? node.nombre, items: leaves });
      }
    }

    return [{ label: null, items: rootless }, ...sections];
  }

  /** Paths registrados bajo el layout autenticado (para filtrar ítems sin ruta en el front). */
  private registeredRoutes(): Set<string> {
    const layout = this.router.config.find((r) => r.path === '' && !!r.children);
    return new Set((layout?.children ?? []).map((c) => c.path ?? '').filter((p) => p.length > 0));
  }
}
