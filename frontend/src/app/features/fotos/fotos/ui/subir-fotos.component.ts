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
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { catchError, concatMap, from, interval, map, of, reduce, tap } from 'rxjs';
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

/** Valor del select de destino que significa "fotos grupales del curso" (sin álbum). */
const GRUPALES = 0;

/**
 * Archivos por request: la API acepta hasta 512 MB por tanda, pero mandar de a 10 mantiene cada
 * request corto (feedback de progreso) y acota el reintento si una tanda falla.
 */
const TAMANO_TANDA = 10;

/** Cada cuánto se refresca el listado mientras el worker procesa pendientes. */
const POLL_MS = 3000;

// TODO (Fase 4 - i18n): textos en español por ahora.
@Component({
  selector: 'tbi-subir-fotos',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButtonModule, MatIconModule, MatProgressBarModule, TbiSelectComponent],
  templateUrl: './subir-fotos.component.html',
  styleUrl: './subir-fotos.component.scss',
})
export class SubirFotosComponent {
  private readonly fotosService = inject(FotosService);
  private readonly cursosService = inject(CursosService);
  private readonly eventosService = inject(EventosService);
  private readonly notify = inject(NotificationService);

  // ── Cascada de destino: evento → curso → grupales/álbum ─────────────────────────────────────

  private readonly eventosResource = rxResource({
    stream: () =>
      this.eventosService.crud.getAll().pipe(
        map((result) => result.items),
        // el toast de error lo emite el errorInterceptor (global)
        catchError(() => of<Evento[]>([])),
      ),
  });

  protected readonly eventoOptions = computed<TbiSelectOption<number>[]>(
    () =>
      this.eventosResource.value()?.map((e) => ({ value: e.id ?? 0, label: e.nombre })) ?? [],
  );
  protected readonly eventoId = signal<number | null>(null);

  // Cambiar el evento resetea el curso (linkedSignal: se recalcula al cambiar la fuente pero
  // sigue siendo escribible por el select).
  protected readonly cursoId = linkedSignal<number | null, number | null>({
    source: this.eventoId,
    computation: () => null,
  });

  private readonly cursosResource = rxResource({
    params: () => this.eventoId() ?? undefined, // sin evento no se buscan cursos
    stream: ({ params: eventoId }) =>
      this.cursosService.crud
        .getAllByCriteria({ eventoId, page: 0, pageSize: 500 })
        .pipe(map((result) => result.items)),
  });
  protected readonly cursoOptions = computed<TbiSelectOption<number>[]>(
    () => this.cursosResource.value()?.map((c) => ({ value: c.id ?? 0, label: c.nombre })) ?? [],
  );

  // Cambiar el curso vuelve el destino a "grupales".
  protected readonly destino = linkedSignal<number | null, number>({
    source: this.cursoId,
    computation: () => GRUPALES,
  });

  /** Detalle del curso: aporta los álbumes para el select de destino y la columna Álbum. */
  private readonly cursoDetalleResource = rxResource({
    params: () => this.cursoId() ?? undefined,
    stream: ({ params: cursoId }) => this.cursosService.crud.getById(cursoId),
  });
  private readonly albumes = computed(() => this.cursoDetalleResource.value()?.albumes ?? []);

  protected readonly destinoOptions = computed<TbiSelectOption<number>[]>(() => [
    { value: GRUPALES, label: 'Fotos grupales del curso' },
    ...this.albumes().map((a) => ({ value: a.id, label: `Álbum: ${a.nombreAlumno}` })),
  ]);
  private readonly albumPorId = computed(
    () => new Map(this.albumes().map((a) => [a.id, a.nombreAlumno])),
  );

  // ── Selección de archivos ────────────────────────────────────────────────────────────────────

  protected readonly archivos = signal<File[]>([]);

  // ── Subida por tandas ────────────────────────────────────────────────────────────────────────

  protected readonly subiendo = signal(false);
  protected readonly tandaActual = signal(0);
  protected readonly totalTandas = signal(0);

  // ── Fotos ya subidas al curso (estado del procesamiento) ─────────────────────────────────────

  protected readonly fotosResource = rxResource({
    params: () => this.cursoId() ?? undefined,
    stream: ({ params: cursoId }) =>
      this.fotosService.listar(cursoId).pipe(catchError(() => of<Foto[]>([]))),
  });
  protected readonly fotos = computed(() => this.fotosResource.value() ?? []);
  protected readonly cantidadPendientes = computed(
    () =>
      this.fotos().filter((f) => f.estadoProcesamiento === EstadoProcesamientoFoto.Pendiente)
        .length,
  );

  constructor() {
    // Mientras el worker tenga fotos en Pendiente, el listado se refresca solo.
    interval(POLL_MS)
      .pipe(takeUntilDestroyed())
      .subscribe(() => {
        if (this.cantidadPendientes() > 0 && !this.subiendo()) {
          this.fotosResource.reload();
        }
      });
  }

  protected readonly puedeSubir = computed(
    () => this.cursoId() != null && this.archivos().length > 0 && !this.subiendo(),
  );

  protected agregarArchivos(event: Event): void {
    const input = event.target as HTMLInputElement;
    const nuevos = Array.from(input.files ?? []);
    if (nuevos.length) {
      // Dedupe por identidad práctica (nombre + tamaño): re-elegir la misma carpeta no duplica.
      const clave = (f: File) => `${f.name}|${f.size}`;
      const existentes = new Set(this.archivos().map(clave));
      this.archivos.update((actuales) => [
        ...actuales,
        ...nuevos.filter((f) => !existentes.has(clave(f))),
      ]);
    }
    input.value = ''; // permite volver a elegir los mismos archivos tras quitarlos
  }

  protected quitarArchivo(archivo: File): void {
    this.archivos.update((actuales) => actuales.filter((f) => f !== archivo));
  }

  protected limpiarArchivos(): void {
    this.archivos.set([]);
  }

  protected subir(): void {
    const cursoId = this.cursoId();
    if (cursoId == null || !this.puedeSubir()) {
      return;
    }
    const albumId = this.destino() === GRUPALES ? null : this.destino();

    const tandas: File[][] = [];
    const archivos = this.archivos();
    for (let i = 0; i < archivos.length; i += TAMANO_TANDA) {
      tandas.push(archivos.slice(i, i + TAMANO_TANDA));
    }

    this.subiendo.set(true);
    this.tandaActual.set(0);
    this.totalTandas.set(tandas.length);

    from(tandas)
      .pipe(
        // Secuencial a propósito: no saturar la conexión del fotógrafo ni el pipeline de la API.
        concatMap((tanda) =>
          this.fotosService.subir(cursoId, albumId, tanda).pipe(
            tap(() => this.tandaActual.update((t) => t + 1)),
            map((fotos) => fotos.length),
          ),
        ),
        reduce((total, subidasEnTanda) => total + subidasEnTanda, 0),
      )
      .subscribe({
        next: (subidas) => {
          this.notify.success(`${subidas} foto(s) subidas. Generando copias con marca de agua…`);
          this.archivos.set([]);
        },
        // Si una tanda falla (el toast lo emite el interceptor), las anteriores YA quedaron
        // subidas: se refresca el listado para verlas y los archivos siguen elegidos para
        // reintentar (la API no duplica nada porque cada foto es una fila nueva... el fotógrafo
        // quita de la lista los que ya ve subidos).
        error: () => this.terminarSubida(),
        complete: () => this.terminarSubida(),
      });
  }

  private terminarSubida(): void {
    this.subiendo.set(false);
    this.fotosResource.reload();
  }

  // ── Presentación ─────────────────────────────────────────────────────────────────────────────

  protected nombreAlbum(foto: Foto): string {
    if (foto.albumId == null) {
      return 'Grupal';
    }
    return this.albumPorId().get(foto.albumId) ?? `Álbum ${foto.albumId}`;
  }

  protected estadoLabel(foto: Foto): string {
    switch (foto.estadoProcesamiento) {
      case EstadoProcesamientoFoto.Lista:
        return 'Lista';
      case EstadoProcesamientoFoto.Error:
        return 'Error';
      default:
        return 'Procesando…';
    }
  }

  protected estadoClass(foto: Foto): string {
    switch (foto.estadoProcesamiento) {
      case EstadoProcesamientoFoto.Lista:
        return 'estado--lista';
      case EstadoProcesamientoFoto.Error:
        return 'estado--error';
      default:
        return 'estado--pendiente';
    }
  }

  protected tamanoLegible(bytes: number): string {
    if (bytes >= 1024 * 1024) {
      return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
    }
    return `${Math.max(1, Math.round(bytes / 1024))} KB`;
  }
}
