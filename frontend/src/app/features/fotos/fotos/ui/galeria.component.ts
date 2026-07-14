import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  linkedSignal,
  signal,
} from '@angular/core';
import { rxResource, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { catchError, filter, interval, map, of, switchMap } from 'rxjs';
import { saveBlobResponse } from '../../../../core/http';
import { ConfirmService } from '../../../../shared/feedback/confirm.service';
import { NotificationService } from '../../../../shared/feedback/notification.service';
import {
  TbiSelectComponent,
  TbiSelectOption,
} from '../../../../shared/ui/tbi-select/tbi-select.component';
import { CursosService } from '../../cursos/data/cursos.service';
import { EventosService } from '../../eventos/data/eventos.service';
import { Evento } from '../../eventos/domain/evento.model';
import { FotosService } from '../data/fotos.service';
import { EstadoProcesamientoFoto, Foto } from '../domain/foto.model';
import { FotoImgComponent } from './foto-img.component';
import { FotoPreviewDialogComponent } from './foto-preview-dialog.component';

/** Filtro de álbum: todas las fotos del curso. */
const TODAS = -1;
/** Filtro de álbum: solo las grupales (albumId null). */
const GRUPALES = 0;

/** Cada cuánto se refresca mientras haya fotos en Pendiente (el worker sigue procesando). */
const POLL_MS = 3000;

// TODO (Fase 4 - i18n): textos en español por ahora.
@Component({
  selector: 'tbi-galeria',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButtonModule, MatIconModule, TbiSelectComponent, FotoImgComponent],
  templateUrl: './galeria.component.html',
  styleUrl: './galeria.component.scss',
})
export class GaleriaComponent {
  private readonly fotosService = inject(FotosService);
  private readonly cursosService = inject(CursosService);
  private readonly eventosService = inject(EventosService);
  private readonly notify = inject(NotificationService);
  private readonly confirmService = inject(ConfirmService);
  private readonly dialog = inject(MatDialog);

  // ── Cascada evento → curso + filtro por álbum (mismo esquema que Subir fotos) ────────────────

  private readonly eventosResource = rxResource({
    stream: () =>
      this.eventosService.crud.getAll().pipe(
        map((result) => result.items),
        // el toast de error lo emite el errorInterceptor (global)
        catchError(() => of<Evento[]>([])),
      ),
  });
  protected readonly eventoOptions = computed<TbiSelectOption<number>[]>(
    () => this.eventosResource.value()?.map((e) => ({ value: e.id ?? 0, label: e.nombre })) ?? [],
  );
  protected readonly eventoId = signal<number | null>(null);

  protected readonly cursoId = linkedSignal<number | null, number | null>({
    source: this.eventoId,
    computation: () => null,
  });

  private readonly cursosResource = rxResource({
    params: () => this.eventoId() ?? undefined,
    stream: ({ params: eventoId }) =>
      this.cursosService.crud
        .getAllByCriteria({ eventoId, page: 0, pageSize: 500 })
        .pipe(map((result) => result.items)),
  });
  protected readonly cursoOptions = computed<TbiSelectOption<number>[]>(
    () => this.cursosResource.value()?.map((c) => ({ value: c.id ?? 0, label: c.nombre })) ?? [],
  );

  protected readonly albumFiltro = linkedSignal<number | null, number>({
    source: this.cursoId,
    computation: () => TODAS,
  });

  private readonly cursoDetalleResource = rxResource({
    params: () => this.cursoId() ?? undefined,
    stream: ({ params: cursoId }) => this.cursosService.crud.getById(cursoId),
  });
  private readonly albumes = computed(() => this.cursoDetalleResource.value()?.albumes ?? []);

  protected readonly albumOptions = computed<TbiSelectOption<number>[]>(() => [
    { value: TODAS, label: 'Todas las fotos' },
    { value: GRUPALES, label: 'Solo grupales' },
    ...this.albumes().map((a) => ({ value: a.id, label: `Álbum: ${a.nombreAlumno}` })),
  ]);
  private readonly albumPorId = computed(
    () => new Map(this.albumes().map((a) => [a.id, a.nombreAlumno])),
  );

  // ── Fotos del curso (el filtro por álbum se aplica en memoria: son pocas por curso) ──────────

  protected readonly fotosResource = rxResource({
    params: () => this.cursoId() ?? undefined,
    stream: ({ params: cursoId }) =>
      this.fotosService.listar(cursoId).pipe(catchError(() => of<Foto[]>([]))),
  });

  protected readonly fotos = computed(() => {
    const filtro = this.albumFiltro();
    const fotos = this.fotosResource.value() ?? [];
    if (filtro === TODAS) {
      return fotos;
    }
    if (filtro === GRUPALES) {
      return fotos.filter((f) => f.albumId == null);
    }
    return fotos.filter((f) => f.albumId === filtro);
  });

  protected readonly cantidadPendientes = computed(
    () =>
      (this.fotosResource.value() ?? []).filter(
        (f) => f.estadoProcesamiento === EstadoProcesamientoFoto.Pendiente,
      ).length,
  );

  constructor() {
    // Si quedaron fotos procesándose (p. ej. recién subidas en la otra pantalla), se refresca solo.
    interval(POLL_MS)
      .pipe(takeUntilDestroyed())
      .subscribe(() => {
        if (this.cantidadPendientes() > 0) {
          this.fotosResource.reload();
        }
      });
  }

  // ── Acciones ─────────────────────────────────────────────────────────────────────────────────

  protected verPreview(foto: Foto): void {
    if (foto.estadoProcesamiento !== EstadoProcesamientoFoto.Lista) {
      return;
    }
    this.dialog.open(FotoPreviewDialogComponent, { data: foto, autoFocus: false });
  }

  protected descargarOriginal(foto: Foto): void {
    this.fotosService
      .descargarOriginal(foto.id)
      .subscribe((response) => saveBlobResponse(response, foto.nombreArchivoOriginal));
  }

  protected borrar(foto: Foto): void {
    this.confirmService
      .confirm({
        title: 'Eliminar foto',
        message: `¿Eliminar "${foto.nombreArchivoOriginal}"? Se borra también del almacenamiento.`,
      })
      .pipe(
        filter((confirmado) => confirmado),
        switchMap(() => this.fotosService.borrar(foto.id)),
      )
      .subscribe(() => {
        this.notify.success('Foto eliminada.');
        this.fotosResource.reload();
      });
  }

  // ── Presentación ─────────────────────────────────────────────────────────────────────────────

  protected esLista(foto: Foto): boolean {
    return foto.estadoProcesamiento === EstadoProcesamientoFoto.Lista;
  }

  protected esError(foto: Foto): boolean {
    return foto.estadoProcesamiento === EstadoProcesamientoFoto.Error;
  }

  protected nombreAlbum(foto: Foto): string {
    if (foto.albumId == null) {
      return 'Grupal';
    }
    return this.albumPorId().get(foto.albumId) ?? `Álbum ${foto.albumId}`;
  }
}
