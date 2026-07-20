import { ChangeDetectionStrategy, Component, computed, inject, viewChild } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { Observable, EMPTY, catchError, of, tap } from 'rxjs';
import { AuthStore } from '../../../core/auth';
import { QueryParams } from '../../../core/models/query-params.model';
import { LicenciasService } from '../../licencias/data/licencias.service';
import { LicenciaResumen } from '../../licencias/domain/licencia-resumen.model';
import { estadoVigencia } from '../../licencias/domain/licencia-metrics';
import { LicenciasKpisComponent } from '../../licencias/ui/licencias-kpis.component';
import { LicenciasWarningBannerComponent } from '../../licencias/ui/licencias-warning-banner.component';
import { NotificationService } from '../../../shared/feedback/notification.service';
import { TbiRowAction } from '../../../shared/ui/tbi-row-actions/tbi-row-actions.component';
import { TbiColumn, TbiTableComponent } from '../../../shared/ui/tbi-table/tbi-table.component';
import { UsuariosService } from '../data/usuarios.service';
import { Usuario } from '../domain/usuario.model';
import { UsuarioEditComponent } from './usuario-edit.component';

// TODO (Fase 4 - i18n): textos en español por ahora.
@Component({
  selector: 'tbi-usuarios-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    MatButtonModule,
    MatIconModule,
    TbiTableComponent,
    LicenciasKpisComponent,
    LicenciasWarningBannerComponent,
  ],
  templateUrl: './usuarios-list.component.html',
  styleUrl: './usuarios-list.component.scss',
})
export class UsuariosListComponent {
  private readonly service = inject(UsuariosService);
  private readonly licenciasService = inject(LicenciasService);
  private readonly dialog = inject(MatDialog);
  private readonly notify = inject(NotificationService);

  protected readonly table = viewChild.required(TbiTableComponent);

  /**
   * Las licencias son un concepto **por tenant cliente**: cupos, asignación y vencimiento son del
   * negocio del cliente. Root opera sobre el tenant de sistema (sus "licencias" son artificiales,
   * p. ej. datos de seed), así que para root no aportan y se ocultan (KPIs, banner y columna). Al
   * **impersonar** un cliente el token deja de ser root → se muestran. Lo valida igual la API.
   */
  protected readonly isRoot = inject(AuthStore).isRoot;

  /**
   * Resumen de licencias del tenant. Una sola consulta alimenta tres cosas: los KPIs del header,
   * el banner de "por vencer" y la columna "Licencia" del listado (resolviendo id→descripción y el
   * tono del chip según vigencia). Por eso se carga acá, no en cada componente hijo.
   *
   * Root opera sobre el tenant de sistema, cuyas licencias son de seed (no de negocio): el resumen
   * devolvería datos que no aportan (p. ej. un Visualizador vencido en 2020) — no se pide (`params`
   * `undefined` = no fetchea).
   */
  private readonly resumenResource = rxResource({
    params: () => (this.isRoot() ? undefined : true),
    stream: () =>
      this.licenciasService.getResumen().pipe(catchError(() => of<LicenciaResumen[]>([]))),
  });
  protected readonly resumen = computed(() => this.resumenResource.value() ?? []);
  /** Índice tipoLicenciaId → resumen, para resolver la celda de licencia de cada fila. */
  private readonly licenciaPorId = computed(
    () => new Map(this.resumen().map((l) => [l.tipoLicenciaId, l])),
  );

  // Listado liviano (UsuarioHeaderDto): sin roles/aplicaciones; el detalle se trae en el edit.
  // `columns` es computed: la columna "Licencia" depende del resumen, así que se rehace cuando éste
  // carga (cambia la referencia del array y la tabla la vuelve a tomar).
  protected readonly columns = computed<TbiColumn<Usuario>[]>(() => {
    const porId = this.licenciaPorId();
    const licenciaColumn: TbiColumn<Usuario> = {
      key: 'licencia',
      header: 'Licencia',
      align: 'center',
      // La fila trae sólo el id de la licencia activa (header liviano); la descripción y la
      // vigencia salen del resumen. Sin licencia activa → chip neutro "Sin licencia".
      chip: (row) => {
        const id = row.tipoLicenciaActiva?.tipoLicenciaId;
        if (id == null) {
          return { label: 'Sin licencia', tone: 'neutral' };
        }
        const lic = porId.get(id);
        if (!lic) {
          return { label: 'Licencia', tone: 'neutral' };
        }
        switch (estadoVigencia(lic)) {
          case 'vencida':
            return { label: lic.descripcion, tone: 'error', icon: 'error' };
          case 'por-vencer':
            return { label: lic.descripcion, tone: 'warning', icon: 'schedule' };
          default:
            return { label: lic.descripcion, tone: 'neutral', icon: 'workspace_premium' };
        }
      },
    };
    return [
      { key: 'id', header: 'Id', sortable: true },
      { key: 'userName', header: 'Usuario', sortable: true },
      { key: 'nombre', header: 'Nombre', sortable: true },
      { key: 'apellido', header: 'Apellido', sortable: true },
      { key: 'email', header: 'Email', sortable: true, optional: true },
      // La columna de licencia no aplica a root (ver `isRoot`).
      ...(this.isRoot() ? [] : [licenciaColumn]),
      {
        key: 'estado',
        header: 'Estado',
        align: 'center',
        optional: true,
        chip: (row) =>
          row.bloqueado
            ? { label: 'Bloqueado', tone: 'error', icon: 'lock' }
            : { label: 'Activo', tone: 'success', icon: 'check_circle' },
      },
      {
        key: 'ultimoLogin',
        header: 'Último login',
        sortable: true,
        optional: true,
        cell: (row) => (row.ultimoLogin ? new Date(row.ultimoLogin).toLocaleString() : '—'),
      },
    ];
  });

  protected readonly fetch = (query: QueryParams) => this.service.crud.getAllByCriteria(query);

  /** Recarga la grilla y el resumen de licencias juntos (tras un cambio que afecta a ambos). */
  private refresh(): void {
    this.table().refresh();
    this.resumenResource.reload();
  }

  // Acciones de fila: Editar siempre; Desbloquear sólo si está bloqueado; Eliminar (destructivo).
  // El backend gatea cada endpoint por permiso (root o permiso mapeado), así que Desbloquear lo
  // puede ver cualquiera con acceso al listado pero sólo lo ejecuta quien está autorizado (la API
  // responde 401 en caso contrario). Ocultarlo por permiso en el front sería sólo UX adicional.
  protected readonly rowActions: TbiRowAction<Usuario>[] = [
    { icon: 'edit', label: 'Editar', handler: (row) => this.edit(row) },
    {
      icon: 'lock_open',
      label: 'Desbloquear',
      hidden: (row) => !row.bloqueado,
      confirm: (row) => ({
        title: 'Desbloquear',
        message: `¿Desbloquear al usuario "${row.userName ?? row.email}"?`,
      }),
      run: (row) => this.unlockFn(row),
    },
    {
      icon: 'delete',
      label: 'Eliminar',
      danger: true,
      confirm: (row) => this.confirmDelete(row),
      run: (row) => this.removeFn(row)(),
    },
  ];

  protected add(): void {
    this.openEdit();
  }

  protected edit(row: Usuario): void {
    this.openEdit(row.id);
  }

  /** Datos del diálogo de confirmación de borrado de la acción Eliminar. */
  protected confirmDelete(row: Usuario) {
    return { title: 'Eliminar', message: `¿Eliminar el usuario "${row.userName ?? row.email}"?` };
  }

  /** Thunk de borrado que invoca la acción Eliminar de `rowActions` (spinner durante la API). */
  protected removeFn(row: Usuario): () => Observable<unknown> {
    return () => {
      if (row.id == null) {
        return EMPTY;
      }
      return this.service.crud.delete(row.id).pipe(
        tap(() => {
          this.notify.success('Usuario eliminado.');
          this.refresh();
        }),
      );
    };
  }

  /** Desbloquea al usuario (toggle de lockout en la API) y recarga la grilla. */
  protected unlockFn(row: Usuario): Observable<unknown> {
    if (!row.userName) {
      return EMPTY;
    }
    return this.service.bloquearDesbloquear(row.userName, false).pipe(
      tap(() => {
        this.notify.success('Usuario desbloqueado.');
        this.refresh();
      }),
    );
  }

  private openEdit(id?: number): void {
    this.dialog
      .open<UsuarioEditComponent, { id?: number }, Usuario>(UsuarioEditComponent, {
        data: { id },
        width: '760px',
        maxWidth: '95vw',
        maxHeight: '92vh',
        autoFocus: false,
      })
      .afterClosed()
      .subscribe((saved) => {
        if (saved) {
          this.notify.success('Usuario guardado.');
          this.refresh();
        }
      });
  }
}
