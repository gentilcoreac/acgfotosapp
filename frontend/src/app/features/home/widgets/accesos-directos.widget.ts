import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { RouterModule } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { catchError, of } from 'rxjs';
import { AuthStore } from '../../../core/auth';
import { MenusService } from '../../menu/data/menus.service';
import { MenuDash } from '../../menu/domain/menu.model';
import { MENU_LABELS } from '../../../layout/menu/menu-item.model';

/** Un acceso directo ya resuelto a presentación (label + ícono + ruta). */
interface Acceso {
  label: string;
  icon: string;
  route: string;
}

/**
 * Widget de **accesos directos**: tiles sutiles a las secciones marcadas `VisibleDash`. La API
 * (`menus/dashboard`) ya las filtra por los permisos del usuario, así que no hace falta gatear acá
 * — se muestra lo que vuelve (si hay). Es lo más útil para el usuario común (su "lanzadera").
 * Label se resuelve por `codigo` (`MENU_LABELS`), igual que el sidenav; el ícono sale directo de
 * `imagenWeb` (backend).
 *
 * `:host { display: contents }` + sección a todo el ancho del grid del home.
 */
@Component({
  selector: 'tbi-accesos-directos-widget',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterModule, MatIconModule],
  template: `
    @if (accesos().length > 0) {
      <section class="accesos">
        <h2 class="accesos__title">Accesos directos</h2>
        <div class="accesos__tiles">
          @for (a of accesos(); track a.route) {
            <a class="acceso" [routerLink]="a.route">
              <mat-icon class="acceso__icon">{{ a.icon }}</mat-icon>
              <span class="acceso__label">{{ a.label }}</span>
            </a>
          }
        </div>
      </section>
    }
  `,
  styles: `
    :host {
      display: contents;
    }

    .accesos {
      grid-column: 1 / -1;
    }

    .accesos__title {
      margin: 0 0 0.75rem;
      font: var(--mat-sys-title-small);
      color: var(--mat-sys-on-surface-variant);
    }

    .accesos__tiles {
      display: flex;
      flex-wrap: wrap;
      gap: 10px;
    }

    // Tile sutil: chip-tarjeta de una línea (ícono + label), más bajo y discreto que una KPI card.
    .acceso {
      display: inline-flex;
      align-items: center;
      gap: 8px;
      padding: 8px 14px;
      border: 1px solid color-mix(in srgb, var(--mat-sys-outline-variant) 55%, transparent);
      border-radius: 12px;
      background: var(--mat-sys-surface-container-low);
      color: var(--mat-sys-on-surface);
      text-decoration: none;
      font: var(--mat-sys-label-large);
      transition: border-color 0.15s ease;
    }

    .acceso:hover {
      border-color: color-mix(in srgb, var(--mat-sys-primary) 45%, transparent);
    }

    .acceso__icon {
      width: 20px;
      height: 20px;
      font-size: 20px;
      color: var(--mat-sys-on-surface-variant);
    }
  `,
})
export class AccesosDirectosWidget {
  private readonly menus = inject(MenusService);
  private readonly store = inject(AuthStore);

  /** Refetchea sola al cambiar el contexto (login / impersonar / parar): cambia el menú del usuario.
   * Cancela el pedido en vuelo anterior automáticamente (antes `Subscription`/`destroyRef` a mano). */
  private readonly dataResource = rxResource({
    params: () => this.store.tenantId(),
    stream: () => this.menus.getAccesosDirectos().pipe(catchError(() => of<MenuDash[]>([]))),
  });

  /** Tiles a mostrar: descarta contenedores sin ruta y resuelve label/ícono por código. */
  protected readonly accesos = computed<Acceso[]>(() =>
    (this.dataResource.value() ?? [])
      .filter((m): m is MenuDash & { routePath: string } => m.routePath != null)
      .map((m) => ({
        label: MENU_LABELS[m.codigo] ?? m.nombre,
        icon: m.imagenWeb ?? 'chevron_right',
        route: m.routePath,
      })),
  );
}
